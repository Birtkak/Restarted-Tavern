# Next session: playtest fixes + MTG Arena board ideas

Paste everything below the line into a new session.

---

You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).
Run `git status` and `git log origin/main..` first. The last push was c48b7ca (LoR presentation, Chain bubbles,
Tools/BugHunt). Rules: ask before commit and push. Put every decision in the GAME_DESIGN.md Decision Log. Sims and
BugHunt may run without asking. The user played 2 playtests and found balance okay.

Work through the phases in order. Ask the user the marked **DECISION** questions with AskUserQuestion (recommended
option first) at the start, all in one go, then build.

## Phase 0: the two new bug reports (Builds/Table/BugReports)

**A. 2026-10-10_125820: Archon Lumen put 6 triggers on the Chain** ("this should be a 1 time X trigger").
- The cause is `repeatCount: EquipmentYouControl` in glitterworld.json (archon_lumen). GameRunner.Chain.cs:287 puts
  one Chain item on per count. It's the only card that uses repeatCount.
- Fix: one trigger, `DealDamageEffect` with a dynamic amount X = Equipment you control. Then remove
  `TriggeredAbility.RepeatCount` if nothing else uses it (check the tests).
- **DECISION:** what should the text be?
  - (a) "deal X damage to any target, where X is the number of Equipment you control" (recommended)
  - (b) "deal X damage divided as you choose among any number of targets", which keeps today's ability to split
  Run the sims afterwards: Archon was already nerfed in Balance pass 1.

