using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// Set v0.2 additions (docs/cards/*.md, approved 2026-10-09), added batch by batch as the engine
    /// learns their mechanics. Batch A: the Gold economy (bank, spend-Gold, the per-player Gold cap,
    /// "pay any amount of Gold") plus the v0.2 cards that only needed existing mechanics.
    /// </summary>
    public static partial class PrototypeCards
    {
        public const string CritterToken = "critter_token";

        private static IEnumerable<CardDefinition> V02Cards()
        {
            // ---------------------------------------------------------------- tokens
            yield return new CardDefinition
            {
                Id = CritterToken, Name = "Critter", Type = CardType.Creature, Power = 1, Health = 1,
                Subtypes = new[] { "Critter" }, Faction = "evergrowing_wild", IsToken = true,
            };

            // ---------------------------------------------------------------- Neutral
            var cellarRat = Creature("cellar_rat", "Cellar Rat", 1, 1, 2, "neutral", Rarity.Common, "Rat", Keyword.None,
                "Last Breath: Gain 1 Gold.");
            cellarRat.Triggers.Add(new TriggeredAbility { When = TriggerEvent.LastBreath, Effects = { new GainGoldEffect { Amount = 1 } } });
            yield return cellarRat;

            yield return new CardDefinition
            {
                Id = "spilled_drink", Name = "Spilled Drink", Type = CardType.Instant, Cost = 1, Faction = "neutral",
                Rarity = Rarity.Common, Text = "Tap target creature. Invest 1: Draw a card.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new TapTargetEffect() },
                InvestCost = 1,
                InvestEffects = { new DrawCardsEffect { Count = 1 } },
            };

            var medic = Creature("field_medic", "Field Medic", 2, 1, 3, "neutral", Rarity.Common, "Human", Keyword.None,
                "Arrival: Heal 2 from target creature.");
            medic.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.Creature, Effects = { new HealEffect { Amount = 2 } },
            });
            yield return medic;

            yield return new CardDefinition
            {
                Id = "house_special", Name = "House Special", Type = CardType.Sorcery, Cost = 2, Faction = "neutral",
                Rarity = Rarity.Common, Text = "Draw a card. Invest 3: Draw two more cards.",
                SpellEffects = { new DrawCardsEffect { Count = 1 } },
                InvestCost = 3,
                InvestEffects = { new DrawCardsEffect { Count = 2 } },
            };

            var recruiter = Creature("tavern_recruiter", "Tavern Recruiter", 3, 2, 3, "neutral", Rarity.Common, "Human",
                Keyword.None, "Invest 2: Create a 2/2 Mercenary.");
            recruiter.InvestCost = 2;
            recruiter.InvestEffects.Add(new CreateTokensEffect { TokenId = MercenaryToken });
            yield return recruiter;

            var doorman = Creature("doorman", "Doorman", 4, 3, 5, "neutral", Rarity.Common, "Human", Keyword.None,
                "Arrival: Gain 1 Gold.");
            doorman.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new GainGoldEffect { Amount = 1 } } });
            yield return doorman;

            yield return Creature("caravan_guard", "Caravan Guard", 5, 4, 6, "neutral", Rarity.Common, "Human", Keyword.Reach,
                "Reach.");

            var bard = Creature("traveling_bard", "Traveling Bard", 2, 2, 2, "neutral", Rarity.Uncommon, "Human", Keyword.None,
                "Whenever you spend 3 or more Gold on a single spell or ability, draw a card.");
            bard.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.GoldSpent, Subject = TriggerSubject.You, MinAmount = 3,
                Effects = { new DrawCardsEffect { Count = 1 } },
            });
            yield return bard;

            yield return new CardDefinition
            {
                Id = "settle_the_tab", Name = "Settle the Tab", Type = CardType.Sorcery, Cost = 3, Faction = "neutral",
                Rarity = Rarity.Uncommon,
                Text = "As an extra cost, pay any amount of Gold (X). Draw X cards, then discard a card.",
                XGoldExtraCost = true,
                SpellEffects = { new DrawCardsEffect { CountIsX = true }, new DiscardCardsEffect { Count = 1 } },
            };

            var innkeeper = Creature("grizzled_innkeeper", "Grizzled Innkeeper", 4, 3, 5, "neutral", Rarity.Rare, "Human",
                Keyword.None, "Whenever you bank Gold, heal that much from target creature you control.");
            innkeeper.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.GoldBanked, Subject = TriggerSubject.You, Target = TargetSpec.CreatureYouControl,
                Effects = { new HealEffect { AmountFromEvent = true } },
            });
            yield return innkeeper;

            // ---------------------------------------------------------------- Shadow Money Wizards
            var juggler = Creature("coin_juggler", "Coin Juggler", 2, 1, 3, "shadow_money_wizards", Rarity.Common, "Wizard",
                Keyword.None, "Whenever you spend Gold, this gets +1/+0 until end of turn.");
            juggler.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.GoldSpent, Subject = TriggerSubject.You, Effects = { new PumpSourceEffect { Power = 1 } },
            });
            yield return juggler;

            var broker = Creature("interest_broker", "Interest Broker", 2, 1, 3, "shadow_money_wizards", Rarity.Uncommon,
                "Wizard", Keyword.None, "Whenever you bank 2 or more Gold, draw a card.");
            broker.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.GoldBanked, Subject = TriggerSubject.You, MinAmount = 2,
                Effects = { new DrawCardsEffect { Count = 1 } },
            });
            yield return broker;

            var offshore = new CardDefinition
            {
                Id = "offshore_account", Name = "Offshore Account", Type = CardType.Relic, Cost = 2,
                Faction = "shadow_money_wizards", Rarity = Rarity.Uncommon, Text = "Your Gold cap is 8.",
            };
            offshore.Statics.Add(new GoldCapAbility { Cap = 8 });
            yield return offshore;

            var feeCollector = Creature("fee_collector", "Fee Collector", 2, 2, 2, "shadow_money_wizards", Rarity.Common,
                "Wizard", Keyword.None, "Whenever an opponent spends Gold, you gain 1 Gold.");
            feeCollector.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.GoldSpent, Subject = TriggerSubject.Opponents, Effects = { new GainGoldEffect { Amount = 1 } },
            });
            yield return feeCollector;

            yield return new CardDefinition
            {
                Id = "compound_interest", Name = "Compound Interest", Type = CardType.Sorcery, Cost = 4,
                Faction = "shadow_money_wizards", Rarity = Rarity.Common,
                Text = "Draw two cards. If you have 5 or more Gold, draw three instead.",
                SpellEffects = { new DrawIfGoldEffect { Count = 2, GoldAtLeast = 5, CountIfGold = 3 } },
            };

            // ---------------------------------------------------------------- Goobers
            var splitter = Creature("loot_splitter", "Loot Splitter", 2, 2, 2, "goobers", Rarity.Common, "Goober", Keyword.None,
                "Invest 2: Create a 1/1 Goober.");
            splitter.InvestCost = 2;
            splitter.InvestEffects.Add(new CreateTokensEffect { TokenId = GooberToken });
            yield return splitter;

            var sapper = Creature("goober_sapper", "Goober Sapper", 3, 2, 2, "goobers", Rarity.Common, "Goober", Keyword.Haste,
                "Haste. Last Breath: Deal 1 damage to each enemy creature.");
            sapper.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.LastBreath, Effects = { new DealDamageToEachEnemyCreatureEffect { Amount = 1 } },
            });
            yield return sapper;

            // ---------------------------------------------------------------- Evergrowing Wild
            yield return new CardDefinition
            {
                Id = "bark_skin", Name = "Bark Skin", Type = CardType.Instant, Cost = 1, Faction = "evergrowing_wild",
                Rarity = Rarity.Common, Text = "Target creature gets +0/+3 until end of turn. Put a +1/+1 counter on it.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new PumpTargetEffect { Health = 3 }, new AddCountersEffect { Count = 1 } },
            };
            yield return new CardDefinition
            {
                Id = "critter_burrow", Name = "Critter Burrow", Type = CardType.Sorcery, Cost = 3, Faction = "evergrowing_wild",
                Rarity = Rarity.Common, Text = "Create three 1/1 Critters.",
                SpellEffects = { new CreateTokensEffect { TokenId = CritterToken, Count = 3 } },
            };
            yield return new CardDefinition
            {
                Id = "regrowth_rain", Name = "Regrowth Rain", Type = CardType.Instant, Cost = 3, Faction = "evergrowing_wild",
                Rarity = Rarity.Common, Text = "Heal 3 from each creature you control.",
                SpellEffects = { new HealOtherCreaturesYouControlEffect { Amount = 3 } }, // the spell itself isn't a creature
            };

            var hippo = Creature("mudwallow_hippo", "Mudwallow Hippo", 5, 3, 7, "evergrowing_wild", Rarity.Common, "Beast",
                Keyword.None, "At the end of your turn, heal 2 from this.");
            hippo.Triggers.Add(new TriggeredAbility { When = TriggerEvent.EndOfYourTurn, Effects = { new HealSelfEffect { Amount = 2 } } });
            yield return hippo;

            yield return new CardDefinition
            {
                Id = "stampede_of_the_deep", Name = "Stampede of the Deep", Type = CardType.Sorcery, Cost = 6,
                Faction = "evergrowing_wild", Rarity = Rarity.Rare,
                Text = "Creatures you control get +2/+2 and Trample until end of turn. Heal them fully.",
                SpellEffects =
                {
                    new PumpYourCreaturesEffect { Power = 2, Health = 2, Grants = Keyword.Trample },
                    new HealOtherCreaturesYouControlEffect { Fully = true },
                },
            };

            var colossus = Creature("titanback_colossus", "Titanback Colossus", 8, 8, 8, "evergrowing_wild", Rarity.Rare, "Beast",
                Keyword.Trample, "Trample. Whenever this attacks, heal it fully.");
            colossus.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Attacks, Effects = { new HealSelfEffect { Fully = true } } });
            yield return colossus;

            // ---------------------------------------------------------------- Sensationalists
            var usher = Creature("crypt_usher", "Crypt Usher", 2, 2, 2, "sensationalists", Rarity.Common, "Human", Keyword.None,
                "Last Breath: Create a 1/1 Spirit with Flying.");
            usher.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.LastBreath, Effects = { new CreateTokensEffect { TokenId = SpiritToken } },
            });
            yield return usher;
        }
    }
}
