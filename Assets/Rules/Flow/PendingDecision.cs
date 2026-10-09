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
        /// <summary>
        /// A resolving effect looked at the top card of the deck: 0 = leave it on top, 1 = put it on
        /// the bottom (Auditor Prime). Answered with ChooseOption.
        /// </summary>
        TopOrBottom,
        /// <summary>A resolving effect makes a player discard (Settle the Tab: "then discard a card"). Answered with Discard.</summary>
        DiscardCards,
        /// <summary>
        /// "Counter target spell unless its controller pays N" (Hush Money): 1 = pay (mana first, then Gold,
        /// §5.2), 0 = don't. Answered with ChooseOption. Only offered when they can pay.
        /// </summary>
        PayTax,
    }

    /// <summary>
    /// A choice the game is waiting on that isn't a normal priority pass. While one is
    /// pending, only <see cref="Player"/> can act.
    /// </summary>
    public sealed class PendingDecision
    {
        public DecisionKind Kind { get; set; }
        public PlayerId Player { get; set; }
        /// <summary>BottomCards / DiscardToHandSize / DiscardCards: how many cards are still to go.</summary>
        public int Count { get; set; }
        /// <summary>ChooseTriggerTarget: the trigger being put on the Chain.</summary>
        public PendingTrigger Trigger { get; set; }
        /// <summary>TopOrBottom: the card looked at. PayTax: the Chain object that is countered if they don't pay.</summary>
        public ObjectId Card { get; set; }
        /// <summary>PayTax: who gets <see cref="RewardGold"/> if the tax is paid ("If they pay, you gain 2 Gold").</summary>
        public PlayerId Beneficiary { get; set; }
        public int RewardGold { get; set; }

        public PendingDecision Clone()
        {
            var d = (PendingDecision)MemberwiseClone();
            d.Trigger = Trigger?.Clone();
            return d;
        }

        public override string ToString() => Kind + " (" + Player + ")";
    }
}
