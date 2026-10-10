using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Logic
{
    /// <summary>One staged attacker (and the player it attacks) or one staged blocker (and the attacker it blocks).</summary>
    public struct StagedCombatant
    {
        public ObjectId Creature;
        public PlayerId Defender;
        public ObjectId Blocks;
    }

    /// <summary>
    /// Legends of Runeterra-style combat for the table (user direction 2026-10-10): the attacker drags creatures
    /// into the combat lane and confirms once; the defender drags blockers in front of attackers and confirms once.
    /// Nothing reaches the engine until the confirm, so the player can rearrange freely.
    ///
    /// The engine declares attackers and blockers one at a time (after the attack action and its priority window),
    /// so <see cref="MatchSession.CommitCombat"/> replays the staged list as engine actions. What may be staged
    /// is worked out on a copy of the state: the engine stays the only judge of legality.
    /// </summary>
    public sealed class CombatStage
    {
        private readonly MatchSession _session;
        private readonly List<StagedCombatant> _staged = new List<StagedCombatant>();

        public bool IsBlocking { get; }
        public PlayerId Player { get; }
        public IReadOnlyList<StagedCombatant> Staged => _staged;

        private CombatStage(MatchSession session, bool blocking)
        {
            _session = session;
            IsBlocking = blocking;
            Player = session.Viewer;
        }

        /// <summary>An attack can be staged: the viewer holds the attack token and may attack now, or is declaring attackers.</summary>
        public static bool CanAttack(MatchSession s) =>
            s.HumanToAct && (s.State.Pending?.Kind == DecisionKind.DeclareAttackers
                || s.LegalForViewer().Any(a => a.Kind == ActionKind.GoToCombat));

        public static bool CanBlock(MatchSession s) => s.HumanToAct && s.State.Pending?.Kind == DecisionKind.DeclareBlockers;

        public static CombatStage ForAttack(MatchSession s) => CanAttack(s) ? new CombatStage(s, false) : null;
        public static CombatStage ForBlock(MatchSession s) => CanBlock(s) ? new CombatStage(s, true) : null;

        /// <summary>The engine's declare actions once the staged ones are done, on a copy (attackers: after going to combat).</summary>
        private List<PlayerAction> NextDeclarations()
        {
            var engine = _session.Engine;
            var state = _session.State.Clone();
            var kind = IsBlocking ? DecisionKind.DeclareBlockers : DecisionKind.DeclareAttackers;
            if (!IsBlocking && state.Pending?.Kind != kind)
            {
                engine.Apply(state, PlayerAction.GoToCombat(Player));
                // Everyone passes the priority window before attackers are declared (on the copy only).
                for (int i = 0; i < 50 && state.Pending?.Kind != kind && engine.WaitingOn(state) is PlayerId w; i++)
                    engine.Apply(state, engine.GetLegalActions(state, w).FirstOrDefault(a => a.Kind == ActionKind.PassPriority)
                        ?? engine.GetLegalActions(state, w)[0]);
                if (state.Pending?.Kind != kind || state.Pending.Player != Player) return new List<PlayerAction>();
            }
            foreach (var c in _staged)
            {
                var action = ToAction(c);
                if (!engine.GetLegalActions(state, Player).Contains(action)) continue;
                engine.Apply(state, action);
            }
            return engine.GetLegalActions(state, Player)
                .Where(a => a.Kind == (IsBlocking ? ActionKind.DeclareBlocker : ActionKind.DeclareAttacker)).ToList();
        }

        public PlayerAction ToAction(StagedCombatant c) =>
            IsBlocking ? PlayerAction.Block(Player, c.Creature, c.Blocks) : PlayerAction.Attack(Player, c.Creature, c.Defender);

        /// <summary>Creatures that can still be put in the lane (glow these).</summary>
        public HashSet<ObjectId> Candidates() => new HashSet<ObjectId>(NextDeclarations().Select(a => a.Card));

        /// <summary>Blocking: the attackers this creature may block now. Attacking: empty.</summary>
        public List<ObjectId> BlockableBy(ObjectId blocker) =>
            IsBlocking ? NextDeclarations().Where(a => a.Card == blocker).Select(a => a.BlockedAttacker).Distinct().ToList() : new List<ObjectId>();

        /// <summary>Attacking: the players this creature may attack (several only in multiplayer).</summary>
        public List<PlayerId> DefendersFor(ObjectId attacker) =>
            IsBlocking ? new List<PlayerId>() : NextDeclarations().Where(a => a.Card == attacker).Select(a => a.Defender).Distinct().ToList();

        /// <summary>Put an attacker in the lane (against <paramref name="defender"/>, or the first legal one).</summary>
        public bool StageAttacker(ObjectId creature, PlayerId? defender = null)
        {
            if (IsBlocking) return false;
            var options = NextDeclarations().Where(a => a.Card == creature && (defender == null || a.Defender == defender)).ToList();
            if (options.Count == 0) return false;
            _staged.Add(new StagedCombatant { Creature = creature, Defender = options[0].Defender });
            return true;
        }

        /// <summary>Put a blocker in front of an attacker.</summary>
        public bool StageBlocker(ObjectId blocker, ObjectId attacker)
        {
            if (!IsBlocking || !NextDeclarations().Any(a => a.Card == blocker && a.BlockedAttacker == attacker)) return false;
            _staged.Add(new StagedCombatant { Creature = blocker, Blocks = attacker });
            return true;
        }

        /// <summary>Take a creature back out of the lane. Later ones that depended on it are dropped too.</summary>
        public void Unstage(ObjectId creature)
        {
            int i = _staged.FindIndex(c => c.Creature == creature);
            if (i < 0) return;
            var rest = _staged.Skip(i + 1).ToList();
            _staged.RemoveRange(i, _staged.Count - i);
            foreach (var c in rest)
            {
                if (IsBlocking) StageBlocker(c.Creature, c.Blocks);
                else StageAttacker(c.Creature, c.Defender);
            }
        }

        public void Clear() => _staged.Clear();
    }
}
