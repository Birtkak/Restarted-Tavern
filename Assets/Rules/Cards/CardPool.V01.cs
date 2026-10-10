using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// The rest of set v0.1 (docs/cards/*.md): the cards that weren't in the engine yet. Batch E:
    /// cards that needed small engine additions (intervening "if", death watchers, extra blocks, graveyard
    /// returns, attacking tokens). Batch F: cards with a choice during resolution (sacrifice, discard, "you
    /// may", put onto the battlefield, Everything Has a Price) and divided damage.
    /// </summary>
    public static partial class CardPool
    {
        private static IEnumerable<CardDefinition> V01RestCards()
        {
            const string wizards = "shadow_money_wizards";
            const string wild = "evergrowing_wild";

            // ---------------------------------------------------------------- Evergrowing Wild
            var sproutling = Creature("sproutling", "Sproutling", 1, 1, 2, wild, Rarity.Common, "Plant", Keyword.None,
                "At the end of your turn, if this has no damage, put a +1/+1 counter on it.");
            sproutling.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.EndOfYourTurn,
                Condition = (s, db, controller, source) => s.FindOnBattlefield(source)?.Damage == 0,
                Effects = { new AddCountersToSourceEffect() },
            });
            yield return sproutling;

            var denMother = Creature("den_mother", "Den Mother", 3, 2, 4, wild, Rarity.Uncommon, "Beast", Keyword.None,
                "Whenever another creature with 5 or more Power enters under your control, draw a card.");
            denMother.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureEnters, Subject = TriggerSubject.You, OthersOnly = true, MinPower = 5,
                Effects = { new DrawCardsEffect { Count = 1 } },
            });
            yield return denMother;

            var warden = Creature("grove_warden", "Grove Warden", 4, 3, 5, wild, Rarity.Rare, "Treefolk", Keyword.None,
                "Your other creatures have \"At the start of your turn, heal 1 from this.\"");
            warden.Statics.Add(new GrantTriggerToYourCreaturesAbility
            {
                OthersOnly = true,
                Triggers =
                {
                    new TriggeredAbility
                    {
                        When = TriggerEvent.StartOfYourTurn, Effects = { new HealSelfEffect { Amount = 1 } },
                        Text = "At the start of your turn, heal 1 from this.",
                    },
                },
            });
            yield return warden;

            yield return new CardDefinition
            {
                Id = "call_of_the_deep_jungle", Name = "Call of the Deep Jungle", Type = CardType.Sorcery, Cost = 5, Faction = wild,
                Rarity = Rarity.Rare,
                Text = "Reveal cards from the top of your deck until you reveal a creature card with cost 5 or more. "
                       + "Put it onto the battlefield. Put the other revealed cards on the bottom of your deck in a random order.",
                SpellEffects = { new RevealUntilCreatureEffect { MinCost = 5 } },
            };

            var hydra = Creature("worldroot_hydra", "Worldroot Hydra", 6, 5, 5, wild, Rarity.Rare, "Hydra", Keyword.None,
                "Whenever this is dealt damage and survives, put a +1/+1 counter on it.");
            hydra.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDealtDamage, OnlySelf = true, OnlyIfSurvives = true,
                Effects = { new AddCountersToSourceEffect() },
            });
            yield return hydra;

            var apex = Creature("apex_of_the_green_deep", "Apex of the Green Deep", 10, 12, 12, wild, Rarity.Legendary, "Leviathan",
                Keyword.Trample, "Trample. At the start of your turn, heal this creature fully.");
            apex.Triggers.Add(new TriggeredAbility { When = TriggerEvent.StartOfYourTurn, Effects = { new HealSelfEffect { Fully = true } } });
            yield return apex;

            // ---------------------------------------------------------------- Goobers
            var grubby = Creature("grubby_pickpocket", "Grubby Pickpocket", 1, 1, 2, "goobers", Rarity.Common, "Goober", Keyword.None,
                "Whenever this attacks, the defending player loses 1 Gold and you gain 1 Gold.");
            grubby.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Attacks, Effects = { new DrainGoldEffect() } });
            yield return grubby;

            var snatchers = Creature("gold_snatcher_crew", "Gold-Snatcher Crew", 3, 2, 2, "goobers", Rarity.Uncommon, "Goober",
                Keyword.Haste,
                "Haste. Whenever this deals combat damage to a player, that player loses up to 2 Gold and you gain that much.");
            snatchers.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.DealsCombatDamageToPlayer, Effects = { new StealGoldEffect { Amount = 2, FromEventPlayer = true } },
            });
            yield return snatchers;

            var demolisher = Creature("goober_demolisher", "Goober Demolisher", 5, 4, 5, "goobers", Rarity.Rare, "Goober",
                Keyword.Trample, "Trample. Whenever another Goober you control dies, deal 1 damage to each opponent.");
            demolisher.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, Subject = TriggerSubject.You, SubjectSubtype = "Goober", OthersOnly = true,
                Effects = { new DealDamageToEachOpponentEffect { Amount = 1 } },
            });
            yield return demolisher;

            yield return new CardDefinition
            {
                Id = "scrapheap_inferno", Name = "Scrapheap Inferno", Type = CardType.Sorcery, Cost = 5, Faction = "goobers",
                Rarity = Rarity.Rare, Text = "Deal X damage to any target, where X is 2 plus the number of Goobers you control.",
                SpellTarget = TargetSpec.AnyTarget,
                SpellEffects = { new DealDamageEffect { Amount = 2, PlusOnePerYourCreatureOfSubtype = "Goober" } },
            };

            var grakka = Creature("grakka_queen_of_the_rabble", "Grakka, Queen of the Rabble", 7, 5, 6, "goobers", Rarity.Legendary,
                "Goober", Keyword.Haste,
                "Haste. Your Goobers get +1/+1. Whenever you attack, create a 1/1 Goober that's tapped and attacking.");
            grakka.Statics.Add(new AnthemAbility { Subtype = "Goober", Power = 1, Health = 1 });
            grakka.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.PlayerAttacks, Subject = TriggerSubject.You,
                Effects = { new CreateAttackingTokenEffect { TokenId = GooberToken } },
            });
            yield return grakka;

            // ---------------------------------------------------------------- Neutral
            var potBoy = Creature("pot_boy", "Pot Boy", 1, 1, 2, "neutral", Rarity.Common, "Human", Keyword.None,
                "Arrival: Heal 1 from a creature.");
            potBoy.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.Creature, Effects = { new HealEffect { Amount = 1 } },
            });
            yield return potBoy;

            yield return new CardDefinition
            {
                Id = "last_call", Name = "Last Call", Type = CardType.Instant, Cost = 2, Faction = "neutral", Rarity = Rarity.Common,
                Text = "Destroy target Equipment, Relic or Curse.",
                SpellTarget = TargetSpec.EquipmentRelicOrCurse,
                SpellEffects = { new DestroyEffect() },
            };
            yield return new CardDefinition
            {
                Id = "round_on_the_house", Name = "Round on the House", Type = CardType.Sorcery, Cost = 3, Faction = "neutral",
                Rarity = Rarity.Uncommon, Text = "Each player draws a card. Then you draw a card.",
                SpellEffects = { new EachPlayerDrawsEffect { Count = 1 }, new DrawCardsEffect { Count = 1 } },
            };

            var keeper = Creature("old_tavern_keeper", "Old Tavern Keeper", 4, 2, 5, "neutral", Rarity.Rare, "Human", Keyword.None,
                "At the end of your turn, heal 2 from each other creature you control.");
            keeper.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.EndOfYourTurn, Effects = { new HealOtherCreaturesYouControlEffect { Amount = 2 } },
            });
            yield return keeper;

            var champion = Creature("retired_champion", "Retired Champion", 5, 5, 6, "neutral", Rarity.Uncommon, "Human", Keyword.None,
                "Can block an additional creature each combat.");
            champion.ExtraBlocks = 1;
            yield return champion;

            // ---------------------------------------------------------------- Sensationalists
            var onlooker = Creature("ghoulish_onlooker", "Ghoulish Onlooker", 2, 2, 1, "sensationalists", Rarity.Common, "Human",
                Keyword.None, "Whenever another creature dies, put a +1/+1 counter on this.");
            onlooker.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, OthersOnly = true, Effects = { new AddCountersToSourceEffect() },
            });
            yield return onlooker;

            yield return new CardDefinition
            {
                Id = "seance", Name = "Séance", Type = CardType.Sorcery, Cost = 2, Faction = "sensationalists", Rarity = Rarity.Uncommon,
                Text = "Return up to two creature cards with cost 3 or less from your graveyard to your hand.",
                SpellTargets =
                {
                    new TargetSlot { Spec = TargetSpec.CreatureCardInYourGraveyard, MaxCost = 3, Optional = true },
                    new TargetSlot { Spec = TargetSpec.CreatureCardInYourGraveyard, MaxCost = 3, Optional = true },
                },
                SpellEffects = { new ReturnToHandFromGraveyardEffect(), new ReturnToHandFromGraveyardEffect { TargetIndex = 1 } },
            };

            var withering = new CardDefinition
            {
                Id = "hex_of_withering", Name = "Hex of Withering", Type = CardType.Curse, Cost = 3, Faction = "sensationalists",
                Rarity = Rarity.Common,
                Text = "Attach to an enemy creature. It gets -2/-0. At the start of its controller's turn, deal 1 damage to it.",
                SpellTarget = TargetSpec.CreatureYouDontControl,
            };
            withering.Statics.Add(new AttachedCreatureModifier { Power = -2 });
            withering.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.StartOfEnchantedCreatureControllersTurn, Effects = { new DealDamageToEventCreatureEffect { Amount = 1 } },
            });
            yield return withering;

            var boneMedium = Creature("bone_medium", "Bone Medium", 3, 2, 3, "sensationalists", Rarity.Common, "Human", Keyword.None,
                "Arrival: Return a creature card with cost 2 or less from your graveyard to your hand.");
            boneMedium.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Target = TargetSpec.CreatureCardInYourGraveyard, TargetMaxCost = 2,
                Effects = { new ReturnToHandFromGraveyardEffect() },
            });
            yield return boneMedium;

            var choirmaster = Creature("cult_choirmaster", "Cult Choirmaster", 4, 3, 4, "sensationalists", Rarity.Uncommon, "Human",
                Keyword.None, "Whenever another creature you control dies, each opponent loses 1 life and you gain 1 life.");
            choirmaster.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, Subject = TriggerSubject.You, OthersOnly = true, Effects = { new DrainEffect { Amount = 1 } },
            });
            yield return choirmaster;

            var spotlight = new CardDefinition
            {
                Id = "curse_of_the_spotlight", Name = "Curse of the Spotlight", Type = CardType.Curse, Cost = 4,
                Faction = "sensationalists", Rarity = Rarity.Rare,
                Text = "Attach to an opponent. Whenever a creature that player controls dies, they lose 2 life and you draw a card.",
                SpellTarget = TargetSpec.Opponent,
            };
            spotlight.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureDies, OnlyEnchantedPlayer = true,
                Effects = { new EventPlayerLosesLifeEffect { Amount = 2 }, new DrawCardsEffect { Count = 1 } },
            });
            yield return spotlight;

            yield return new CardDefinition
            {
                Id = "mass_hysteria", Name = "Mass Hysteria", Type = CardType.Sorcery, Cost = 6, Faction = "sensationalists",
                Rarity = Rarity.Rare,
                Text = "All creatures get -3/-3 until end of turn. You gain 1 life for each creature that dies this way.",
                SpellEffects = { new AllCreaturesGetEffect { Power = -3, Health = -3, GainLifePerDeath = 1 } },
            };

            var morbida = Creature("madame_morbida", "Madame Morbida, Star of the Séance", 8, 6, 7, "sensationalists",
                Rarity.Legendary, "Human", Keyword.Flying | Keyword.Lifelink,
                "Flying. Lifelink. Arrival: Return all creature cards with cost 3 or less from your graveyard to the battlefield.");
            morbida.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Effects = { new ReturnAllFromGraveyardToBattlefieldEffect { MaxCost = 3 } },
            });
            yield return morbida;

            // ---------------------------------------------------------------- Shadow Money Wizards
            var accountant = Creature("crooked_accountant", "Crooked Accountant", 3, 2, 3, wizards, Rarity.Common, "Wizard",
                Keyword.None, "Whenever you cast a spell that costs 5 or more, draw a card.");
            accountant.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.SpellCast, Subject = TriggerSubject.You, MinCost = 5, Effects = { new DrawCardsEffect { Count = 1 } },
            });
            yield return accountant;

            var embezzler = Creature("velvet_embezzler", "Velvet Embezzler", 4, 3, 3, wizards, Rarity.Rare, "Wizard", Keyword.Flying,
                "Flying. At the end of your turn, if you have 3 or more Gold, draw a card.");
            embezzler.Triggers.Add(new TriggeredAbility
            {
                // Decided 2026-10-10: 3 or more, the Standard Gold cap (was 5 under the Classic cap of 5).
                When = TriggerEvent.EndOfYourTurn,
                Condition = (s, db, controller, source) => s.GetPlayer(controller).Gold >= 3,
                Effects = { new DrawCardsEffect { Count = 1 } },
            });
            yield return embezzler;

            yield return new CardDefinition
            {
                Id = "pocket_change", Name = "Pocket Change", Type = CardType.Instant, Cost = 1, Faction = wizards, Rarity = Rarity.Common,
                Text = "Look at the top 2 cards of your deck. Put one in your hand and the other on the bottom. Invest 1: Put both in your hand.",
                SpellEffects = { new LookAtTopPutOneInHandRestOnBottomEffect { Count = 2, AllToHandIfInvested = true } },
                InvestCost = 1, // its effect is built into the main effect (checks EffectContext.Invested)
            };

            var collector = Creature("debt_collector", "Debt Collector", 4, 3, 4, wizards, Rarity.Uncommon, "Wizard", Keyword.None,
                "Arrival: Each opponent loses up to 2 Gold. You gain that much Gold.");
            collector.Triggers.Add(new TriggeredAbility { When = TriggerEvent.Arrival, Effects = { new StealGoldEffect { Amount = 2 } } });
            yield return collector;

            var shark = Creature("card_shark", "Card Shark", 3, 3, 3, wizards, Rarity.Common, "Wizard", Keyword.None,
                "Whenever you cast your second spell each turn, draw a card.");
            shark.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.SpellCast, Subject = TriggerSubject.You, NthSpellThisTurn = 2, Effects = { new DrawCardsEffect { Count = 1 } },
            });
            yield return shark;

            var taxOffice = new CardDefinition
            {
                Id = "tax_office", Name = "Tax Office", Type = CardType.Relic, Cost = 5, Faction = wizards, Rarity = Rarity.Uncommon,
                Text = "Whenever an opponent casts a spell, they lose 1 Gold. If they couldn't, you gain 1 Gold.",
            };
            taxOffice.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.SpellCast, Subject = TriggerSubject.Opponents, Effects = { new TaxOfficeEffect() },
            });
            yield return taxOffice;

            foreach (var c in V01ChoiceCards()) yield return c;
        }

        /// <summary>Batch F: choices during resolution and divided damage.</summary>
        private static IEnumerable<CardDefinition> V01ChoiceCards()
        {
            const string wizards = "shadow_money_wizards";
            const string sens = "sensationalists";

            var imp = Creature("ledger_imp", "Ledger Imp", 1, 1, 2, wizards, Rarity.Common, "Imp", Keyword.None,
                "Arrival: You may lose 2 life. If you do, gain 1 Gold.");
            imp.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival,
                Effects =
                {
                    new YouMayEffect
                    {
                        Prompt = "Lose 2 life to gain 1 Gold?",
                        Then = { new LoseLifeEffect { Amount = 2 }, new GainGoldEffect { Amount = 1 } },
                    },
                },
            });
            yield return imp;

            var dealer = Creature("the_dealer", "The Dealer", 6, 4, 6, wizards, Rarity.Rare, "Wizard", Keyword.Flying,
                "Flying. At the start of your turn, each opponent may give you 2 Gold. For each one who doesn't, draw a card.");
            dealer.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.StartOfYourTurn,
                Effects = { new EachOpponentMayPayGoldEffect { Gold = 2, IfNot = { new DrawCardsEffect { Count = 1 } } } },
            });
            yield return dealer;

            yield return new CardDefinition
            {
                Id = "everything_has_a_price", Name = "Everything Has a Price", Type = CardType.Sorcery, Cost = 9, Faction = wizards,
                Rarity = Rarity.Legendary,
                Text = "For each opponent, gain control of the creature they control with the highest cost. That player gains 5 Gold and draws 2 cards.",
                SpellEffects = { new EverythingHasAPriceEffect { Gold = 5, Cards = 2 } },
            };

            var shaman = Creature("goober_shaman", "Goober Shaman", 2, 2, 2, "goobers", Rarity.Uncommon, "Goober", Keyword.None,
                "Arrival: You may discard a card. If you do, draw a card.");
            shaman.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival, Effects = { new DiscardChoiceEffect { Then = { new DrawCardsEffect { Count = 1 } } } },
            });
            yield return shaman;

            yield return new CardDefinition
            {
                Id = "firecracker_volley", Name = "Firecracker Volley", Type = CardType.Sorcery, Cost = 3, Faction = "goobers",
                Rarity = Rarity.Uncommon,
                Text = "Deal 3 damage divided as you choose among any number of creatures and/or players.",
                DividedDamage = 3,
                // "any number" can't be more than 3: each target gets at least 1.
                SpellTargets =
                {
                    TargetSlot.Of(TargetSpec.AnyTarget),
                    TargetSlot.Of(TargetSpec.AnyTarget, optional: true),
                    TargetSlot.Of(TargetSpec.AnyTarget, optional: true),
                },
                SpellEffects = { new DealDividedDamageEffect() },
            };

            yield return new CardDefinition
            {
                Id = "midnight_ritual", Name = "Midnight Ritual", Type = CardType.Instant, Cost = 1, Faction = sens, Rarity = Rarity.Uncommon,
                Text = "As an extra cost, sacrifice a creature. Draw 2 cards. "
                       + "Invest 2: Return a creature card with cost 3 or less from your graveyard to the battlefield.",
                SacrificeCreatureCost = true,
                SpellEffects = { new DrawCardsEffect { Count = 2 } },
                InvestCost = 2,
                InvestEffects = { new ChooseCreatureCardToBattlefieldEffect { OnlyYourGraveyard = true, MaxCost = 3 } },
            };

            yield return new CardDefinition
            {
                Id = "spectacle_of_blood", Name = "Spectacle of Blood", Type = CardType.Sorcery, Cost = 3, Faction = sens,
                Rarity = Rarity.Uncommon, Text = "Each opponent sacrifices a creature.",
                SpellEffects = { new SacrificeChoiceEffect { Who = Chooser.EachOpponent } },
            };

            var ringmaster = Creature("midnight_ringmaster", "Midnight Ringmaster", 5, 4, 5, sens, Rarity.Common, "Human",
                Keyword.Lifelink, "Lifelink. Arrival: You may sacrifice another creature. If you do, draw 2 cards.");
            ringmaster.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Arrival,
                Effects = { new SacrificeChoiceEffect { OthersOnly = true, Optional = true, Then = { new DrawCardsEffect { Count = 2 } } } },
            });
            yield return ringmaster;

            yield return new CardDefinition
            {
                Id = "exhumation_broadcast", Name = "Exhumation Broadcast", Type = CardType.Sorcery, Cost = 5, Faction = sens,
                Rarity = Rarity.Rare, Text = "Choose a creature card in each graveyard. Put them onto the battlefield under your control.",
                SpellEffects = { new ChooseCreatureCardToBattlefieldEffect() },
            };

            var headliner = Creature("abyssal_headliner", "Abyssal Headliner", 6, 6, 6, sens, Rarity.Rare, "Horror", Keyword.Flying,
                "Flying. At the start of your turn, sacrifice another creature or lose 3 life.");
            headliner.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.StartOfYourTurn,
                // Choosing no creature means losing 3 life; with no other creature, you lose the life.
                Effects = { new SacrificeChoiceEffect { OthersOnly = true, Optional = true, Else = { new LoseLifeEffect { Amount = 3 } } } },
            });
            yield return headliner;
        }
    }
}
