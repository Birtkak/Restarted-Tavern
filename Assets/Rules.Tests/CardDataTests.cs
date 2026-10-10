using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.Cards;
using RestartedTavern.Rules.Data;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>The card and deck data files (Assets/StreamingAssets): they load, round-trip exactly, and agree with each other.</summary>
    public class CardDataTests
    {
        private static string CardsDir => Path.Combine(CardPool.DataRoot, "Cards");

        [Test]
        public void EveryCardFile_LoadsAndWritesBackUnchanged()
        {
            var files = Directory.GetFiles(CardsDir, "*.json");
            Assert.GreaterOrEqual(files.Length, 7);
            foreach (var file in files)
            {
                string text = File.ReadAllText(file).Replace("\r\n", "\n");
                var cards = CardJson.ReadCards(text, Path.GetFileName(file));
                Assert.AreEqual(text, CardJson.WriteCards(cards),
                    Path.GetFileName(file) + " isn't in the canonical form: a field the engine doesn't know, a default written out, or hand formatting");
            }
        }

        /// <summary>
        /// The "may" rule (Decision Log 2026-10-10): a card lets you choose only if its text says "may" or "up to"; anything
        /// else is forced. So every optional flag or "you may" effect in the data has the word in the text, and text that
        /// offers a choice ("may fight", "up to one target") has the flag. "Loses up to 2 Gold" is a cap, not a choice; "one, two or three targets" and "sacrifice
        /// a creature or lose 3 life" are choices too.
        /// </summary>
        [Test]
        public void MayInTheText_MatchesTheOptionalFlags()
        {
            var optional = new System.Text.RegularExpressions.Regex(
                "\"(optional|targetOptional)\": true|YouMayEffect|DiscardChoiceEffect|EachOpponentMayPayGoldEffect|ChooseUpToX");
            var saysChoice = new System.Text.RegularExpressions.Regex(@"\bmay\b|\bup to\b|\bone, two or three\b|\bor lose \d+ life\b",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var offersChoice = new System.Text.RegularExpressions.Regex(
                @"\bmay (fight|attach|sacrifice|discard|lose|return)\b|\bup to (one|two|three|X) (target|other)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var problems = new System.Collections.Generic.List<string>();
            foreach (var file in Directory.GetFiles(CardsDir, "*.json"))
                foreach (var card in CardJson.ReadCards(File.ReadAllText(file), Path.GetFileName(file)))
                {
                    string data = CardJson.WriteCards(new[] { card });
                    bool flagged = optional.IsMatch(data);
                    if (flagged && !saysChoice.IsMatch(card.Text)) problems.Add(card.Id + ": optional in the data, but the text has no \"may\" / \"up to\"");
                    if (offersChoice.IsMatch(card.Text) && !flagged) problems.Add(card.Id + ": the text offers a choice, but the data forces it");
                }
            Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
        }

        [Test]
        public void CardIdsAreUnique_AndEveryReferencedCardExists()
        {
            var db = CardPool.CreateDatabase(); // throws on duplicate ids
            Assert.Greater(db.All.Count(), 200);
            foreach (var card in db.All)
            {
                foreach (var e in card.SpellEffects.Concat(card.InvestEffects).Concat(card.Triggers.SelectMany(t => t.Effects))
                             .Concat(card.Abilities.SelectMany(a => a.Effects)))
                    if (e is CreateTokensEffect t) Assert.IsTrue(db.Contains(t.TokenId), card.Id + " makes an unknown token " + t.TokenId);
            }
            foreach (var deck in CardPool.PrototypeDecks())
            {
                Assert.IsTrue(db.Contains(deck.TavernDweller), deck.Id);
                foreach (var id in deck.Cards) Assert.IsTrue(db.Contains(id), deck.Id + ": unknown card " + id);
                Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), deck.Cards, deck.TavernDweller), deck.Id);
            }
        }

        [Test]
        public void ReadingFailsLoudly_OnTyposAndUnknownBuildingBlocks()
        {
            Assert.Throws<FormatException>(() => CardJson.ReadCards("[{\"id\": \"x\", \"powr\": 2}]"), "unknown property");
            Assert.Throws<FormatException>(() => CardJson.ReadCards("[{\"id\": \"x\", \"keywords\": \"Flyng\"}]"), "unknown keyword");
            Assert.Throws<FormatException>(() => CardJson.ReadCards(
                "[{\"id\": \"x\", \"spellEffects\": [{\"$type\": \"DealDamgeEffect\", \"amount\": 2}]}]"), "unknown building block");
            Assert.Throws<FormatException>(() => CardJson.ReadCards(
                "[{\"id\": \"x\", \"spellEffects\": [{\"amount\": 2}]}]"), "an effect needs $type");
            Assert.Throws<FormatException>(() => CardJson.ReadCards("[{\"id\": \"x\", \"cost\": \"two\"}]"), "wrong value type");
        }

        [Test]
        public void ABuildingBlockRoundTrips_WithNestedEffectsAndFlags()
        {
            var card = new CardDefinition
            {
                Id = "test_card", Name = "Test", Type = CardType.Creature, Cost = 3, Power = 2, Health = 2,
                Keywords = Keyword.Flying | Keyword.Trample, Subtypes = new[] { "Goober" },
                Statics = { new ReplacementAbility { Event = ReplacementEvent.Dies, OnlySelf = true, Destination = Zone.Exile } },
                Triggers =
                {
                    new TriggeredAbility
                    {
                        When = TriggerEvent.EndOfYourTurn,
                        Condition = new TriggerCondition { Kind = ConditionKind.YouHaveGoldAtLeast, Amount = 3 },
                        Effects = { new YouMayEffect { Prompt = "Pay?", Then = { new DrawCardsEffect { Count = 2 } } } },
                    },
                },
            };
            string json = CardJson.WriteCards(new[] { card });
            var back = CardJson.ReadCards(json).Single();
            Assert.AreEqual(json, CardJson.WriteCards(new[] { back }));
            Assert.AreEqual(Keyword.Flying | Keyword.Trample, back.Keywords);
            Assert.AreEqual(Zone.Exile, ((ReplacementAbility)back.Statics[0]).Destination);
            var may = (YouMayEffect)back.Triggers[0].Effects[0];
            Assert.AreEqual(2, ((DrawCardsEffect)may.Then[0]).Count);
            Assert.AreEqual(3, back.Triggers[0].Condition.Amount);
        }
    }
}
