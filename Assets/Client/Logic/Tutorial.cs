using System;
using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Rules;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Client.Logic
{
    public enum TutorialStepKind
    {
        /// <summary>A coach box with a button; the game waits until it's pressed.</summary>
        Info,
        /// <summary>The player has to do one thing (only that is allowed); the box stays up until it's done.</summary>
        You,
        /// <summary>The opponent plays its scripted action; the player's priority passes go by on their own.</summary>
        Opponent,
    }

    public sealed class TutorialStep
    {
        public TutorialStepKind Kind;
        public string Title;
        public string Text;
        /// <summary>Info: the button label.</summary>
        public string Button = "Next";
        /// <summary>You: what the player may do now.</summary>
        public Func<MatchSession, PlayerAction, bool> Allow;
        /// <summary>The opponent's action during this step, or null for the default (pass, keep, the bot's blocks).</summary>
        public Func<MatchSession, PlayerId, PlayerAction> Opponent;
        /// <summary>You / Opponent: the step is over.</summary>
        public Func<MatchSession, bool> Done;
    }

    /// <summary>
    /// The scripted tutorial (user, 2026-10-10): a real game of Goober Mob (you, Skabba) against Jungle Stampede (the
    /// bot, Mukk) with stacked decks and you going first. For the first three rounds every card in both hands and every
    /// play is fixed: each step explains a rule and lets the player do only the one thing it teaches (the table's
    /// legal actions go through <see cref="MatchSession.HumanFilter"/>), and the bot follows the script
    /// (<see cref="MatchSession.BotOverride"/>). After the round 3 attack the game is released: a normal bot game.
    /// </summary>
    public sealed class Tutorial
    {
        public const string MyDeck = "goober_mob", TheirDeck = "jungle_stampede";

        /// <summary>Your opening hand, then your draws for rounds 1, 2 and 3.</summary>
        public static readonly string[] MyTop =
        {
            "goober_rascal", "brawling_runt", "spark_snot", "gob_gang", "hog_rider", "goober_warchief", "barrel_bomber",
            "fuse_goober", "crypt_usher", "mob_rush",
        };

        public static readonly string[] TheirTop =
        {
            "vine_spider", "razorhide_boar", "ironbark_grizzly", "mossgut_grower", "jungle_remedy", "tusked_mammoth", "pit_fighter",
            "thornback_ravager", "hog_rider", "gift_of_the_grove",
        };

        public MatchSession Session { get; }
        public IReadOnlyList<TutorialStep> Steps { get; }
        public int Index { get; private set; }
        public bool Released { get; private set; }
        public TutorialStep Current => Released || Index >= Steps.Count ? null : Steps[Index];

        /// <summary>Nothing moves while an Info box is up (no bot, no auto-passes).</summary>
        public bool Frozen => Current?.Kind == TutorialStepKind.Info;

        private PlayerId Me => Session.State.Players[0].Id;
        private PlayerId Them => Session.State.Players[1].Id;

        /// <summary>The match: the two prototype decks with the scripted cards on top (the rest in a fixed order), you first.</summary>
        public static MatchSetup Setup(ulong seed)
        {
            return new MatchSetup
            {
                Decks = { Stack(CardPool.PrototypeDeck(MyDeck), MyTop, seed), Stack(CardPool.PrototypeDeck(TheirDeck), TheirTop, seed + 1) },
                Seats = { SeatKind.Human, SeatKind.Bot },
                Seed = seed,
                StackedDecks = true,
                FirstSeat = 0,
            };
        }

        private static CardPool.DeckList Stack(CardPool.DeckList deck, string[] top, ulong seed)
        {
            var rest = new List<string>(deck.Cards);
            foreach (var id in top)
                if (!rest.Remove(id)) throw new InvalidOperationException(deck.Id + " has no " + id + " for the tutorial.");
            // The rest in a shuffled but repeatable order (the game after the script stays a fair draw).
            var rng = new Random((int)(seed % int.MaxValue));
            for (int i = rest.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (rest[i], rest[j]) = (rest[j], rest[i]);
            }
            return new CardPool.DeckList
            {
                Id = deck.Id, Name = deck.Name, TavernDweller = deck.TavernDweller, Description = deck.Description,
                Cards = top.Concat(rest).ToList(),
            };
        }

        public Tutorial(MatchSession session)
        {
            Session = session;
            Steps = BuildSteps();
            session.HumanFilter = AllowHuman;
            session.BotOverride = OpponentAction;
        }

        /// <summary>The Info button.</summary>
        public void Next()
        {
            if (Current?.Kind != TutorialStepKind.Info) return;
            Index++;
            if (Index >= Steps.Count) Release();
            else Advance();
        }

        /// <summary>Hands the game over to the normal rules and bot.</summary>
        public void Release()
        {
            Released = true;
            Session.HumanFilter = null;
            Session.BotOverride = null;
        }

        /// <summary>Call after every change: moves past finished steps. True when the step changed.</summary>
        public bool Advance()
        {
            bool moved = false;
            while (!Released && Current != null && Current.Kind != TutorialStepKind.Info && Current.Done(Session))
            {
                Index++;
                moved = true;
            }
            if (!Released && Index >= Steps.Count) Release();
            return moved;
        }

        /// <summary>
        /// One move of a player who does exactly what each step asks (tests, -tutorialstep screenshots): Info → Next,
        /// the bot's turn → the bot, else the auto pass or the lesson (attack with everything, the first block, the one
        /// allowed play). False when there is nothing to do (released or game over).
        /// </summary>
        public bool AutoPlayStep()
        {
            Advance();
            if (Released || Session.State.IsGameOver) return false;
            if (Frozen) { Next(); return true; }
            if (Session.BotToAct) { Session.StepBot(); return true; }
            if (!Session.HumanToAct) return false;
            var auto = AutoHumanAction();
            if (auto != null) { Session.Submit(auto); return true; }
            if (CombatStage.CanBlock(Session))
            {
                var stage = CombatStage.ForBlock(Session);
                var blocker = stage.Candidates().First();
                stage.StageBlocker(blocker, stage.BlockableBy(blocker)[0]);
                Session.CommitCombat(stage);
            }
            else if (CombatStage.CanAttack(Session))
            {
                var stage = CombatStage.ForAttack(Session);
                foreach (var id in stage.Candidates().ToList()) stage.StageAttacker(id);
                Session.CommitCombat(stage);
            }
            else Session.Submit(Session.LegalForViewer()[0]);
            return true;
        }

        private bool AllowHuman(PlayerAction a)
        {
            var step = Current;
            if (step == null) return true;
            return step.Kind == TutorialStepKind.You && step.Allow(Session, a);
        }

        /// <summary>
        /// What the tutorial plays for the player right now: the opening Keep, and every priority pass outside the lesson
        /// (the opponent's spells resolving, combat windows, the end of the round). Null = wait for the player.
        /// </summary>
        public PlayerAction AutoHumanAction()
        {
            if (Released || Frozen || !Session.HumanToAct) return null;
            if (Session.LegalForViewer().Count > 0) return null; // the lesson's action is available: the player does it
            var legal = Session.Engine.GetLegalActions(Session.State, Me);
            return legal.FirstOrDefault(a => a.Kind == ActionKind.Keep)
                ?? legal.FirstOrDefault(a => a.Kind == ActionKind.PassPriority)
                ?? legal.FirstOrDefault(a => a.Kind == ActionKind.FinishBlocks)
                ?? legal.FirstOrDefault(a => a.Kind == ActionKind.FinishAttacks)
                ?? legal.FirstOrDefault();
        }

        private PlayerAction OpponentAction(MatchSession s, PlayerId p)
        {
            if (Released) return null;
            var scripted = Current?.Opponent?.Invoke(s, p);
            if (scripted != null) return scripted;
            var legal = s.Engine.GetLegalActions(s.State, p);
            return legal.FirstOrDefault(a => a.Kind == ActionKind.Keep)
                ?? legal.FirstOrDefault(a => a.Kind == ActionKind.PassPriority)
                ?? legal.FirstOrDefault(a => a.Kind == ActionKind.FinishAttacks); // blocks, discards: the bot decides
        }

        // ------------------------------------------------------------------ helpers

        private static PlayerState Player(MatchSession s, int seat) => s.State.Players[seat];
        private static bool OnBoard(MatchSession s, int seat, string id) => Player(s, seat).Battlefield.Any(c => c.DefinitionId == id);
        private static bool InGrave(MatchSession s, int seat, string id) => Player(s, seat).Graveyard.Any(c => c.DefinitionId == id);
        private static string Def(MatchSession s, ObjectId id) => s.State.FindObject(id)?.DefinitionId;

        /// <summary>Action phase, nothing on the Chain or in combat, waiting on <paramref name="seat"/>.</summary>
        private static bool HasAction(MatchSession s, int seat) =>
            s.State.Step == Step.Main1 && s.State.Chain.Count == 0 && s.State.Combat == null && s.State.Pending == null
            && s.WaitingOn == Player(s, seat).Id;

        private static bool Plays(MatchSession s, PlayerAction a, string id) => a.Kind == ActionKind.PlayCard && Def(s, a.Card) == id;

        private static bool Attacks(PlayerAction a) => a.Kind == ActionKind.GoToCombat || a.Kind == ActionKind.DeclareAttacker;

        private static bool PassToEndRound(MatchSession s, PlayerAction a) =>
            a.Kind == ActionKind.PassPriority && s.State.Step == Step.Main1 && s.State.Chain.Count == 0;

        private static PlayerAction OpponentPlays(MatchSession s, PlayerId p, string id) =>
            !HasAction(s, 1) ? null : s.Engine.GetLegalActions(s.State, p).FirstOrDefault(a => Plays(s, a, id));

        private static PlayerAction OpponentAttacksWith(MatchSession s, PlayerId p, string id)
        {
            var legal = s.Engine.GetLegalActions(s.State, p);
            if (s.State.Pending?.Kind == DecisionKind.DeclareAttackers)
            {
                bool declared = s.State.Combat != null && Player(s, 1).Battlefield.Any(c => c.DefinitionId == id && s.State.Combat.IsAttacking(c.Id));
                return declared ? PlayerAction.FinishAttacks(p)
                    : legal.FirstOrDefault(a => a.Kind == ActionKind.DeclareAttacker && Def(s, a.Card) == id) ?? PlayerAction.FinishAttacks(p);
            }
            if (HasAction(s, 1)) return legal.FirstOrDefault(a => a.Kind == ActionKind.GoToCombat);
            return null;
        }

        // ------------------------------------------------------------------ the script

        private List<TutorialStep> BuildSteps()
        {
            TutorialStep Info(string title, string text, string button = "Next") =>
                new TutorialStep { Kind = TutorialStepKind.Info, Title = title, Text = text, Button = button };
            TutorialStep You(string title, string text, Func<MatchSession, PlayerAction, bool> allow, Func<MatchSession, bool> done) =>
                new TutorialStep { Kind = TutorialStepKind.You, Title = title, Text = text, Allow = allow, Done = done };
            TutorialStep Them(string title, string text, Func<MatchSession, PlayerId, PlayerAction> act, Func<MatchSession, bool> done) =>
                new TutorialStep { Kind = TutorialStepKind.Opponent, Title = title, Text = text, Opponent = act, Done = done };

            return new List<TutorialStep>
            {
                // Round 1: you lead. Mana, actions, playing a creature, attacking, ending the round.
                Info("Welcome to the Tavern",
                    "You play as <b>Skabba</b>, the Tavern Dweller at the bottom left. Your opponent is <b>Mukk the Grub King</b>, top left.\n\n"
                    + "Each of you has <b>30 life</b> (the red gem). Bring Mukk to 0 to win."),
                Info("Mana",
                    "Every round, both players gain <b>1 more max mana</b> and refill it: the blue gems on the right.\n\n"
                    + "This is round 1, so you have <b>1 mana</b>. A card's cost is the blue gem in its top left corner."),
                Info("Rounds and actions",
                    "Players take turns doing <b>one action at a time</b>: play a card, use a Power, or attack. Then the other player acts.\n\n"
                    + "You are this round's <b>leader</b>: you act first, and you hold the <b>attack token</b> (ATK, right side). Only the token holder can attack."),
                You("Play a creature",
                    "Drag <b>Goober Rascal</b> from your hand onto the table. It costs 1 mana.",
                    (s, a) => Plays(s, a, "goober_rascal"),
                    s => OnBoard(s, 0, "goober_rascal") && s.State.Chain.Count == 0),
                Info("Creatures",
                    "Goober Rascal is a <b>1/1</b>: 1 Power (damage it deals), 1 Health. This one can't block.\n\n"
                    + "There is <b>no summoning sickness</b>: creatures can attack the round they arrive. Now Mukk gets an action."),
                You("Attack!",
                    "Mukk had nothing to play and passed. Your action again.\n\n"
                    + "Drag <b>Goober Rascal</b> into the combat lane in the middle, then press <b>Attack</b>.",
                    (s, a) => Attacks(a),
                    s => s.State.Players[1].Life < 30 && s.State.Combat == null),
                Info("Hit!",
                    "Nothing blocked, so the Rascal hit Mukk for 1. Mukk is at <b>29</b>.\n\n"
                    + "Attacking <b>taps</b> a creature (it tilts). It only untaps at the start of your next attack round, so it can't block in between."),
                You("End the round",
                    "Your mana is spent. Press <b>End round</b> (or Space). When both players pass in a row, the round ends.\n\n"
                    + "Mana you don't spend becomes <b>Gold</b> at the end of the round (yellow diamonds, up to 3). Gold pays for Instants and abilities.",
                    PassToEndRound,
                    s => s.State.RoundNumber >= 2),

                // Round 2: Mukk leads and attacks. Blocking, permanent damage, Skabba's passive.
                Info("Round 2",
                    "Everyone drew a card and has <b>2 mana</b>.\n\n"
                    + "The attack token moved to <b>Mukk</b>: this round Mukk leads and may attack. It passes back and forth every round."),
                Them("Mukk's action",
                    "Mukk is the leader, so Mukk acts first...",
                    (s, p) => OpponentPlays(s, p, "vine_spider"),
                    s => OnBoard(s, 1, "vine_spider") && HasAction(s, 0)),
                You("Your answer",
                    "Mukk played <b>Vine Spider</b>, a 2/3 with Reach (it can block flyers) and Vigilance (attacking doesn't tap it).\n\n"
                    + "Your action: play <b>Brawling Runt</b>, a 2/2 for 2 mana.",
                    (s, a) => Plays(s, a, "brawling_runt"),
                    s => OnBoard(s, 0, "brawling_runt") && s.State.Chain.Count == 0),
                Them("Mukk's action",
                    "Mukk holds the attack token this round...",
                    (s, p) => OpponentAttacksWith(s, p, "vine_spider"),
                    s => s.State.Pending?.Kind == DecisionKind.DeclareBlockers),
                You("Block",
                    "Mukk attacks with Vine Spider! Untapped creatures can block.\n\n"
                    + "Drag <b>Brawling Runt</b> in front of the Spider, then press <b>Block</b>. Blocked creatures deal their damage to each other instead of to you.",
                    (s, a) => a.Kind == ActionKind.DeclareBlocker,
                    s => InGrave(s, 0, "brawling_runt") && s.State.Combat == null && s.State.Chain.Count == 0 && s.State.PendingTriggers.Count == 0),
                Info("Damage stays",
                    "Both dealt 2 damage at the same time. The Runt died, and the Spider survived with <b>1 Health left</b>.\n\n"
                    + "Damage is <b>permanent</b> here: it doesn't heal at the end of the round. Remember that Spider.\n\n"
                    + "And when one of your creatures dies, <b>Skabba</b>'s passive deals 1 damage to Mukk. Hover your portrait to read it."),
                You("End the round",
                    "Nothing left to do this round. Press <b>End round</b>.",
                    PassToEndRound,
                    s => s.State.RoundNumber >= 3),

                // Round 3: you lead again. Instants and the Chain, finishing a damaged creature, attacking wide.
                Info("Round 3",
                    "<b>3 mana</b> each, and the attack token is back with you. Your creatures untapped.\n\n"
                    + "The Vine Spider still has its 2 damage: only 1 Health left."),
                You("Instants",
                    "<b>Spark Snot</b> is an Instant: \"Deal 2 damage to target creature.\"\n\n"
                    + "Drag it onto the damaged <b>Vine Spider</b> (or click it, then click the Spider).",
                    (s, a) => Plays(s, a, "spark_snot") && a.Targets.Length > 0 && !a.Targets[0].IsPlayer && Def(s, a.Targets[0].Object) == "vine_spider",
                    s => InGrave(s, 1, "vine_spider") && s.State.Chain.Count == 0),
                Info("The Chain",
                    "Spells and abilities wait on the <b>Chain</b> (the bubbles in the middle) so the other player can respond. The newest one resolves first.\n\n"
                    + "<b>Instants</b> can be played whenever you have priority, even in response to your opponent. Creatures and Sorceries only as your action."),
                Them("Mukk's action",
                    "Mukk's turn to act...",
                    (s, p) => OpponentPlays(s, p, "razorhide_boar"),
                    s => OnBoard(s, 1, "razorhide_boar") && HasAction(s, 0)),
                You("Go wide",
                    "Mukk played <b>Razorhide Boar</b>, a 3/3 with Trample (extra damage goes through blockers). Mukk's passive makes it 4/3.\n\n"
                    + "Play <b>Gob Gang</b> (2 mana, a Sorcery): it makes two 1/1 Goobers.",
                    (s, a) => Plays(s, a, "gob_gang"),
                    s => Player(s, 0).Battlefield.Count(c => c.DefinitionId == CardPool.GooberToken) >= 2 && s.State.Chain.Count == 0),
                You("Attack!",
                    "You hold the attack token. Drag your Goobers and the Rascal into the lane and press <b>Attack</b>.\n\n"
                    + "The Boar can block only one of them. Whatever isn't blocked hits Mukk.",
                    (s, a) => Attacks(a),
                    s => (s.State.AttackedThisRound & 1) != 0 && s.State.Step == Step.Main1 && s.State.Combat == null && s.State.Chain.Count == 0 && s.State.PendingTriggers.Count == 0),
                Info("Your turn now",
                    "That's the basics: mana and Gold, actions, the attack token, blocking, permanent damage and the Chain.\n\n"
                    + "Skabba also has a <b>Power</b> (the coin next to the portrait): 1 mana and a creature sacrificed to draw a card, once a round.\n\n"
                    + "From here on Mukk plays for real. <b>Try and beat the AI now!</b>",
                    "Fight!"),
            };
        }
    }
}
