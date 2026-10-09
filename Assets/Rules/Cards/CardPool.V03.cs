using System.Collections.Generic;

namespace RestartedTavern.Rules.Cards
{
    /// <summary>
    /// Set v0.3 (approved 2026-10-09): mana scarcity. Each faction gets 2 card draw, 2 mana sinks (X spells, repeatable
    /// abilities, a big Invest) and 2 finishers (X burn or drains); see docs/cards/*.md, numbers 31-36.
    /// </summary>
    public static partial class CardPool
    {
        private static CardDefinition Spell(string id, string name, CardType type, int cost, string faction, Rarity rarity, string text,
            params Effect[] effects)
        {
            var def = new CardDefinition { Id = id, Name = name, Type = type, Cost = cost, Faction = faction, Rarity = rarity, Text = text };
            def.SpellEffects.AddRange(effects);
            return def;
        }

        private static CardDefinition XSpell(string id, string name, CardType type, int cost, string faction, Rarity rarity, string text,
            params Effect[] effects)
        {
            var def = Spell(id, name, type, cost, faction, rarity, text, effects);
            def.XCost = true;
            return def;
        }

        private static CardDefinition Relic(string id, string name, int cost, string faction, Rarity rarity, string text) =>
            new CardDefinition { Id = id, Name = name, Type = CardType.Relic, Cost = cost, Faction = faction, Rarity = rarity, Text = text };

        private static IEnumerable<CardDefinition> V03Cards()
        {
            // ---------------------------------------------------------------- Shadow Money Wizards
            var clerk = Creature("ledger_clerk", "Ledger Clerk", 2, 1, 3, Wizards, Rarity.Common, "Wizard", Keyword.None,
                "(3), Tap: Draw a card.");
            clerk.Abilities.Add(new ActivatedAbility { Cost = 3, TapCost = true, Effects = { new DrawCardsEffect { Count = 1 } }, Text = "(3), Tap: Draw a card." });
            yield return clerk;

            yield return XSpell("insider_trading", "Insider Trading", CardType.Instant, 2, Wizards, Rarity.Uncommon,
                "Draw X cards. Each opponent gains 1 Gold.",
                new DrawCardsEffect { CountIsX = true }, new GainGoldEffect { Amount = 1, EachOpponent = true });

            var eviction = XSpell("eviction_notice", "Eviction Notice", CardType.Instant, 1, Wizards, Rarity.Common,
                "Return target creature with cost X or less to its owner's hand. Draw a card.",
                new ReturnToHandEffect(), new DrawCardsEffect { Count = 1 });
            eviction.SpellTarget = TargetSpec.Creature;
            eviction.TargetMaxCostIsX = true;
            yield return eviction;

            var audit = Spell("audit_the_books", "Audit the Books", CardType.Sorcery, 3, Wizards, Rarity.Uncommon,
                "Return target creature with cost 4 or less to its owner's hand. Invest 3: Draw two cards.", new ReturnToHandEffect());
            audit.SpellTargets.Add(new TargetSlot { Spec = TargetSpec.Creature, MaxCost = 4 });
            audit.InvestCost = 3;
            audit.InvestEffects.Add(new DrawCardsEffect { Count = 2 });
            yield return audit;

            yield return XSpell("foreclosure", "Foreclosure", CardType.Sorcery, 3, Wizards, Rarity.Rare,
                "Each opponent loses X life. You gain X Gold.",
                new EachOpponentLosesLifeEffect { AmountIsX = true }, new GainGoldEffect { AmountIsX = true });

            var shark = Creature("loan_shark", "Loan Shark", 6, 3, 5, Wizards, Rarity.Rare, "Wizard", Keyword.Flying,
                "Flying. At the end of your turn, each opponent loses life equal to the Gold you have.");
            shark.Triggers.Add(new TriggeredAbility { When = TriggerEvent.EndOfYourTurn, Effects = { new EachOpponentLosesLifeEqualToYourGoldEffect() } });
            yield return shark;

            // ---------------------------------------------------------------- Goobers
            yield return Spell("dumpster_dive", "Dumpster Dive", CardType.Instant, 2, "goobers", Rarity.Common,
                "Discard any number of cards, then draw that many cards plus one.", new RummageAnyEffect());

            var bookie = Creature("goober_bookie", "Goober Bookie", 3, 2, 3, "goobers", Rarity.Uncommon, "Goober", Keyword.Haste,
                "Haste. Whenever this attacks, you may discard a card. If you do, draw two cards.");
            bookie.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.Attacks, Effects = { new DiscardChoiceEffect { Then = { new DrawCardsEffect { Count = 2 } } } },
            });
            yield return bookie;

