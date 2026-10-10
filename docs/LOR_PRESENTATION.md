# LoR-Style Presentation: Research and Plan

Status: research doc (2026-10-10), kept for the reasoning. Steps 1–4 of §4 are built (see §6); step 5 (juice: particles, sound) is for a later release. The client as built is described in [CLIENT_DESIGN.md](CLIENT_DESIGN.md).

**The problem.** Playtest 2026-10-10 showed that the table is hard to read:
- It isn't obvious whose action it is.
- You can't see what just happened, or that the opponent passed.
- You can't tell when you may still respond, for example after blocks.
- Cost changes are invisible (Sparkwrench made Gilded Knuckles free and it looked like a bug).

Legends of Runeterra (LoR) solves exactly these problems. This doc lists how it does that and maps each technique to our engine.

---

## 1. What LoR does, and why it reads so clearly

LoR has one rule: **every state change is shown as a short, distinct beat, and input waits until the beat is over.** Nothing changes silently.

| # | Moment | What LoR shows | What the player learns |
|---|---|---|---|
| 1 | **Whose action** | One big button on the right. It is **blue/lit** when you can act, **green** when passing is your only option, and **grey with "Opponent's Turn"** when the opponent has priority. It is **blank while an animation plays**. The label is the verb: PASS · END ROUND · ATTACK · BLOCK · SKIP BLOCK · OK. | You never have to look anywhere else. |
| 2 | **Opponent passed** | A "Pass" callout next to the opponent's portrait, and the button switches to **END ROUND**. | "If I pass now, the round ends." |
| 3 | **Round start** | A "Round N" banner. The **attack token** flips to the attacker with a clang (sword for the attacker, shield for the defender). A new mana gem crystallizes, then **all gems refill one by one** with a chime. Both players draw. | "New round, I attack, I have N mana." |
| 4 | **Round end** | Unspent gems **fly into the spell-mana slots** (up to 3, purple). | Banking is visible, not just a number change. |
| 5 | **Spending** | Hovering or dragging a card **previews which gems it will use** (spell mana first). Paying drains them top-down. Costs that changed are recolored on the card. | Costs and discounts are explained by the UI. |
| 6 | **Cards played** | The opponent's card **flies to the centre, big, for about a second**, then drops onto the spell stack. Stack cards draw **target lines to their targets** while they wait. | "They cast X at my Y." |
| 7 | **Resolution** | When both pass, the top stack card pulses, **fires a projectile or flash at its target**, a damage number pops, and the card fades. A countered card shatters. | Cause → effect, one at a time. |
| 8 | **Attack** | Attackers **slide into the lane** with a whoosh. The defender gets a BLOCK prompt, and blockers slide in opposite their attacker. | "Who fights whom." |
| 9 | **After blocks** | The attacker gets an explicit response window before damage (the button lights up again). | "I can still pump or remove." This is the window your Titan bug report was about. |
| 10 | **Combat damage** | Pairs **lunge at each other**, damage numbers pop, and dead units crumble. Unblocked damage hits the Nexus: it shakes, flashes red and the number ticks down. | Damage is never just a changed number. |
| 11 | **Triggers** | A small icon pops on the source unit, and the triggered skill goes onto the stack as its own mini card. | "Why did that happen?" |
| 12 | **Big moments** | A champion level-up gets a full-screen splash. | Rare events feel rare. |
| 13 | **History** | A column of recent plays as mini cards on the left. Hover one to see who did what and to whom. | You can catch up after looking away. |
| 14 | **Audio** | Distinct sounds for: your turn, opponent passed, the round, the attack token, damage and death. | You can tell the state without looking. |

