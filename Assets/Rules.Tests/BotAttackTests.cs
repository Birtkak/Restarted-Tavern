using System.Collections.Generic;
using NUnit.Framework;
using RestartedTavern.Rules.AI;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>GreedyBot plans its attack as a whole: alpha strikes, holding back for the crack-back.</summary>
    public class BotAttackTests
    {
        /// <summary>Let the bot declare attackers until it's done; returns the attackers it chose.</summary>
        private static List<ObjectId> BotAttacks(TestGame g)
        {
            g.GoToCombat();
            var bot = new GreedyBot(g.Engine);
            var chosen = new List<ObjectId>();
            while (g.State.Pending?.Kind == DecisionKind.DeclareAttackers)
            {
                var a = bot.Choose(g.State, g.Active);
                g.Do(a);
                if (a.Kind == ActionKind.DeclareAttacker) chosen.Add(a.Card);
            }
            return chosen;
        }

        [Test]
        public void AlphaStrike_WhenTheBlockersCantStopLethal()
        {
            var g = TestGame.AtFirstMainPhase();
            for (int i = 0; i < 3; i++) g.AddToBattlefield(g.Active, "razorhide_boar"); // 3/3 each
            g.AddToBattlefield(g.Other, "tavern_bouncer");                                // 2/5 eats one Boar
            g.P(g.Other).Life = 5;

            Assert.AreEqual(3, BotAttacks(g).Count, "one block stops 3 of 9 damage: 6 is still lethal");
        }

        [Test]
        public void AlphaStrike_BreaksABoardStall()
        {
            var g = TestGame.AtFirstMainPhase();
            for (int i = 0; i < 4; i++) g.AddToBattlefield(g.Active, "ironbark_grizzly");  // 4/5 each
            for (int i = 0; i < 3; i++) g.AddToBattlefield(g.Other, "tusked_mammoth");     // 5/5 each
            g.P(g.Active).Life = 4;
            g.P(g.Other).Life = 4;

            Assert.AreEqual(4, BotAttacks(g).Count, "three blockers, four attackers: 4 damage gets through");
        }

        [Test]
        public void HoldsBack_WhenTheCrackBackWouldKill()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "pit_fighter");  // 4/4 Trample: no single blocker punishes it
            g.AddToBattlefield(g.Other, "razorhide_boar"); // 3/3
            g.P(g.Active).Life = 3;

            Assert.AreEqual(0, BotAttacks(g).Count, "attacking leaves nothing to block the Boar's 3 damage next turn");
        }

        [Test]
        public void StillAttacks_WhenItsSafe()
        {
            var g = TestGame.AtFirstMainPhase();
            g.AddToBattlefield(g.Active, "pit_fighter");   // 4/4 Trample: the Boar can't punish it
            g.AddToBattlefield(g.Other, "razorhide_boar"); // 3/3: its crack-back (3 of 30 life) is fine

            var attackers = BotAttacks(g);
            Assert.AreEqual(1, attackers.Count);
            Assert.AreEqual("pit_fighter", g.State.FindOnBattlefield(attackers[0]).DefinitionId);
        }
    }
}
