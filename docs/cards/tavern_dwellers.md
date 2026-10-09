# Tavern Dwellers — Card List

One Tavern Dweller per faction pair (10 total). The Tavern Dweller **is your face**: it starts in the Tavern Dweller zone, has your life total, and never attacks or blocks (GAME_DESIGN §9). (Called **Patrons** until 2026-10-09.)
- **Passive**: always on.
- **Power (N)**: costs N, paid like any activated ability: **mana first, then Gold** (GAME_DESIGN §5.2). On other players' turns you have no mana, so it's paid with Gold there. **Once each turn** (MTG: once in each turn, yours or an opponent's), at instant speed (it goes on the Chain).
- All 10 are implemented in the engine (`Assets/Rules/Cards/PrototypeCards.TavernDwellers.cs`).

**Token:** *Spawn*, a 2/2 Creature: Spawn (made by The Rotmother).

Status tags: ✅ approved · 🟡 draft · ✏️ revised in the final pass

---

| # | Pair | Tavern Dweller | Passive | Power | Status |
|---|---|---|---|---|---|
| 1 | Wizards + Goobers | **Grizzle Coinflick**, goblin pyromancer-for-hire | Whenever you cast a spell that costs **5 or more**, create a 1/1 Goober. | (2) Deal 1 damage to any target. | ✅ |
| 2 | Wizards + Sensationalists | **Madame Vesper**, the debt collector | Whenever a creature an opponent controls dies, gain 1 Gold. | (3) Draw a card and lose 2 life. | ✅ |
| 3 | Wizards + Wild | **Old Mossbank**, the druid banker | Your **spells and creatures** that cost 6 or more cost 1 less. | (2) Give a creature +2/+2 until end of turn. | ✅ |
| 4 | Wizards + Glitterworld | **Auditor Prime**, a construct accountant | Your Invest **and Equip** costs are 1 lower (minimum 1). | (1) Look at the top card of your deck. You may put it on the bottom. | ✅ |
| 5 | Goobers + Sensationalists | **Skabba**, goblin cult chieftain | Whenever one of your creatures dies, deal 1 damage to each opponent. **This triggers at most 3 times each turn.** | (1) Sacrifice a creature: Draw a card. | ✅ |
| 6 | Goobers + Wild | **Mukk the Grub King**, a goblin riding a giant beast | Your creatures with Trample get +1/+0. | (2) A creature you control gains Trample until end of turn. | ✅ |
| 7 | Goobers + Glitterworld | **Sparkwrench**, goblin mechanic | Your Equipment spells cost 1 less. | (2) Attach an Equipment you control to another creature you control. | ✅ |
| 8 | Sensationalists + Wild | **The Rotmother**, a jungle witch of rot and rebirth | Whenever a creature with 5 or more Power you control dies, create a 2/2 Spawn. | (3) Return a creature card from your graveyard to your hand, then lose 3 life. | ✅ |
| 9 | Sensationalists + Glitterworld | **Vox Nocturne**, a cult leader who broadcasts horror live on the city's screens | Whenever a creature an opponent controls dies, you gain 1 life. | (2) Deal 1 damage to a creature. If it dies, draw a card. | ✅ |
| 10 | Wild + Glitterworld | **Keeper Z-00**, the city's zookeeper unit | Your creatures with 5 or more Health enter with a +1/+1 counter. | (2) Heal 3 from a creature. | ✅ |

✅ **All 10 approved (v0.1)**

## Final-pass changes
1. **Grizzle**: 6+ → **5+**. After Call the Horde was cut, Goobers had no spells costing 6+. Now Overrun the Gates, Scrapheap Inferno and Golden Handshake count too, and it lines up with Crooked Accountant's "5 or more".
2. **Old Mossbank**: it discounted big *creatures* only, which did nothing for the Wizards half of the pair. Now it also discounts big *spells* (Grand Illusion, The Grand Ledger, Everything Has a Price).
3. **Auditor Prime**: Glitterworld has no Invest cards, so the passive only helped Wizards. It now also lowers **Equip** costs.
4. **Skabba**: Goober tokens + Snik's copies + Goober Demolisher could deal 10+ damage from a single board wipe. Capped at 3 triggers per turn.