Sources: [Inven, "The LoR board explained"](https://www.invenglobal.com/articles/10266/the-legends-of-runeterra-board-explained), [LoR wiki: Board](https://wiki.leagueoflegends.com/en-us/LoR:Board), [ludo.guide: understanding the UI](https://www.ludo.guide/guide/legends-of-runeterra/understanding-the-ui), [Riot on clarity and input blocking](https://www.invenglobal.com/articles/10641/the-current-state-of-legends-of-runeterra-and-what-riot-has-planned-next).

---

## 2. Mapping to Restarted Tavern

Our rules already have the same structure:
- A round pool of mana.
- **Gold = spell mana**: cap 3, spent first, banked from unspent mana.
- A rotating attack token, alternating actions, and a Chain (the stack).

So almost every LoR technique carries over 1:1.

| LoR technique | Our engine event or state | Status | What's needed |
|---|---|---|---|
| Button colours and verbs | `TableControls.Main` (`ButtonMode`) | Labels ✅ | Colour per mode (lit / pass-only / grey "Opponent's action" / blank while animating), and a pulse when the action comes to you. |
| "Opponent passed" | — | ❌ | **Engine: a `PriorityPassedEvent`.** The client can't see passes today; it only sees `PassesInRow`. Then show a "PASSED" chip on that portrait until the next action, and label the button "End round" with the note "Opponent passed". |
| Round banner + token flip + gem refill | `RoundStartedEvent`, `ManaChangedEvent`, `StepStartedEvent` | Events ✅ | A banner beat (Long), a token flip beat, and gems refilling one per 60 ms. |
| Gold banking | `GoldBankedEvent`, `GoldChangedEvent` | Events ✅ | Gems fly from the mana column into the Gold slots. |
| Spend preview | `Costs.SpellCost`, `Payment` | ✅ | On hover or drag: flash the gems (Gold first) that the card would use. |
| Changed costs visible | `CardView.Cost` vs `PrintedCost` | **✅ done this session** | Green = cheaper, red = dearer, on hand cards. |
| Card reveal | `SpellCastEvent`, `AbilityActivatedEvent` | Events ✅ | The opponent's card zooms to the centre for about 0.8 s (skip for our own plays), then goes to the Chain. |
| Chain as cards + target lines | `ChainItem` targets | Partly (text list) | Mini cards in the Chain column, with a line from each item to its targets. |
| Resolution beat | `ChainItemResolvedEvent`, `DamageDealtEvent`, `CounteredEvent`, `FizzledEvent` | Events ✅ | Pulse → projectile → number pop → fade, or shatter when countered or fizzled. |
| Lane slide-in | `AttackerDeclaredEvent`, `BlockerDeclaredEvent` | Events ✅ | Tween units into the lane. |
| After-blocks window | Engine priority in `Step.DeclareBlockers` | Engine ✅, client unclear | Banner: "Blocks are in. Respond before damage". The button reads **"To damage"** instead of "Continue". |
| Damage, death, Nexus hit | `DamageDealtEvent`, `CreatureDiedEvent`, `LifeChangedEvent` | Events ✅ | Lunge, number pop, crumble; portrait shake + red flash. |
| Trigger pop | `AbilityTriggeredEvent` | Event ✅ | An icon pop on the source, and a mini card on the Chain. |
| Big moments | Tavern Dweller Power (`AbilityActivatedEvent` with the Power flag), `GameOverEvent` | ✅ | A portrait flash and a name banner for Powers, and a game-over splash. |
| History rail | `GameText` log (debug panel only) | Partly | A left rail of mini cards for the last ~8 actions; hover shows the line plus target lines. |
| Input blocking | `PresentationQueue` | Logic ✅ | Lock input and blank the button while beats play. The speed setting scales the beats. |

---

## 3. How to get the "LoR look" in Unity (our setup: built-in pipeline, uGUI)

| Need | Recommendation | Why |
|---|---|---|
| Tweens | **PrimeTween 1.3.3** 🔒 (user, 2026-10-10). It's on OpenUPM, free for games (its own license: you may not resell or repackage it) | DOTween isn't on UPM. PrimeTween is allocation-free and has sequences, which match the beat model 1:1. |
| Glow and bloom | Additive glow sprites behind cards (cheap, works in uGUI now). Later, optionally URP 2D + bloom post-processing. | LoR's "flash" is mostly additive glow + bloom. Glow sprites give ~80% of it without a pipeline switch. |
| Particles in UI | **UIParticle** (mob-sakai, MIT), or our Screen-Space-Camera canvas with a particle layer | Sparks on hits, embers for the tavern, gem shatter. |
| Camera feel | Small screen shake on Nexus hits and big spells, plus slight scale punches | Cheap, and it makes impacts read. |
| Sound | A small set of UI SFX (CC0 packs, e.g. Kenney) | §1 #14. It's the cheapest clarity win after colour. |
| Board | Keep the 2D slight-tilt tavern table (CLIENT_DESIGN §1) | LoR's board is painted 2D with depth through lighting. That matches our direction. |

**Architecture (fits DEVELOPMENT §1.3, "animate from events"):**

1. Engine events.
2. `PresentationQueue` (beats, weights, hidden flags).
3. A new `BeatDirector` in `Assets/Client/Table`. It maps each event type to a short tween sequence on the widgets and locks input while it plays.
4. When the queue is empty, re-snapshot the table so it matches the engine exactly.

The rules engine stays Unity-free. Its only change is the new `PriorityPassedEvent`.

---

## 4. Suggested order (each step is playable on its own)

1. **Clarity without animation.** Button colours and states, the "PASSED" chip (needs `PriorityPassedEvent`), the after-blocks banner and "To damage" label, a whose-action glow on the portrait, the spend preview on gems. It's cheap and fixes most of the playtest confusion.
2. **The tweener + `BeatDirector`** with the core beats: card reveal, play to the Chain, resolution, damage numbers, death, lane slide-in.
3. **The round ceremony.** Banner, token flip, gem refill, Gold banking.
4. **The Chain as cards** with target lines, plus the history rail.
5. **Juice.** Glow sprites, particles, screen shake, SFX, the Tavern Dweller Power banner.

---

## 5. Later releases
- Sound (placeholder CC0 or commissioned).
- Steps 3 and 4 are done (round ceremony, Chain bubbles, history strip). Left: step 5 juice (particles, glow sprites, screen shake on big hits, SFX) and real art.

## 6. Built (2026-10-10)

**Engine.** A new `PriorityPassedEvent` (player, step, Chain size, automatic). Nothing else changed.

**Logic.**
- `ContextButton.Hint`: a one-line "what this does" under the button.
- After blocks, the button reads **"To damage"**.
- `CardView.Cost` is the current cost in hand; `PrintedCost` is the printed one.

**Table.**

Step 1, clarity:
- Button colours by verb:
  - red: Attack / Block
  - blue: End round
  - amber: OK / To damage / Continue
  - grey: the opponent acts
- The button pulses when it's yours and is blank while beats play.
- The hint shows under the button. In combat windows it's also in the prompt bar.
- A gold glow plus "YOUR ACTION" / "THEIR ACTION" on the portrait of whoever has the action.
- A "PASSED" chip on a player who passed the action.
- Hovering a playable hand card blinks the gems it would spend: Gold as the rules spend it, then mana.
- The cost gem is green when a card costs less than printed.

Step 2, beats (`TableView.Beats.cs`):
- FLIP slides. Every card starts where it was drawn before and slides home, followed through zone changes. That covers plays, draws from the deck pile, attackers and blockers walking into the lane, and resolved permanents coming from the Chain.
- Tokens pop in.
- Effects in event order:
  - the opponent's card reveal ("Opponent plays", big in the middle, then off to the Chain)
  - the Tavern Dweller Power banner
  - trigger "!" pops
  - resolution / countered / fizzles flashes where the Chain top stood
  - damage and heal numbers with a flash, and a portrait shake on face damage
  - dying units shrink and tilt away
  - the round banner ("Round N" + who has the attack token) with the gems refilling one by one
  - Gold banking: gems fly into the Gold slots
  - "Passed" / "OK" bubbles by the opponent's portrait
- Input waits while beats play, and the bot waits too. A click or Space skips them. The Speed button scales everything.

Bug-report fixes:
- Right-clicking to cancel targeting no longer pins the zoom of the card under the mouse. That was the "ghost card".
- A pinned zoom closes on a left click or Escape.
- A hover zoom closes when its card is gone.

**The Chain, LoR-style (user, 2026-10-10).**
- The Chain is no longer a list on the right. It's a row of round bubbles in the middle of the table, over a dimmed band: a ring in the caster's colour, the faction emblem and cost inside, the name on a pill below. Hover shows the full card.
- The oldest item is on the left. The next to resolve is on the right, larger, marked "NEXT".
- Each bubble is tagged You / Opponent.
- Abilities, triggers and Powers get a POWER / TRIGGER / ABILITY tag.
- Lines run from each bubble to its targets: blue for yours, red for theirs.
- Each bubble is a widget keyed by the item's object id (`ChainView.ObjectId`). So "counter target spell / ability" picks it by clicking it, and hovering it zooms the full card.
- During combat (units in the lane) the bubbles shrink to the right end of the lane.
- The opponent's reveal lands on the new bubble. A card you play slides from your hand into the row.

**Button labels say what passing does (user, 2026-10-10).**
- **Pass turn**: an empty Chain, and the opponent hasn't passed. Your action goes to them.
- **End round**: the opponent passed, so passing ends the round.
- **Pass priority**: your own item is on top of the Chain (the opponent may respond), or a combat window before blocks.
- **Let it resolve**: the opponent's item is on top.
- **To damage**: after blocks.
- **Continue**: the end of the round.
- **Opponent's turn**: greyed out, with a dimmed label, while they act.

**MTG Arena board (user, 2026-10-10; `TableView.Board.cs`).**
- Tapped units lie sideways (a quarter turn clockwise) with a light dim. The row makes room for the wider card.
- Attacking taps: declared attackers lie sideways in the combat lane. Staged ones stay upright until you confirm.
- Tapping animates as a quarter turn. Untapping (only at the start of your attack rounds) waits until the gems have refilled, before the draws.
- Equipment and Curses on a creature are tucked behind it, each strip 22 px higher, with a gold "E" pip on the host (with the count when 2+). Hovering the host or a strip fans them out beside it, upright. Hover zooms, click uses (Equip again). Equipping slides the card from the row to the host (FLIP).
- Each side scales to fit: one line from full size down to half size, then two lines (creatures on the lane side), then overlap. Scale changes tween. Hover zoom is always full size, and sits beside the card whatever its width.
- Merged triggers show a gold "×N" badge on their bubble. A trigger that found no target, or was declined, flashes "<card>: no target".
- Player Curses still sit in the row with an "on Player" tag (not under the portrait yet).

**Tavern Dweller spot, Hearthstone hero style (user, 2026-10-10).** It sits in LoR's Nexus place on the left edge.
- An oval portrait in a gold frame, in the Tavern Dweller's two faction colours with its initials until the art arrives. A name ribbon runs across the bottom.
- Life is a red gem at the bottom right; it turns bright red at 5 or less.
- The Power is a round coin beside the portrait with its cost gem on top. It's blue and pulsing when usable, dim when not, and dark "USED" once used this round (`PlayerView.PowerUsed`, via `GameEngine.UsedThisRound`).
- Hovering the coin shows the Tavern Dweller card; clicking it uses the Power.
- The portrait stays the player's target (attacks, "any target"). It flashes round when hit.

**Round start, completed.** In order:
1. The round banner (which player has the attack token).
2. The attack token flips over to the new leader (it slides, spins and punches).
3. This round's new mana gem crystallizes with a flash.
4. The gems refill one by one.
5. The cards drawn fly in from the deck only after that.

**History strip (LoR).** "RECENT PLAYS" sits on the left rail between the portraits: the last 5 plays, newest on top.
- Each entry shows who played (a blue or red edge), the card's faction orb and "You: Spark Snot → Vine Spider".
- Attacks are grouped ("Opp: attack with 2 units", with a red ATK orb).
- Hover an entry to zoom the card.

**Bug hunt (2026-10-10).** `Tools/BugHunt`, about 6,000 games in about 15 s. It found two things, both fixed:
- An exploding combat damage choice, now asked one blocker at a time.
- The Scrap Collector infinite loop, now a death trigger.

**Test hooks.**
- `-zoom chain|dweller|<card id on the board>` pins that zoom for the screenshot (the bubble zoom is verified this way).
- `-shotat` now counts game time, so the clamped first frames don't skew it.

**Test hook.** `-shotat seconds` with `-autoshot` takes the screenshot that long after the table is drawn, and with `-autoplay` only the last action animates. Use it to catch beats mid-way.