            yield return XSpell("goober_avalanche", "Goober Avalanche", CardType.Sorcery, 1, "goobers", Rarity.Uncommon,
                "Create X 1/1 Goobers with Haste.",
                new CreateTokensEffect { TokenId = GooberToken, CountIsX = true, GrantUntilEndOfTurn = Keyword.Haste });

            var stand = Relic("fireworks_stand", "Fireworks Stand", 2, "goobers", Rarity.Common,
                "(3), Sacrifice a creature: Deal 2 damage to any target.");
            stand.Abilities.Add(new ActivatedAbility
            {
                Cost = 3, SacrificeCreatureCost = true, Targets = { TargetSlot.Of(TargetSpec.AnyTarget) },
                Effects = { new DealDamageEffect { Amount = 2 } }, Text = "(3), Sacrifice a creature: Deal 2 damage to any target.",
            });
            yield return stand;

            var boom = XSpell("big_boom", "Big Boom", CardType.Sorcery, 1, "goobers", Rarity.Rare, "Deal X damage to any target.",
                new DealDamageEffect { AmountIsX = true });
            boom.SpellTarget = TargetSpec.AnyTarget;
            yield return boom;

            yield return XSpell("grand_finale", "Grand Finale", CardType.Sorcery, 3, "goobers", Rarity.Rare,
                "Deal X damage to each opponent and 1 damage to each creature.",
                new DealDamageToEachOpponentEffect { AmountIsX = true }, new DealDamageToEachCreatureEffect { Amount = 1 });

            // ---------------------------------------------------------------- Sensationalists
            yield return Spell("blood_oath", "Blood Oath", CardType.Sorcery, 2, "sensationalists", Rarity.Common,
                "Draw two cards. You lose 2 life.", new DrawCardsEffect { Count = 2 }, new LoseLifeEffect { Amount = 2 });

            var hotline = Creature("seance_hotline", "Séance Hotline", 3, 2, 3, "sensationalists", Rarity.Uncommon, "Human", Keyword.None,
                "(2), Pay 1 life: Draw a card. Activate only once each turn.");
            hotline.Abilities.Add(new ActivatedAbility
            {
                Cost = 2, LifeCost = 1, OncePerTurn = true, Effects = { new DrawCardsEffect { Count = 1 } }, Text = "(2), Pay 1 life: Draw a card.",
            });
            yield return hotline;

            var wither = XSpell("wither_away", "Wither Away", CardType.Instant, 1, "sensationalists", Rarity.Common,
                "Target creature gets -X/-X until end of turn.", new PumpTargetEffect { PowerPerX = -1, HealthPerX = -1 });
            wither.SpellTarget = TargetSpec.Creature;
            yield return wither;

            var casket = Spell("open_casket", "Open Casket", CardType.Sorcery, 3, "sensationalists", Rarity.Uncommon,
                "Return a creature card with cost 3 or less from your graveyard to your hand. Invest 3: Put it onto the battlefield instead.",
                new ReturnGraveyardCreatureEffect { ToBattlefieldIfInvested = true });
            casket.SpellTargets.Add(new TargetSlot { Spec = TargetSpec.CreatureCardInYourGraveyard, MaxCost = 3 });
            casket.InvestCost = 3;
            yield return casket;

            yield return XSpell("final_broadcast", "Final Broadcast", CardType.Sorcery, 2, "sensationalists", Rarity.Rare,
                "Each opponent loses X life and you gain X life.", new DrainEffect { AmountIsX = true });

            var primeTime = new CardDefinition
            {
                Id = "curse_of_prime_time", Name = "Curse of Prime Time", Type = CardType.Curse, Cost = 4, Faction = "sensationalists",
                Rarity = Rarity.Rare, SpellTarget = TargetSpec.Opponent,
                Text = "Attach to an opponent. At the start of that player's turn, they lose 1 life. (3): That player loses 1 life and you gain 1 life.",
            };
            primeTime.Triggers.Add(new TriggeredAbility { When = TriggerEvent.StartOfEnchantedPlayersTurn, Effects = { new EventPlayerLosesLifeEffect { Amount = 1 } } });
            primeTime.Abilities.Add(new ActivatedAbility
            {
                Cost = 3, Effects = { new AttachedPlayerLosesLifeEffect { Amount = 1, YouGainLife = true } },
                Text = "(3): That player loses 1 life and you gain 1 life.",
            });
            yield return primeTime;

