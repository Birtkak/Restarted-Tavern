using System.IO;
using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Client.Logic.Tests
{
    public class TavernGuideTests
    {
        [Test]
        public void Guide_IdsAreUnique_AndLinksResolve()
        {
            var ids = TavernGuide.Entries.Select(e => e.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "duplicate ids");
            foreach (var e in TavernGuide.Entries)
            {
                Assert.Contains(e.Category, TavernGuide.Categories, e.Id);
                foreach (var r in e.Related) Assert.IsNotNull(TavernGuide.Get(r), e.Id + " links to " + r);
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Text), e.Id);
            }
        }

        [Test]
        public void Guide_ExampleCardsExist()
        {
            var db = CardPool.CreateDatabase();
            foreach (var e in TavernGuide.Entries)
                foreach (var c in e.Cards) Assert.IsTrue(db.Contains(c), e.Id + ": unknown card " + c);
        }

        [Test]
        public void Guide_EveryScreenshotIsInResources()
        {
            foreach (var e in TavernGuide.Entries)
                foreach (var f in e.Frames)
                    Assert.IsTrue(File.Exists("Assets/Resources/Guide/" + f.Image + ".jpg") || File.Exists("Assets/Resources/Guide/" + f.Image + ".png"),
                        e.Id + ": missing screenshot " + f.Image);
        }

        /// <summary>Every keyword and rules word explained on the cards (KeywordGlossary) has a guide page that a search finds first.</summary>
        [Test]
        public void Guide_CoversEveryGlossaryWord()
        {
            var words = new[] { "Flying", "Reach", "Trample", "Lifelink", "Vigilance", "Can't block", "Arrival", "Last Breath", "Invest",
                "Equip", "Fight", "Sacrifice", "Gold", "Tap", "Instant", "Sorcery" };
            foreach (var w in words)
            {
                var hits = TavernGuide.Search(w);
                Assert.IsNotEmpty(hits, w);
                var top = hits[0];
                Assert.IsTrue(top.Title.ToLowerInvariant().Contains(w.ToLowerInvariant().TrimEnd('y'))
                              || top.Aliases.Any(a => a.ToLowerInvariant().Contains(w.ToLowerInvariant())), w + " -> " + top.Title);
            }
        }

        [Test]
        public void Guide_SearchRanksTitlesFirst_AndNeedsEveryWord()
        {
            Assert.AreEqual("flying", TavernGuide.Search("flying")[0].Id);
            Assert.AreEqual("chain", TavernGuide.Search("stack")[0].Id);
            Assert.AreEqual("permanent_damage", TavernGuide.Search("permanent damage")[0].Id);
            Assert.IsEmpty(TavernGuide.Search("flying zzqx"));
            Assert.AreEqual(TavernGuide.Entries.Count, TavernGuide.Search("").Count);
        }

        [Test]
        public void Guide_EveryCardTypeHasAPage()
        {
            foreach (var t in new[] { CardType.Creature, CardType.Instant, CardType.Sorcery, CardType.Equipment, CardType.Relic, CardType.Curse })
                Assert.IsNotEmpty(TavernGuide.Search(t.ToString()), t.ToString());
        }
    }
}
