using System;
using System.Collections.Generic;
using System.Linq;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using RestartedTavern.Rules.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    /// <summary>
    /// The main menu (overhaul, user 2026-10-10), LoR / Hearthstone home screen style: the title and a column of big
    /// tiles on the left (Continue, Play, Tutorial, Deck editor, Quit), a fan of showcase cards (one per faction) on the
    /// right. Play opens the match setup page: per seat human / bot, a deck and a Tavern Dweller that can lead it.
    /// </summary>
    public sealed partial class TableView
    {
        private enum MenuPage { Home, Play }

        private MenuPage _menuPage = MenuPage.Home;
        private List<string> _showcase;

        private void DrawMenu()
        {
            var bg = Ui.FillPanel(_overlay, "Menu", Ui.Hex("#0C1018"), 0f, raycast: true);
            var glow = Ui.FillPanel(bg.transform, "Glow", Ui.Hex("#2C3650") * new Color(1, 1, 1, 0.55f));
            glow.sprite = Ui.GradientSprite;
            // Thin gold rules top and bottom, like a card frame around the screen.
            Ui.Panel(_overlay, "RuleTop", 60, 40, Ui.Width - 120, 2, Ui.Gold * new Color(1, 1, 1, 0.45f));
            Ui.Panel(_overlay, "RuleBottom", 60, Ui.Height - 42, Ui.Width - 120, 2, Ui.Gold * new Color(1, 1, 1, 0.45f));
            if (_menuPage == MenuPage.Play) DrawPlaySetup();
            else DrawHome();
        }

        private void DrawHome()
        {
            // Title.
            const float left = 110f;
            Ui.Tmp(_overlay, "RESTARTED", left, 78, 700, 90, 78f, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, outline: true);
            Ui.Tmp(_overlay, "TAVERN", left, 156, 700, 130, 120f, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, outline: true);
            Ui.Tmp(_overlay, "Pull up a chair. The cards are already dealt.", left + 4, 282, 700, 34, 22f, Ui.Gold * new Color(1, 1, 1, 0.8f), TextAnchor.MiddleLeft, FontStyle.Italic);

            float y = 346f;
            bool inGame = _battleStarted && !_s.State.IsGameOver;
            float step = inGame ? 112f : 122f; // six tiles with Continue
            if (inGame)
            {
                MenuTile(left, y, "CONTINUE", TutorialRunning ? "Back to the tutorial" : "Back to your game (round " + _s.State.RoundNumber + ")",
                    ContextOn, () => { _menuOpen = false; _dirty = true; }, "RESUME");
                y += step;
            }
            MenuTile(left, y, "PLAY", "Pick your deck and Tavern Dweller, then battle the AI or a friend", inGame ? ButtonColor : ContextOn,
                () => { _menuPage = MenuPage.Play; _dirty = true; });
            y += step;
            MenuTile(left, y, "TUTORIAL", "New here? Learn the rules in three guided rounds", Ui.Hex("#2A6A50"), StartTutorial, "START HERE");
            y += step;
            MenuTile(left, y, "DECK EDITOR", "Build your own decks from every faction", ButtonColor, () => OpenEditor());
            y += step;
            MenuTile(left, y, "TAVERN GUIDE", "Every rule, searchable", Ui.Hex("#2A5A6A"), () => OpenGuide(), w: 296f);
            MenuTile(left + 304f, y, "SETTINGS", "Speed, window, hints", ButtonColor, OpenSettings, w: 296f);
            y += step;
            MenuTile(left, y, "QUIT", "Leave the tavern", Ui.Hex("#4A2420"), () => { Debug.Log("Quit: the menu's QUIT tile"); Application.Quit(); });

            DrawShowcase();
            Ui.Tmp(_overlay, "Prototype · " + CardPool.PrototypeDecks().Count + " decks · " + _db.All.Count(d => !d.IsToken && !d.IsTavernDweller) + " cards",
                left, Ui.Height - 36, 800, 30, 15f, Ui.Cream * new Color(1, 1, 1, 0.4f), TextAnchor.MiddleLeft);
        }

        /// <summary>One home tile: a big gold-rimmed button with a title, a line under it and an optional badge.</summary>
        private void MenuTile(float x, float y, string title, string subtitle, Color color, Action click, string badge = null, float w = 600f)
        {
            const float h = 106f;
            var b = Ui.Button(_overlay, "", x, y, w, h, color, click);
            Ui.Panel(b.transform, "Accent", 14, 16, 6, h - 32, Ui.Gold);
            Ui.Tmp(b.transform, title, 40, 12, w - 110, 50, w < 600f ? 30f : 36f, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold, outline: true);
            Ui.Tmp(b.transform, subtitle, 42, 60, w - 90, 32, 18f, Ui.Cream * new Color(1, 1, 1, 0.75f), TextAnchor.MiddleLeft);
            Ui.Tmp(b.transform, ">", w - 60, 0, 40, h, 54f, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (badge == null) return;
            var tag = Ui.Panel(b.transform, "Badge", w - 190, -14, 150, 30, Ui.Hex("#E0A030"));
            Ui.AddOutline(tag.gameObject, new Color(0, 0, 0, 0.7f), 2f);
            Ui.FillTmp(tag.transform, badge, 16f, Ui.Hex("#1A1408"), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>The right side: a fan of five showcase cards (the rarest creature of each faction), faction names under it.</summary>
        private void DrawShowcase()
        {
            if (_showcase == null)
            {
                _showcase = new List<string>();
                foreach (var f in new[] { "goobers", "evergrowing_wild", "glitterworld", "sensationalists", "shadow_money_wizards" })
                {
                    var best = _db.All.Where(d => d.Faction == f && d.IsCreature && !d.IsToken)
                        .OrderByDescending(d => d.Rarity).ThenByDescending(d => d.Cost).FirstOrDefault();
                    if (best != null) _showcase.Add(best.Id);
                }
            }
            const float cw = 262f, ch = 366f, cx = 1300f, cy = 520f;
            int n = _showcase.Count;
            for (int i = 0; i < n; i++)
            {
                float off = i - (n - 1) / 2f;
                var rt = Ui.Rect(_overlay, "Showcase", 0, 0, cw, ch);
                rt.pivot = new Vector2(0.5f, 0.5f);
                var home = new Vector2(cx + off * 150f, -(cy + off * off * 14f));
                float tilt = -off * 7f;
                int sibling = rt.GetSiblingIndex();
                rt.anchoredPosition = home;
                rt.localEulerAngles = new Vector3(0, 0, tilt);
                CardFaces.Build(rt, ViewOf(_showcase[i], _s.Viewer), FaceStyle.Hand);
                // Hover lifts the card out of the fan (moved in place: a redraw would re-trigger the hover).
                EditorCard.Attach(rt.gameObject, null, null, on =>
                {
                    rt.anchoredPosition = on ? home + new Vector2(0f, 60f) : home;
                    rt.localEulerAngles = new Vector3(0, 0, on ? 0f : tilt);
                    rt.localScale = Vector3.one * (on ? 1.18f : 1f);
                    if (on) rt.SetAsLastSibling();
                    else rt.SetSiblingIndex(sibling);
                });
            }

            // The five factions as emblem discs.
            float ex = cx - 2f * 150f;
            foreach (var id in _showcase)
            {
                var style = CardFaces.Style(_db.Get(id).Faction);
                var disc = Ui.Circle(_overlay, "Faction", ex - 32f, 840f, 64f, style.Frame);
                Ui.AddOutline(disc.gameObject, Ui.Gold, 2f);
                Ui.FillTmp(disc.transform, style.Emblem, 18f, style.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Tmp(_overlay, style.Name, ex - 80f, 910f, 160f, 44f, 17f, Ui.Cream * new Color(1, 1, 1, 0.8f), TextAnchor.UpperCenter);
                ex += 150f;
            }
        }

        /// <summary>The match setup page: per seat human / bot, a prototype or custom deck and a Tavern Dweller that can lead it; Battle.</summary>
        private void DrawPlaySetup()
        {
            Ui.Button(_overlay, "BACK", 60, 62, 170, 54, ButtonColor, () => { _menuPage = MenuPage.Home; _dirty = true; }, 20);
            Ui.Tmp(_overlay, "PLAY", 0, 56, Ui.Width, 70, 54f, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, outline: true);
            var decks = CardPool.PrototypeDecks();
            for (int seat = 0; seat < 2; seat++)
            {
                int s = seat;
                _deck[s] = Mathf.Clamp(_deck[s], 0, decks.Count - 1);
                float x = 110 + s * 870;
                var deck = decks[_deck[s]];
                var column = Ui.Panel(_overlay, "Column", x - 20, 136, 840, 800, CoachColor);
                column.sprite = Ui.GradientSprite;
                Ui.Frame(column.transform, "ring", Ui.Gold * new Color(0.75f, 0.75f, 0.75f, 1f), 20f);
                Ui.Tmp(_overlay, s == 0 ? "PLAYER 1" : "PLAYER 2", x, 150, 400, 50, 32f, Ui.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Button(_overlay, _seats[s] == SeatKind.Human ? "Human" : "Bot", x + 600, 152, 200, 46,
                    _seats[s] == SeatKind.Human ? Mine : Theirs, () =>
                    {
                        _seats[s] = _seats[s] == SeatKind.Human ? SeatKind.Bot : SeatKind.Human;
                        _dirty = true;
                    }, 22);

                Ui.Label(_overlay, "Deck", x, 206, 400, 30, 20, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft, FontStyle.Bold);
                for (int d = 0; d < decks.Count; d++)
                {
                    int di = d;
                    Ui.Button(_overlay, decks[d].Name, x + (d % 2) * 405, 240 + (d / 2) * 58, 395, 50,
                        d == _deck[s] ? ContextOn : ButtonColor, () =>
                        {
                            _deck[s] = di;
                            _dweller[s] = null;
                            _dirty = true;
                        }, 20);
                }
                float y = 240 + (decks.Count + 1) / 2 * 58 + 6;
                Ui.Label(_overlay, deck.Description ?? "", x, y, 800, 56, 16, Ui.Hex("#D8C8B0"), TextAnchor.UpperLeft);

                y += 64;
                Ui.Label(_overlay, "Tavern Dweller", x, y, 400, 30, 20, Ui.Hex("#E0C890"), TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 35;
                var dwellers = MatchSetup.TavernDwellersFor(deck, _db);
                string chosen = _dweller[s] ?? deck.TavernDweller;
                for (int i = 0; i < dwellers.Count; i++)
                {
                    string id = dwellers[i];
                    Ui.Button(_overlay, _db.Get(id).Name + (id == deck.TavernDweller ? " (deck's own)" : ""),
                        x + (i % 2) * 405, y + (i / 2) * 58, 395, 50, id == chosen ? ContextOn : ButtonColor, () =>
                        {
                            _dweller[s] = id;
                            _dirty = true;
                        }, 20);
                }
                y += (dwellers.Count + 1) / 2 * 58 + 8;
                var def = _db.Get(chosen);
                var box = Ui.Panel(_overlay, "Dweller", x, y, 800, Math.Min(150f, 920f - y), new Color(0.95f, 0.9f, 0.8f, 0.92f));
                Ui.FillLabel(box.transform, def.Name + "\n" + def.Text, 17, new Color(0.15f, 0.1f, 0.05f), TextAnchor.UpperLeft, FontStyle.Normal, 8f);
            }
            var battle = Ui.Button(_overlay, "BATTLE", Ui.Width / 2f - 170, 952, 340, 84, ContextOn, () =>
            {
                // A deck from the editor may not be legal yet (60 cards, copies, factions).
                for (int s = 0; s < 2; s++)
                {
                    var d = decks[_deck[s]];
                    try { DeckValidator.Validate(_db, FormatConfig.Standard(), d.Cards, _dweller[s] ?? d.TavernDweller); }
                    catch (ArgumentException e) { ShowToast("Player " + (s + 1) + "'s deck " + d.Name + ": " + e.Message); return; }
                }
                _battleStarted = true;
                NewGame(_seed + 1, toss: true);
            }, 44);
            Ui.AddOutline(battle.gameObject, Ui.Hex("#FFE0A0"), 4f);
        }
    }
}
