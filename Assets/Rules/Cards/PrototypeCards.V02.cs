using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// Set v0.2 additions (docs/cards/*.md, approved 2026-10-09), added batch by batch as the engine
    /// learns their mechanics. Batch A: the Gold economy (bank, spend-Gold, the per-player Gold cap,
    /// "pay any amount of Gold") plus the v0.2 cards that only needed existing mechanics.
    /// Batch B: damage and healing ("damaged", Health remaining, can't be healed, damage prevention,
    /// damage to each creature, damage/heal/arrival watchers, extra costs on spells).
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

            foreach (var c in V02DamageCards()) yield return c;
        }

        /// <summary>Batch B: damage and healing.</summary>
        private static IEnumerable<CardDefinition> V02DamageCards()
        {
            // ---------------------------------------------------------------- Neutral
            yield return new CardDefinition
            {
                Id = "called_shot", Name = "Called Shot", Type = CardType.Instant, Cost = 2, Faction = "neutral",
                Rarity = Rarity.Common, Text = "Deal 2 damage to target attacking or blocking creature.",
                SpellTargets = { new TargetSlot { Spec = TargetSpec.Creature, AttackingOrBlocking = true } },
                SpellEffects = { new DealDamageEffect { Amount = 2 } },
            };
            yield return new CardDefinition
            {
                Id = "bar_brawl", Name = "Bar Brawl", Type = CardType.Sorcery, Cost = 3, Faction = "neutral",
                Rarity = Rarity.Uncommon, Text = "Deal 1 damage to each creature.",
                SpellEffects = { new DealDamageToEachCreatureEffect { Amount = 1 } },
            };

            var veteran = Creature("scarred_veteran", "Scarred Veteran", 3, 2, 4, "neutral", Rarity.Uncommon, "Human",
                Keyword.None, "This gets +1/+0 for each damage on it.");
            veteran.Statics.Add(new PowerPerDamageAbility());
            yield return veteran;

            yield return new CardDefinition
            {
                Id = "tavern_brawl_night", Name = "Tavern Brawl Night", Type = CardType.Sorcery, Cost = 5, Faction = "neutral",
                Rarity = Rarity.Rare, Text = "Deal 2 damage to each creature. Then heal 2 from each creature you control.",
                // State-based actions wait until the spell is done, so your creatures at 2 Health survive.
                SpellEffects =
                {
                    new DealDamageToEachCreatureEffect { Amount = 2 },
                    new HealOtherCreaturesYouControlEffect { Amount = 2 },
                },
            };

            yield return Equipment("champions_belt", "Champion's Belt", 3, "neutral", Rarity.Rare, 3,
                new AttachedCreatureModifier
                {
                    Power = 2, Health = 2,
                    Triggers =
                    {
                        new TriggeredAbility
                        {
                            When = TriggerEvent.DestroysCreatureInCombat, Effects = { new HealSelfEffect { Fully = true } },
                            Text = "Whenever this destroys a creature in combat, heal it fully.",
                        },
                    },
                },
                "Equipped creature gets +2/+2. Whenever equipped creature destroys a creature in combat, heal it fully. Equip 3.");

            // ---------------------------------------------------------------- Goobers
            yield return new CardDefinition
            {
                Id = "kick_em_while_theyre_down", Name = "Kick 'Em While They're Down", Type = CardType.Sorcery, Cost = 2,
                Faction = "goobers", Rarity = Rarity.Common,
                Text = "Deal 2 damage to target creature. If it was already damaged, deal 4 damage instead.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new DealDamageEffect { Amount = 2, AmountIfDamaged = 4 } },
            };
            yield return new CardDefinition
            {
                Id = "fling_the_runt", Name = "Fling the Runt", Type = CardType.Instant, Cost = 1, Faction = "goobers",
                Rarity = Rarity.Uncommon,
                Text = "As an extra cost, sacrifice a creature. Deal damage equal to its Power to any target.",
                SacrificeCreatureCost = true,
                SpellTarget = TargetSpec.AnyTarget,
                SpellEffects = { new DealDamageEffect { AmountIsSacrificedPower = true } },
            };

            var chaos = Creature("chaos_engine", "Chaos Engine", 5, 4, 4, "goobers", Rarity.Rare, "Goober", Keyword.Haste,
                "Haste. At the start of your turn, deal 1 damage to each other creature.");
            chaos.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.StartOfYourTurn, Effects = { new DealDamageToEachCreatureEffect { Amount = 1, ExcludeSource = true } },
            });
            yield return chaos;

            // ---------------------------------------------------------------- Glitterworld
            yield return new CardDefinition
            {
                Id = "finisher_protocol", Name = "Finisher Protocol", Type = CardType.Instant, Cost = 2, Faction = "glitterworld",
                Rarity = Rarity.Common, Text = "Destroy target creature with 2 or less Health remaining.",
                SpellTargets = { new TargetSlot { Spec = TargetSpec.Creature, MaxRemainingHealth = 2 } },
                SpellEffects = { new DestroyEffect() },
            };
            yield return new CardDefinition
            {
                Id = "smart_rounds", Name = "Smart Rounds", Type = CardType.Sorcery, Cost = 3, Faction = "glitterworld",
                Rarity = Rarity.Uncommon,
                Text = "Deal 1 damage to target creature. Then deal 1 damage to each other creature that already had damage.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new DamageTargetThenEachDamagedEffect { Amount = 1 } },
            };
            yield return Equipment("hardlight_aegis", "Hardlight Aegis", 4, "glitterworld", Rarity.Rare, 2,
                new AttachedCreatureModifier { Health = 3, MaxDamageEachTurn = 2 },
                "Equipped creature gets +0/+3 and can't be dealt more than 2 damage each turn. Equip 2.");

            var executioner = Creature("neon_executioner", "Neon Executioner", 6, 4, 6, "glitterworld", Rarity.Rare, "Construct",
                Keyword.None, "Whenever an enemy creature is dealt damage, if it has 2 or less Health remaining, destroy it.");
            executioner.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDealtDamage, Subject = TriggerSubject.Opponents, MaxRemainingHealth = 2,
                Effects = { new DestroyEventCreatureEffect { MaxRemainingHealth = 2 } },
            });
            yield return executioner;

            // ---------------------------------------------------------------- Evergrowing Wild
            yield return new CardDefinition
            {
                Id = "overflowing_spring", Name = "Overflowing Spring", Type = CardType.Instant, Cost = 2,
                Faction = "evergrowing_wild", Rarity = Rarity.Uncommon,
                Text = "Heal 4 from target creature. If it had no damage, put two +1/+1 counters on it instead.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new HealOrCountersEffect { Amount = 4, CountersIfUndamaged = 2 } },
            };

            var mender = Creature("sap_mender", "Sap Mender", 2, 1, 3, "evergrowing_wild", Rarity.Uncommon, "Plant", Keyword.None,
                "Whenever you heal a creature, put a +1/+1 counter on it.");
            mender.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureHealed, Subject = TriggerSubject.You, Effects = { new AddCountersToEventCreatureEffect() },
            });
            yield return mender;

            var predator = Creature("ambush_predator", "Ambush Predator", 3, 3, 2, "evergrowing_wild", Rarity.Common, "Cat",
                Keyword.None, "Arrival: This fights up to one target damaged creature you don't control.");
            predator.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.CreatureYouDontControl, TargetDamaged = true, TargetOptional = true,
                Effects = { new FightEffect { SourceFights = true } },
            });
            yield return predator;

            var matriarch = Creature("herd_matriarch", "Herd Matriarch", 4, 3, 5, "evergrowing_wild", Rarity.Uncommon, "Beast",
                Keyword.None,
                "Whenever another creature with 5 or more Power enters the battlefield under your control, put two +1/+1 counters on it.");
            matriarch.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureEnters, Subject = TriggerSubject.You, OthersOnly = true, MinPower = 5,
                Effects = { new AddCountersToEventCreatureEffect { Count = 2 } },
            });
            yield return matriarch;

            // ---------------------------------------------------------------- Sensationalists
            var festering = new CardDefinition
            {
                Id = "hex_of_festering", Name = "Hex of Festering", Type = CardType.Curse, Cost = 2, Faction = "sensationalists",
                Rarity = Rarity.Common,
                Text = "Attach to an enemy creature. It can't be healed. Whenever it's dealt damage, its controller loses 1 life.",
                SpellTarget = TargetSpec.CreatureYouDontControl,
            };
            festering.Statics.Add(new CantBeHealedAbility());
            festering.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDealtDamage, OnlyAttachedCreature = true, Effects = { new EventPlayerLosesLifeEffect() },
            });
            yield return festering;

            yield return new CardDefinition
            {
                Id = "blood_price", Name = "Blood Price", Type = CardType.Instant, Cost = 2, Faction = "sensationalists",
                Rarity = Rarity.Common, Text = "As an extra cost, pay 3 life. Destroy target damaged creature.",
                ExtraLifeCost = 3,
                SpellTargets = { new TargetSlot { Spec = TargetSpec.Creature, Damaged = true } },
                SpellEffects = { new DestroyEffect() },
            };

            var hollow = new CardDefinition
            {
                Id = "hex_of_hollow_bones", Name = "Hex of Hollow Bones", Type = CardType.Curse, Cost = 4, Faction = "sensationalists",
                Rarity = Rarity.Uncommon,
                Text = "Attach to an enemy creature. It gets -1/-1 for each creature card in your graveyard (up to -4/-4).",
                SpellTarget = TargetSpec.CreatureYouDontControl,
            };
            hollow.Statics.Add(new AttachedScalingModifier
            {
                PowerPer = -1, HealthPer = -1,
                Count = (s, db, curse) =>
                {
                    int n = 0;
                    foreach (var c in s.GetPlayer(curse.Controller).Graveyard)
                        if (db.Get(c.DefinitionId).IsCreature) n++;
                    return System.Math.Min(4, n);
                },
            });
            yield return hollow;

            var rot = new CardDefinition
            {
                Id = "curse_of_rot", Name = "Curse of Rot", Type = CardType.Curse, Cost = 5, Faction = "sensationalists",
                Rarity = Rarity.Rare,
                Text = "Attach to an opponent. At the start of that player's turn, deal 1 damage to each creature they control. "
                       + "Creatures they control can't be healed.",
                SpellTarget = TargetSpec.Opponent,
            };
            rot.Statics.Add(new CantBeHealedAbility());
            rot.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.StartOfEnchantedPlayersTurn,
                Effects = { new DealDamageToEachCreatureEffect { Amount = 1, OnlyEventPlayer = true } },
            });
            yield return rot;
        }
    }
}
