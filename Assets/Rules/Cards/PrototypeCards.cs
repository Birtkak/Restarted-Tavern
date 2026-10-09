using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// The ~20 test cards for the engine prototype (DEVELOPMENT §5 roadmap step 2), taken
    /// as-is from the v0.1 card lists. Only cards the engine can fully express today are here.
    /// Later these move to data files (DEVELOPMENT §3).
    /// </summary>
    public static class PrototypeCards
    {
        public const string GooberToken = "goober_token";

        public static CardDatabase CreateDatabase() => new CardDatabase(All());

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
                Rarity = Rarity.Common, Text = "Heal 4 from a creature. Overcharge 1: Put a +1/+1 counter on it.",
                SpellTarget = TargetSpec.Creature,
                SpellEffects = { new HealEffect { Amount = 4 } },
                OverchargeCost = 1,
                OverchargeEffects = { new AddCountersEffect { Count = 1 } },
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

            // ---------------------------------------------------------------- Neutral
            yield return new CardDefinition
            {
                Id = "barkeeps_tonic", Name = "Barkeep's Tonic", Type = CardType.Instant, Cost = 1, Faction = "neutral",
                Rarity = Rarity.Common, Text = "Heal 3 from a creature or your Patron. Overcharge 1: Draw a card.",
                SpellTarget = TargetSpec.CreatureOrYou,
                SpellEffects = { new HealEffect { Amount = 3 } },
                OverchargeCost = 1,
                OverchargeEffects = { new DrawCardsEffect { Count = 1 } },
            };
            yield return Creature("hired_sellsword", "Hired Sellsword", 2, 2, 3, "neutral", Rarity.Common, "Human",
                Keyword.None, "");
            yield return Creature("tavern_bouncer", "Tavern Bouncer", 3, 2, 5, "neutral", Rarity.Common, "Human",
                Keyword.None, "");
            var adventurer = Creature("wandering_adventurer", "Wandering Adventurer", 4, 3, 4, "neutral", Rarity.Uncommon,
                "Human", Keyword.None, "Arrival: Draw a card.");
            adventurer.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new DrawCardsEffect { Count = 1 } } });
            yield return adventurer;
        }

        private static CardDefinition Creature(string id, string name, int cost, int power, int health, string faction,
            Rarity rarity, string subtype, Keyword keywords, string text) => new CardDefinition
        {
            Id = id, Name = name, Type = CardType.Creature, Cost = cost, Power = power, Health = health,
            Faction = faction, Rarity = rarity, Subtypes = new[] { subtype }, Keywords = keywords, Text = text,
        };
    }
}
