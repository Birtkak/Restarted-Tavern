using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// Every card in docs/cards (sets v0.1 and v0.2, the Tavern Dwellers and tokens), split over partial files
    /// by set, plus the six prototype decks. Later these move to data files (DEVELOPMENT §3).
    /// </summary>
    public static partial class CardPool
    {
        public const string GooberToken = "goober_token";
        public const string SpiritToken = "spirit_token";

        public static CardDatabase CreateDatabase() => new CardDatabase(All());

        /// <summary>
        /// Legal Standard deck (60, max 4): Goobers + Sensationalists (the Skabba pair). Wide boards, deaths
        /// that feed Skabba, a sacrifice outlet (Fling the Runt) and Gold theft. v0.2 cards added 2026-10-09.
        /// </summary>
        public static List<string> GooberMobDeck() => FourOfEach(
            "goober_rascal", "spark_snot", "fuse_goober", "gob_gang", "brawling_runt", "goober_warchief",
            "hog_rider", "mob_rush", "barrel_bomber", "gold_tooth_bruiser", "overrun_the_gates", "pit_champion",
            "crypt_usher", "pickpocket_boss", "fling_the_runt");

        /// <summary>Legal Standard deck: Evergrowing Wild + big Goobers (the Mukk the Grub King pair). Trample everywhere for Mukk.</summary>
        public static List<string> JungleStampedeDeck() => FourOfEach(
            "jungle_remedy", "vine_spider", "razorhide_boar", "ironbark_grizzly", "thornback_ravager",
            "tusked_mammoth", "hog_rider", "pit_fighter", "pit_champion", "spark_snot",
            "reckless_charge", "kick_em_while_theyre_down", "gold_tooth_bruiser", "titanback_colossus", "brawling_runt");

        /// <summary>Legal Standard deck: Wild + Glitterworld (the Keeper Z-00 pair). Pings, fights and healing, so permanent damage shows up.</summary>
        public static List<string> ZooPatrolDeck() => FourOfEach(
            "static_shock", "spark_drone", "chain_zap", "ambush_predator", "riot_suppressor", "grid_overload",
            "neon_executioner", "orbital_strike_network",
            "finisher_protocol", "mossback_tortoise", "primal_clash", "overflowing_spring", "apex_instinct",
            "sabretooth_prowler", "primeval_behemoth");

        /// <summary>Legal Standard deck: Wizards + Sensationalists (the Madame Vesper pair). Lifelink, drains, removal, flyers: a slower deck.</summary>
        public static List<string> VespersLedgerDeck() => FourOfEach(
            "gilded_rat", "candle_cultist", "hex_of_frailty", "apprentice_forger", "candlelit_acolyte",
            "blood_price", "fatal_rumor", "hungry_shade", "ritual_slaughter", "hired_enforcer",
            "wraith_swarm", "the_grand_ledger", "hush_money", "body_snatcher", "compound_interest");

        /// <summary>
        /// Legal Standard deck: Goobers + Glitterworld (the Sparkwrench pair). Burn and pings, plus cheap
        /// Equipment for Sparkwrench's discount and Power (added 2026-10-09).
        /// </summary>
        public static List<string> SparkwrenchScrappersDeck() => FourOfEach(
            "goober_rascal", "spark_snot", "fuse_goober", "brawling_runt", "goober_warchief", "hog_rider",
            "barrel_bomber", "marksman_scope", "static_shock", "spark_drone", "chain_zap", "scrap_collector",
            "riot_suppressor", "gilded_knuckles", "gold_tooth_bruiser");

        /// <summary>Legal Standard deck: Wizards + Glitterworld (the Auditor Prime pair). The Equipment deck: Equip costs are a Gold sink.</summary>
        public static List<string> AuditorsArsenalDeck() => FourOfEach(
            "neon_shiv", "courier_bot", "marksman_scope", "pulse_blade", "back_street_mechanic", "alley_tinker",
            "scrap_collector", "overclock_rig", "retainer_mage", "arc_welder", "rail_cannon", "hardlight_aegis",
            "megacorp_exosuit", "patrol_captain", "titan_frame_guardian");

        /// <summary>The Tavern Dweller each prototype deck is built around (GAME_DESIGN §9).</summary>
        public const string GooberMobTavernDweller = "skabba";
        public const string JungleStampedeTavernDweller = "mukk_the_grub_king";
        public const string ZooPatrolTavernDweller = "keeper_z00";
        public const string VespersLedgerTavernDweller = "madame_vesper";
        public const string SparkwrenchScrappersTavernDweller = "sparkwrench";
        public const string AuditorsArsenalTavernDweller = "auditor_prime";

        private static List<string> FourOfEach(params string[] ids)
        {
            var deck = new List<string>();
            foreach (var id in ids)
                for (int i = 0; i < 4; i++) deck.Add(id);
            return deck;
        }

        public static IEnumerable<CardDefinition> All()
        {
            // ---------------------------------------------------------------- Goobers
            yield return new CardDefinition
            {
                Id = GooberToken, Name = "Goober", Type = CardType.Creature, Power = 1, Health = 1,
                Subtypes = new[] { "Goober" }, Faction = "goobers", IsToken = true,
            };
            yield return Creature("goober_rascal", "Goober Rascal", 1, 2, 1, "goobers", Rarity.Common, "Goober",
                Keyword.Haste | Keyword.CantBlock, "Haste. Can't block.");
            yield return new CardDefinition
            {
                Id = "spark_snot", Name = "Spark Snot", Type = CardType.Instant, Cost = 1, Faction = "goobers",
                Rarity = Rarity.Common, Text = "Deal 2 damage to a creature.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new DealDamageEffect { Amount = 2 } },
            };
            var fuse = Creature("fuse_goober", "Fuse Goober", 2, 1, 2, "goobers", Rarity.Common, "Goober",
                Keyword.None, "Last Breath: Deal 2 damage to any target.");
            fuse.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.LastBreath, Target = TargetSpec.AnyTarget,
                Effects = { new DealDamageEffect { Amount = 2 } },
            });
            yield return fuse;
            yield return new CardDefinition
            {
                Id = "gob_gang", Name = "Gob Gang", Type = CardType.Sorcery, Cost = 2, Faction = "goobers",
                Rarity = Rarity.Common, Text = "Create two 1/1 Goobers.",
                SpellEffects = { new CreateTokensEffect { TokenId = GooberToken, Count = 2 } },
            };
            yield return Creature("brawling_runt", "Brawling Runt", 2, 2, 2, "goobers", Rarity.Common, "Goober",
                Keyword.Haste, "Haste.");
            var warchief = Creature("goober_warchief", "Goober Warchief", 3, 2, 3, "goobers", Rarity.Uncommon, "Goober",
                Keyword.None, "Your other Goobers get +1/+0.");
            warchief.Statics.Add(new AnthemAbility { Subtype = "Goober", OthersOnly = true, Power = 1 });
            yield return warchief;
            yield return Creature("hog_rider", "Hog-Rider", 3, 3, 3, "goobers", Rarity.Common, "Goober",
                Keyword.Trample, "Trample.");
            yield return new CardDefinition
            {
                Id = "mob_rush", Name = "Mob Rush", Type = CardType.Sorcery, Cost = 4, Faction = "goobers",
                Rarity = Rarity.Uncommon,
                Text = "Create three 1/1 Goobers with Haste. Your creatures get +1/+0 until end of turn.",
                SpellEffects =
                {
                    new CreateTokensEffect { TokenId = GooberToken, Count = 3, GrantUntilEndOfTurn = Keyword.Haste },
                    new PumpYourCreaturesEffect { Power = 1 },
                },
            };
            var bomber = Creature("barrel_bomber", "Barrel Bomber", 4, 3, 3, "goobers", Rarity.Common, "Goober",
                Keyword.None, "Arrival: Deal 2 damage to any target.");
            bomber.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.AnyTarget,
                Effects = { new DealDamageEffect { Amount = 2 } },
            });
            yield return bomber;
            yield return Creature("pit_fighter", "Pit-Fighter", 4, 4, 4, "goobers", Rarity.Common, "Goober",
                Keyword.Trample, "Trample.");
            yield return new CardDefinition
            {
                Id = "overrun_the_gates", Name = "Overrun the Gates", Type = CardType.Sorcery, Cost = 5,
                Faction = "goobers", Rarity = Rarity.Uncommon,
                Text = "Your creatures get +2/+0 and Trample until end of turn.",
                SpellEffects = { new PumpYourCreaturesEffect { Power = 2, Grants = Keyword.Trample } },
            };
            yield return Creature("pit_champion", "Pit Champion", 6, 6, 5, "goobers", Rarity.Rare, "Goober",
                Keyword.Haste | Keyword.Trample, "Haste. Trample.");

            // ---------------------------------------------------------------- Evergrowing Wild
            yield return new CardDefinition
            {
                Id = "jungle_remedy", Name = "Jungle Remedy", Type = CardType.Instant, Cost = 1, Faction = "evergrowing_wild",
                Rarity = Rarity.Common, Text = "Heal 4 from a creature. Invest 1: Put a +1/+1 counter on it.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new HealEffect { Amount = 4 } },
                InvestCost = 1,
                InvestEffects = { new AddCountersEffect { Count = 1 } },
            };
            yield return Creature("vine_spider", "Vine Spider", 2, 2, 3, "evergrowing_wild", Rarity.Common, "Spider",
                Keyword.Reach, "Reach.");
            yield return Creature("razorhide_boar", "Razorhide Boar", 3, 3, 3, "evergrowing_wild", Rarity.Common, "Beast",
                Keyword.Trample, "Trample.");
            yield return Creature("ironbark_grizzly", "Ironbark Grizzly", 4, 4, 5, "evergrowing_wild", Rarity.Common, "Beast",
                Keyword.None, "");
            var ravager = Creature("thornback_ravager", "Thornback Ravager", 5, 4, 4, "evergrowing_wild", Rarity.Uncommon,
                "Beast", Keyword.Trample, "Trample. Arrival: Draw a card.");
            ravager.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new DrawCardsEffect { Count = 1 } } });
            yield return ravager;
            yield return Creature("tusked_mammoth", "Tusked Mammoth", 5, 5, 5, "evergrowing_wild", Rarity.Common, "Beast",
                Keyword.Trample, "Trample.");

            var critter = Creature("canopy_critter", "Canopy Critter", 1, 1, 1, "evergrowing_wild", Rarity.Common, "Critter",
                Keyword.None, "Arrival: Heal 2 from another creature.");
            critter.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.Creature, TargetNotSelf = true,
                Effects = { new HealEffect { Amount = 2 } },
            });
            yield return critter;
            var tortoise = Creature("mossback_tortoise", "Mossback Tortoise", 2, 1, 4, "evergrowing_wild", Rarity.Common, "Turtle",
                Keyword.None, "At the start of your turn, heal 1 from this.");
            tortoise.Triggers.Add(new TriggeredAbility { When = TriggerEvent.StartOfYourTurn, Effects = { new HealSelfEffect { Amount = 1 } } });
            yield return tortoise;
            yield return new CardDefinition
            {
                Id = "primal_clash", Name = "Primal Clash", Type = CardType.Sorcery, Cost = 2, Faction = "evergrowing_wild",
                Rarity = Rarity.Common, Text = "Target creature you control fights target creature you don't control.",
                SpellTargets = { TargetSlot.Of(TargetSpec.CreatureYouControl), TargetSlot.Of(TargetSpec.CreatureYouDontControl) },
                SpellEffects = { new FightEffect() },
            };
            yield return new CardDefinition
            {
                Id = "growth_spurt", Name = "Growth Spurt", Type = CardType.Instant, Cost = 2, Faction = "evergrowing_wild",
                Rarity = Rarity.Uncommon, Text = "Put two +1/+1 counters on target creature. Heal 2 from it.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new AddCountersEffect { Count = 2 }, new HealEffect { Amount = 2 } },
            };
            yield return new CardDefinition
            {
                Id = "apex_instinct", Name = "Apex Instinct", Type = CardType.Instant, Cost = 3, Faction = "evergrowing_wild",
                Rarity = Rarity.Uncommon,
                Text = "Target creature you control gets +2/+2 until end of turn, then it fights target creature you don't control.",
                SpellTargets = { TargetSlot.Of(TargetSpec.CreatureYouControl), TargetSlot.Of(TargetSpec.CreatureYouDontControl) },
                SpellEffects = { new PumpTargetEffect { Power = 2, Health = 2 }, new FightEffect() },
            };
            var prowler = Creature("sabretooth_prowler", "Sabretooth Prowler", 4, 4, 4, "evergrowing_wild", Rarity.Rare, "Cat",
                Keyword.Trample, "Trample. Arrival: This fights up to one target creature you don't control.");
            prowler.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.CreatureYouDontControl,
                Effects = { new FightEffect { SourceFights = true } },
            });
            yield return prowler;
            var behemoth = Creature("primeval_behemoth", "Primeval Behemoth", 6, 6, 6, "evergrowing_wild", Rarity.Uncommon, "Beast",
                Keyword.Trample, "Trample. Arrival: Heal all other creatures you control fully.");
            behemoth.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new HealOtherCreaturesYouControlEffect { Fully = true } } });
            yield return behemoth;

            // ---------------------------------------------------------------- Glitterworld (no Equipment yet: Equip needs activated abilities)
            yield return new CardDefinition
            {
                Id = "static_shock", Name = "Static Shock", Type = CardType.Instant, Cost = 1, Faction = "glitterworld",
                Rarity = Rarity.Common, Text = "Deal 1 damage to target creature. Draw a card.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new DealDamageEffect { Amount = 1 }, new DrawCardsEffect { Count = 1 } },
            };
            var sparkDrone = Creature("spark_drone", "Spark Drone", 2, 1, 1, "glitterworld", Rarity.Common, "Construct",
                Keyword.Flying, "Flying. Arrival: Deal 1 damage to a creature.");
            sparkDrone.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.Creature,
                Effects = { new DealDamageEffect { Amount = 1 } },
            });
            yield return sparkDrone;
            yield return new CardDefinition
            {
                Id = "chain_zap", Name = "Chain Zap", Type = CardType.Sorcery, Cost = 3, Faction = "glitterworld",
                Rarity = Rarity.Common, Text = "Deal 1 damage to each of up to three target creatures.",
                SpellTargets =
                {
                    TargetSlot.Of(TargetSpec.Creature, optional: true),
                    TargetSlot.Of(TargetSpec.Creature, optional: true),
                    TargetSlot.Of(TargetSpec.Creature, optional: true),
                },
                SpellEffects = { new DealDamageEffect { Amount = 1, EachTarget = true } },
            };
            yield return Creature("sky_patrol_drone", "Sky Patrol Drone", 3, 2, 3, "glitterworld", Rarity.Common, "Construct",
                Keyword.Flying, "Flying.");
            var suppressor = Creature("riot_suppressor", "Riot Suppressor", 4, 3, 3, "glitterworld", Rarity.Uncommon, "Construct",
                Keyword.None, "Arrival: Deal 1 damage to each enemy creature.");
            suppressor.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new DealDamageToEachEnemyCreatureEffect { Amount = 1 } } });
            yield return suppressor;
            yield return new CardDefinition
            {
                Id = "grid_overload", Name = "Grid Overload", Type = CardType.Sorcery, Cost = 4, Faction = "glitterworld",
                Rarity = Rarity.Rare, Text = "Deal 1 damage to each enemy creature three times.",
                SpellEffects =
                {
                    new DealDamageToEachEnemyCreatureEffect { Amount = 1 },
                    new DealDamageToEachEnemyCreatureEffect { Amount = 1 },
                    new DealDamageToEachEnemyCreatureEffect { Amount = 1 },
                },
            };
            var hoverTank = Creature("hover_tank", "Hover Tank", 5, 4, 5, "glitterworld", Rarity.Common, "Construct",
                Keyword.None, "Arrival: Deal 1 damage to a creature.");
            hoverTank.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.Creature,
                Effects = { new DealDamageEffect { Amount = 1 } },
            });
            yield return hoverTank;
            var orbital = new CardDefinition
            {
                Id = "orbital_strike_network", Name = "Orbital Strike Network", Type = CardType.Relic, Cost = 6,
                Faction = "glitterworld", Rarity = Rarity.Rare,
                Text = "At the start of your turn, deal 1 damage to each enemy creature and each opponent.",
            };
            orbital.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.StartOfYourTurn,
                Effects = { new DealDamageToEachEnemyCreatureEffect { Amount = 1, AlsoOpponents = true } },
            });
            yield return orbital;

            // ---------------------------------------------------------------- Shadow Money Wizards
            var forger = Creature("apprentice_forger", "Apprentice Forger", 2, 1, 2, "shadow_money_wizards", Rarity.Common, "Wizard",
                Keyword.Flying, "Flying. Arrival: Gain 1 Gold.");
            forger.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new GainGoldEffect { Amount = 1 } } });
            yield return forger;
            var rat = Creature("gilded_rat", "Gilded Rat", 1, 2, 1, "shadow_money_wizards", Rarity.Common, "Rat",
                Keyword.None, "Last Breath: Gain 2 Gold.");
            rat.Triggers.Add(new TriggeredAbility { When = TriggerEvent.LastBreath, Effects = { new GainGoldEffect { Amount = 2 } } });
            yield return rat;
            yield return new CardDefinition
            {
                Id = "sticky_fingers", Name = "Sticky Fingers", Type = CardType.Instant, Cost = 2, Faction = "shadow_money_wizards",
                Rarity = Rarity.Common, Text = "Target creature gets -3/-0 until end of turn. Invest 1: Draw a card.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new PumpTargetEffect { Power = -3 } },
                InvestCost = 1,
                InvestEffects = { new DrawCardsEffect { Count = 1 } },
            };
            var enforcer = Creature("hired_enforcer", "Hired Enforcer", 5, 5, 4, "shadow_money_wizards", Rarity.Common, "Wizard",
                Keyword.Flying, "Flying. Arrival: Each opponent gains 2 Gold.");
            enforcer.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new GainGoldEffect { Amount = 2, EachOpponent = true } } });
            yield return enforcer;
            yield return new CardDefinition
            {
                Id = "the_grand_ledger", Name = "The Grand Ledger", Type = CardType.Sorcery, Cost = 7, Faction = "shadow_money_wizards",
                Rarity = Rarity.Rare, Text = "Draw 4 cards. Invest 3: Draw 2 more and gain 3 life.",
                SpellEffects = { new DrawCardsEffect { Count = 4 } },
                InvestCost = 3,
                InvestEffects = { new DrawCardsEffect { Count = 2 }, new GainLifeEffect { Amount = 3 } },
            };

            // ---------------------------------------------------------------- Sensationalists
            yield return new CardDefinition
            {
                Id = SpiritToken, Name = "Spirit", Type = CardType.Creature, Power = 1, Health = 1, Keywords = Keyword.Flying,
                Subtypes = new[] { "Spirit" }, Faction = "sensationalists", IsToken = true,
            };
            var cultist = Creature("candle_cultist", "Candle Cultist", 1, 1, 1, "sensationalists", Rarity.Common, "Human",
                Keyword.None, "Last Breath: Each opponent loses 1 life and you gain 1 life.");
            cultist.Triggers.Add(new TriggeredAbility { When = TriggerEvent.LastBreath, Effects = { new DrainEffect { Amount = 1 } } });
            yield return cultist;
            var frailty = new CardDefinition
            {
                Id = "hex_of_frailty", Name = "Hex of Frailty", Type = CardType.Curse, Cost = 1, Faction = "sensationalists",
                Rarity = Rarity.Common, Text = "Attach to an enemy creature. It gets -1/-1.",
                SpellTarget = TargetSpec.CreatureYouDontControl,
            };
            frailty.Statics.Add(new AttachedCreatureModifier { Power = -1, Health = -1 });
            yield return frailty;
            yield return Creature("candlelit_acolyte", "Candlelit Acolyte", 2, 2, 2, "sensationalists", Rarity.Common, "Human",
                Keyword.Lifelink, "Lifelink.");
            yield return new CardDefinition
            {
                Id = "fatal_rumor", Name = "Fatal Rumor", Type = CardType.Instant, Cost = 2, Faction = "sensationalists",
                Rarity = Rarity.Common, Text = "Target creature gets -2/-2 until end of turn.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new PumpTargetEffect { Power = -2, Health = -2 } },
            };
            yield return Creature("hungry_shade", "Hungry Shade", 3, 3, 2, "sensationalists", Rarity.Uncommon, "Spirit",
                Keyword.Flying | Keyword.Lifelink, "Flying. Lifelink.");
            yield return new CardDefinition
            {
                Id = "ritual_slaughter", Name = "Ritual Slaughter", Type = CardType.Instant, Cost = 4, Faction = "sensationalists",
                Rarity = Rarity.Common, Text = "Destroy target creature. You lose 2 life.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new DestroyEffect(), new LoseLifeEffect { Amount = 2 } },
            };
            yield return new CardDefinition
            {
                Id = "wraith_swarm", Name = "Wraith Swarm", Type = CardType.Sorcery, Cost = 5, Faction = "sensationalists",
                Rarity = Rarity.Uncommon, Text = "Create three 1/1 Spirits with Flying.",
                SpellEffects = { new CreateTokensEffect { TokenId = SpiritToken, Count = 3 } },
            };

            // ---------------------------------------------------------------- Neutral
            yield return new CardDefinition
            {
                Id = "barkeeps_tonic", Name = "Barkeep's Tonic", Type = CardType.Instant, Cost = 1, Faction = "neutral",
                Rarity = Rarity.Common, Text = "Heal 3 from a creature or your Tavern Dweller. Invest 1: Draw a card.",
                SpellTarget = TargetSpec.CreatureOrYou,
                SpellEffects = { new HealEffect { Amount = 3 } },
                InvestCost = 1,
                InvestEffects = { new DrawCardsEffect { Count = 1 } },
            };
            yield return Creature("hired_sellsword", "Hired Sellsword", 2, 2, 3, "neutral", Rarity.Common, "Human",
                Keyword.None, "");
            yield return Creature("tavern_bouncer", "Tavern Bouncer", 3, 2, 5, "neutral", Rarity.Common, "Human",
                Keyword.None, "");
            var adventurer = Creature("wandering_adventurer", "Wandering Adventurer", 4, 3, 4, "neutral", Rarity.Uncommon,
                "Human", Keyword.None, "Arrival: Draw a card.");
            adventurer.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new DrawCardsEffect { Count = 1 } } });
            yield return adventurer;

            foreach (var c in AbilityCards()) yield return c;
            foreach (var c in V02Cards()) yield return c;
            foreach (var c in V01RestCards()) yield return c;
            foreach (var c in V03Cards()) yield return c;
            foreach (var c in TavernDwellers()) yield return c;
        }

        private static CardDefinition Creature(string id, string name, int cost, int power, int health, string faction,
            Rarity rarity, string subtype, Keyword keywords, string text) => new CardDefinition
        {
            Id = id, Name = name, Type = CardType.Creature, Cost = cost, Power = power, Health = health,
            Faction = faction, Rarity = rarity, Subtypes = new[] { subtype }, Keywords = keywords, Text = text,
        };
    }
}
