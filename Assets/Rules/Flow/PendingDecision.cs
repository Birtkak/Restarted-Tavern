using System.Collections.Generic;

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
        /// <summary>
        /// "Look at the top N cards of your deck. Put one into your hand and the rest into your graveyard."
        /// (Grave Gossip). ChooseOption = the index from the top (0 = top card).
        /// </summary>
        ChooseFromTop,
        /// <summary>
        /// "Each player may pay any amount of Gold." (Dice Game). Players choose in turn order, starting with
        /// the active player, and see earlier choices (MTG 101.4, decided 2026-10-09). ChooseOption = Gold paid.
        /// </summary>
        PayAnyGold,
        /// <summary>
        /// "Choose a [creature / card] ..." during resolution (sacrifice, discard, put onto the battlefield...).
        /// Answered with ChooseTarget (one of <see cref="PendingDecision.Choices"/>), or ChooseTarget with no
        /// target to decline when <see cref="PendingDecision.Optional"/>. Then its follow-up effects run.
        /// </summary>
        ChooseObject,
        /// <summary>"You may ..." / "may give you 2 Gold" during resolution: ChooseOption 1 = yes, 0 = no.</summary>
        YesNo,
        /// <summary>
        /// Legendary rule (MTG 704.5j, decided 2026-10-09): the player controls two or more Legendary permanents
        /// with the same name and keeps one of <see cref="PendingDecision.Choices"/>; the rest go to the graveyard.
        /// Answered with ChooseTarget.
        /// </summary>
        KeepLegendary,
        /// <summary>
        /// Combat damage (§7.2.6): <see cref="PendingDecision.Card"/> deals <see cref="PendingDecision.Count"/> damage
        /// and divides it among <see cref="PendingDecision.Choices"/> (its blockers, or the attackers it blocks) however
        /// its controller likes. Only asked when it can't kill them all. Answered with AssignCombatDamage.
        /// </summary>
        AssignCombatDamage,
        /// <summary>
        /// The player has two or more different triggers waiting to go on the Chain at the same time and picks
        /// which goes on next (MTG 603.3b; the last one put on resolves first). ChooseOption = an index into
        /// GameState.PendingTriggers. Triggers of the same ability of the same card are offered once.
        /// </summary>
        OrderTriggers,
        /// <summary>
        /// "Deal N damage divided as you choose among any number of creatures and/or opponents" (Arc Cascade): pick who gets
        /// the next point (ChooseTarget, one of <see cref="PendingDecision.TargetChoices"/>). After <see cref="PendingDecision.Count"/>
        /// points all the damage is dealt at once.
        /// </summary>
        DivideDamage,
        /// <summary>
        /// "Choose up to N [objects]" during resolution (Snik, MTG 608.2d: choices without "target" are made on resolution).
        /// Pick one at a time: ChooseTarget adds one of <see cref="PendingDecision.Choices"/>, ChooseTarget with no target
        /// stops. After <see cref="PendingDecision.Count"/> picks (or none left) <see cref="PendingDecision.Then"/> runs once,
        /// with the chosen objects as its targets (<see cref="PendingDecision.Assigned"/>).
        /// </summary>
        ChooseUpTo,
    }

    /// <summary>
    /// A choice the game is waiting on that isn't a normal priority pass. While one is
    /// pending, only <see cref="Player"/> can act.
    /// </summary>
    public sealed class PendingDecision
    {
        public DecisionKind Kind { get; set; }
        public PlayerId Player { get; set; }
        /// <summary>BottomCards / DiscardToHandSize / DiscardCards: how many cards are still to go. AssignCombatDamage: the damage to divide.</summary>
        public int Count { get; set; }
        /// <summary>ChooseTriggerTarget: the trigger being put on the Chain.</summary>
        public PendingTrigger Trigger { get; set; }
        /// <summary>TopOrBottom: the card looked at. PayTax: the Chain object that is countered if they don't pay. AssignCombatDamage: the creature dealing the damage.</summary>
        public ObjectId Card { get; set; }
        /// <summary>PayTax: who gets <see cref="RewardGold"/> if the tax is paid ("If they pay, you gain 2 Gold").</summary>
        public PlayerId Beneficiary { get; set; }
        public int RewardGold { get; set; }
        /// <summary>ChooseFromTop: the other cards go to the bottom of the deck instead of the graveyard (Pocket Change).</summary>
        public bool RestToBottom { get; set; }
        /// <summary>ChooseObject / KeepLegendary: the objects that can be chosen. AssignCombatDamage: the recipients, in order.</summary>
        public List<ObjectId> Choices { get; set; }
        /// <summary>ChooseObject: choosing nothing is allowed ("you may").</summary>
        public bool Optional { get; set; }
        /// <summary>ChooseObject / YesNo: what happens after a choice (EventObject = the chosen object, EventPlayer = the chooser) or a decline / "no".</summary>
        public List<Effect> Then { get; set; }
        public List<Effect> Else { get; set; }
        /// <summary>ChooseObject / YesNo: who controls the follow-up effects, and where they come from.</summary>
        public PlayerId EffectController { get; set; }
        public ObjectId Source { get; set; }
        public string SourceDefinitionId { get; set; }
        /// <summary>ChooseObject / YesNo: what is being asked, for UIs and logs ("Sacrifice a creature").</summary>
        public string Prompt { get; set; }
        /// <summary>PayAnyGold: who has chosen so far, and how much each paid (same order).</summary>
        /// <summary>DivideDamage: who can get a point, and the points assigned so far.</summary>
        public List<Target> TargetChoices { get; set; }
        public List<Target> Assigned { get; set; }
        public List<PlayerId> Bidders { get; set; }
        public List<int> Bids { get; set; }

        public PendingDecision Clone()
        {
            var d = (PendingDecision)MemberwiseClone();
            d.Trigger = Trigger?.Clone();
            if (Bidders != null) d.Bidders = new List<PlayerId>(Bidders);
            if (Bids != null) d.Bids = new List<int>(Bids);
            if (Choices != null) d.Choices = new List<ObjectId>(Choices);
            if (TargetChoices != null) d.TargetChoices = new List<Target>(TargetChoices);
            if (Assigned != null) d.Assigned = new List<Target>(Assigned);
            return d;
        }

        public override string ToString() => Kind + " (" + Player + ")";
    }
}
