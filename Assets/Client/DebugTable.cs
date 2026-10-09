using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RestartedTavern.Rules;
using RestartedTavern.Rules.AI;
using RestartedTavern.Rules.Cards;
using UnityEngine;

namespace RestartedTavern.Client
{
    /// <summary>
    /// Hot-seat debug table (DEVELOPMENT §5, roadmap step 2): plays the rules engine through
    /// IMGUI, so the rules can be tested by hand before any real client exists.
    ///
    /// Click a card to show only the actions that involve it; click an action to do it. Hover over or click a card
    /// to see everything about it in the inspector. Either player can be handed to the GreedyBot (P2 is a bot by
    /// default, for solo playtests). Undo keeps the last 200 states. Every finished game is saved to Playtests/
    /// next to the game (Save log saves the current one), for playtest feedback.
    ///
    /// The deck buttons in the top bar pick each seat's deck for the next New game.
    ///
    /// Command line (for automated checks): -seed N, -bot1, -bot2, -deck1/-deck2 N, -autoplay N (bots play N
    /// actions at startup), -autoshot path.png (take a screenshot, then quit).
    /// </summary>
    public sealed class DebugTable : MonoBehaviour
    {
        private const float VirtualHeight = 900f;
        private const float CardWidth = 150f;
        private const float CardHeight = 96f;
        private const int MaxUndo = 200;
        private const int MaxLog = 400;

        private static readonly string[] DeckNames =
            { "Goober Mob", "Jungle Stampede", "Zoo Patrol", "Vesper's Ledger", "Sparkwrench Scrappers", "Auditor's Arsenal" };
        private static readonly Func<List<string>>[] Decks =
        {
            CardPool.GooberMobDeck, CardPool.JungleStampedeDeck, CardPool.ZooPatrolDeck,
            CardPool.VespersLedgerDeck, CardPool.SparkwrenchScrappersDeck, CardPool.AuditorsArsenalDeck,
        };
        private static readonly string[] TavernDwellers =
        {
            CardPool.GooberMobTavernDweller, CardPool.JungleStampedeTavernDweller, CardPool.ZooPatrolTavernDweller,
            CardPool.VespersLedgerTavernDweller, CardPool.SparkwrenchScrappersTavernDweller, CardPool.AuditorsArsenalTavernDweller,
        };

        /// <summary>Deck choice per seat (index into <see cref="Decks"/>). Applies from the next new game.</summary>
        private readonly int[] _deckChoice = { 0, 1 };
        /// <summary>The decks of the game being played (the choice may have changed since).</summary>
        private readonly int[] _deckInPlay = { 0, 1 };

        private GameEngine _engine;
        private GameState _state;
        private GameText _text;
        private readonly List<GameState> _undo = new List<GameState>();
        private readonly List<string> _log = new List<string>();

        private ulong _seed = 1;
        private string _seedText = "1";
        private bool _showAllHands;
        private readonly bool[] _bot = { false, true };
        private ObjectId _hover = ObjectId.None;
        /// <summary>Cards involved in the action button under the mouse (drawn highlighted on the board; one frame late).</summary>
        private HashSet<ObjectId> _actionCards = new HashSet<ObjectId>(), _actionCardsNext = new HashSet<ObjectId>();
        private bool _showRules;
        private string _savedPath;
        private bool _savedThisGame;
        private GreedyBot _botPlayer;
        private float _nextBotTime;
        private ObjectId _focus = ObjectId.None;

        private Vector2 _boardScroll, _actionsScroll, _logScroll, _inspectScroll, _rulesScroll;
        private GUIStyle _cardStyle, _buttonStyle, _headerStyle, _labelStyle, _bigStyle;

        private int _autoplay;
        private string _autoshot;
        private int _shotFrame = -1;

