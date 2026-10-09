using NUnit.Framework;

namespace RestartedTavern.Rules.Tests
{
    /// <summary>
    /// GAME_DESIGN §7.3 (decided 2026-10-09): when a Health buff ends, a wounded creature keeps
    /// at least 1 Health. Its damage is capped instead (a deviation from MTG).
    /// </summary>
    public class BuffEndingTests
    {
        private static CardDefinition Shield => new CardDefinition
        {
            Id = "test_shield", Name = "Test Shield", Type = CardType.Instant, Cost = 1,
            SpellTarget = TargetSpec.Creature,
            SpellEffects = { new PumpTargetEffect { Health = 2 } },
        };

        private static CardDefinition Captain
        {
            get
            {
                var c = new CardDefinition
                {
                    Id = "test_captain", Name = "Test Captain", Type = CardType.Creature, Cost = 1, Power = 1, Health = 1,
                };
                c.Statics.Add(new AnthemAbility { OthersOnly = true, Health = 2 });
                return c;
            }
        }

        [Test]
        public void UntilEndOfTurnBuffEnding_DoesNotKill()
        {
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { Shield });
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword", damage: 2); // 2/3, 1 left
            var shield = g.AddToHand(g.Active, "test_shield");
            var snot = g.AddToHand(g.Active, "spark_snot");
            g.SetMana(g.Active, 2);

            g.Do(PlayerAction.Play(g.Active, shield.Id, Target.ForObject(sword.Id)));
            g.PassRound();
            g.Do(PlayerAction.Play(g.Active, snot.Id, Target.ForObject(sword.Id)));
            g.PassRound();
            Assert.AreEqual(4, sword.Damage);
            Assert.AreEqual(1, g.Stats(sword).RemainingHealth, "2/5 with 4 damage");

            g.PassToStep(Step.Main1, g.Other);
            Assert.IsNotNull(g.State.FindOnBattlefield(sword.Id), "survives the buff ending");
            Assert.AreEqual(2, sword.Damage, "damage capped at max Health − 1");
            Assert.AreEqual(1, g.Stats(sword).RemainingHealth);
        }

        [Test]
        public void LordLeaving_DoesNotKill()
        {
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { Captain });
            var captain = g.AddToBattlefield(g.Active, "test_captain");
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword", damage: 4); // 2/5 under the captain, 1 left
            Assert.AreEqual(1, g.Stats(sword).RemainingHealth);

            var snot = g.AddToHand(g.Active, "spark_snot");
            g.Do(PlayerAction.Play(g.Active, snot.Id, Target.ForObject(captain.Id)));
            g.PassRound();

            Assert.IsNull(g.State.FindOnBattlefield(captain.Id));
            Assert.IsNotNull(g.State.FindOnBattlefield(sword.Id));
            Assert.AreEqual(2, sword.Damage);
        }

        [Test]
        public void RealDamage_StillKills()
        {
            var g = TestGame.AtFirstMainPhase(extraCards: new[] { Shield });
            var sword = g.AddToBattlefield(g.Active, "hired_sellsword", damage: 2);
            var shield = g.AddToHand(g.Active, "test_shield");
            var snot1 = g.AddToHand(g.Active, "spark_snot");
            var snot2 = g.AddToHand(g.Active, "spark_snot");
            g.SetMana(g.Active, 3);

            g.Do(PlayerAction.Play(g.Active, shield.Id, Target.ForObject(sword.Id)));
            g.PassRound();
            g.Do(PlayerAction.Play(g.Active, snot1.Id, Target.ForObject(sword.Id)));
            g.PassRound();
            g.Do(PlayerAction.Play(g.Active, snot2.Id, Target.ForObject(sword.Id)));
            g.PassRound();
            Assert.IsNull(g.State.FindOnBattlefield(sword.Id), "6 damage on a 2/5 is lethal");
        }
    }
}
