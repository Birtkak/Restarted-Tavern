namespace RestartedTavern.Rules
{
    public enum DecisionKind
    {
        /// <summary>Keep or mulligan (London mulligan, GAME_DESIGN §3).</summary>
        Mulligan,
        /// <summary>Put cards from hand on the bottom after keeping a mulligan.</summary>
        BottomCards,
        DeclareAttackers,
        DeclareBlockers,
        /// <summary>Pick the target for a triggered ability as it goes on the Chain.</summary>
        ChooseTriggerTarget,
        /// <summary>Cleanup: discard down to the max hand size.</summary>
        DiscardToHandSize,
    }

    /// <summary>
    /// A choice the game is waiting on that isn't a normal priority pass. While one is
    /// pending, only <see cref="Player"/> can act.
    /// </summary>
    public sealed class PendingDecision
    {
        public DecisionKind Kind { get; set; }
        public PlayerId Player { get; set; }
        /// <summary>BottomCards / DiscardToHandSize: how many cards are still to go.</summary>
        public int Count { get; set; }
        /// <summary>ChooseTriggerTarget: the trigger being put on the Chain.</summary>
        public PendingTrigger Trigger { get; set; }

        public PendingDecision Clone()
        {
            var d = (PendingDecision)MemberwiseClone();
            d.Trigger = Trigger?.Clone();
            return d;
        }

        public override string ToString() => Kind + " (" + Player + ")";
    }
}
