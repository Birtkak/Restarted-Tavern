namespace RestartedTavern.Rules
{
    /// <summary>
    /// Creates a replacement effect for a while (MTG 614): "Until end of turn, if target creature would die, return it to its
    /// owner's hand instead", "The next time a source would deal damage to you this turn, prevent that damage".
    /// </summary>
    public sealed class AddReplacementEffect : Effect
    {
        public ReplacementAbility Replacement { get; set; }
        /// <summary>The target creature is the one it's about (set <see cref="ReplacementAbility.OnlyChosenObject"/> too).</summary>
        public bool ForTarget { get; set; }
        public bool UntilEndOfTurn { get; set; } = true;
        /// <summary>"The next time": how many times it applies. 0 = every time while it lasts.</summary>
        public int Uses { get; set; }

        public override void Resolve(EffectContext ctx)
        {
            var affected = ObjectId.None;
            if (ForTarget)
            {
                var c = ctx.CreatureAt(TargetIndex);
                if (c == null) return;
                affected = c.Id;
            }
            ctx.AddReplacement(Replacement, affected, UntilEndOfTurn, Uses);
        }
    }
}
