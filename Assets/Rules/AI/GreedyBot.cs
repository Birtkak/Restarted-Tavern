using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Rules.AI
{
    /// <summary>
    /// A simple rule-based player for simulations and the debug table (DEVELOPMENT §6: "rule-based
    /// first"). It looks one action ahead and never bluffs, so it plays like a careful beginner:
    /// it develops its biggest threat, uses removal only when it kills, attacks when no blocker
    /// would eat its creature for free, blocks when it wins or trades evenly, and chump-blocks only
    /// to survive. Deterministic: same state, same choice. A <see cref="BotStyle"/> tunes it
    /// (Greedy by default, or Control).
    /// Activated abilities and Tavern Dweller Powers are worth their effect minus what they cost: mana
    /// that would only be banked is cheap, Gold that would overflow the cap is cheap, and Gold on
    /// your own turn costs extra (it's your only resource on other turns). Instant-speed Powers
    /// wait for the opponent's end step unless something big is on offer (a kill, a combat trick).
    /// </summary>
    public sealed partial class GreedyBot
    {
        private readonly GameEngine _engine;
        private readonly BotStyle _style;
        private CardDatabase Db => _engine.Cards;

        public GreedyBot(GameEngine engine, BotStyle style = null)
        {
            _engine = engine;
            _style = style ?? BotStyle.Greedy();
        }

        public BotStyle Style => _style;

        public PlayerAction Choose(GameState s, PlayerId me)
        {
            var legal = _engine.GetLegalActions(s, me);
            if (legal.Count == 0) throw new InvalidOperationException(me + " has no legal actions.");
            if (legal.Count == 1) return legal[0];

            switch (s.Pending?.Kind)
            {
                case DecisionKind.Mulligan: return KeepOrMulligan(s, me, legal);
                case DecisionKind.BottomCards:
                case DecisionKind.DiscardToHandSize:
                case DecisionKind.DiscardCards:
                    return legal.OrderByDescending(a => Def(s, a.Card).Cost).First();
                case DecisionKind.DeclareAttackers: return ChooseAttack(s, me, legal);
                case DecisionKind.DeclareBlockers: return ChooseBlock(s, me, legal);
                case DecisionKind.ChooseTriggerTarget:
                {
                    var trigger = s.Pending.Trigger;
                    return legal.OrderByDescending(a => EffectValue(s, me, trigger.Ability.Effects, a.Targets, trigger.SourceId, trigger.Amount)).First();
                }
                case DecisionKind.ChooseFromTop:
                {
                    // Take the best card we can cast soon.
                    var deck = s.GetPlayer(me).Deck;
                    int mana = s.GetPlayer(me).MaxMana + 1;
                    return legal.OrderByDescending(a =>
                    {
                        int cost = Db.Get(deck[a.Option].DefinitionId).Cost;
                        return cost <= mana ? cost : 0.5;
                    }).First();
                }
                case DecisionKind.PayAnyGold:
                {
                    // Bid just enough to win if that's cheap, otherwise keep the Gold.
                    int highest = s.Pending.Bids.Count > 0 ? s.Pending.Bids.Max() : 0;
                    int gold = s.GetPlayer(me).Gold;
                    int bid = s.Pending.Bids.Count == 0 ? Math.Min(gold, 2) : highest + 1 <= Math.Min(gold, 4) ? highest + 1 : 0;
                    return legal.First(a => a.Option == bid);
                }
                case DecisionKind.ChooseObject: return ChooseObject(s, me, legal);
                case DecisionKind.ChooseUpTo:
                    // Choosing more is always better for the "choose up to" effects we have (Snik's copies): the most valuable first.
                    return legal.Where(a => a.Target.HasValue)
                               .OrderByDescending(a => Worth(s, s.FindObject(a.Target.Value.Object))).FirstOrDefault()
                           ?? legal[0];
                case DecisionKind.YesNo:
                {
                    // Our own "you may lose 2 life": only with life to spare. Someone else's offer (The Dealer): pay.
                    bool yes = s.Pending.EffectController != me || s.GetPlayer(me).Life > 12;
                    return legal.First(a => a.Option == (yes ? 1 : 0));
                }
                case DecisionKind.PayTax:
                    // Pay the tax when we can: the spell was worth casting.
                    return legal.OrderByDescending(a => a.Option).First();
                case DecisionKind.KeepLegendary:
                    // Keep the healthiest, best-equipped copy.
                    return legal.OrderByDescending(a => KeepValue(s, s.FindOnBattlefield(a.Target.Value.Object))).First();
                case DecisionKind.AssignCombatDamage: return ChooseDamageSplit(s, legal);
                case DecisionKind.DivideDamage: return ChooseDividePoint(s, me, legal);
                case DecisionKind.OrderTriggers:
                    // The order rarely matters for these cards: keep the order the triggers happened in.
                    return legal[0];
                case DecisionKind.TopOrBottom:
                {
                    // Keep it if it can be cast soon; bottom expensive cards.
                    var card = s.FindObject(s.Pending.Card);
                    bool bottom = card != null && Db.Get(card.DefinitionId).Cost > s.GetPlayer(me).MaxMana + 2;
                    return legal.First(a => a.Option == (bottom ? 1 : 0));
                }
            }

            PlayerAction best = null;
            double bestScore = 0;
            int reserve = InstantReserve(s, me, out double reserveValue);
            foreach (var a in legal)
            {
                double score;
                if (a.Kind == ActionKind.PlayCard)
                {
                    score = PlayValue(s, me, a) - 0.9; // anything below 0.9 isn't worth a card
                    if (reserve > 0 && BreaksReserve(s, me, a, reserve)) score -= reserveValue;
                }
                else if (a.Kind == ActionKind.ActivateAbility) score = AbilityValue(s, me, a) - 0.25;
                else continue;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = a;
                }
            }
            return best ?? legal.First(a => a.Kind == ActionKind.PassPriority);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Round pool (FormatConfig.ManaPerRound): mana left at the end of your turn can still be used on the
        /// opponent's turn. On its own turn the bot keeps enough mana and Gold for the cheapest Instant in hand
        /// (cost 3 or less). Returns that cost (0 = nothing to hold) and, in <paramref name="value"/>, roughly
        /// what keeping it ready is worth.
        /// </summary>
        private int InstantReserve(GameState s, PlayerId me, out double value)
        {
            value = 0;
            if (!s.Format.ManaPerRound || s.ActivePlayer != me || s.Pending != null) return 0;
            // Only worth it if an opponent's turn still comes in this round; the last player's leftovers are banked at once.
            int n = s.Players.Count;
            if ((s.GetPlayer(me).Seat - s.RoundLeaderSeat + n) % n == n - 1) return 0;
            int cheapest = 0;
            foreach (var c in s.GetPlayer(me).Hand)
            {
                var def = Db.Get(c.DefinitionId);
                if (def.Type != CardType.Instant && !def.Flash) continue;
                int cost = Costs.SpellCost(s, Db, me, def);
                if (cost <= 3 && (cheapest == 0 || cost < cheapest)) cheapest = cost;
            }
            value = 0.6 * (2 * cheapest + 1);
            return cheapest;
        }

        /// <summary>Would this sorcery-speed play leave less mana + Gold than the Instant reserve?</summary>
        private bool BreaksReserve(GameState s, PlayerId me, PlayerAction a, int reserve)
        {
            var p = s.GetPlayer(me);
            var def = Db.Get(p.Hand.Find(c => c.Id == a.Card).DefinitionId);
            if (def.Type == CardType.Instant || def.Flash) return false;
            return p.Mana + p.Gold - Costs.SpellCost(s, Db, me, def) < reserve;
        }

        private CardDefinition Def(GameState s, ObjectId id) => Db.Get(s.FindObject(id).DefinitionId);
        // The bot never changes the state while deciding, so stats are cached per state and version.
        private readonly Dictionary<ObjectId, Characteristics> _stats = new Dictionary<ObjectId, Characteristics>();
        private GameState _statsState;
        private long _statsVersion;

        private Characteristics Stats(GameState s, CardInstance c)
        {
            if (!ReferenceEquals(s, _statsState) || s.Version != _statsVersion)
            {
                _stats.Clear();
                _statsState = s;
                _statsVersion = s.Version;
            }
            if (!_stats.TryGetValue(c.Id, out var st)) _stats[c.Id] = st = _engine.GetCharacteristics(s, c);
            return st;
        }

        /// <summary>Keep a hand with 2–5 early plays (cost 3 or less); mulligan at most once.</summary>
        private PlayerAction KeepOrMulligan(GameState s, PlayerId me, List<PlayerAction> legal)
        {
            var p = s.GetPlayer(me);
            int early = p.Hand.Count(c => Db.Get(c.DefinitionId).Cost <= 3);
            bool keep = p.MulligansTaken >= 1 || (early >= 2 && early <= 5);
            return legal.First(a => a.Kind == (keep ? ActionKind.Keep : ActionKind.Mulligan));
        }

        private double PlayValue(GameState s, PlayerId me, PlayerAction a)
        {
            var p = s.GetPlayer(me);
            var card = p.Hand.Find(c => c.Id == a.Card);
            var def = Db.Get(card.DefinitionId);
            double value;

            if (def.Type == CardType.Curse)
            {
                value = CurseValue(s, me, def, a.Target);
            }
            else if (def.Type == CardType.Equipment && !p.Battlefield.Any(c => Db.Get(c.DefinitionId).IsCreature))
            {
                value = def.Cost; // nothing to equip yet
            }
            else if (def.IsPermanent)
            {
                // Develop: bigger is better, so the curve gets used.
                value = 2 * def.Cost + 1;
                if (_style.ValueArrivalDamage) value += ArrivalDamageValue(s, me, def);
                if (def.Keywords.HasFlag(Keyword.Haste) && s.Step == Step.Main1) value += 1;
            }
            else if (def.DividedDamage > 0)
            {
                value = 0;
                for (int i = 0; i < a.Targets.Length && i < a.Division.Length; i++) value += DamageValue(s, me, a.Division[i], a.Targets[i]);
            }
            else
            {
                var victim = a.Sacrifice.IsNone ? null : s.FindOnBattlefield(a.Sacrifice);
                int x = victim != null ? Stats(s, victim).Power : a.X;
                value = EffectValue(s, me, def.SpellEffects, a.Targets, a.Card, x, def, a.Invest);
                if (victim != null) value -= Worth(s, victim) + 0.5;
            }
            if (def.XGoldExtraCost) value -= a.X * GoldUnitValue(s, p);
            if (def.ExtraLifeCost > 0) value -= LifeLossValue(p, def.ExtraLifeCost);

            if (a.Invest) value += EffectValue(s, me, def.InvestEffects, a.Targets, a.Card) + 0.1;

            // Gold is flexible (instant speed); spend mana first when it's our turn.
            if (s.ActivePlayer == me) value -= _style.GoldOnOwnTurnPenalty * Math.Max(0, Payment.GoldNeeded(s, Db, p, def));
            // Round pool: mana spent before our own turn in the round is mana we can't develop with.
            if (MyTurnStillAhead(s, me))
                value -= DevelopmentManaValue * Math.Max(0, Costs.SpellCost(s, Db, me, def) - Math.Max(0, Payment.GoldNeeded(s, Db, p, def)));
            return value;
        }

        /// <summary>
        /// A choice during resolution. Losing something (sacrifice, discard, a creature someone takes): the least
        /// valuable, or nothing when that's allowed and it isn't worth it. Getting something: the best.
        /// </summary>
        private PlayerAction ChooseObject(GameState s, PlayerId me, List<PlayerAction> legal)
        {
            var d = s.Pending;
            var p = s.GetPlayer(me);
            var decline = legal.FirstOrDefault(a => !a.Target.HasValue);
            var options = legal.Where(a => a.Target.HasValue).ToList();
            double Value(PlayerAction a)
            {
                var obj = s.FindObject(a.Target.Value.Object);
                if (obj == null) return 0;
                return obj.Zone == Zone.Battlefield ? Worth(s, obj) : Db.Get(obj.DefinitionId).Cost;
            }

            bool discard = d.Then.Any(e => e is DiscardEventObjectEffect);
            bool losing = discard || d.Then.Any(e => e is SacrificeEventObjectEffect || e is GainControlOfEventObjectEffect);
            if (!losing) return options.OrderByDescending(Value).First();

            if (discard)
            {
                // Swap away an expensive card we can't cast soon.
                var worst = options.OrderByDescending(Value).First();
                bool swap = Value(worst) > p.MaxMana + 2;
                return swap || decline == null ? worst : decline;
            }

            var cheapest = options.OrderBy(Value).First();
            if (decline == null) return cheapest;
            bool elseLosesLife = d.Else != null && d.Else.Any(e => e is LoseLifeEffect);
            bool worthIt = elseLosesLife ? Value(cheapest) < 6 || p.Life <= 6 : Value(cheapest) <= 4;
            return worthIt ? cheapest : decline;
        }
    }
}
