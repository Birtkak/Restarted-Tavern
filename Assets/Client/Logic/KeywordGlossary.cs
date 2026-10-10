using System.Collections.Generic;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Logic
{
    /// <summary>
    /// Reminder text for the keywords and rules words on a card, shown in boxes next to the card zoom
    /// (playtest 2026-10-10_155239: "hover a card and its keywords explain what they do").
    /// </summary>
    public static class KeywordGlossary
    {
        private static readonly (Keyword Flag, string Name, string Text)[] Flags =
        {
            (Keyword.Flying, "Flying", "Can only be blocked by creatures with Flying or Reach."),
            (Keyword.Reach, "Reach", "Can block creatures with Flying."),
            (Keyword.Trample, "Trample", "Damage beyond what kills its blockers goes through to the defending player."),
            (Keyword.Lifelink, "Lifelink", "Damage it deals also heals its controller (not above starting life)."),
            (Keyword.Vigilance, "Vigilance", "Attacking doesn't tap it, so it can still block in the opponent's attack round."),
            (Keyword.CantBlock, "Can't block", "This creature can't be declared as a blocker."),
        };

        /// <summary>Words in the rules text, the first match explains it.</summary>
        private static readonly (string Word, string Name, string Text)[] Words =
        {
            ("Arrival", "Arrival", "Triggers when this enters the battlefield."),
            ("Last Breath", "Last Breath", "Triggers when this dies."),
            ("Invest", "Invest", "An optional extra cost paid only with Gold. Pay it when you play the card for the bonus."),
            ("Equip", "Equip", "Pay the cost as your action to attach this to a creature you control. If the creature leaves, this stays on the battlefield."),
            ("fight", "Fight", "Each creature deals damage equal to its Power to the other."),
            ("Sacrifice", "Sacrifice", "Put a creature you control into its graveyard. It counts as dying (Last Breath triggers)."),
            ("Gold", "Gold", "Unspent mana becomes Gold at the end of the round (up to 3). Gold pays for Instants, Sorceries, abilities and Invest."),
            ("Tap:", "Tap", "Tap this creature to use the ability. Tapped creatures can't attack or block."),
        };

        /// <summary>The (name, explanation) pairs for this card, keywords first. Empty for hidden cards.</summary>
        public static List<(string Name, string Text)> For(CardView v)
        {
            var list = new List<(string, string)>();
            if (v == null || v.IsHidden) return list;
            foreach (var f in Flags)
                if ((v.Keywords & f.Flag) != 0) list.Add((f.Name, f.Text));
            string text = v.Text ?? "";
            foreach (var w in Words)
                if (text.IndexOf(w.Word, System.StringComparison.Ordinal) >= 0
                    || w.Word == "fight" && text.Contains("Fight"))
                    list.Add((w.Name, w.Text));
            if (v.Type == CardType.Instant)
                list.Add(("Instant", "Play it whenever you have priority, even in response to your opponent. Gold can pay for it."));
            else if (v.Type == CardType.Sorcery)
                list.Add(("Sorcery", "Play it only as your action, with nothing on the Chain. Gold can pay for it."));
            return list;
        }
    }
}