            // ---------------------------------------------------------------- Evergrowing Wild
            yield return Spell("gift_of_the_grove", "Gift of the Grove", CardType.Sorcery, 3, "evergrowing_wild", Rarity.Common,
                "Draw a card for each creature with 5 or more Power you control (at least one).", new DrawPerBigCreatureEffect { MinPower = 5 });

            var hole = Relic("watering_hole", "Watering Hole", 2, "evergrowing_wild", Rarity.Uncommon,
                "Whenever a creature with 5 or more Power enters under your control, draw a card. (2): Heal 2 from a creature.");
            hole.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.CreatureEnters, Subject = TriggerSubject.You, MinPower = 5, Effects = { new DrawCardsEffect { Count = 1 } },
            });
            hole.Abilities.Add(new ActivatedAbility
            {
                Cost = 2, Targets = { TargetSlot.Of(TargetSpec.Creature) }, Effects = { new HealEffect { Amount = 2 } }, Text = "(2): Heal 2 from a creature.",
            });
            yield return hole;

            var overgrowth = XSpell("overgrowth", "Overgrowth", CardType.Sorcery, 1, "evergrowing_wild", Rarity.Common,
                "Put X +1/+1 counters on target creature you control.", new AddCountersEffect { CountIsX = true });
            overgrowth.SpellTarget = TargetSpec.CreatureYouControl;
            yield return overgrowth;

            var grower = Creature("mossgut_grower", "Mossgut Grower", 3, 2, 3, "evergrowing_wild", Rarity.Uncommon, "Beast", Keyword.Trample,
                "Trample. (3): Put a +1/+1 counter on this creature.");
            grower.Abilities.Add(new ActivatedAbility { Cost = 3, Effects = { new AddCountersToSourceEffect { Count = 1 } }, Text = "(3): Put a +1/+1 counter on this creature." });
            yield return grower;

            var call = XSpell("call_of_the_deep", "Call of the Deep", CardType.Sorcery, 2, "evergrowing_wild", Rarity.Rare,
                "Target creature you control gets +X/+X and Trample until end of turn. Then it fights up to one target creature you don't control.",
                new PumpTargetEffect { PowerPerX = 1, HealthPerX = 1, Grants = Keyword.Trample }, new FightEffect());
            call.SpellTargets.Add(TargetSlot.Of(TargetSpec.CreatureYouControl));
            call.SpellTargets.Add(TargetSlot.Of(TargetSpec.CreatureYouDontControl, optional: true));
            yield return call;

            var titan = Creature("rampaging_titan", "Rampaging Titan", 7, 7, 7, "evergrowing_wild", Rarity.Rare, "Beast", Keyword.Trample,
                "Trample. (X): This gets +X/+0 until end of turn.");
            titan.Abilities.Add(new ActivatedAbility { HasX = true, Effects = { new PumpSourceEffect { PowerPerX = 1 } }, Text = "(X): This gets +X/+0 until end of turn." });
            yield return titan;

            // ---------------------------------------------------------------- Glitterworld
            var feed = Relic("market_data_feed", "Market Data Feed", 2, "glitterworld", Rarity.Uncommon,
                "Whenever an Equipment becomes attached to a creature you control, draw a card. This triggers at most once each turn.");
            feed.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.EquipmentAttached, Subject = TriggerSubject.You, MaxPerTurn = 1, Effects = { new DrawCardsEffect { Count = 1 } },
            });
            yield return feed;

            var analyst = Creature("overclocked_analyst", "Overclocked Analyst", 3, 2, 3, "glitterworld", Rarity.Common, "Construct", Keyword.None,
                "(3), Tap: Draw a card, then discard a card.");
            analyst.Abilities.Add(new ActivatedAbility
            {
                Cost = 3, TapCost = true, Effects = { new DrawCardsEffect { Count = 1 }, new DiscardCardsEffect { Count = 1 } },
                Text = "(3), Tap: Draw a card, then discard a card.",
            });
            yield return analyst;

            yield return XSpell("arc_cascade", "Arc Cascade", CardType.Sorcery, 1, "glitterworld", Rarity.Uncommon,
                "Deal X damage divided as you choose among any number of creatures and/or opponents.", new DivideXDamageEffect());

            yield return Equipment("turret_rig", "Turret Rig", 2, "glitterworld", Rarity.Common, 2,
                new AttachedCreatureModifier
                {
                    Abilities =
                    {
                        new ActivatedAbility
                        {
                            Cost = 2, TapCost = true, Targets = { TargetSlot.Of(TargetSpec.AnyTarget) },
                            Effects = { new DealDamageEffect { Amount = 1 } }, Text = "(2), Tap: Deal 1 damage to any target.",
                        },
                    },
                },
                "Equipped creature has \"(2), Tap: Deal 1 damage to any target.\" Equip 2.");

            var laser = XSpell("orbital_laser", "Orbital Laser", CardType.Sorcery, 2, "glitterworld", Rarity.Rare,
                "Deal X damage to any target. If X is 5 or more, also deal 1 damage to each enemy creature.",
                new DealDamageEffect { AmountIsX = true }, new DealDamageToEachEnemyCreatureEffect { Amount = 1, MinX = 5 });
            laser.SpellTarget = TargetSpec.AnyTarget;
            yield return laser;

            var uplink = Relic("satellite_uplink", "Satellite Uplink", 6, "glitterworld", Rarity.Rare, "(3), Tap: Deal 2 damage to each opponent.");
            uplink.Abilities.Add(new ActivatedAbility
            {
                Cost = 3, TapCost = true, Effects = { new DealDamageToEachOpponentEffect { Amount = 2 } }, Text = "(3), Tap: Deal 2 damage to each opponent.",
            });
            yield return uplink;

            // ---------------------------------------------------------------- Neutral
            yield return Spell("last_orders", "Last Orders", CardType.Sorcery, 2, "neutral", Rarity.Common,
                "Draw two cards, then discard a card.", new DrawCardsEffect { Count = 2 }, new DiscardCardsEffect { Count = 1 });

            var barkeep = Creature("night_shift_barkeep", "Night Shift Barkeep", 3, 2, 4, "neutral", Rarity.Common, "Human", Keyword.None,
                "(4): Draw a card. Activate only once each turn.");
            barkeep.Abilities.Add(new ActivatedAbility { Cost = 4, OncePerTurn = true, Effects = { new DrawCardsEffect { Count = 1 } }, Text = "(4): Draw a card." });
            yield return barkeep;

            var champion = Creature("tavern_brawl_champion", "Tavern Brawl Champion", 4, 3, 4, "neutral", Rarity.Uncommon, "Human", Keyword.None,
                "(2): This gets +1/+0 until end of turn.");
            champion.Abilities.Add(new ActivatedAbility { Cost = 2, Effects = { new PumpSourceEffect { Power = 1 } }, Text = "(2): This gets +1/+0 until end of turn." });
            yield return champion;

            var mercenary = Creature("gilded_mercenary", "Gilded Mercenary", 4, 4, 4, "neutral", Rarity.Uncommon, "Human", Keyword.None,
                "Invest 3: This enters with two +1/+1 counters and Trample.");
            mercenary.InvestCost = 3;
            mercenary.InvestEffects.Add(new AddCountersToSourceEffect { Count = 2 });
            mercenary.InvestEffects.Add(new GrantKeywordToSourceEffect { Keyword = Keyword.Trample });
            yield return mercenary;

            var legend = Creature("tavern_legend", "Tavern Legend", 6, 5, 6, "neutral", Rarity.Rare, "Human", Keyword.None,
                "(X): This gets +X/+0 and Trample until end of turn.");
            legend.Abilities.Add(new ActivatedAbility
            {
                HasX = true, Effects = { new PumpSourceEffect { PowerPerX = 1, Grants = Keyword.Trample } }, Text = "(X): This gets +X/+0 and Trample until end of turn.",
            });
            yield return legend;

            var bell = Relic("closing_bell", "Closing Bell", 3, "neutral", Rarity.Rare,
                "At the start of your turn, if each opponent has 10 or less life, each opponent loses 2 life.");
            bell.Triggers.Add(new TriggeredAbility
            {
                When = TriggerEvent.StartOfYourTurn,
                Condition = (s, db, controller, source) =>
                {
                    foreach (var p in s.Players)
                        if (!p.HasLost && s.AreOpponents(controller, p.Id) && p.Life > 10) return false;
                    return true;
                },
                Effects = { new EachOpponentLosesLifeEffect { Amount = 2 } },
            });
            yield return bell;
        }
    }
}
