using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// Plain-text descriptions of cards, actions and events, for debug UIs, logs and replays.
    /// Objects change id when they change zone, so callers can pass a name cache
    /// (<see cref="Remember"/>) to keep naming things that have left the battlefield.
    /// </summary>
    public sealed class GameText
    {
        private readonly CardDatabase _db;
        private readonly Dictionary<ObjectId, string> _known = new Dictionary<ObjectId, string>();

        public GameText(CardDatabase db) { _db = db; }

        /// <summary>Record the names of every object currently in the game, and objects named in events.</summary>
        public void Remember(GameState state, IEnumerable<GameEvent> events = null)
        {
            foreach (var p in state.Players)
            {
                RememberAll(p.Battlefield);
                RememberAll(p.Hand);
                RememberAll(p.Graveyard);
                RememberAll(p.Exile);
                RememberAll(p.TavernDwellerZone);
            }
            foreach (var item in state.Chain)
                if (item.Card != null) _known[item.Card.Id] = item.Card.DefinitionId;
            if (events == null) return;
            foreach (var e in events)
            {
                switch (e)
                {
                    case ZoneChangedEvent z:
                        _known[z.OldId] = z.DefinitionId;
                        if (!z.NewId.IsNone) _known[z.NewId] = z.DefinitionId;
                        break;
                    case TokenCreatedEvent t: _known[t.Token] = t.DefinitionId; break;
                    case CardDrawnEvent d: _known[d.Card] = d.DefinitionId; break;
                    case SpellCastEvent s: _known[s.Card] = s.DefinitionId; break;
                    case CreatureDiedEvent c: _known[c.Card] = c.DefinitionId; break;
                }
            }
        }

        private void RememberAll(List<CardInstance> cards)
        {
            foreach (var c in cards)
                if (c.DefinitionId != null) _known[c.Id] = c.DefinitionId;
        }

        public string Name(string definitionId) =>
            definitionId != null && _db.Contains(definitionId) ? _db.Get(definitionId).Name : "?";

        public string Name(GameState state, ObjectId id)
        {
            var obj = state.FindObject(id);
            if (obj?.DefinitionId != null) return Name(obj.DefinitionId) + CopyTag(state, obj);
            var item = state.FindOnChain(id);
            if (item != null) return Name(item.SourceDefinitionId) + (item.IsTavernDwellerPower ? " (Power)" : " (ability)");
            return _known.TryGetValue(id, out var def) ? Name(def) : id.ToString();
        }

        /// <summary>
        /// " #2" when two or more permanents on the battlefield share this one's name (two Goober tokens),
        /// numbered in board order (players in seat order), so the copies can be told apart. Otherwise "".
        /// </summary>
        public string CopyTag(GameState state, CardInstance card)
        {
            if (card?.DefinitionId == null || card.Zone != Zone.Battlefield) return "";
            string name = Name(card.DefinitionId);
            int index = 0, count = 0;
            foreach (var p in state.Players)
                foreach (var c in p.Battlefield)
                {
                    if (c.DefinitionId == null || Name(c.DefinitionId) != name) continue;
                    count++;
                    if (c.Id == card.Id) index = count;
                }
            return count >= 2 && index > 0 ? " #" + index : "";
        }

        public string Name(GameState state, Target target) =>
            target.IsPlayer ? target.Player.ToString() : Name(state, target.Object);

        /// <summary>"Hog-Rider 3/3", plus damage, keywords and status.</summary>
        public string Describe(GameState state, CardInstance card, bool multiline = true)
        {
            if (card.IsHidden) return "(hidden)";
            var def = _db.Get(card.DefinitionId);
            var sb = new StringBuilder();
            string sep = multiline ? "\n" : "  ";
            sb.Append(def.Name);
            if (card.IsToken) sb.Append(" (token)");
            sb.Append(CopyTag(state, card));

            if (def.IsTavernDweller)
            {
                sb.Append(sep).Append("Tavern Dweller (").Append(string.Join(" + ", def.TavernDwellerFactions)).Append(')');
                if (multiline && def.Text.Length > 0) sb.Append('\n').Append(def.Text);
                if (PowerUsedThisTurn(state, card)) sb.Append(sep).Append("Power used this turn");
            }
            else if (def.IsCreature && card.Zone == Zone.Battlefield)
            {
                var ch = CharacteristicsCalculator.Compute(state, _db, card);
                sb.Append(sep).Append(ch.Power).Append('/').Append(ch.MaxHealth);
                if (card.Damage > 0) sb.Append("  dmg ").Append(card.Damage).Append(" (").Append(ch.RemainingHealth).Append(" left)");
                var kw = KeywordText(ch.Keywords);
                if (kw.Length > 0) sb.Append(sep).Append(kw);
                var status = new List<string>();
                if (card.Tapped) status.Add("TAPPED");
                if (card.SummoningSick && !ch.Has(Keyword.Haste) && !state.Format.NoSummoningSickness) status.Add("sick");
                if (state.Combat != null && state.Combat.IsAttacking(card.Id)) status.Add("ATTACKING");
                if (state.Combat != null && state.Combat.IsBlocking(card.Id)) status.Add("BLOCKING");
                if (CharacteristicsCalculator.IsEquipped(state, _db, card)) status.Add("equipped");
                if (status.Count > 0) sb.Append(sep).Append(string.Join(" ", status));
            }
            else
            {
                sb.Append(sep).Append('(').Append(def.Cost).Append(") ").Append(def.Type);
                if (def.IsCreature) sb.Append(' ').Append(def.Power).Append('/').Append(def.Health);
                if (multiline && def.Text.Length > 0) sb.Append('\n').Append(def.Text);
                if (card.Zone == Zone.Battlefield && card.Tapped) sb.Append(sep).Append("TAPPED");
                if (card.Zone == Zone.Battlefield && !card.AttachedToObject.IsNone)
                    sb.Append(sep).Append("on ").Append(Name(state, card.AttachedToObject));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Everything about one card, for an inspector panel: cost, type, subtypes, faction and rarity; current and
        /// printed stats, damage and counters; keywords; the full rules text; what's attached to it or what it's
        /// attached to; status and controller.
        /// </summary>
        public string Details(GameState state, CardInstance card)
        {
            if (card == null) return "";
            if (card.IsHidden) return "(hidden card)";
            var def = _db.Get(card.DefinitionId);
            var sb = new StringBuilder();
            sb.Append(def.Name);
            if (card.IsToken) sb.Append(" (token)");
            sb.Append(CopyTag(state, card));
            sb.Append('\n');

            if (def.IsTavernDweller)
                sb.Append("Tavern Dweller · ").Append(string.Join(" + ", def.TavernDwellerFactions.Select(FactionName)));
            else
            {
                sb.Append("Cost ").Append(def.Cost).Append(" · ").Append(def.Type);
                if (def.Subtypes.Length > 0) sb.Append(" — ").Append(string.Join(" ", def.Subtypes));
                sb.Append(" · ").Append(FactionName(def.Faction)).Append(" · ").Append(def.Rarity);
            }
            sb.Append('\n');

            if (def.IsCreature)
            {
                if (card.Zone == Zone.Battlefield)
                {
                    var ch = CharacteristicsCalculator.Compute(state, _db, card);
                    sb.Append("Power/Health ").Append(ch.Power).Append('/').Append(ch.MaxHealth);
                    if (ch.Power != def.Power || ch.MaxHealth != def.Health)
                        sb.Append(" (printed ").Append(def.Power).Append('/').Append(def.Health).Append(')');
                    sb.Append('\n');
                    if (card.Damage > 0)
                        sb.Append("Damage ").Append(card.Damage).Append(": ").Append(ch.RemainingHealth).Append(" Health left (damage stays)\n");
                    if (card.PlusOneCounters > 0) sb.Append("+1/+1 counters: ").Append(card.PlusOneCounters).Append('\n');
                    var kw = KeywordText(ch.Keywords);
                    if (kw.Length > 0) sb.Append("Keywords: ").Append(kw).Append('\n');
                }
                else
                {
                    sb.Append("Power/Health ").Append(def.Power).Append('/').Append(def.Health).Append('\n');
                }
            }

            if (def.Text.Length > 0) sb.Append('\n').Append(def.Text).Append('\n');

            if (card.Zone == Zone.Battlefield)
            {
                var attached = state.AllPermanents().Where(p => p.AttachedToObject == card.Id).Select(p => Name(p.DefinitionId)).ToList();
                if (attached.Count > 0) sb.Append("\nAttached: ").Append(string.Join(", ", attached)).Append('\n');
                if (!card.AttachedToObject.IsNone) sb.Append("\nAttached to ").Append(Name(state, card.AttachedToObject)).Append('\n');
                if (card.AttachedToPlayer.HasValue) sb.Append("\nCursing ").Append(card.AttachedToPlayer.Value).Append('\n');

                var status = new List<string>();
                if (card.Tapped) status.Add("tapped");
                if (def.IsCreature && card.SummoningSick && !state.Format.NoSummoningSickness
                    && !CharacteristicsCalculator.Compute(state, _db, card).Has(Keyword.Haste))
                    status.Add("summoning sick (can't attack or tap yet)");
                if (state.Combat != null && state.Combat.IsAttacking(card.Id)) status.Add("attacking");
                if (state.Combat != null && state.Combat.IsBlocking(card.Id)) status.Add("blocking");
                if (status.Count > 0) sb.Append("Status: ").Append(string.Join(", ", status)).Append('\n');
            }
            if (def.IsTavernDweller && PowerUsedThisTurn(state, card)) sb.Append("Power already used this turn\n");
            sb.Append("Controller: ").Append(card.Controller);
            if (card.Owner != card.Controller) sb.Append(" (owner ").Append(card.Owner).Append(')');
            return sb.ToString();
        }

        /// <summary>"shadow_money_wizards" → "Shadow Money Wizards".</summary>
        public static string FactionName(string faction) =>
            string.Join(" ", (faction ?? "").Split('_').Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w.Substring(1)));

        /// <summary>Step names for players: "Main phase 1", "Declare attackers".</summary>
        public static string StepName(Step step)
        {
            switch (step)
            {
                case Step.Mulligan: return "Mulligan";
                case Step.Start: return "Start of turn";
                case Step.Draw: return "Draw";
                case Step.Main1: return "Main phase 1";
                case Step.BeginCombat: return "Beginning of combat";
                case Step.DeclareAttackers: return "Declare attackers";
                case Step.DeclareBlockers: return "Declare blockers";
                case Step.CombatDamage: return "Combat damage";
                case Step.Main2: return "Main phase 2";
                case Step.End: return "End step";
                case Step.Cleanup: return "Cleanup";
                case Step.GameOver: return "Game over";
                default: return step.ToString();
            }
        }

        /// <summary>"Hog-Rider (2 Health left, equipped)": a creature on the battlefield, for choices between copies.</summary>
        private string DescribeOneLine(GameState state, ObjectId id)
        {
            var card = state.FindOnBattlefield(id);
            if (card == null || !_db.Get(card.DefinitionId).IsCreature) return Name(state, id);
            var ch = CharacteristicsCalculator.Compute(state, _db, card);
            return Name(state, id) + " (" + ch.RemainingHealth + " Health left"
                   + (CharacteristicsCalculator.IsEquipped(state, _db, card) ? ", equipped" : "") + ")";
        }

        private static bool PowerUsedThisTurn(GameState state, CardInstance tavernDweller) =>
            state.UsesThisTurn.ContainsKey("ability:" + tavernDweller.Id.Value + ":0");

        public static string KeywordText(Keyword k)
        {
            if (k == Keyword.None) return "";
            var parts = new List<string>();
            foreach (Keyword flag in Enum.GetValues(typeof(Keyword)))
                if (flag != Keyword.None && (k & flag) != 0)
                    parts.Add(flag == Keyword.CantBlock ? "Can't block" : flag.ToString());
            return string.Join(", ", parts);
        }

        public string Describe(GameState state, PlayerAction a)
        {
            switch (a.Kind)
            {
                case ActionKind.PassPriority:
                    return state.Chain.Count > 0 ? "Pass (let the top of the Chain resolve)" : "Pass (move on from " + StepName(state.Step) + ")";
                case ActionKind.Keep: return "Keep hand";
                case ActionKind.Mulligan: return "Mulligan";
                case ActionKind.BottomCard: return "Put on the bottom: " + Name(state, a.Card);
                case ActionKind.Discard: return "Discard: " + Name(state, a.Card);
                case ActionKind.FinishAttacks: return "Done attacking";
                case ActionKind.FinishBlocks: return "Done blocking";
                case ActionKind.ChooseTarget:
                    if (state.Pending?.Kind == DecisionKind.ChooseObject)
                        return a.Target.HasValue ? state.Pending.Prompt + ": " + Name(state, a.Target.Value) : "Choose nothing";
                    if (state.Pending?.Kind == DecisionKind.DivideDamage && a.Target.HasValue)
                        return "Deal 1 damage to " + Name(state, a.Target.Value) + "  (" + (state.Pending.Assigned.Count + 1) + " of " + state.Pending.Count + ")";
                    if (state.Pending?.Kind == DecisionKind.KeepLegendary)
                        return "Keep " + DescribeOneLine(state, a.Target.Value.Object) + " (the other copies go to the graveyard)";
                    return a.Target.HasValue ? "Target: " + Name(state, a.Target.Value) : "No target (decline)";
                case ActionKind.ChooseOption:
                    if (state.Pending?.Kind == DecisionKind.TopOrBottom)
                        return (a.Option == 1 ? "Put on the bottom: " : "Leave on top: ") + Name(state, state.Pending.Card);
                    if (state.Pending?.Kind == DecisionKind.ChooseFromTop)
                    {
                        var deck = state.GetPlayer(a.Player).Deck;
                        return "Take " + (a.Option < deck.Count ? Name(state, deck[a.Option].Id) : "?") + " (the rest go to the graveyard)";
                    }
                    if (state.Pending?.Kind == DecisionKind.PayAnyGold) return "Pay " + a.Option + " Gold";
                    if (state.Pending?.Kind == DecisionKind.OrderTriggers)
                    {
                        var t = a.Option < state.PendingTriggers.Count ? state.PendingTriggers[a.Option] : null;
                        return "Put on the Chain next (resolves after the ones you put later): "
                               + (t == null ? "?" : Name(t.SourceDefinitionId) + " trigger" + (string.IsNullOrEmpty(t.Ability.Text) ? "" : " [" + t.Ability.Text + "]"));
                    }
                    if (state.Pending?.Kind == DecisionKind.YesNo) return (a.Option == 1 ? "Yes: " : "No: ") + state.Pending.Prompt;
                    if (state.Pending?.Kind == DecisionKind.PayTax)
                        return a.Option == 1 ? "Pay " + state.Pending.Count : "Don't pay (" + Name(state, state.Pending.Card) + " is countered)";
                    return "Option " + a.Option;
                case ActionKind.ActivateAbility: return DescribeActivation(state, a);
                case ActionKind.AssignCombatDamage:
                {
                    var d = state.Pending;
                    if (d == null || d.Choices == null) return a.ToString();
                    var parts = new List<string>();
                    for (int i = 0; i < d.Choices.Count && i < a.Division.Length; i++)
                        parts.Add(a.Division[i] + " to " + DescribeOneLine(state, d.Choices[i]));
                    return "Damage from " + Name(state, d.Card) + ": " + string.Join(", ", parts);
                }
                case ActionKind.DeclareAttacker: return "Attack " + a.Defender + " with " + Name(state, a.Card);
                case ActionKind.DeclareBlocker: return "Block " + Name(state, a.BlockedAttacker) + " with " + Name(state, a.Card);
                case ActionKind.PlayCard:
                {
                    var sb = new StringBuilder("Play ").Append(Name(state, a.Card));
                    for (int i = 0; i < a.Targets.Length; i++)
                    {
                        var target = a.Targets[i];
                        sb.Append(i == 0 ? " -> " : ", ").Append(Name(state, target));
                        var t = target.IsPlayer ? null : state.FindObject(target.Object);
                        if (t != null) sb.Append(" (").Append(t.Controller).Append(')');
                    }
                    var card = state.FindObject(a.Card);
                    if (card?.DefinitionId != null)
                    {
                        var def = _db.Get(card.DefinitionId);
                        int gold = Payment.GoldNeeded(state, _db, state.GetPlayer(a.Player), def, a.X);
                        if (gold > 0) sb.Append("  [uses ").Append(gold).Append(" Gold]");
                        if (def.XGoldExtraCost) sb.Append("  [X=").Append(a.X).Append(" Gold]");
                        if (def.XCost) sb.Append("  [X=").Append(a.X).Append(']');
                    }
                    if (a.Invest) sb.Append("  +INVEST");
                    if (!a.Sacrifice.IsNone) sb.Append("  [sacrifice ").Append(Name(state, a.Sacrifice)).Append(']');
                    if (a.Division.Length > 0) sb.Append("  [split ").Append(string.Join("/", a.Division)).Append(']');
                    return sb.ToString();
                }
                default: return a.ToString();
            }
        }

        private string DescribeActivation(GameState state, PlayerAction a)
        {
            var source = state.FindObject(a.Card);
            var def = _db.Get(source.DefinitionId);
            var abilities = new GameRunner(_db, state, null).AbilitiesOf(source);
            var ab = abilities[a.AbilityIndex];
            var sb = new StringBuilder();
            sb.Append(ab.IsTavernDwellerPower ? "Tavern Dweller Power (" + def.Name + "): " : def.Name + ": ").Append(ab.Text);
            for (int i = 0; i < a.Targets.Length; i++)
            {
                var target = a.Targets[i];
                sb.Append(i == 0 ? " -> " : ", ").Append(Name(state, target));
                var t = target.IsPlayer ? null : state.FindObject(target.Object);
                if (t != null && t.Zone == Zone.Battlefield) sb.Append(" (").Append(t.Controller).Append(')');
            }
            if (ab.HasX) sb.Append("  [X=").Append(a.X).Append(']');
            if (!a.Sacrifice.IsNone) sb.Append("  [sacrifice ").Append(Name(state, a.Sacrifice)).Append(']');
            var p = state.GetPlayer(a.Player);
            int generic = Costs.AbilityCost(state, _db, a.Player, ab) + (ab.HasX ? a.X : 0);
            int gold = Payment.GoldNeeded(p, generic, true, ab.GoldCost);
            if (gold > 0) sb.Append("  [uses ").Append(gold).Append(" Gold]");
            return sb.ToString();
        }

        /// <summary>Describe an event. Draws are only named for <paramref name="viewer"/> (null = everyone sees everything).</summary>
        public string Describe(GameState state, GameEvent e, PlayerId? viewer = null)
        {
            switch (e)
            {
                case TurnStartedEvent t: return "=== Turn " + t.Turn + ": " + t.Player + " ===";
                case CardDrawnEvent d:
                    return viewer == null || viewer == d.Player ? d.Player + " draws " + Name(d.DefinitionId) : d.Player + " draws a card";
                case SpellCastEvent s:
                    return s.Player + " casts " + Name(s.DefinitionId)
                           + (s.Targets != null && s.Targets.Length > 0 ? " -> " + string.Join(", ", s.Targets.Select(t => Name(state, t))) : "")
                           + (s.Invested ? " (Invested)" : "");
                case AbilityActivatedEvent act:
                    return act.Player + (act.IsTavernDwellerPower ? " uses the Tavern Dweller Power of " : " activates ") + Name(act.SourceDefinitionId)
                           + (act.Targets != null && act.Targets.Length > 0 ? " -> " + string.Join(", ", act.Targets.Select(t => Name(state, t))) : "")
                           + (act.X > 0 ? " (X=" + act.X + ")" : "");
                case AttachedEvent at: return Name(at.EquipmentDefinitionId) + " is attached to " + Name(state, at.AttachedTo);
                case AbilityTriggeredEvent t:
                    return Name(t.SourceDefinitionId) + " triggers (" + t.When + ")"
                           + (t.Target.HasValue ? " -> " + Name(state, t.Target.Value) : "");
                case ChainItemResolvedEvent r: return "Resolved: " + Name(r.SourceDefinitionId);
                case FizzledEvent f: return Name(f.SourceDefinitionId) + " fizzles (target gone)";
                case CounteredEvent c: return Name(c.SourceDefinitionId) + " is countered";
                case ControlChangedEvent cc: return cc.To + " gains control of " + Name(cc.DefinitionId);
                case DamageDealtEvent d:
                    return Name(state, d.Source) + " deals " + d.Amount + " to " + Name(state, d.Target) + (d.IsCombat ? " (combat)" : "");
                case HealedEvent h: return Name(state, h.Target) + " is healed for " + h.Amount;
                case LifeChangedEvent l: return l.Player + " life " + l.OldLife + " -> " + l.NewLife;
                case GoldChangedEvent g: return g.Player + " Gold " + g.OldGold + " -> " + g.NewGold;
                case TokenCreatedEvent t: return t.Controller + " creates a " + Name(t.DefinitionId) + " token";
                case AttackerDeclaredEvent a: return Name(state, a.Attacker) + " attacks " + a.Defender;
                case BlockerDeclaredEvent b: return Name(state, b.Blocker) + " blocks " + Name(state, b.Attacker);
                case CreatureDiedEvent c: return Name(c.DefinitionId) + " dies";
                case PlayerLostEvent l: return l.Player + " LOSES (" + l.Reason + ")";
                case GameOverEvent g: return "GAME OVER. Winner: " + string.Join(", ", g.Winners);
                case MulliganEvent m: return m.Player + " mulligans";
                case GameStartedEvent s: return "Game started. " + s.StartingPlayer + " goes first.";
                case ZoneChangedEvent z:
                    if (z.To == Zone.Graveyard && z.From == Zone.Hand) return Name(z.DefinitionId) + " is discarded";
                    return null; // other zone changes are covered by more specific events
                default: return null; // step/mana changes are too noisy for the log
            }
        }
    }
}
