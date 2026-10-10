# Tavern Dwellers — Card List

One Tavern Dweller per faction pair (10 total). The Tavern Dweller **is your face**: it starts in the Tavern Dweller zone, has your life total, and never attacks or blocks (GAME_DESIGN §9). (Called **Patrons** until 2026-10-09.)
- **Passive**: always on.
- **Power (N)**: costs N, paid like any activated ability: **Gold first, then mana** (GAME_DESIGN §5.2, since 2026-10-10). Mana lasts the whole round, so leftover mana can pay for it on other players' turns too. **Once each turn** (MTG: once in each turn, yours or an opponent's), at instant speed (it goes on the Chain).
- All 10 are implemented in the engine (`Assets/Rules/Cards/CardPool.TavernDwellers.cs`).

**Token:** *Spawn*, a 2/2 Creature: Spawn (made by The Rotmother).

Status tags: ✅ approved · 🟡 draft · ✏️ revised in the final pass

---

| # | Pair | Tavern Dweller | Passive | Power | Status |
|---|---|---|---|---|---|
| 1 | Wizards + Goobers | **Grizzle Coinflick**, goblin pyromancer-for-hire | Whenever you cast a spell that costs **5 or more**, create a 1/1 Goober. | (2) Deal 1 damage to any target. | ✅ |
| 2 | Wizards + Sensationalists | **Madame Vesper**, the debt collector | Whenever a creature an opponent controls dies, gain 1 Gold. | (3) Draw a card and lose 2 life. | ✅ |
| 3 | Wizards + Wild | **Old Mossbank**, the druid banker | Your **spells and creatures** that cost 6 or more cost 1 less. | (2) Give a creature +2/+2 until end of turn. | ✅ |
| 4 | Wizards + Glitterworld | **Auditor Prime**, a construct accountant | Your Invest **and Equip** costs are 1 lower (minimum 1). | ✏️ (2) Draw a card. Activate only if you have **3 or more Gold**. | ✅ |
| 5 | Goobers + Sensationalists | **Skabba**, goblin cult chieftain | Whenever one of your creatures dies, deal 1 damage to each opponent. **This triggers at most 3 times each turn.** | (1) Sacrifice a creature: Draw a card. | ✅ |
| 6 | Goobers + Wild | **Mukk the Grub King**, a goblin riding a giant beast | Your creatures with Trample get +1/+0. | ✏️ (3) Target creature you control **with Trample** fights target creature you don't control. | ✅ |
| 7 | Goobers + Glitterworld | **Sparkwrench**, goblin mechanic | Your Equipment spells cost 1 less. | ✏️ (2) Attach up to one target Equipment you control to target creature you control. If no Equipment became attached, that creature gets **+1/+1** until end of turn. | ✅ |
| 8 | Sensationalists + Wild | **The Rotmother**, a jungle witch of rot and rebirth | Whenever a creature with 5 or more Power you control dies, create a 2/2 Spawn. | (3) Return a creature card from your graveyard to your hand, then lose 3 life. | ✅ |
| 9 | Sensationalists + Glitterworld | **Vox Nocturne**, a cult leader who broadcasts horror live on the city's screens | Whenever a creature an opponent controls dies, you gain 1 life. | (2) Deal 1 damage to a creature. If it dies, draw a card. | ✅ |
| 10 | Wild + Glitterworld | **Keeper Z-00**, the city's zookeeper unit | Your creatures with 5 or more Health enter with a +1/+1 counter. | (2) Heal 3 from a creature. | ✅ |

✅ **All 10 approved (v0.1)**

## Power changes (2026-10-10)
In simulations Mukk, Sparkwrench and Auditor Prime hardly used their Powers: they only did something in narrow board states. The user picked new Powers:
- **Mukk the Grub King**: was "(2) A creature you control gains Trample until end of turn". Now (3) a Trample creature fights an enemy creature: removal that leans on the passive (+1/+0 counts in the fight).
- **Sparkwrench**: was "(2) Attach an Equipment you control to another creature you control", useless without Equipment. Now it moves an Equipment, or pumps +1/+1 when none is attached.
- **Auditor Prime**: was "(1) Look at the top card of your deck. You may put it on the bottom." Now (2) draw a card, only while you have 3 or more Gold (checked before paying; it's paid Gold first). Rewards sitting at the Gold cap.

## Final-pass changes
1. **Grizzle**: 6+ → **5+**. After Call the Horde was cut, Goobers had no spells costing 6+. Now Overrun the Gates, Scrapheap Inferno and Golden Handshake count too, and it lines up with Crooked Accountant's "5 or more".
2. **Old Mossbank**: it discounted big *creatures* only, which did nothing for the Wizards half of the pair. Now it also discounts big *spells* (Grand Illusion, The Grand Ledger, Everything Has a Price).
3. **Auditor Prime**: Glitterworld has no Invest cards, so the passive only helped Wizards. It now also lowers **Equip** costs.
4. **Skabba**: Goober tokens + Snik's copies + Goober Demolisher could deal 10+ damage from a single board wipe. Capped at 3 triggers per turn.
