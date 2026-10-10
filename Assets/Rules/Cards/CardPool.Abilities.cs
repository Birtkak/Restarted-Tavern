using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// Cards that need activated abilities or Equip: the Glitterworld Equipment package, Snik,
    /// Grove Elder, and the Neutral Gold sinks (set v0.1 and the v0.2 additions).
    /// </summary>
    public static partial class CardPool
    {
        public const string MercenaryToken = "mercenary_token";
        public const string DroneToken = "drone_token";
        public const string SpawnToken = "spawn_token";
        public const string ScrapPlatingToken = "scrap_plating_token";

        /// <summary>Equip N (GAME_DESIGN §10): sorcery speed, attach to target creature you control.</summary>
        private static ActivatedAbility Equip(int cost) => new ActivatedAbility
        {
            Cost = cost,
            IsEquip = true,
            Targets = { TargetSlot.Of(TargetSpec.CreatureYouControl) },
            // Re-equipping to the creature it's already on does nothing, so it isn't offered.
            TargetsAllowed = (s, source, t) => t[0].Object != source.AttachedToObject,
            Effects = { new AttachSourceEffect() },
            Text = "Equip " + cost,
        };

        private static CardDefinition Equipment(string id, string name, int cost, string faction, Rarity rarity, int equip,
            AttachedCreatureModifier bonus, string text)
        {
            var def = new CardDefinition
            {
                Id = id, Name = name, Type = CardType.Equipment, Cost = cost, Faction = faction, Rarity = rarity, Text = text,
            };
            def.Statics.Add(bonus);
            def.Abilities.Add(Equip(equip));
            return def;
        }

        private static TriggeredAbility AttackPing() => new TriggeredAbility
        {
            When = TriggerEvent.Attacks, Target = TargetSpec.Creature,
            Effects = { new DealDamageEffect { Amount = 1 } },
            Text = "Whenever this creature attacks, deal 1 damage to a creature.",
        };

        private static IEnumerable<CardDefinition> AbilityCards()
        {
            // ---------------------------------------------------------------- tokens
            yield return new CardDefinition
            {
                Id = MercenaryToken, Name = "Mercenary", Type = CardType.Creature, Power = 2, Health = 2,
                Subtypes = new[] { "Human" }, Faction = "neutral", IsToken = true,
            };
            yield return new CardDefinition
            {
                Id = DroneToken, Name = "Drone", Type = CardType.Creature, Power = 1, Health = 1, Keywords = Keyword.Flying,
                Subtypes = new[] { "Construct" }, Faction = "glitterworld", IsToken = true,
            };
            yield return new CardDefinition
            {
                Id = SpawnToken, Name = "Spawn", Type = CardType.Creature, Power = 2, Health = 2,
                Subtypes = new[] { "Spawn" }, Faction = "neutral", IsToken = true,
            };
            var plating = Equipment(ScrapPlatingToken, "Scrap Plating", 0, "glitterworld", Rarity.Common, 1,
                new AttachedCreatureModifier { Health = 2 }, "Equipped creature gets +0/+2. Equip 1.");
            plating.IsToken = true;
            yield return plating;

            // ---------------------------------------------------------------- Glitterworld Equipment (v0.1)
            yield return Equipment("neon_shiv", "Neon Shiv", 1, "glitterworld", Rarity.Common, 1,
                new AttachedCreatureModifier { Power = 1, Health = 1 }, "Equipped creature gets +1/+1. Equip 1.");
            yield return Equipment("pulse_blade", "Pulse Blade", 2, "glitterworld", Rarity.Common, 2,
                new AttachedCreatureModifier { Power = 1, Health = 1, Triggers = { AttackPing() } },
                "Equipped creature gets +1/+1 and has \"Whenever this creature attacks, deal 1 damage to a creature.\" Equip 2.");
            yield return Equipment("overclock_rig", "Overclock Rig", 3, "glitterworld", Rarity.Uncommon, 2,
                new AttachedCreatureModifier
                {
                    Power = 2, Health = 1,
                    Abilities =
                    {
                        new ActivatedAbility
                        {
                            TapCost = true, Targets = { TargetSlot.Of(TargetSpec.AnyTarget) },
                            Effects = { new DealDamageEffect { Amount = 1 } }, Text = "Tap: Deal 1 damage to any target.",
                        },
                    },
                },
                "Equipped creature gets +2/+1 and has \"Tap: Deal 1 damage to any target.\" Equip 2.");
            yield return Equipment("rail_cannon", "Rail Cannon", 4, "glitterworld", Rarity.Uncommon, 3,
                new AttachedCreatureModifier { Power = 3, Triggers = { AttackPing() } },
                "Equipped creature gets +3/+0 and has \"Whenever this creature attacks, deal 1 damage to a creature.\" Equip 3.");
            yield return Equipment("megacorp_exosuit", "Megacorp Exosuit", 5, "glitterworld", Rarity.Rare, 3,
                new AttachedCreatureModifier { Power = 3, Health = 3, Grants = Keyword.Flying | Keyword.Trample },
                "Equipped creature gets +3/+3 and has Flying and Trample. Equip 3.");

            // ---------------------------------------------------------------- Glitterworld Equipment (v0.2)
            yield return Equipment("gilded_knuckles", "Gilded Knuckles", 1, "glitterworld", Rarity.Common, 1,
                new AttachedCreatureModifier { Power = 2 }, "Equipped creature gets +2/+0. Equip 1.");
            yield return Equipment("marksman_scope", "Marksman Scope", 2, "glitterworld", Rarity.Common, 1,
                new AttachedCreatureModifier
                {
                    Power = 1,
                    Triggers =
                    {
                        new TriggeredAbility
                        {
                            When = TriggerEvent.DealsCombatDamageToPlayer, Target = TargetSpec.Creature,
                            Effects = { new DealDamageEffect { Amount = 1 } },
                            Text = "Whenever this deals combat damage to a player, deal 1 damage to a creature.",
                        },
                    },
                },
                "Equipped creature gets +1/+0 and has \"Whenever this deals combat damage to a player, deal 1 damage to a creature.\" Equip 1.");
            yield return Equipment("drone_launcher", "Drone Launcher", 3, "glitterworld", Rarity.Uncommon, 2,
                new AttachedCreatureModifier
                {
                    Triggers =
                    {
                        new TriggeredAbility
                        {
                            When = TriggerEvent.Attacks, Effects = { new CreateTokensEffect { TokenId = DroneToken } },
                            Text = "Whenever this attacks, create a 1/1 Drone with Flying.",
                        },
                    },
                },
                "Equipped creature has \"Whenever this attacks, create a 1/1 Drone with Flying.\" Equip 2.");

            // ---------------------------------------------------------------- Glitterworld creatures and relics
            var courier = Creature("courier_bot", "Courier Bot", 1, 1, 2, "glitterworld", Rarity.Common, "Construct",
                Keyword.None, "Arrival: You may attach target Equipment you control to this.");
            courier.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.EquipmentYouControl, TargetOptional = true,
                Effects = { new AttachTargetEquipmentEffect { ToSource = true } },
            });
            yield return courier;

            var mechanic = Creature("back_street_mechanic", "Back-Street Mechanic", 2, 2, 2, "glitterworld", Rarity.Uncommon,
                "Citizen", Keyword.None, "Tap: Heal 2 from target Construct or equipped creature.");
            mechanic.Abilities.Add(new ActivatedAbility
            {
                TapCost = true, Targets = { TargetSlot.Of(TargetSpec.ConstructOrEquippedCreature) },
                Effects = { new HealEffect { Amount = 2 } }, Text = "Tap: Heal 2 from target Construct or equipped creature.",
            });
            yield return mechanic;

            var tinker = Creature("alley_tinker", "Alley Tinker", 2, 2, 2, "glitterworld", Rarity.Common, "Citizen",
                Keyword.None, "Arrival: Create a Scrap Plating Equipment token with \"Equipped creature gets +0/+2. Equip 1.\"");
            tinker.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new CreateTokensEffect { TokenId = ScrapPlatingToken } } });
            yield return tinker;

            var welder = Creature("arc_welder", "Arc Welder", 3, 2, 3, "glitterworld", Rarity.Uncommon, "Citizen",
                Keyword.None, "Whenever you pay an Equip cost, deal 1 damage to any target.");
            welder.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.EquipActivated, Subject = TriggerSubject.You, Target = TargetSpec.AnyTarget,
                Effects = { new DealDamageEffect { Amount = 1 } },
            });
            yield return welder;

            var captain = Creature("patrol_captain", "Patrol Captain", 5, 4, 5, "glitterworld", Rarity.Uncommon, "Citizen",
                Keyword.None, "Equipped creatures you control get +1/+1.");
            captain.Statics.Add(new AnthemAbility { RequiresEquipped = true, Power = 1, Health = 1 });
            yield return captain;

            var titan = Creature("titan_frame_guardian", "Titan-Frame Guardian", 6, 5, 7, "glitterworld", Rarity.Rare, "Construct",
                Keyword.None, "Whenever an Equipment becomes attached to this, heal it fully and draw a card.");
            titan.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.EquipmentAttachedToThis,
                Effects = { new HealSelfEffect { Fully = true }, new DrawCardsEffect { Count = 1 } },
            });
            yield return titan;

            var archon = Creature("archon_lumen", "Archon Lumen, Mind of the City", 7, 5, 7, "glitterworld", Rarity.Legendary,
                "Construct", Keyword.Flying,
                "Flying. Your Equip costs are 0. At the end of your turn, deal 1 damage to any target for each Equipment you control.");
            archon.Statics.Add(new CostModifierAbility { Kind = CostKind.Equip, SetToZero = true });
            archon.Triggers.Add(new TriggeredAbility
            {
                // Decided 2026-10-09: one separate 1-damage ping per Equipment, each with its own target.
                When = TriggerEvent.EndOfYourTurn, Target = TargetSpec.AnyTarget,
                Effects = { new DealDamageEffect { Amount = 1 } },
                RepeatCount = (s, db, source) =>
                {
                    int n = 0;
                    foreach (var c in s.GetPlayer(source.Controller).Battlefield)
                        if (db.Get(c.DefinitionId).Type == CardType.Equipment) n++;
                    return n;
                },
            });
            yield return archon;

            var patchUp = Creature("patch_up_drone", "Patch-Up Drone", 2, 1, 1, "glitterworld", Rarity.Common, "Construct",
                Keyword.Flying, "Flying. Arrival: Heal 2 from target Construct or equipped creature.");
            patchUp.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.ConstructOrEquippedCreature,
                Effects = { new HealEffect { Amount = 2 } },
            });
            yield return patchUp;

            var collector = Creature("scrap_collector", "Scrap Collector", 2, 2, 2, "glitterworld", Rarity.Common, "Construct",
                Keyword.None, "Whenever an Equipment you control becomes unattached, gain 1 Gold.");
            collector.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.EquipmentUnattached, Subject = TriggerSubject.You,
                Effects = { new GainGoldEffect { Amount = 1 } },
            });
            yield return collector;

            var repairBay = new CardDefinition
            {
                Id = "repair_bay", Name = "Repair Bay", Type = CardType.Relic, Cost = 3, Faction = "glitterworld",
                Rarity = Rarity.Uncommon,
                Text = "At the start of your turn, heal 1 from each Construct and each equipped creature you control.",
            };
            repairBay.Triggers.Add(new TriggeredAbility { When = TriggerEvent.StartOfYourTurn, Effects = { new HealYourMachinesEffect { Amount = 1 } } });
            yield return repairBay;

            // ---------------------------------------------------------------- Goobers / Wild
            var snik = Creature("snik", "Snik, the Goober Doubler", 4, 2, 4, "goobers", Rarity.Legendary, "Goober", Keyword.None,
                "X, Tap: Choose up to X other Goobers you control. For each one, create a token copy of it. The copies gain Haste until end of turn.");
            snik.Abilities.Add(new ActivatedAbility
            {
                // The Goobers are chosen on resolution (MTG 608.2d), not on activation (changed 2026-10-10).
                HasX = true, TapCost = true,
                Effects =
                {
                    new ChooseUpToXYourCreaturesEffect
                    {
                        Subtype = "Goober", Then = { new CreateTokenCopiesEffect { GrantUntilEndOfTurn = Keyword.Haste } },
                    },
                },
                Text = "X, Tap: Copy up to X other Goobers you control. The copies gain Haste until end of turn.",
            });
            yield return snik;

            var elder = Creature("grove_elder", "Grove Elder", 3, 2, 3, "evergrowing_wild", Rarity.Uncommon, "Shaman",
                Keyword.None, "Tap: Heal 2 from target creature.");
            elder.Abilities.Add(new ActivatedAbility
            {
                TapCost = true, Targets = { TargetSlot.Of(TargetSpec.Creature) },
                Effects = { new HealEffect { Amount = 2 } }, Text = "Tap: Heal 2 from target creature.",
            });
            yield return elder;

            // ---------------------------------------------------------------- Neutral Gold sinks
            var contract = new CardDefinition
            {
                Id = "mercenary_contract", Name = "Mercenary Contract", Type = CardType.Relic, Cost = 4, Faction = "neutral",
                Rarity = Rarity.Rare,
                Text = "Pay 2 Gold: Create a 2/2 Mercenary. Activate only once each turn and only as a sorcery.",
            };
            contract.Abilities.Add(new ActivatedAbility
            {
                GoldCost = 2, OncePerTurn = true, SorcerySpeed = true,
                Effects = { new CreateTokensEffect { TokenId = MercenaryToken } }, Text = "Pay 2 Gold: Create a 2/2 Mercenary.",
            });
            yield return contract;

            var tipJar = new CardDefinition
            {
                Id = "tip_jar", Name = "Tip Jar", Type = CardType.Relic, Cost = 1, Faction = "neutral", Rarity = Rarity.Common,
                Text = "Pay 3 Gold: Draw a card. Activate only once each turn.",
            };
            tipJar.Abilities.Add(new ActivatedAbility
            {
                GoldCost = 3, OncePerTurn = true, Effects = { new DrawCardsEffect { Count = 1 } }, Text = "Pay 3 Gold: Draw a card.",
            });
            yield return tipJar;

            var muscle = Creature("hired_muscle", "Hired Muscle", 3, 3, 3, "neutral", Rarity.Common, "Human", Keyword.None,
                "Pay 2 Gold: This gets +2/+0 until end of turn. Activate only once each turn.");
            muscle.Abilities.Add(new ActivatedAbility
            {
                GoldCost = 2, OncePerTurn = true, Effects = { new PumpSourceEffect { Power = 2 } },
                Text = "Pay 2 Gold: This gets +2/+0 until end of turn.",
            });
            yield return muscle;

            var dresser = Creature("wound_dresser", "Wound Dresser", 3, 2, 3, "neutral", Rarity.Uncommon, "Human", Keyword.None,
                "Pay 2 Gold: Heal 2 from target creature. Activate only once each turn.");
            dresser.Abilities.Add(new ActivatedAbility
            {
                GoldCost = 2, OncePerTurn = true, Targets = { TargetSlot.Of(TargetSpec.Creature) },
                Effects = { new HealEffect { Amount = 2 } }, Text = "Pay 2 Gold: Heal 2 from target creature.",
            });
            yield return dresser;
        }
    }
}
