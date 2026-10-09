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
            if (obj?.DefinitionId != null) return Name(obj.DefinitionId);
            return _known.TryGetValue(id, out var def) ? Name(def) : id.ToString();
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
                if (card.SummoningSick && !ch.Has(Keyword.Haste)) status.Add("sick");
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
                    return state.Chain.Count > 0 ? "Pass (let the top of the Chain resolve)" : "Pass (" + state.Step + ")";
                case ActionKind.Keep: return "Keep hand";
                case ActionKind.Mulligan: return "Mulligan";
                case ActionKind.BottomCard: return "Put on the bottom: " + Name(state, a.Card);
                case ActionKind.Discard: return "Discard: " + Name(state, a.Card);
                case ActionKind.FinishAttacks: return "Done attacking";
                case ActionKind.FinishBlocks: return "Done blocking";
                case ActionKind.ChooseTarget: return a.Target.HasValue ? "Target: " + Name(state, a.Target.Value) : "No target (decline)";
                case ActionKind.ChooseOption:
                    if (state.Pending?.Kind == DecisionKind.TopOrBottom)
                        return (a.Option == 1 ? "Put on the bottom: " : "Leave on top: ") + Name(state, state.Pending.Card);
                    return "Option " + a.Option;
                case ActionKind.ActivateAbility: return DescribeActivation(state, a);
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
                        int gold = Payment.GoldNeeded(state, _db, state.GetPlayer(a.Player), def);
                        if (gold > 0) sb.Append("  [uses ").Append(gold).Append(" Gold]");
                        if (def.XGoldExtraCost) sb.Append("  [X=").Append(a.X).Append(" Gold]");
                    }
                    if (a.Invest) sb.Append("  +INVEST");
                    if (!a.Sacrifice.IsNone) sb.Append("  [sacrifice ").Append(Name(state, a.Sacrifice)).Append(']');
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