**B. 2026-10-10_130452: Ambush Predator did nothing** ("opponent seemed to pick nothing; should say u may fight up to
one target creature").
- Its text is "Arrival: This fights up to one target damaged creature you don't control." My only creature was
  undamaged, so the trigger had no target. That was correct, but you couldn't see it.
- Fix the card: drop "damaged" → "Arrival: This may fight up to one target creature you don't control." (it's in
  evergrowing_wild.json and docs/cards/evergrowing_wild.md; remove `targetDamaged`). Check the balance with sims: a 3-mana
  3/2 that fights anything is stronger. If it's too strong, make it a 3/1 or cost 4, and ask the user.
- Client fix, for every card: when a trigger or spell has "up to one" and no target was chosen, show a callout or
  history line like "Ambush Predator: no target", so it doesn't look like nothing happened.

## Phase 1: one trigger per source, not many copies (rules)

The user's rule: when a single source would put several identical triggers on the Chain for the same thing, it should
be **one trigger** that does it X times or with X as the amount. The exception is a card that says it triggers for
each one separately.

**Why, and the limit (user, 2026-10-10):** don't hand lots of triggers to regions that aren't built around them.
Batching is the **default**, not a ban:
- Trigger count is a resource. A future region or archetype can be built around it: pinging, "whenever you cast a
  spell" chains, a **storm** playstyle that counts spells or triggers this round. Those cards get separate triggers
  on purpose.
- So the engine needs both modes. Batch by default, with a per-trigger flag (e.g. `"separate": true`, text "for each
  one" / "whenever … each time") that keeps one Chain item per event.
- Counting has to stay exact whichever way it's shown: keep or add counters for spells cast this round, triggers
  resolved this round and pings dealt, so storm-style cards can count them later.
- When batching a card, check it isn't one of the trigger-payoff designs. Make the list of separate-trigger cards
  short and deliberate, and write it into CARD_DESIGN.md (which regions get many triggers, and why).
- Client: separate triggers from one source can show as one bubble with a "×N" badge, and play out quickly one after
  another. That keeps a storm turn readable without changing the rules.
- Case 1, repeat counts: Archon Lumen (Phase 0A). Nothing else.
- Case 2, simultaneous events: one sweeper kills 5 creatures, and "Whenever a creature dies" watchers (Scrap
  Collector, Body Snatcher, …) trigger 5 times. MTG does this too, but it floods the Chain.
  **DECISION:** batch these into one trigger with a count, like MTG's "whenever one or more …": Scrap Collector gains
  N Gold in one trigger (recommended). Or keep one trigger per creature.
- To find all cases: grep the card JSON for every `when` that can fire for several objects in one event
  (CreatureDies, EntersBattlefield of another, EquipmentUnattached, damage dealt, …). List each card and its new text.
- Engine: collect pending triggers per (source, ability, event batch) before they go on the Chain, and pass the count
  to the effect (a DynamicCount kind like `TriggerCount`). Targeted triggers can't batch if each one targets the
  object that caused it. Keep those separate and write them down.
- Add a BugHunt invariant: no two Chain items from the same source + ability created in the same event batch, unless
  the card is marked as an exception.
- Tests for each batched card (one item on the Chain, the right total).

## Phase 1b: "may" means a choice, everything else is forced (cards)

The user's rule: if a card lets you choose, its text says **"may"** (or "up to"). If it doesn't say so, it's forced:
it happens when you cast the card, or the trigger always does it.
- What's there now: 11 `"optional"` flags and 2 `"targetOptional"` in the card JSON, and about 22 texts with "may"
  or "up to".
- Check every card, both ways:
  - an optional flag in the JSON but no "may" / "up to" in the text: add the word, or drop the flag
  - "may" / "up to" in the text but no flag in the JSON: add the flag
  - the text reads like a choice but the engine forces it, or the other way round
- For each card, decide with the user's rule whether it should be a choice or forced. Forced is the default: a
  creature's Arrival or a spell's effect simply happens. Use "may" only where skipping it is a real choice, e.g.
  fighting would kill your own creature, or paying life or Gold.
  **DECISION:** show the user the list of cards that change from optional to forced or back, in one table, before
  editing.
- Make the "may" yes/no prompt clear in the client: a "Use <card>?" button pair, and the bot passes it in BugHunt.
- Add a test that keeps the text and JSON in line: every card whose text has "may" / "up to" has an optional flag,
  and the other way round, the same way the canonical-JSON test works.
- Ambush Predator (Phase 0B) follows this rule: "may fight up to one target creature".

## Phase 2: tapped cards turned sideways, like Arena (client)

- Today a tapped card is only darker (CardFaces.cs:136, a 35% black overlay). Arena turns it 90° clockwise.
- Draw tapped units turned 90° in `DrawRow` (TableView.cs:695). The row spacing has to use the turned width (UnitH),
  so cards don't overlap. Keep a light dim so tapped cards are still easy to spot.
- Animate it in TableView.Beats.cs: a short PrimeTween turn when a card taps (attack, Tap abilities), and the untap
  as part of the round-start sequence (after the gems refill, before the draws).
- Attackers in the combat lane stay upright (the lane already shows they attack). They show as turned when they
  go back to the row.

## Phase 3: Equipment shown on the creature, like Arena (client)

- Today attached Equipment is its own card in the row with an "on X" tag.
- Arena tucks attachments behind the host, offset upward so each one's title bar peeks out. Do the same here:
  - an attached Equipment or Curse is drawn behind its host creature, about 22px up per attachment
  - it isn't drawn in the row anymore
  - unattached Equipment stays in the row
- Hovering the host fans its attachments out to the side, and hovering or clicking one zooms it. Clicking it
  activates its Equip / moves it (the picker and targets must still find the widget: the BugHunt client check
  "every legal target is on the table" must keep passing, so update TableSnapshot / target lookup).
- The host already shows its changed stats. Add a small Equipment pip on the host so you see at a glance that it
  carries gear.
- Equip animation: FLIP-slide the Equipment from the row into the tucked slot. When it falls off (host dies), slide
  it back into the row.
- Curses attached to a player: show them under that player's portrait, the same way.

## Phase 4: board scale adjusts to how many permanents there are (client)

- Arena shrinks each side's battlefield as it fills up. Today `DrawRow` only squeezes the spacing (cards overlap
  past ~10).
- Count the "slots" per side (creatures + unattached non-creatures; tucked attachments don't count; tapped units are
  wider).
- Choose one scale per side so the row fits between CenterLeft and CenterRight with a 12px gap: 1.0 down to a minimum
  of about 0.55. Below the minimum, use two rows (creatures in front, other permanents behind) before overlapping.
- Tween the scale when the count changes (FLIP already moves positions; add scale to it). Hover zoom stays full size.
- Optional, like Arena: group identical untapped, unattached tokens (Scrap Plating, Goobers) into one card with an
  "×N" badge, and expand them on hover.
- Test layouts: add a `-board N` screenshot hook that fills both sides with N permanents (3, 8, 14, 20), and check for
  overlaps, readable stats, and that the Chain bubbles and the combat lane don't collide.

## Phase 5: verify and wrap up

- Unity EditMode tests, .NET tests, and the BugHunt default run (must be NO FAILURES).
- Rebuild the exe (TableBuilder.BuildWindows) and send screenshots: the tapped row, the equipped host with its fan
  open, and a full board at a smaller scale. Then ask the user to playtest.
- Decision Log entries (Archon, Ambush Predator, trigger batching, board presentation), CLIENT_DESIGN.md and
  LOR_PRESENTATION.md §6 (what's built), card docs. Ask before commit and push.
