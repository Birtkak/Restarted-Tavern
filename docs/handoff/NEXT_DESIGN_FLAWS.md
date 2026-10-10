# Next session: design-flaw pass (handoff prompt)

Paste everything below the line into a new session.

---

You're continuing work on Restarted Tavern, a Unity 6 (6000.6.4f1) + C# trading-card game.
Repo: C:\Users\Birre\Desktop\Claude shizzle\Restarted-Tavern (GitHub: Birtkak/Restarted-Tavern, main).

STATE (2026-10-10)
- Every card in docs/cards (sets v0.1, v0.2 and v0.3, 36 mana-scarcity cards) is in the engine. Six prototype
  decks, v0.3 deck pass done (commit 3815be2). Some commits and test edits are unpushed/uncommitted: run
  `git status` and `git log origin/main..` first and tell the user what's pending.
- The debug table and the sims use Legends of Runeterra-style mana (FormatConfig.Runeterra()): round mana pool,
  unspent mana becomes Gold at round end (cap 3), Gold is spent first on spells/abilities, rotating round leader
  (A B | B A | A B), only the round leader attacks (attack token), summoning sickness kept.
- A design review on 2026-10-10 found the flaws below. Nothing has been decided or changed yet.

GOAL OF THIS SESSION
1. Fix the doc contradictions (flaw 12) after asking the user whether Runeterra mana is really locked.
2. Add the flaws to docs/playtest/RULES_REVIEW.md as a new dated section (same severity icons as the file).
3. Go through the design decisions with the user, one round at a time, with AskUserQuestion (multiple choice,
   recommended option first, MTG default shown next to alternatives where it applies). Suggested order:
   turn order (1, 3), Gold rules (5, 8, 9), then the rest. Record each decision in GAME_DESIGN.md + Decision Log.
4. Implement what's decided, with EditMode tests. Fix the Invest payment bug (flaw 8) regardless; it's a bug.

THE FLAWS (2026-10-10 review)
🔴 Structural
1. Double turns remove the opponent's sorcery-speed window. Under A B | B A, each player's build turn is always
   followed straight away by their own attack turn. Creatures played on the build turn attack next turn, and the
   opponent can only answer with Instants in between. Summoning sickness almost never matters; Haste only on attack
   turns. (The PLAYTEST.md open question "does the double turn feel good?" is this.)
