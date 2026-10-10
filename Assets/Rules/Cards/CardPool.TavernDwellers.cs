using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// The 10 approved Tavern Dwellers (docs/cards/tavern_dwellers.md is the source of truth). Each one has a
    /// passive (a triggered or static ability) and a Power: an activated ability paid with mana
    /// and/or Gold (mana first), once each turn, at instant speed (GAME_DESIGN §9.1).
    /// </summary>
    public static partial class CardPool
    {
        public const string Wizards = "shadow_money_wizards";
        public const string Goobers = "goobers";
        public const string Sensationalists = "sensationalists";
        public const string Wild = "evergrowing_wild";
        public const string Glitterworld = "glitterworld";

        private static CardDefinition TavernDweller(string id, string name, string factionA, string factionB, string text,
            ActivatedAbility power)
        {
            power.IsTavernDwellerPower = true;
            var def = new CardDefinition
            {
                Id = id, Name = name, Type = CardType.TavernDweller, Faction = "neutral", Rarity = Rarity.Legendary,
                TavernDwellerFactions = new[] { factionA, factionB }, Text = text,
            };
            def.Abilities.Add(power);
            return def;
        }

        private static IEnumerable<CardDefinition> TavernDwellers()
        {
            var grizzle = TavernDweller("grizzle_coinflick", "Grizzle Coinflick", Wizards, Goobers,
                "Whenever you cast a spell that costs 5 or more, create a 1/1 Goober. Power (2): Deal 1 damage to any target.",
                new ActivatedAbility
                {
                    Cost = 2, Targets = { TargetSlot.Of(TargetSpec.AnyTarget) },
                    Effects = { new DealDamageEffect { Amount = 1 } }, Text = "(2) Deal 1 damage to any target.",
                });
            grizzle.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.SpellCast, Subject = TriggerSubject.You, MinCost = 5,
                Effects = { new CreateTokensEffect { TokenId = GooberToken } },
            });
            yield return grizzle;

            var vesper = TavernDweller("madame_vesper", "Madame Vesper", Wizards, Sensationalists,
                "Whenever a creature an opponent controls dies, gain 1 Gold. Power (3): Draw a card and lose 2 life.",
                new ActivatedAbility
                {
                    Cost = 3, Effects = { new DrawCardsEffect { Count = 1 }, new LoseLifeEffect { Amount = 2 } },
                    Text = "(3) Draw a card and lose 2 life.",
                });
            vesper.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, Subject = TriggerSubject.Opponents, Effects = { new GainGoldEffect { Amount = 1 } },
            });
            yield return vesper;

            var mossbank = TavernDweller("old_mossbank", "Old Mossbank", Wizards, Wild,
                "Your spells and creatures that cost 6 or more cost 1 less. Power (2): Give a creature +2/+2 until end of turn.",
                new ActivatedAbility
                {
                    Cost = 2, Targets = { TargetSlot.Of(TargetSpec.Creature) },
                    Effects = { new PumpTargetEffect { Power = 2, Health = 2 } }, Text = "(2) Give a creature +2/+2 until end of turn.",
                });
            mossbank.Statics.Add(new CostModifierAbility { Kind = CostKind.Spell, MinPrintedCost = 6, Reduction = 1 });
            yield return mossbank;

            var auditor = TavernDweller("auditor_prime", "Auditor Prime", Wizards, Glitterworld,
                "Your Invest and Equip costs are 1 lower (minimum 1). Power (2): Draw a card. Activate only if you have 3 or more Gold.",
                new ActivatedAbility
                {
                    // Changed 2026-10-10 (was "(1) Look at the top card, you may put it on the bottom"): rewards sitting on Gold.
                    Cost = 2, ActivateOnlyWithGold = 3, Effects = { new DrawCardsEffect { Count = 1 } },
                    Text = "(2) Draw a card. Activate only if you have 3 or more Gold.",
                });
            auditor.Statics.Add(new CostModifierAbility { Kind = CostKind.Invest, Reduction = 1, NotBelowOne = true });
            auditor.Statics.Add(new CostModifierAbility { Kind = CostKind.Equip, Reduction = 1, NotBelowOne = true });
            yield return auditor;

            var skabba = TavernDweller("skabba", "Skabba", Goobers, Sensationalists,
                "Whenever one of your creatures dies, deal 1 damage to each opponent. This triggers at most 3 times each turn. Power (1), sacrifice a creature: Draw a card.",
                new ActivatedAbility
                {
                    Cost = 1, SacrificeCreatureCost = true, Effects = { new DrawCardsEffect { Count = 1 } },
                    Text = "(1) Sacrifice a creature: Draw a card.",
                });
            skabba.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, Subject = TriggerSubject.You, MaxPerTurn = 3,
                Effects = { new DealDamageToEachOpponentEffect { Amount = 1 } },
            });
            yield return skabba;

            var mukk = TavernDweller("mukk_the_grub_king", "Mukk the Grub King", Goobers, Wild,
                "Your creatures with Trample get +1/+0. Power (3): Target creature you control with Trample fights target creature you don't control.",
                new ActivatedAbility
                {
                    // Changed 2026-10-10 (was "(2) A creature you control gains Trample until end of turn").
                    Cost = 3,
                    Targets =
                    {
                        new TargetSlot { Spec = TargetSpec.CreatureYouControl, Keyword = Keyword.Trample },
                        TargetSlot.Of(TargetSpec.CreatureYouDontControl),
                    },
                    Effects = { new FightEffect() },
                    Text = "(3) Target creature you control with Trample fights target creature you don't control.",
                });
            mukk.Statics.Add(new AnthemAbility { RequiresKeyword = Keyword.Trample, Power = 1 });
            yield return mukk;

            var sparkwrench = TavernDweller("sparkwrench", "Sparkwrench", Goobers, Glitterworld,
                "Your Equipment spells cost 1 less. Power (2): Attach up to one target Equipment you control to target creature you control. If no Equipment became attached, that creature gets +1/+1 until end of turn.",
                new ActivatedAbility
                {
                    // Changed 2026-10-10 (was "(2) Attach an Equipment you control to another creature you control"): useful without Equipment.
                    Cost = 2,
                    Targets = { TargetSlot.Of(TargetSpec.CreatureYouControl), TargetSlot.Of(TargetSpec.EquipmentYouControl, optional: true) },
                    Effects = { new AttachEquipmentOrPumpEffect() },
                    Text = "(2) Attach up to one target Equipment you control to target creature you control. If none became attached, it gets +1/+1 until end of turn.",
                });
            sparkwrench.Statics.Add(new CostModifierAbility { Kind = CostKind.Spell, OnlyType = CardType.Equipment, Reduction = 1 });
            yield return sparkwrench;

            var rotmother = TavernDweller("the_rotmother", "The Rotmother", Sensationalists, Wild,
                "Whenever a creature with 5 or more Power you control dies, create a 2/2 Spawn. Power (3): Return a creature card from your graveyard to your hand, then lose 3 life.",
                new ActivatedAbility
                {
                    Cost = 3, Targets = { TargetSlot.Of(TargetSpec.CreatureCardInYourGraveyard) },
                    Effects = { new ReturnToHandFromGraveyardEffect(), new LoseLifeEffect { Amount = 3 } },
                    Text = "(3) Return a creature card from your graveyard to your hand, then lose 3 life.",
                });
            rotmother.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, Subject = TriggerSubject.You, MinPower = 5,
                Effects = { new CreateTokensEffect { TokenId = SpawnToken } },
            });
            yield return rotmother;

            var vox = TavernDweller("vox_nocturne", "Vox Nocturne", Sensationalists, Glitterworld,
                "Whenever a creature an opponent controls dies, you gain 1 life. Power (2): Deal 1 damage to a creature. If it dies, draw a card.",
                new ActivatedAbility
                {
                    Cost = 2, Targets = { TargetSlot.Of(TargetSpec.Creature) },
                    Effects = { new DamageThenDrawIfLethalEffect { Amount = 1 } },
                    Text = "(2) Deal 1 damage to a creature. If it dies, draw a card.",
                });
            vox.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, Subject = TriggerSubject.Opponents, Effects = { new GainLifeEffect { Amount = 1 } },
            });
            yield return vox;

            var keeper = TavernDweller("keeper_z00", "Keeper Z-00", Wild, Glitterworld,
                "Your creatures with 5 or more Health enter with a +1/+1 counter. Power (2): Heal 3 from a creature.",
                new ActivatedAbility
                {
                    Cost = 2, Targets = { TargetSlot.Of(TargetSpec.Creature) },
                    Effects = { new HealEffect { Amount = 3 } }, Text = "(2) Heal 3 from a creature.",
                });
            keeper.Statics.Add(new EntersWithCountersAbility { MinHealth = 5 });
            yield return keeper;
        }
    }
}