        private void Start()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string next = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "-seed": ulong.TryParse(next, out _seed); break;
                    case "-bot1": _bot[0] = true; break;
                    case "-bot2": _bot[1] = true; break;
                    case "-hotseat": _bot[0] = _bot[1] = false; break;
                    case "-deck1": int.TryParse(next, out _deckChoice[0]); break;
                    case "-deck2": int.TryParse(next, out _deckChoice[1]); break;
                    case "-autoplay": int.TryParse(next, out _autoplay); break;
                    case "-autoshot": _autoshot = next; break;
                }
            }
            if (_seed == 0) _seed = 1;
            NewGame(_seed);
            if (_autoshot != null) _shotFrame = 0;
        }

        // ------------------------------------------------------------------ game control

        private void NewGame(ulong seed)
        {
            _seed = seed;
            _seedText = seed.ToString(CultureInfo.InvariantCulture);
            var db = CardPool.CreateDatabase();
            _engine = new GameEngine(db);
            _text = new GameText(db);
            _botPlayer = new GreedyBot(_engine);
            _undo.Clear();
            _log.Clear();
            _focus = ObjectId.None;
            _hover = ObjectId.None;
            _savedPath = null;
            _savedThisGame = false;
            _deckInPlay[0] = _deckChoice[0];
            _deckInPlay[1] = _deckChoice[1];

            var events = new List<GameEvent>();
            _state = _engine.CreateGame(FormatConfig.Standard(), new[]
            {
                new PlayerSetup { Deck = Decks[_deckChoice[0]](), TavernDwellerId = TavernDwellers[_deckChoice[0]] },
                new PlayerSetup { Deck = Decks[_deckChoice[1]](), TavernDwellerId = TavernDwellers[_deckChoice[1]] },
            }, seed, events);
            _text.Remember(_state, events);
            AddToLog(events);
        }

        private void Do(PlayerAction action)
        {
            string description = _text.Describe(_state, action);
            _undo.Add(_state.Clone());
            if (_undo.Count > MaxUndo) _undo.RemoveAt(0);

            _text.Remember(_state);
            var events = _engine.Apply(_state, action);
            _text.Remember(_state, events);

            if (action.Kind != ActionKind.PassPriority) _log.Add("> " + action.Player + ": " + description);
            AddToLog(events);
            _focus = ObjectId.None;
            if (_state.IsGameOver && !_savedThisGame && _autoshot == null) SaveLog();
        }

        private void Undo()
        {
            if (_undo.Count == 0) return;
            _state = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            _log.Add("(undo)");
            _focus = ObjectId.None;
        }

        /// <summary>Writes the game (decks, seed, result and the full log) to Playtests/ next to the game, for feedback.</summary>
        private void SaveLog()
        {
            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Playtests");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture) + "_seed" + _seed + ".txt");
                var lines = new List<string>
                {
                    "Restarted Tavern playtest log",
                    "Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    "Seed: " + _seed,
                };
                for (int seat = 0; seat < 2; seat++)
                    lines.Add("P" + (seat + 1) + ": " + DeckNames[_deckInPlay[seat]] + " (" + _text.Name(TavernDwellers[_deckInPlay[seat]]) + ")"
                              + (_bot[seat] ? ", bot" : ", human") + (_state.Players[seat].Seat == _state.StartingPlayerIndex ? ", went first" : ""));
                lines.Add("Result: " + (_state.IsGameOver ? "winner " + string.Join(", ", _state.Winners) : "not finished")
                          + " after turn " + _state.TurnNumber + " | life " + string.Join(" vs ", _state.Players.Select(p => p.Life)));
                lines.Add("");
                lines.Add("Notes (fill in): what felt good, what felt bad, confusing rules or cards, misplays caused by the UI:");
                lines.Add("");
                lines.Add("----- log -----");
                lines.AddRange(_log);
                File.WriteAllLines(file, lines);
                _savedPath = file;
                _savedThisGame = _state.IsGameOver;
            }
            catch (Exception e)
            {
                _savedPath = "could not save: " + e.Message;
            }
        }

        private void AddToLog(List<GameEvent> events)
        {
            foreach (var e in events)
            {
                var line = _text.Describe(_state, e, HandViewer());
                if (line != null) _log.Add(line);
            }
            if (_log.Count > MaxLog) _log.RemoveRange(0, _log.Count - MaxLog);
            _logScroll.y = float.MaxValue;
        }

        /// <summary>
        /// Whose draws the log may name. Everyone's with "show all hands"; with one human, only theirs.
        /// In hot-seat (several humans sharing a screen) nobody's: each hand shows only on its owner's turn to act.
        /// </summary>
        private PlayerId? HandViewer()
        {
            if (_showAllHands) return null;
            var humans = _state.Players.Where(p => !_bot[p.Seat]).ToList();
            return humans.Count == 1 ? humans[0].Id : NoViewer;
        }

        private static readonly PlayerId NoViewer = new PlayerId(0);

        private bool IsBot(PlayerId p) => _bot[_state.GetPlayer(p).Seat];

        private void Update()
        {
            if (_state == null) return;

            if (_autoplay > 0)
            {
                while (_autoplay > 0 && !_state.IsGameOver)
                {
                    BotStep(_engine.WaitingOn(_state).Value);
                    _autoplay--;
                }
                _autoplay = 0;
            }

            if (_shotFrame >= 0)
            {
                _shotFrame++;
                if (_shotFrame == 3) ScreenCapture.CaptureScreenshot(_autoshot);
                if (_shotFrame == 10) Application.Quit();
                return;
            }

            var who = _engine.WaitingOn(_state);
            if (who.HasValue && IsBot(who.Value) && Time.unscaledTime >= _nextBotTime)
            {
                BotStep(who.Value);
                _nextBotTime = Time.unscaledTime + 0.35f;
            }
        }

        private void BotStep(PlayerId who) => Do(_botPlayer.Choose(_state, who));

        // ------------------------------------------------------------------ drawing

        private void OnGUI()
        {
            if (_state == null) return;
            EnsureStyles();

            float scale = Screen.height / VirtualHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = VirtualHeight;
            float rightW = Mathf.Clamp(w * 0.3f, 320f, 520f);

            DrawTopBar(new Rect(4, 4, w - 8, 30));
            if (_showRules) DrawRules(new Rect(4, 40, w - rightW - 12, h - 44));
            else DrawBoard(new Rect(4, 40, w - rightW - 12, h - 44));
            DrawInspector(new Rect(w - rightW - 4, 40, rightW, h * 0.27f));
            DrawActions(new Rect(w - rightW - 4, 40 + h * 0.27f + 4, rightW, h * 0.36f - 4));
            DrawLog(new Rect(w - rightW - 4, 40 + h * 0.63f + 4, rightW, h * 0.37f - 52));

            if (Event.current.type == EventType.Repaint)
            {
                (_actionCards, _actionCardsNext) = (_actionCardsNext, _actionCards);
                _actionCardsNext.Clear();
            }
        }

        private void EnsureStyles()
        {
            if (_cardStyle != null) return;
            _cardStyle = new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = 12, padding = new RectOffset(6, 6, 4, 4) };
            _buttonStyle = new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.MiddleLeft, fontSize = 13, padding = new RectOffset(8, 8, 4, 4) };
            _labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 13 };
            _headerStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 14, fontStyle = FontStyle.Bold, padding = new RectOffset(8, 8, 4, 4) };
            _bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _headerStyle.normal.textColor = Color.white;
        }

        private void DrawTopBar(Rect r)
        {
            GUILayout.BeginArea(r);
            GUILayout.BeginHorizontal();

            string status = _state.IsGameOver
                ? "GAME OVER. Winner: " + string.Join(", ", _state.Winners)
                : "Turn " + _state.TurnNumber + "  |  " + _state.ActivePlayer + "'s turn  |  " + GameText.StepName(_state.Step)
                  + "  |  waiting on " + _engine.WaitingOn(_state);
            GUILayout.Label(status, _labelStyle, GUILayout.Width(330));
            if (GUILayout.Button(_showRules ? "Back to table" : "Rules", GUILayout.Width(95))) _showRules = !_showRules;
            if (GUILayout.Button("Save log", GUILayout.Width(80))) SaveLog();

            if (GUILayout.Button("New game", GUILayout.Width(90))) NewGame(NextSeed());
            GUILayout.Label("seed", GUILayout.Width(32));
            _seedText = GUILayout.TextField(_seedText, GUILayout.Width(70));
            GUI.enabled = _undo.Count > 0;
            if (GUILayout.Button("Undo (" + _undo.Count + ")", GUILayout.Width(90))) Undo();
            GUI.enabled = true;
            _state.AutoPass = GUILayout.Toggle(_state.AutoPass, " Auto-pass", GUILayout.Width(95));
            _showAllHands = GUILayout.Toggle(_showAllHands, " Show all hands", GUILayout.Width(125));
            _bot[0] = GUILayout.Toggle(_bot[0], " P1 bot", GUILayout.Width(70));
            _bot[1] = GUILayout.Toggle(_bot[1], " P2 bot", GUILayout.Width(70));
            for (int seat = 0; seat < 2; seat++)
                if (GUILayout.Button("P" + (seat + 1) + ": " + DeckNames[_deckChoice[seat]], GUILayout.Width(170)))
                    _deckChoice[seat] = (_deckChoice[seat] + 1) % Decks.Length; // used by the next New game

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>The seed typed in the box if it was changed, otherwise the next seed.</summary>
        private ulong NextSeed()
        {
            ulong typed = ParseSeed();
            return typed == _seed ? _seed + 1 : typed;
        }

        private ulong ParseSeed() =>
            ulong.TryParse(_seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) && s > 0 ? s : 1;

        private void DrawBoard(Rect r)
        {
            GUILayout.BeginArea(r);
            _boardScroll = GUILayout.BeginScrollView(_boardScroll);
            float width = r.width - 24;

            // Opponents on top, P1 at the bottom, the Chain and combat in between.
            for (int i = _state.Players.Count - 1; i >= 1; i--) DrawPlayer(_state.Players[i], width);
            DrawCenter();
            DrawPlayer(_state.Players[0], width);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPlayer(PlayerState p, float width)
        {
            var waiting = _engine.WaitingOn(_state);
            string marker = p.Id == _state.ActivePlayer ? ">> " : "";
            string header = marker + p.Id + " " + DeckNames[_deckInPlay[p.Seat]] + (_bot[p.Seat] ? " (bot)" : "")
                            + "    Life " + p.Life + "    Mana " + p.Mana + "/" + p.MaxMana + "    Gold " + p.Gold
                            + "    Deck " + p.Deck.Count + "    Hand " + p.Hand.Count + "    Graveyard " + p.Graveyard.Count
                            + (p.HasLost ? "    LOST" : "");
            var old = GUI.contentColor;
            GUI.contentColor = waiting == p.Id ? new Color(0.45f, 1f, 0.55f) : p.HasLost ? new Color(1f, 0.4f, 0.4f) : Color.white;
            GUILayout.Box(header + (waiting == p.Id ? "    <- to act" : ""), _headerStyle, GUILayout.ExpandWidth(true));
            GUI.contentColor = old;

            GUILayout.Label("Tavern Dweller", _labelStyle);
            DrawCardRow(p.TavernDwellerZone, width, true, CardWidth * 2.4f);
            GUILayout.Label("Battlefield", _labelStyle);
            DrawCardRow(p.Battlefield, width, true);

            bool showHand = _showAllHands || (waiting == p.Id && !_bot[p.Seat]) || HandViewer() == p.Id;
            GUILayout.Label("Hand" + (showHand ? "" : " (hidden)"), _labelStyle);
            if (showHand) DrawCardRow(p.Hand, width, true);
            else GUILayout.Label(new string('#', p.Hand.Count), _labelStyle);

            if (p.Graveyard.Count > 0)
            {
                var names = p.Graveyard.Skip(Math.Max(0, p.Graveyard.Count - 8)).Reverse().Select(c => _text.Name(c.DefinitionId));
                GUILayout.Label("Graveyard (newest first): " + string.Join(", ", names), _labelStyle);
            }
            GUILayout.Space(10);
        }

        private void DrawCardRow(List<CardInstance> cards, float width, bool clickable, float cardWidth = CardWidth)
        {
            if (cards.Count == 0)
            {
                GUILayout.Label("  (empty)", _labelStyle);
                return;
            }
            int perRow = Mathf.Max(1, Mathf.FloorToInt(width / (cardWidth + 6)));
            var actable = ActableObjects();
            for (int start = 0; start < cards.Count; start += perRow)
            {
                GUILayout.BeginHorizontal();
                for (int i = start; i < Mathf.Min(cards.Count, start + perRow); i++)
                {
                    var c = cards[i];
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = CardColor(c, actable.Contains(c.Id));
                    if (GUILayout.Button(_text.Describe(_state, c), _cardStyle, GUILayout.Width(cardWidth), GUILayout.Height(CardHeight)) && clickable)
                        _focus = _focus == c.Id ? ObjectId.None : c.Id;
                    if (Event.current.type == EventType.Repaint && !c.IsHidden
                        && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                        _hover = c.Id;
                    GUI.backgroundColor = old;
                }
                GUILayout.EndHorizontal();
            }
        }

        private Color CardColor(CardInstance c, bool actable)
        {
            if (c.Id == _focus) return new Color(1f, 0.9f, 0.2f);
            if (_actionCards.Contains(c.Id)) return new Color(0.35f, 0.85f, 1f);
            if (_state.Combat != null && (_state.Combat.IsAttacking(c.Id) || _state.Combat.IsBlocking(c.Id))) return new Color(1f, 0.55f, 0.2f);
            if (c.Tapped) return new Color(0.45f, 0.45f, 0.45f);
            if (actable) return new Color(0.5f, 1f, 0.6f);
            if (c.Damage > 0) return new Color(1f, 0.6f, 0.6f);
            return Color.white;
        }

        /// <summary>Objects that appear in the waiting player's legal actions (highlighted green).</summary>
        private HashSet<ObjectId> ActableObjects()
        {
            var set = new HashSet<ObjectId>();
            var who = _engine.WaitingOn(_state);
            if (!who.HasValue || IsBot(who.Value)) return set;
            foreach (var a in _engine.GetLegalActions(_state, who.Value))
            {
                if (!a.Card.IsNone) set.Add(a.Card);
                if (a.Target.HasValue && !a.Target.Value.IsPlayer) set.Add(a.Target.Value.Object);
            }
            return set;
        }

        private void DrawCenter()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            if (_state.IsGameOver)
            {
                GUILayout.Label("GAME OVER. Winner: " + string.Join(", ", _state.Winners) + " (turn " + _state.TurnNumber + ")", _bigStyle);
                if (_savedPath != null) GUILayout.Label("Game log saved to " + _savedPath, _labelStyle);
            }

            if (_state.Pending != null)
                GUILayout.Label("Waiting on " + _state.Pending.Player + ": " + DecisionText(_state.Pending), _labelStyle);

            if (_state.Combat != null && _state.Combat.Attacks.Count > 0)
            {
                foreach (var attack in _state.Combat.Attacks)
                {
                    var blockers = _state.Combat.Blocks.Where(b => b.Attacker == attack.Attacker)
                        .Select(b => _text.Name(_state, b.Blocker)).ToList();
                    GUILayout.Label("Combat: " + _text.Name(_state, attack.Attacker) + " -> " + attack.Defender
                                    + (blockers.Count > 0 ? "   blocked by " + string.Join(", ", blockers) : attack.Blocked ? "   (blocked)" : ""),
                        _labelStyle);
                }
            }

            if (_state.Chain.Count == 0)
            {
                GUILayout.Label("Chain: empty", _labelStyle);
            }
            else
            {
                GUILayout.Label("Chain (top resolves first):", _labelStyle);
                for (int i = _state.Chain.Count - 1; i >= 0; i--)
                {
                    var item = _state.Chain[i];
                    string kind = item.Kind == ChainItemKind.Spell ? "Spell"
                        : item.IsTavernDwellerPower ? "Tavern Dweller Power"
                        : item.Kind == ChainItemKind.ActivatedAbility ? "Ability" : "Trigger";
                    string target = item.Targets.Count > 0 ? " -> " + string.Join(", ", item.Targets.Select(t => _text.Name(_state, t))) : "";
                    string text = item.Text.Length > 0 ? " [" + item.Text + "]" : "";
                    GUILayout.Label("   " + (i == _state.Chain.Count - 1 ? "TOP  " : "     ") + kind + ": "
                                    + _text.Name(item.SourceDefinitionId) + " (" + item.Controller + ")" + target + text, _labelStyle);
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private string DecisionText(PendingDecision d)
        {
            switch (d.Kind)
            {
                case DecisionKind.Mulligan: return "keep or mulligan (London)";
                case DecisionKind.BottomCards: return "put " + d.Count + " card(s) on the bottom of the deck";
                case DecisionKind.DeclareAttackers: return "declare attackers (one at a time), then Done";
                case DecisionKind.DeclareBlockers: return "declare blockers (one at a time), then Done";
                case DecisionKind.ChooseTriggerTarget: return "choose a target for " + _text.Name(d.Trigger.SourceDefinitionId);
                case DecisionKind.DiscardToHandSize: return "discard " + d.Count + " card(s) down to 7";
                case DecisionKind.DiscardCards: return "discard " + d.Count + " card(s)";
                case DecisionKind.ChooseFromTop: return "put one of the top " + d.Count + " cards into your hand";
                case DecisionKind.PayAnyGold:
                    return "Dice Game: pay any amount of Gold (bids so far: "
                           + (d.Bids.Count == 0 ? "none" : string.Join(", ", d.Bidders.Select((p, i) => p + " " + d.Bids[i]))) + ")";
                case DecisionKind.ChooseObject:
                case DecisionKind.YesNo:
                    return d.Prompt + " (" + _text.Name(d.SourceDefinitionId) + ")";
                case DecisionKind.PayTax: return "pay " + d.Count + " or " + _text.Name(_state, d.Card) + " is countered";
                case DecisionKind.TopOrBottom: return "top card of your deck is " + _text.Name(_state, d.Card) + ": leave it or put it on the bottom";
                case DecisionKind.KeepLegendary:
                    return "Legendary rule: keep one " + _text.Name(_state, d.Choices[0]) + ", the others go to the graveyard";
                case DecisionKind.AssignCombatDamage:
                    return "divide " + _text.Name(_state, d.Card) + "'s " + d.Count + " combat damage";
                case DecisionKind.OrderTriggers:
                    return "order your triggers: pick the one to put on the Chain next (the last one put on resolves first)";
                default: return d.Kind.ToString();
            }
        }

        /// <summary>Everything about the hovered card (or the clicked one).</summary>
        private void DrawInspector(Rect r)
        {
            GUILayout.BeginArea(r, GUI.skin.box);
            var id = !_focus.IsNone ? _focus : _hover;
            var card = id.IsNone ? null : _state.FindObject(id);
            if (card == null || card.IsHidden)
            {
                GUILayout.Label("Card details", _headerStyle);
                GUILayout.Label("Hover over a card to read it. Click a card to keep it here and to see only its actions.", _labelStyle);
            }
            else
            {
                _inspectScroll = GUILayout.BeginScrollView(_inspectScroll);
                GUILayout.Label(_text.Details(_state, card), _labelStyle);
                GUILayout.EndScrollView();
            }
            if (_savedPath != null && !_state.IsGameOver) GUILayout.Label("Saved: " + _savedPath, _labelStyle);
            GUILayout.EndArea();
        }

        private void DrawRules(Rect r)
        {
            GUILayout.BeginArea(r, GUI.skin.box);
            _rulesScroll = GUILayout.BeginScrollView(_rulesScroll);
            GUILayout.Label("Quick rules (full rules: docs/GAME_DESIGN.md)", _bigStyle);
            GUILayout.Label(RulesText, _labelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private const string RulesText =
            "GOAL\n" +
            "Bring the opponent from 30 life to 0. You also lose if you must draw from an empty deck.\n\n" +
            "TURN\n" +
            "Start (untap, +1 max mana up to 10, refill) → Draw → Main phase 1 → Combat → Main phase 2 → End. " +
            "The first player skips their first draw. Hand size 7 at the end of your turn.\n\n" +
            "MANA AND GOLD\n" +
            "• Mana grows by 1 each of your turns (max 10) and refills. It only exists on your own turn.\n" +
            "• Unspent mana becomes Gold at the end of your turn (max 5 Gold).\n" +
            "• Creatures and other permanents are paid with mana only.\n" +
            "• Instants, Sorceries and abilities use mana first, then Gold. Gold is what you use on the opponent's turn.\n" +
            "• Invest and \"Pay N Gold\" costs are paid with Gold only.\n" +
            "• Tip: mana is spent first automatically. Cast creatures before spells if you want the Gold to pay for the spell.\n\n" +
            "COMBAT\n" +
            "• Attackers tap. Blocking doesn't tap, and creatures can block the turn they arrive.\n" +
            "• Several blockers can block one attacker; the attacker divides its damage among them.\n" +
            "• Trample: damage beyond lethal on every blocker goes to the player.\n\n" +
            "DAMAGE STAYS\n" +
            "Damage on creatures does not wear off. A creature dies when its Health left reaches 0. Only healing removes damage. " +
            "Losing a buff can't kill a creature (it stays at 1 Health).\n\n" +
            "TAVERN DWELLER\n" +
            "Your face card. Its passive always works, and its Power can be used once each turn (also on the opponent's turn), " +
            "paid with mana first, then Gold.\n\n" +
            "THE CHAIN\n" +
            "Spells and abilities go on the Chain; the last one added resolves first. You can answer with Instants and abilities " +
            "whenever you have priority.\n\n" +
            "USING THIS TABLE\n" +
            "• Your possible actions are the buttons on the right. Cards you can use are green; click one to filter the actions.\n" +
            "• Hover over any card to read it in Card details.\n" +
            "• Undo takes back the last action. Auto-pass skips moments where passing is your only option.\n" +
            "• Each finished game is saved in the Playtests folder next to the game. Add your notes at the top of the file.";

        private void DrawActions(Rect r)
        {
            GUILayout.BeginArea(r, GUI.skin.box);
            var who = _engine.WaitingOn(_state);
            if (!who.HasValue)
            {
                GUILayout.Label("No actions: the game is over.", _labelStyle);
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label("Actions for " + who.Value + (IsBot(who.Value) ? " (bot)" : ""), _headerStyle);
            var legal = _engine.GetLegalActions(_state, who.Value);
            var shown = _focus.IsNone
                ? legal
                : legal.Where(a => a.Card == _focus || a.BlockedAttacker == _focus
                                   || a.Target.HasValue && !a.Target.Value.IsPlayer && a.Target.Value.Object == _focus).ToList();

            if (!_focus.IsNone)
            {
                if (GUILayout.Button("Showing actions for " + _text.Name(_state, _focus) + " (click to show all)", _buttonStyle))
                    _focus = ObjectId.None;
                if (shown.Count == 0) GUILayout.Label("Nothing to do with that card right now.", _labelStyle);
            }

            _actionsScroll = GUILayout.BeginScrollView(_actionsScroll);
            GUI.enabled = !IsBot(who.Value);
            foreach (var a in shown)
            {
                if (GUILayout.Button(_text.Describe(_state, a), _buttonStyle))
                {
                    Do(a);
                    break; // the list is stale after acting
                }
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    AddActionCards(a, _actionCardsNext);
            }
            GUI.enabled = true;
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>The objects an action is about: the card, the attacker it blocks, its targets, what it sacrifices, damage recipients.</summary>
        private void AddActionCards(PlayerAction a, HashSet<ObjectId> into)
        {
            if (!a.Card.IsNone) into.Add(a.Card);
            if (!a.BlockedAttacker.IsNone) into.Add(a.BlockedAttacker);
            if (!a.Sacrifice.IsNone) into.Add(a.Sacrifice);
            foreach (var t in a.Targets)
                if (!t.IsPlayer) into.Add(t.Object);
            if (a.Kind == ActionKind.AssignCombatDamage && _state.Pending?.Choices != null)
            {
                into.Add(_state.Pending.Card);
                for (int i = 0; i < a.Division.Length && i < _state.Pending.Choices.Count; i++)
                    if (a.Division[i] > 0) into.Add(_state.Pending.Choices[i]);
            }
            if (_state.Pending?.Kind == DecisionKind.KeepLegendary && a.Target.HasValue) into.Add(a.Target.Value.Object);
        }

        private void DrawLog(Rect r)
        {
            GUILayout.BeginArea(r, GUI.skin.box);
            GUILayout.Label("Log", _headerStyle);
            _logScroll = GUILayout.BeginScrollView(_logScroll);
            foreach (var line in _log) GUILayout.Label(line, _labelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
