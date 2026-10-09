using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// One building block of what a card does (DEVELOPMENT §1.6). Most cards are lists of
    /// these; rare cards can subclass Effect for custom behavior.
    /// Effects are shared between games, so they must not hold game state.
    /// </summary>
    public abstract class Effect
    {
        /// <summary>Which of the spell's targets this effect uses (0 = the first "target").</summary>
        public int TargetIndex { get; set; }

        public abstract void Resolve(EffectContext ctx);
    }

    /// <summary>What a resolving effect can see and do. Wraps the engine's game actions.</summary>
    public sealed class EffectContext
    {
        private readonly GameRunner _runner;

        internal EffectContext(GameRunner runner, PlayerId controller, ObjectId source, List<Target?> targets, int x = 0)
        {
            _runner = runner;
            Controller = controller;
            Source = source;
            Targets = targets;
            X = x;
        }

        /// <summary>The X paid for an activated ability (0 otherwise).</summary>
        public int X { get; }

        public GameState State => _runner.State;
        public CardDatabase Cards => _runner.Db;
        public PlayerId Controller { get; }
        /// <summary>The spell or the permanent the ability came from (it may have left the battlefield).</summary>
        public ObjectId Source { get; }
        /// <summary>Chosen targets in slot order; null where a target became illegal (MTG 608.2b).</summary>
        public IReadOnlyList<Target?> Targets { get; }
        public Target? Target => TargetAt(0);
        public Target? TargetAt(int index) => index >= 0 && index < Targets.Count ? Targets[index] : null;

        /// <summary>A creature target that is still on the battlefield, or null.</summary>
        public CardInstance CreatureAt(int index)
        {
            var t = TargetAt(index);
            return t.HasValue && !t.Value.IsPlayer ? State.FindOnBattlefield(t.Value.Object) : null;
        }

        public void DealDamage(Target target, int amount) => _runner.DealDamage(Source, target, amount, false);
        public void Heal(Target target, int amount) => _runner.Heal(target, amount);
        public void Draw(PlayerId player, int count) => _runner.Draw(player, count);
        public void GainGold(PlayerId player, int amount) => _runner.ChangeGold(player, amount);
        public void LoseLife(PlayerId player, int amount) => _runner.ChangeLife(player, -amount);
        public CardInstance CreateToken(PlayerId controller, string definitionId) => _runner.CreateToken(controller, definitionId);
        public void AddCounters(ObjectId creature, int count) => _runner.AddCounters(creature, count);

        public void Fight(CardInstance a, CardInstance b) => _runner.Fight(a, b);
        public void Attach(CardInstance equipment, CardInstance creature) => _runner.Attach(equipment, creature);
        public CardInstance MoveToHand(CardInstance card) => _runner.MoveCard(card, Zone.Hand);
        /// <summary>Ask the controller whether to put the top card of their deck on the bottom. Must be the last effect.</summary>
        public void AskTopOrBottom() => _runner.AskTopOrBottom(Controller);
        public void Destroy(CardInstance permanent) => _runner.MoveCard(permanent, Zone.Graveyard);

        public void ModifyUntilEndOfTurn(ObjectId creature, int power, int health, Keyword grants) =>
            _runner.ModifyUntilEndOfTurn(creature, power, health, grants);

        public IEnumerable<PlayerState> Opponents()
        {
            foreach (var p in State.LivingPlayersFrom(Controller))
                if (State.AreOpponents(Controller, p.Id)) yield return p;
        }

        /// <summary>Creatures controlled by opponents, as a snapshot (safe to damage while iterating).</summary>
        public List<CardInstance> EnemyCreatures()
        {
            var list = new List<CardInstance>();
            foreach (var p in Opponents())
                foreach (var c in p.Battlefield)
                    if (Cards.Get(c.DefinitionId).IsCreature) list.Add(c);
            return list;
        }

        public Characteristics GetCharacteristics(CardInstance card) =>
            CharacteristicsCalculator.Compute(State, Cards, card);
    }
}
