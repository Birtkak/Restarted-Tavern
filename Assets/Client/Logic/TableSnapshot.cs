using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Logic
{
    /// <summary>One card as the table shows it. Hidden cards (opponent's hand, decks) only have an id.</summary>
    public sealed class CardView
    {
        public ObjectId Id;
        /// <summary>Null for a hidden card.</summary>
        public string DefinitionId;
        public bool IsHidden => DefinitionId == null;
        public string Name;
        public CardType Type;
        public string Faction;
        public Rarity Rarity;
        public string Text;
        public string[] Subtypes;
        public bool IsToken;
        public PlayerId Owner;
        public PlayerId Controller;
        public Zone Zone;

        /// <summary>What the card costs to play now: in hand, with the controller's cost changes (Sparkwrench...).</summary>
        public int Cost;
        public int PrintedCost;
        public int PrintedPower;
        public int PrintedHealth;
        /// <summary>Current values, with counters, anthems, Equipment and until-end-of-turn changes (creatures on the battlefield).</summary>
        public int Power;
        public int MaxHealth;
        public int Damage;
        public int RemainingHealth => MaxHealth - Damage;
        public Keyword Keywords;
        public int PlusOneCounters;
        public bool Tapped;

        /// <summary>Equipment and Curses: what it's attached to.</summary>
        public ObjectId AttachedTo;
        public PlayerId? AttachedToPlayer;

        public bool IsAttacking;
        /// <summary>The player this creature attacks (when attacking).</summary>
        public PlayerId? Attacks;
        /// <summary>The attacker this creature blocks (None when not blocking).</summary>
        public ObjectId Blocks;
    }

    public sealed class PlayerView
    {
        public PlayerId Id;
        public int Seat;
        public bool HasLost;
        public int Life;
        public int Mana;
        public int MaxMana;
        public int Gold;
        public int GoldCap;
        public CardView TavernDweller;
        public List<CardView> Hand = new List<CardView>();
        public int DeckCount;
        public List<CardView> Battlefield = new List<CardView>();
        public List<CardView> Graveyard = new List<CardView>();
        public List<CardView> Exile = new List<CardView>();
        /// <summary>This player leads the round and holds the attack token (GAME_DESIGN §6.1).</summary>
        public bool HasAttackToken;
        /// <summary>The game waits on this player (their action, a response, or a decision).</summary>
        public bool IsWaitedOn;
        /// <summary>The Tavern Dweller Power (Hearthstone hero power button): cost, text, used this round.</summary>
        public bool HasPower;
        public int PowerCost;
        public string PowerText;
        public bool PowerUsed;
    }

    /// <summary>A spell or ability on the Chain, bottom first.</summary>
    public sealed class ChainView
    {
        public int Id;
        /// <summary>What "target spell / ability" chooses (Target.ForObject), and what the table keys its bubble by.</summary>
        public ObjectId ObjectId;
        public ChainItemKind Kind;
        public PlayerId Controller;
        public string SourceDefinitionId;
        public ObjectId Source;
        public string Text;
        public List<Target> Targets;
        public bool IsTavernDwellerPower;
        /// <summary>A merged trigger does it this many times ("×3" badge).</summary>
        public int Times = 1;
    }

    /// <summary>
    /// Everything the table draws, for one viewer, built from the engine state. A visual client binds to
    /// this and never reads <see cref="GameState"/> itself, so hidden information stays hidden (a hot-seat
    /// client rebuilds it for whoever is at the screen).
    /// </summary>
    public sealed class TableSnapshot
    {
        public PlayerId Viewer;
        public int Round;
        public Step Step;
        public bool IsGameOver;
        public List<PlayerId> Winners = new List<PlayerId>();
        /// <summary>The player the game waits on, or null when it's over.</summary>
        public PlayerId? WaitingOn;
        /// <summary>The decision being made (mulligan, blocks, a trigger target...), or null for a normal action.</summary>
        public DecisionKind? Decision;
        public string DecisionPrompt;
        /// <summary>ChooseTriggerTarget: the permanent (or Tavern Dweller) whose trigger is choosing its target.</summary>
        public ObjectId DecisionSource = ObjectId.None;
        /// <summary>Who leads the round (takes the first action; holds the attack token when the format has one).</summary>
        public PlayerId RoundLeader;
        /// <summary>The attack token was used this round.</summary>
        public bool AttackUsed;
        public List<PlayerView> Players = new List<PlayerView>();
        public List<ChainView> Chain = new List<ChainView>();

        public PlayerView Player(PlayerId id) => Players.First(p => p.Id == id);

        public CardView Find(ObjectId id)
        {
            foreach (var p in Players)
            {
                if (p.TavernDweller?.Id == id) return p.TavernDweller;
                var c = p.Battlefield.Concat(p.Hand).Concat(p.Graveyard).Concat(p.Exile).FirstOrDefault(v => v.Id == id);
                if (c != null) return c;
            }
            return null;
        }

        public static TableSnapshot Build(GameEngine engine, GameState state, PlayerId viewer)
        {
            var db = engine.Cards;
            var waiting = engine.WaitingOn(state);
            var snap = new TableSnapshot
            {
                Viewer = viewer,
                Round = state.RoundNumber,
                Step = state.Step,
                IsGameOver = state.IsGameOver,
                Winners = state.Winners.ToList(),
                WaitingOn = waiting,
                Decision = state.Pending?.Kind,
                DecisionPrompt = state.Pending?.Prompt,
                DecisionSource = state.Pending?.Trigger?.SourceId ?? ObjectId.None,
                AttackUsed = state.AttackedThisRound > 0,
            };
            // The engine's rule (GameRunner.RoundLeader): the first living player from the round leader's seat.
            var leader = state.LivingPlayersFrom(state.Players[state.RoundLeaderSeat].Id)[0].Id;
            snap.RoundLeader = leader;
            foreach (var p in state.Players)
            {
                var pv = new PlayerView
                {
                    Id = p.Id, Seat = p.Seat, HasLost = p.HasLost, Life = p.Life,
                    Mana = p.Mana, MaxMana = p.MaxMana, Gold = p.Gold, GoldCap = GoldRules.Cap(state, db, p.Id),
                    DeckCount = p.Deck.Count,
                    HasAttackToken = p.Id == leader,
                    IsWaitedOn = waiting == p.Id,
                };
                if (p.TavernDweller != null)
                {
                    pv.TavernDweller = View(engine, state, p.TavernDweller, false);
                    var abilities = engine.GetAbilities(state, p.TavernDweller);
                    int power = abilities.FindIndex(ab => ab.IsTavernDwellerPower);
                    if (power >= 0)
                    {
                        pv.HasPower = true;
                        pv.PowerCost = abilities[power].Cost;
                        pv.PowerText = abilities[power].Text;
                        pv.PowerUsed = GameEngine.UsedThisRound(state, p.TavernDweller, power);
                    }
                }
                pv.Hand = p.Hand.Select(c => View(engine, state, c, p.Id != viewer)).ToList();
                pv.Battlefield = p.Battlefield.Select(c => View(engine, state, c, false)).ToList();
                pv.Graveyard = p.Graveyard.Select(c => View(engine, state, c, false)).ToList();
                pv.Exile = p.Exile.Select(c => View(engine, state, c, false)).ToList();
                snap.Players.Add(pv);
            }
            foreach (var item in state.Chain)
                snap.Chain.Add(new ChainView
                {
                    Id = item.Id, ObjectId = item.ObjectId, Kind = item.Kind, Controller = item.Controller,
                    SourceDefinitionId = item.SourceDefinitionId ?? item.Card?.DefinitionId,
                    Source = item.Card?.Id ?? item.SourceId,
                    Text = item.Text, Targets = item.Targets.ToList(), IsTavernDwellerPower = item.IsTavernDwellerPower,
                    Times = item.Times,
                });
            return snap;
        }

        private static CardView View(GameEngine engine, GameState state, CardInstance c, bool hide)
        {
            var v = new CardView { Id = c.Id, Owner = c.Owner, Controller = c.Controller, Zone = c.Zone };
            if (hide || c.IsHidden) return v;
            var def = engine.Cards.Get(c.DefinitionId);
            v.DefinitionId = c.DefinitionId;
            v.Name = def.Name;
            v.Type = def.Type;
            v.Faction = def.Faction;
            v.Rarity = def.Rarity;
            v.Text = def.Text;
            v.Subtypes = def.Subtypes;
            v.IsToken = c.IsToken;
            v.PrintedCost = v.Cost = def.Cost;
            if (c.Zone == Zone.Hand) v.Cost = Costs.SpellCost(state, engine.Cards, c.Controller, def);
            v.PrintedPower = v.Power = def.Power;
            v.PrintedHealth = v.MaxHealth = def.Health;
            v.Keywords = def.Keywords;
            v.Damage = c.Damage;
            v.PlusOneCounters = c.PlusOneCounters;
            v.Tapped = c.Tapped;
            v.AttachedTo = c.AttachedToObject;
            v.AttachedToPlayer = c.AttachedToPlayer;
            if (c.Zone == Zone.Battlefield && def.Type == CardType.Creature)
            {
                var ch = engine.GetCharacteristics(state, c);
                v.Power = ch.Power;
                v.MaxHealth = ch.MaxHealth;
                v.Keywords = ch.Keywords;
                var combat = state.Combat;
                var attack = combat?.AttackOf(c.Id);
                if (attack != null)
                {
                    v.IsAttacking = true;
                    v.Attacks = attack.Defender;
                }
                var block = combat?.Blocks.Find(b => b.Blocker == c.Id);
                if (block != null) v.Blocks = block.Attacker;
            }
            return v;
        }
    }
}
