using System.Linq;
using NUnit.Framework;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>The Wizards and Sensationalists prototype cards, the two new decks and the Control bot style.</summary>
    public class VesperCardTests
    {
        [Test]
        public void NewDecks_AreLegal()
        {
            var db = CardPool.CreateDatabase();
            Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), CardPool.VespersLedgerDeck(), CardPool.VespersLedgerTavernDweller));
            Assert.DoesNotThrow(() => DeckValidator.Validate(db, FormatConfig.Standard(), CardPool.SparkwrenchScrappersDeck(), CardPool.SparkwrenchScrappersTavernDweller));
        }

        [Test]
        public void RitualSlaughter_DestroysAndCostsLife()
        {
            var g = TestGame.AtFirstMainPhase();
            var big = g.AddToBattlefield(g.Other, "tusked_mammoth");
            var slaughter = g.AddToHand(g.Active, "ritual_slaughter");
            g.SetMana(g.Active, 4);
            g.Do(PlayerAction.Play(g.Active, slaughter.Id, Target.ForObject(big.Id)));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(big.Id));
            Assert.AreEqual(28, g.P(g.Active).Life);
        }

        [Test]
        public void HexOfFrailty_ShrinksTheCreature_AndFallsOffWhenItDies()
        {
            var g = TestGame.AtFirstMainPhase();
            var bouncer = g.AddToBattlefield(g.Other, "tavern_bouncer", damage: 3); // 2/5, 2 left
            var hex = g.AddToHand(g.Active, "hex_of_frailty");
            var hex2 = g.AddToHand(g.Active, "hex_of_frailty");
            g.SetMana(g.Active, 2);

            g.Do(PlayerAction.Play(g.Active, hex.Id, Target.ForObject(bouncer.Id)));
            g.PassRound();
            Assert.AreEqual(1, g.Stats(bouncer).Power);
            Assert.AreEqual(1, g.Stats(bouncer).RemainingHealth, "-1/-1 lowers max Health; the damage stays");

            g.Do(PlayerAction.Play(g.Active, hex2.Id, Target.ForObject(bouncer.Id)));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(bouncer.Id), "a second -1/-1 finishes the wounded creature");
            Assert.AreEqual(2, g.P(g.Active).Graveyard.Count(c => c.DefinitionId == "hex_of_frailty"), "unattached Curses go to the graveyard");
        }

        [Test]
        public void CandleCultist_DrainsOnLastBreath()
        {
            var g = TestGame.AtFirstMainPhase();
            var cultist = g.AddToBattlefield(g.Active, "candle_cultist");
            var snot = g.AddToHand(g.Active, "spark_snot");
            g.P(g.Active).Life = 25;
            g.Do(PlayerAction.Play(g.Active, snot.Id, Target.ForObject(cultist.Id)));
            g.PassRound();
            g.PassRound(); // the Last Breath trigger
            Assert.AreEqual(29, g.P(g.Other).Life);
            Assert.AreEqual(26, g.P(g.Active).Life);
        }

        [Test]
        public void HiredEnforcer_PaysTheOpponent()
        {
            var g = TestGame.AtFirstMainPhase();
            var enforcer = g.AddToHand(g.Active, "hired_enforcer");
            g.SetMana(g.Active, 5);
            g.Do(PlayerAction.Play(g.Active, enforcer.Id));
            g.PassRound();
            g.PassRound();
            Assert.AreEqual(2, g.P(g.Other).Gold);
        }

        [Test]
        public void ControlStyle_KeepsBlockersHome()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "hired_sellsword");
            g.AddToBattlefield(g.Other, "goober_rascal"); // can't block, so attacking is safe
            g.GoToCombat();
            Assert.AreEqual(ActionKind.DeclareAttacker, new GreedyBot(g.Engine, BotStyle.Greedy()).Choose(g.State, g.Active).Kind);
            Assert.AreEqual(ActionKind.FinishAttacks, new GreedyBot(g.Engine, BotStyle.Control()).Choose(g.State, g.Active).Kind,
                "Control keeps its only creature back against an enemy creature");
        }
    }
}