2. The attack token weakens Goobers' core identity and shifts card values without any repricing. Goober Rascal
   (Haste, can't block) does nothing on build turns. "Whenever you attack" cards trigger half as often (Grakka,
   Pulse Blade, Sparkwrench's attack triggers). Encore From Beyond and Silver-Tongued Deal are dead cards on build
   turns. "At the start of your turn" effects are worth twice as much per attack (Closing Bell, the Glitterworld
   pings, Wild heal-per-turn creatures). Runeterra rules were only measured in mirrors, never cross-faction.
3. The going-first rule is now backwards. §3 still has the first player skip their turn-1 draw, but with rotation the
   second player gets the first real attack (plays turn 2, attacks turn 3; the first player's next attack is turn 5).
   The first player wins only 41-50% under Runeterra (Vesper 40.8%). Rotation without the draw skip was never measured.
4. The attack token breaks multiplayer (the game must work for up to 4 players). With 4 players and a rotating
   token, each player attacks once every 4 rounds, and one round's mana pool covers 4 turns of Instants. §13 isn't
   updated.
🟠 Core promises
5. "Unused mana is not wasted" (Vision 2) is false in long games: 35-65% of leftover mana is lost to Gold cap 3
   (Jungle, Zoo, Vesper, Sparkwrench). The cap never changes who wins (3/5/8 are the same), so the cap only adds waste.
   Options: raise the cap, or have overflow mana become something else.
6. Permanent damage (Vision 1) is invisible in 4 of 6 decks: damage wearing off like MTG changes win rates by
   about 1%. Only the ping decks (Zoo and Sparkwrench) feel it.
7. Stalls remain: Runeterra mana made games 10-20% longer. The Vesper mirror has 21% of games over 25 turns and
   ~34 healing per game. No rule ends stalled games, and the v0.3 finishers aren't measured for this yet.
🟠 Gold spent first works against the Wizard cards
8. Gold spent first works against "hold Gold" payoffs: Loan Shark, Velvet Embezzler, The Dealer, Invest. The player
   can't choose. **Engine bug**: Payment.GoldNeeded (Assets/Rules/Core/Payment.cs, the PaysGoldFirst branch) takes the
   spell's own cost from Gold without keeping Gold back for Invest. Example: with 3 Gold and 6 mana, a 3-cost spell
   with Invest 3 eats all 3 Gold, so you can't pay the Invest, even though mana could have paid the spell. Fix: when
   paying Invest, keep its Gold back before taking Gold first for the spell (CanInvest/InvestSplit and the
   real payment path), and add a test.
9. Gold cap 3 removes most of the shady-deal downside: Golden Handshake ("up to 5 Gold"), Everything Has a Price
   ("gains 5 Gold"), Hostile Takeover. An opponent already at the cap gets nothing. Everything Has a Price's text can't
   do what it says.
🟡 Smaller
10. Temporary Health buffs are repeatable damage shields ("losing a buff can't kill", §7.3). Old Mossbank's
    "(2) +2/+2" can be used once each turn, so twice in a row across the double turn.
11. Weak Tavern Dwellers: Auditor Prime's Power is used 0-0.5 times per game; Mukk's and Sparkwrench's depend on
    the situation. There's one Tavern Dweller per pair, so a weak one weakens the whole pair. Auditor's Arsenal
    loses to every deck.
12. Doc contradictions:
    - GAME_DESIGN §5 says Runeterra mana was locked on 2026-10-10, but the Decision Log has no 2026-10-10 entry.
      The previous session's notes said it was still waiting on the user's playtest verdict, so ASK.
    - §1.1's table still says Gold cap 5 and mana spent first; §3 still has the old going-first text.
    - CARD_DESIGN §2.2 prices Gold at ~0.7 mana and says "up to 5 extra mana".
    - tavern_dwellers.md says "on other players' turns you have no mana".
    - The Decision Log says Velvet Embezzler needs "5 or more Gold"; the card list says 3.
    - PLAYTEST.md "What to look for" still asks about cap 5 and +1 mana compensation.
    - docs/handoff/NEXT_SESSION.md is from before Runeterra/v0.3; replace it or delete it once this file is done.

READ FIRST
- docs/GAME_DESIGN.md (MTG Comprehensive Rules are the backbone, §1.1; the Decision Log is the source of truth).
- docs/playtest/RULES_REVIEW.md, docs/playtest/PLAYTEST.md (Runeterra and v0.3 sections),
  docs/playtest/SIMULATION_REPORT.md ("Runeterra-style mana" section).
- docs/cards/*.md, docs/cards/tavern_dwellers.md. DEVELOPMENT.md §7 for the engine.
- FormatConfig (Runeterra(), Classic(), ManaPerRound, GoldFirstOffTurn, RotateRoundLeader, attack token,
  FirstPlayerSkipsFirstMana) for the switches.

HOW TO WORK WITH THIS USER
- The user has the vision and wants Claude to propose details. Ask design questions with AskUserQuestion,
  round by round, recommended option first, MTG default next to alternatives. They often take the recommended
  option but sometimes override it with their own idea.
- Anything outside mana, Gold and damage follows MTG by default; don't invent rules MTG already answers.
- **Don't run simulations or bot playtests unless the user asks.** You may offer one (e.g. "rotation without the
  draw skip", cross-faction under Runeterra) as a choice. Unit tests are fine and expected.
- Commit when a piece of work is done; ask before pushing to GitHub.
- Card text says "an opponent" / "each opponent", never "your opponent". Say "Tavern Dweller", never "Patron".

PRACTICAL NOTES
- Tests headless (~40 s): "C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics
  -projectPath <repo> -runTests -testPlatform EditMode -testResults <file>.xml -logFile <log>
  (results/logs outside the repo; only one Unity instance per project).
- Sims (only when asked): build Tools/SimRunner with Unity's SDK
  ("C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data/DotNetSdk/dotnet.exe" build Tools/SimRunner -c Release),
  run Tools/SimRunner/bin/Release/net8.0/SimRunner.exe [-simGames 500] [-simSections "..."]. ~15 s for the full suite.
  Stop and iterate if a run isn't close to done after ~2 minutes.
- Python edits on Windows: open files with newline='' (the repo keeps .cs/.md as LF). New .cs files get .meta
  files on the next Unity run; commit them together.
