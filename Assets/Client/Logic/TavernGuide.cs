using System;
using System.Collections.Generic;
using System.Linq;

namespace RestartedTavern.Client.Logic
{
    /// <summary>One picture in a guide entry: a screenshot from the game (Resources/Guide/&lt;Image&gt;) and what it shows.</summary>
    public sealed class GuideFrame
    {
        public string Image;
        public string Caption;
    }

    /// <summary>One Tavern Guide page: a mechanic, card type, keyword or part of the screen.</summary>
    public sealed class GuideEntry
    {
        public string Id;
        public string Title;
        public string Category;
        /// <summary>Rich text (TMP: &lt;b&gt;), paragraphs separated by blank lines.</summary>
        public string Text;
        /// <summary>Other words players might search for.</summary>
        public string[] Aliases = Array.Empty<string>();
        /// <summary>Real cards that show it, drawn live next to the text.</summary>
        public string[] Cards = Array.Empty<string>();
        /// <summary>Screenshots, played as a slideshow when there are several (like a GIF).</summary>
        public GuideFrame[] Frames = Array.Empty<GuideFrame>();
        public string[] Related = Array.Empty<string>();
    }

    /// <summary>
    /// The Tavern Guide (user, 2026-10-10): the in-game encyclopedia. Every mechanic of the locked v1.0 rules
    /// (GAME_DESIGN.md) with a text explanation, screenshots and example cards; searchable. New mechanics add an entry here
    /// (TavernGuideTests checks that every keyword the cards explain has one).
    /// </summary>
    public static class TavernGuide
    {
        public static readonly string[] Categories =
            { "Basics", "Rounds & Mana", "Cards", "Combat", "The Chain", "Keywords", "Rules Terms", "Playing the Game" };

        private static List<GuideEntry> _entries;
        public static IReadOnlyList<GuideEntry> Entries => _entries ??= Build();

        public static GuideEntry Get(string id) => Entries.FirstOrDefault(e => e.Id == id);

        /// <summary>
        /// Entries matching every word of the query, best first: title, then aliases, then category, then text. An empty
        /// query lists everything in category order.
        /// </summary>
        public static List<GuideEntry> Search(string query)
        {
            var words = (query ?? "").ToLowerInvariant().Split(new[] { ' ', ',', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return Entries.OrderBy(e => Array.IndexOf(Categories, e.Category)).ToList();
            var scored = new List<(GuideEntry Entry, int Score)>();
            foreach (var e in Entries)
            {
                int total = 0;
                bool all = true;
                foreach (var w in words)
                {
                    int s = Score(e, w);
                    if (s == 0) { all = false; break; }
                    total += s;
                }
                if (all) scored.Add((e, total));
            }
            return scored.OrderByDescending(x => x.Score).ThenBy(x => Array.IndexOf(Categories, x.Entry.Category))
                .Select(x => x.Entry).ToList();
        }

        private static int Score(GuideEntry e, string w)
        {
            string title = e.Title.ToLowerInvariant();
            if (title == w) return 100;
            if (title.StartsWith(w, StringComparison.Ordinal)) return 80;
            if (title.Contains(w)) return 60;
            if (e.Aliases.Any(a => a.ToLowerInvariant() == w)) return 55;
            if (e.Aliases.Any(a => a.ToLowerInvariant().Contains(w))) return 45;
            if (e.Category.ToLowerInvariant().Contains(w)) return 20;
            if (Plain(e.Text).ToLowerInvariant().Contains(w)) return 10;
            return 0;
        }

        /// <summary>The text without rich-text tags.</summary>
        public static string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text ?? "", "<[^>]+>", "");

        private static GuideFrame F(string image, string caption) => new GuideFrame { Image = image, Caption = caption };

        private static List<GuideEntry> Build()
        {
            GuideEntry E(string id, string title, string category, string text, string[] aliases = null, string[] cards = null,
                GuideFrame[] frames = null, string[] related = null) =>
                new GuideEntry
                {
                    Id = id, Title = title, Category = category, Text = text,
                    Aliases = aliases ?? Array.Empty<string>(), Cards = cards ?? Array.Empty<string>(),
                    Frames = frames ?? Array.Empty<GuideFrame>(), Related = related ?? Array.Empty<string>(),
                };

            return new List<GuideEntry>
            {
                // ------------------------------------------------------------------ Basics
                E("winning", "Winning the Game", "Basics",
                    "Both players start at <b>30 life</b>, shown on your Tavern Dweller's portrait. Bring your opponent to <b>0 life</b> to win.\n\n"
                    + "You also lose if you have to <b>draw from an empty deck</b>. Decks are 60 cards, so this only happens in very long games.",
                    new[] { "win", "lose", "life", "goal", "victory", "defeat", "empty deck" },
                    frames: new[] { F("table", "Your Tavern Dweller is bottom left, your opponent's top left. Life is the red gem.") },
                    related: new[] { "tavern_dwellers", "table", "rounds" }),

                E("table", "The Table", "Basics",
                    "<b>Bottom</b>: your hand, fanned out. Hover a card to lift it, drag it up to play it.\n\n"
                    + "<b>Left</b>: the Tavern Dwellers, yours at the bottom, your opponent's at the top, with life and the Power coin.\n\n"
                    + "<b>Middle</b>: your creatures below the <b>combat lane</b>, your opponent's above it. Spells waiting on the <b>Chain</b> show as bubbles in the lane.\n\n"
                    + "<b>Right</b>: deck, graveyard and exile piles (click to look through them), the blue <b>mana</b> gems, the yellow <b>Gold</b> diamonds, the <b>attack token</b> and the big <b>context button</b>.",
                    new[] { "layout", "screen", "board", "battlefield", "zones", "graveyard", "exile", "deck", "piles" },
                    frames: new[] { F("table", "The whole table at the start of the tutorial.") },
                    related: new[] { "actions", "mana", "chain" }),

                E("tavern_dwellers", "Tavern Dwellers", "Basics",
                    "Every deck is led by a <b>Tavern Dweller</b>, a tavern regular you play as. It isn't one of your 60 cards: it sits in its own zone on the left.\n\n"
                    + "The Tavern Dweller <b>is you</b>: your life is its life, and attacking a player means attacking their Tavern Dweller. It never attacks or blocks and can't be removed.\n\n"
                    + "Each one has an always-on <b>passive</b> and a <b>Power</b> (see Powers). It also picks your deck's <b>two factions</b>.",
                    new[] { "dweller", "hero", "leader", "face", "passive", "portrait", "nexus", "champion" },
                    new[] { "skabba", "mukk_the_grub_king" },
                    new[] { F("dweller", "Hover (or right-click) a Tavern Dweller to read its passive and Power.") },
                    new[] { "powers", "factions", "winning" }),

                E("powers", "Powers", "Basics",
                    "Each Tavern Dweller has a <b>Power</b>: an activated ability you can use <b>once each round</b>. Click the coin next to your portrait when it glows.\n\n"
                    + "It's paid like any ability (<b>Gold first</b>, then mana) and goes on the <b>Chain</b>, so it works at instant speed: as your action, or in response to your opponent. They can respond to it too.",
                    new[] { "power", "hero power", "dweller power", "coin" },
                    new[] { "grizzle_coinflick", "keeper_z00" },
                    new[] { F("dweller", "The Power is written under the passive. Its coin sits next to the portrait.") },
                    new[] { "tavern_dwellers", "activated", "gold" }),

                E("factions", "Factions", "Basics",
                    "There are five factions. Every card belongs to one, or is <b>Neutral</b> (any deck can play it).\n\n"
                    + "<b>Shadow Money Wizards</b>: shady deals with Gold, card draw, big spells. <b>Goobers</b>: goblins that go wide, burn and steal Gold. "
                    + "<b>Sensationalists</b>: death, sacrifice, drains, Curses and the graveyard. <b>Evergrowing Wild</b>: huge creatures, Trample, fights and healing. "
                    + "<b>Glitterworld</b>: pings, drones and Equipment.\n\n"
                    + "Each Tavern Dweller unlocks a fixed <b>pair</b> of factions: your deck uses those two plus Neutral.",
                    new[] { "faction", "wizards", "goobers", "sensationalists", "wild", "glitterworld", "neutral", "colors" },
                    new[] { "barrel_bomber", "hungry_shade", "pulse_blade" },
                    new[] { F("editor", "The deck editor's faction tabs.") },
                    new[] { "deck_building", "tavern_dwellers" }),

                E("deck_building", "Deck Building", "Basics",
                    "A deck is <b>exactly 60 cards</b> plus one Tavern Dweller. You may play up to <b>4 copies</b> of a card. Every card must be from your Tavern Dweller's two factions or Neutral.\n\n"
                    + "Open <b>Deck Editor</b> on the main menu. <b>New deck</b> starts by picking a Tavern Dweller; a prototype deck opens as a copy you can change. "
                    + "Click a card to add it, right-click (or click its row) to take one out. Battle refuses a deck that isn't legal yet.",
                    new[] { "deck", "deck editor", "60 cards", "copies", "4 copies", "collection", "build" },
                    frames: new[] { F("editor", "Faction tabs, the card book, filters and search; your list on the right.") },
                    related: new[] { "factions", "rarity" }),

                E("mulligan", "Mulligan", "Basics",
                    "You start with <b>7 cards</b>. Before the game you can <b>keep</b> them or <b>mulligan</b>.\n\n"
                    + "A mulligan (the London mulligan) shuffles your hand back and draws 7 new cards. Then you put <b>1 card on the bottom</b> of your deck for each mulligan you took.",
                    new[] { "starting hand", "keep", "redraw", "opening hand", "london" },
                    frames: new[] { F("mulligan", "Keep with the context button, or pick Mulligan.") },
                    related: new[] { "going_first", "hand_size" }),

                E("going_first", "Coin Toss and Going First", "Basics",
                    "A <b>coin toss</b> picks who goes first: that player leads round 1 and holds the attack token.\n\n"
                    + "There's no compensation for going second: everyone draws in round 1, and the round leader <b>changes every round</b>, so going first evens out. "
                    + "You can skip the coin animation in Settings.",
                    new[] { "coin", "toss", "first player", "who starts", "start" },
                    frames: new[] { F("coin", "The coin lands on whoever goes first.") },
                    related: new[] { "attack_token", "rounds" }),

                // ------------------------------------------------------------------ Rounds & Mana
                E("rounds", "Rounds", "Rounds & Mana",
                    "A <b>round</b> is everyone's turn at once (like Legends of Runeterra). Each round:\n\n"
                    + "<b>1. Start</b>: everyone gets +1 max mana (up to 10) and refills. Only the player with the <b>attack token</b> untaps their permanents.\n"
                    + "<b>2. Draw</b>: everyone draws a card.\n"
                    + "<b>3. Actions</b>: players take one action at a time, starting with the round leader. When everyone passes in a row, the round ends.\n"
                    + "<b>4. End</b>: everyone discards down to 7, unspent mana becomes Gold, and \"until end of round\" effects end. The attack token moves on.\n\n"
                    + "Cards count in <b>rounds</b>: \"at the start of each round\" triggers for every player, and \"once each round\" is once per round.",
                    new[] { "turn", "round", "turn structure", "phases", "end of turn", "start of turn" },
                    frames: new[] { F("round", "Round 2: both players have 2 mana and the attack token moved to Mukk.") },
                    related: new[] { "actions", "attack_token", "mana", "gold" }),

                E("actions", "Actions and Passing", "Rounds & Mana",
                    "Players act <b>one at a time</b>. An action is: play a card, use an ability or Power, or attack (round leader only). Then it's the other player's action.\n\n"
                    + "Nothing to do? <b>Pass</b>. If your opponent just passed, the button says <b>End round</b>: passing now ends the round.\n\n"
                    + "Everything goes through the <b>context button</b> on the right (or Space). Its label always says what it will do: Keep, Pass, End round, OK, Attack, Block, No blocks...",
                    new[] { "pass", "end round", "context button", "priority", "space", "whose turn" },
                    frames: new[] { F("context", "The context button and the phase tracker.") },
                    related: new[] { "rounds", "chain", "controls" }),

                E("attack_token", "Round Leader and Attack Token", "Rounds & Mana",
                    "Each round has a <b>leader</b>, who acts first and holds the <b>attack token</b> (ATK). Only the token holder can attack, once that round, as one of their actions.\n\n"
                    + "The token passes every round, so in a 1v1 you attack every other round. In the rounds between, your opponent attacks and you block.\n\n"
                    + "The token holder is also the only one who <b>untaps</b> at the start of the round.",
                    new[] { "leader", "token", "atk", "attack token", "round leader" },
                    frames: new[] { F("context", "ATK sits next to the leader's gems.") },
                    related: new[] { "attacking", "tapping", "rounds" }),

                E("mana", "Mana", "Rounds & Mana",
                    "Mana pays for cards. At the start of every round, every player's <b>max mana goes up by 1</b> (up to <b>10</b>) and refills.\n\n"
                    + "Mana lasts the <b>whole round</b>: spend it on your actions or on responses. Mana has no colour.\n\n"
                    + "A card's cost is the blue gem in its top left corner. Hand cards show their <b>current</b> cost: green means cheaper than printed.",
                    new[] { "mana", "crystals", "gems", "cost", "max mana", "resources" },
                    frames: new[] { F("mana", "Your mana column (bottom right): 2/2 in round 2, filled gems are available.") },
                    related: new[] { "gold", "rounds" }),

                E("gold", "Gold", "Rounds & Mana",
                    "Unspent mana isn't wasted. At the <b>end of the round</b>, each unspent mana becomes <b>1 Gold</b> (the yellow diamonds), up to a cap of <b>3</b>. Extra is lost.\n\n"
                    + "<b>Gold pays for</b> Instants, Sorceries and abilities (Equip and Powers too). It's <b>always spent first</b>, automatically, then mana pays the rest.\n\n"
                    + "<b>Gold can't pay for permanents</b> (creatures, Equipment, Relics, Curses): those are mana only. <b>Invest</b> and \"Pay N Gold\" can only be paid with Gold.\n\n"
                    + "Some cards change your Gold cap (\"Your Gold cap is 8\").",
                    new[] { "gold", "spell mana", "bank", "banking", "unspent mana", "gold cap", "cap" },
                    new[] { "tip_jar", "offshore_account" },
                    new[] { F("gold", "Two Gold each: the yellow diamonds beside the context button (yours below, the opponent's above).") },
                    new[] { "mana", "invest", "bank", "spend_gold" }),

                E("hand_size", "Drawing and Hand Size", "Rounds & Mana",
                    "Everyone draws 1 card at the start of every round, round 1 included.\n\n"
                    + "Your <b>maximum hand size is 7</b>: at the end of each round you discard down to 7.\n\n"
                    + "Drawing from an <b>empty deck</b> makes you lose.",
                    new[] { "draw", "discard", "hand", "max hand", "seven", "7" },
                    related: new[] { "rounds", "winning" }),

                // ------------------------------------------------------------------ Cards
                E("reading_cards", "Reading a Card", "Cards",
                    "<b>Top left</b>: the cost. <b>Top tab</b>: the card type and subtypes (CREATURE — GOOBER). <b>Middle</b>: the name, keyword plates and rules text. "
                    + "<b>Bottom</b>: the faction icon and the rarity gem. Creatures have <b>Power</b> (yellow, left) and <b>Health</b> (red, right).\n\n"
                    + "Hover a card to zoom it: boxes beside it explain every keyword. Right-click a card on the table to pin the zoom.\n\n"
                    + "On the table, buffs show in <b>green</b> and damage in <b>red</b>.",
                    new[] { "card", "anatomy", "zoom", "hover", "power", "health", "stats", "cost", "text" },
                    new[] { "brawling_runt" },
                    new[] { F("hover", "A hand card lifted, with its keyword box.") },
                    new[] { "rarity", "creatures", "glows" }),

                E("creatures", "Creatures", "Cards",
                    "Creatures are played as one of your actions and stay on the battlefield. They have <b>Power</b> (the damage they deal) and <b>Health</b>.\n\n"
                    + "They can attack and use Tap abilities the round they arrive: there's <b>no summoning sickness</b>.\n\n"
                    + "Damage on them is <b>permanent</b> until healed.",
                    new[] { "creature", "unit", "minion", "summon", "power", "health" },
                    new[] { "brawling_runt", "vine_spider" },
                    new[] { F("play", "Goober Rascal after being played.") },
                    new[] { "permanent_damage", "attacking", "summoning_sickness" }),

                E("instants", "Instants", "Cards",
                    "An <b>Instant</b> can be played whenever you have priority: as your action, or <b>in response</b> to your opponent (even during combat).\n\n"
                    + "Instants can be paid with <b>Gold</b> (spent first). They do their thing and go to the graveyard.",
                    new[] { "instant", "fast", "response", "trick", "instant speed" },
                    new[] { "spark_snot", "hush_money" },
                    new[] { F("target", "Spark Snot: an Instant that targets a creature.") },
                    new[] { "chain", "responding", "sorceries" }),

                E("sorceries", "Sorceries", "Cards",
                    "A <b>Sorcery</b> can only be played as <b>one of your actions</b>, with nothing on the Chain. It can be paid with Gold (spent first).",
                    new[] { "sorcery", "slow", "spell" },
                    new[] { "gob_gang", "primal_clash" },
                    related: new[] { "instants", "actions" }),

                E("equipment", "Equipment", "Cards",
                    "<b>Equipment</b> is a permanent (paid with mana only). <b>Equip X</b> attaches it to a creature you control: pay X (Gold first) as one of your actions, with an empty Chain.\n\n"
                    + "The creature gets the bonus. Equipping again moves it; losing the bonus can't kill (see Permanent Damage). When the creature leaves, the Equipment <b>stays</b> on the battlefield, unattached.\n\n"
                    + "On the table it's tucked behind its creature; hover the creature to fan it out.",
                    new[] { "equip", "gear", "weapon", "attach", "armor", "artifact" },
                    new[] { "pulse_blade", "gilded_knuckles" },
                    new[] { F("equip", "Hover Tank wearing two Equipment, tucked behind it."), F("equip2", "Its zoom: printed 4/5, now bigger with the Equipment.") },
                    new[] { "equip", "relics", "curses" }),

                E("relics", "Relics", "Cards",
                    "A <b>Relic</b> is a permanent that isn't a creature: ongoing effects or abilities. Played as your action, with mana only.",
                    new[] { "relic", "artifact", "enchantment", "permanent" },
                    new[] { "tip_jar", "fireworks_stand" },
                    related: new[] { "activated", "equipment" }),

                E("curses", "Curses", "Cards",
                    "A <b>Curse</b> is attached to an <b>enemy creature</b> or an <b>opponent</b> and does something bad to it. Played as your action, with mana only.\n\n"
                    + "A Curse on a creature is tucked behind it; a Curse on a player sits on <b>that player's side</b> of the table, tagged <b>Your Curse</b> or <b>Their Curse</b>. "
                    + "The player who cast it still controls it.\n\n"
                    + "If what it's attached to leaves, the Curse goes to its owner's graveyard. The Sensationalists' speciality.",
                    new[] { "curse", "hex", "aura", "debuff" },
                    new[] { "hex_of_withering", "curse_of_rot" },
                    new[] { F("curse", "Hex of Withering tucked behind the creature it's on."), F("curse2", "The cursed creature's zoom shows the lost Power.") },
                    new[] { "equipment", "relics" }),

                E("tokens", "Tokens", "Cards",
                    "A <b>token</b> is a creature made by an effect (\"Create two 1/1 Goobers\"). It isn't a card: when it leaves the battlefield it simply disappears.\n\n"
                    + "On the table, tokens are drawn as domes.",
                    new[] { "token", "create", "goober token", "spawn", "summon" },
                    new[] { "gob_gang", "goober_token" },
                    new[] { F("wide", "Two Goober tokens attacking with the Rascal.") },
                    new[] { "creatures" }),

                E("rarity", "Rarity and Legendary", "Cards",
                    "The gem at the bottom shows rarity: <b>Common</b> (green), <b>Uncommon</b> (blue), <b>Rare</b> (purple), <b>Legendary</b> (gold).\n\n"
                    + "<b>Legendary rule</b>: you can control only one permanent with a given Legendary name. If you'd have two, you choose which to keep; the other goes to the graveyard. "
                    + "Your opponent can have their own copy, and you can still put 4 in your deck.",
                    new[] { "rarity", "legendary", "common", "uncommon", "rare", "gem", "unique" },
                    new[] { "snik", "apex_of_the_green_deep" },
                    related: new[] { "reading_cards", "deck_building" }),

                // ------------------------------------------------------------------ Combat
                E("attacking", "Attacking", "Combat",
                    "If you hold the <b>attack token</b>, one of your actions can be an attack. <b>Drag</b> creatures into the combat lane (or click them), then press <b>Attack</b>. "
                    + "Nothing happens until you press it, so you can rearrange.\n\n"
                    + "Attacking <b>taps</b> your creatures (they tilt), unless they have Vigilance. They stay tapped until your next attack round, so they can't block in between.\n\n"
                    + "After attackers and after blockers, both players can still respond.",
                    new[] { "attack", "combat", "lane", "declare attackers", "swing", "hit face" },
                    frames: new[]
                    {
                        F("attack1", "1. You hold the attack token."),
                        F("attack2", "2. Drag the creature into the lane and press Attack."),
                        F("attack3", "3. Unblocked: Mukk loses 1 life. The Rascal is tapped."),
                    },
                    related: new[] { "blocking", "combat_damage", "attack_token", "tapping" }),

                E("blocking", "Blocking", "Combat",
                    "When your opponent attacks, your <b>untapped</b> creatures can block. Drag a creature onto an attacker (or click it, then the attacker), then press <b>Block</b>. "
                    + "Press <b>No blocks</b> to take the damage.\n\n"
                    + "Each blocker blocks one attacker, but several blockers can gang up on one attacker. Blocking doesn't tap.\n\n"
                    + "A blocked attacker and its blockers deal their damage to each other instead of to you.",
                    new[] { "block", "defend", "blocker", "declare blockers", "no blocks" },
                    frames: new[]
                    {
                        F("block1", "1. Mukk attacks with Vine Spider."),
                        F("block2", "2. Brawling Runt steps in front of it."),
                        F("block3", "3. Both deal 2: the Runt dies, the Spider keeps its damage."),
                    },
                    related: new[] { "attacking", "combat_damage", "flying", "reach" }),

                E("combat_damage", "Combat Damage", "Combat",
                    "All combat damage happens <b>at the same time</b>. Unblocked attackers hit the player. Blocked attackers and their blockers hit each other.\n\n"
                    + "When several blockers block one attacker, the attacker's controller <b>splits its damage</b> however they like (the game only asks when it can't kill them all). "
                    + "Damage beyond what a creature needs to die is lost, unless the attacker has <b>Trample</b>.",
                    new[] { "damage", "combat", "split", "divide", "assign damage", "multiple blockers" },
                    frames: new[] { F("wide", "Three attackers into one blocker: whatever isn't blocked hits.") },
                    related: new[] { "trample", "permanent_damage", "blocking" }),

                E("permanent_damage", "Permanent Damage", "Combat",
                    "The big one: <b>damage stays</b> on creatures from round to round. It only goes away when the creature is <b>healed</b> or dies. "
                    + "The red Health number on the table is what's left.\n\n"
                    + "So chip damage adds up: a ping today finishes a creature tomorrow, and big creatures wear down over time.\n\n"
                    + "<b>Losing a buff can't kill</b>: when a Health bonus ends (\"until end of round\" wears off, an Equipment moves), a creature that was alive keeps at least 1 Health.",
                    new[] { "damage", "wounds", "health", "chip damage", "damaged", "buff" },
                    new[] { "spark_snot", "canopy_critter" },
                    new[] { F("block3", "The Vine Spider keeps its 2 damage: 1 Health left.") },
                    new[] { "healing", "damaged", "combat_damage" }),

                E("tapping", "Tapping and Untapping", "Combat",
                    "A <b>tapped</b> creature is tilted. Tapped creatures can't attack or block.\n\n"
                    + "Attacking taps (unless Vigilance), and <b>Tap:</b> abilities tap the creature as their cost.\n\n"
                    + "You only <b>untap</b> at the start of rounds where <b>you hold the attack token</b>. So a creature that attacked, or used a Tap ability, can't block during your opponent's next attack.",
                    new[] { "tap", "tapped", "untap", "tilt", "exhausted", "tap:" },
                    new[] { "grove_elder" },
                    new[] { F("attack3", "The Rascal attacked, so it's tapped until your next attack round.") },
                    new[] { "vigilance", "attack_token", "activated" }),

                E("summoning_sickness", "No Summoning Sickness", "Combat",
                    "Creatures can attack and use Tap abilities <b>the round they arrive</b>. There's no summoning sickness and no Haste.\n\n"
                    + "It's fair because players alternate actions: your opponent always gets an action between your creature arriving and attacking.",
                    new[] { "haste", "summoning sickness", "sick", "rush", "charge" },
                    related: new[] { "creatures", "attacking" }),

                E("healing", "Healing and Life", "Combat",
                    "<b>Heal X</b> removes up to X damage from a creature. \"Heal fully\" removes all of it. Healing your Tavern Dweller restores life.\n\n"
                    + "Nothing heals on its own: healing only comes from cards and Powers.\n\n"
                    + "<b>Gaining life</b> (Lifelink, drains) can't take you above your <b>starting life</b> (30).",
                    new[] { "heal", "healing", "gain life", "life gain", "restore", "repair" },
                    new[] { "jungle_remedy", "barkeeps_tonic" },
                    related: new[] { "permanent_damage", "lifelink" }),

                // ------------------------------------------------------------------ The Chain
                E("chain", "The Chain", "The Chain",
                    "Spells, abilities and triggers don't happen right away: they wait on the <b>Chain</b> (the bubbles in the middle), so the other player can respond.\n\n"
                    + "When both players pass in a row, the <b>newest</b> item resolves first. Then everyone can respond again, until the Chain is empty.\n\n"
                    + "Lines from a bubble show what it targets. Press <b>OK</b> to let it resolve.",
                    new[] { "stack", "chain", "resolve", "priority", "respond", "bubbles", "lifo" },
                    frames: new[] { F("chain", "Spark Snot on the Chain, aimed at the Vine Spider.") },
                    related: new[] { "responding", "targets", "triggers", "counter" }),

                E("responding", "Responding", "The Chain",
                    "Whenever something goes on the Chain, the other player gets a chance to answer with an <b>Instant</b>, an ability or a Power. Responses don't use up an action.\n\n"
                    + "Combat has its own windows: after attackers are declared and after blockers, both players can still play Instants before damage.",
                    new[] { "response", "instant speed", "priority", "react", "answer" },
                    new[] { "bounced_check", "apex_instinct" },
                    related: new[] { "chain", "instants" }),

                E("targets", "Targets", "The Chain",
                    "A card that targets says so (\"target creature\", \"any target\"). Pick targets by <b>clicking</b> them, or drop the card straight onto one. Legal targets glow red, and an arrow follows the pointer.\n\n"
                    + "Even when there's only one legal target you click it, so nothing is cast by surprise. Right-click or Esc cancels.\n\n"
                    + "If <b>all</b> of a spell's targets are gone when it resolves, it <b>fizzles</b> and does nothing. If only some are, it skips those.",
                    new[] { "target", "aim", "fizzle", "arrow", "any target", "choose" },
                    new[] { "spark_snot", "barrel_bomber" },
                    new[] { F("target", "Spark Snot picking its target: legal targets glow.") },
                    new[] { "chain", "glows" }),

                E("triggers", "Triggered Abilities", "The Chain",
                    "<b>\"When\"</b>, <b>\"whenever\"</b> and <b>\"at\"</b> abilities trigger on their own and go on the Chain, so they can be responded to. Arrival and Last Breath are triggers.\n\n"
                    + "If you have several at once, you choose their order. When one source triggers for several things at once, it goes on the Chain as <b>one</b> item that happens once per event (shown as ×N).\n\n"
                    + "Your card glows green while its trigger picks a target or waits on the Chain.",
                    new[] { "trigger", "whenever", "when", "at the start", "at the end", "x2", "batch" },
                    new[] { "fuse_goober", "spark_drone" },
                    related: new[] { "arrival", "last_breath", "chain" }),

                E("activated", "Activated Abilities", "The Chain",
                    "Abilities written <b>\"[Cost]: [Effect]\"</b> are used by clicking the card when it glows. They go on the Chain and work at <b>instant speed</b>, unless they say \"only as a sorcery\".\n\n"
                    + "A number cost is paid <b>Gold first</b>, then mana. \"Pay N Gold\" is Gold only. \"Activate only once each round\" means once per round.",
                    new[] { "ability", "activate", "abilities", "cost", "tap:" },
                    new[] { "wound_dresser", "grove_elder" },
                    related: new[] { "powers", "gold", "tapping" }),

                E("counter", "Counter", "The Chain",
                    "<b>Counter target spell</b>: the spell goes to the graveyard without doing anything. A countered ability does nothing (a countered Power still counts as used).\n\n"
                    + "A <b>tax</b> (\"unless its controller pays 3\") can be paid with Gold first, then mana.",
                    new[] { "counterspell", "counter", "tax", "unless pays", "negate" },
                    new[] { "hush_money", "counterfeit_coin" },
                    related: new[] { "chain", "responding" }),

                // ------------------------------------------------------------------ Keywords
                E("flying", "Flying", "Keywords",
                    "A creature with <b>Flying</b> can only be blocked by creatures with <b>Flying</b> or <b>Reach</b>. It can block anything.",
                    new[] { "fly", "flyer", "flier", "evasion", "air" },
                    new[] { "spark_drone", "hungry_shade" },
                    related: new[] { "reach", "blocking" }),

                E("reach", "Reach", "Keywords",
                    "A creature with <b>Reach</b> can block creatures with Flying.",
                    new[] { "block flyers", "anti air" },
                    new[] { "vine_spider", "caravan_guard" },
                    new[] { F("spider", "Vine Spider: Reach and Vigilance, explained beside the zoom.") },
                    new[] { "flying", "blocking" }),

                E("trample", "Trample", "Keywords",
                    "When a creature with <b>Trample</b> is blocked, damage beyond what's needed to kill its blockers goes through to the player.\n\n"
                    + "It has to assign lethal damage to every blocker first, and damage already on a blocker counts: a wounded blocker soaks less.\n\n"
                    + "Trample only works in combat, not in fights.",
                    new[] { "overflow", "excess damage", "pierce" },
                    new[] { "razorhide_boar", "thornback_ravager" },
                    new[] { F("boar", "Razorhide Boar: Trample. Mukk's passive gives it +1/+0.") },
                    new[] { "combat_damage", "fight" }),

                E("lifelink", "Lifelink", "Keywords",
                    "Damage dealt by a creature with <b>Lifelink</b> also heals its controller by that much (never above starting life).",
                    new[] { "lifesteal", "drain", "life gain", "vampire" },
                    new[] { "candlelit_acolyte", "hungry_shade" },
                    related: new[] { "healing" }),

                E("vigilance", "Vigilance", "Keywords",
                    "Attacking doesn't tap a creature with <b>Vigilance</b>, so it can still block in your opponent's attack round.",
                    new[] { "doesn't tap", "untapped", "defender" },
                    new[] { "vine_spider", "ironbark_grizzly" },
                    new[] { F("spider", "Vine Spider has Reach and Vigilance.") },
                    new[] { "tapping", "attacking" }),

                E("cant_block", "Can't Block", "Keywords",
                    "This creature can't be declared as a blocker. Usually on cheap, aggressive creatures.",
                    new[] { "cannot block", "can't block", "aggressive" },
                    new[] { "goober_rascal" },
                    new[] { F("hover", "Goober Rascal: Can't block.") },
                    new[] { "blocking" }),

                E("invest", "Invest", "Keywords",
                    "<b>Invest X</b> is an optional extra cost paid <b>only with Gold</b>. Pay it when you play the card to get the bonus.\n\n"
                    + "Invest costs are 3 or less, so your banked Gold can always cover them.",
                    new[] { "invest", "overload", "kicker", "extra cost", "bonus" },
                    new[] { "barkeeps_tonic", "open_casket" },
                    related: new[] { "gold" }),

                E("arrival", "Arrival", "Keywords",
                    "<b>Arrival</b> triggers when this enters the battlefield (a battlecry). It goes on the Chain like any trigger.",
                    new[] { "enters", "etb", "battlecry", "enter the battlefield", "play" },
                    new[] { "spark_drone", "barrel_bomber" },
                    related: new[] { "triggers", "last_breath" }),

                E("last_breath", "Last Breath", "Keywords",
                    "<b>Last Breath</b> triggers when this creature dies (a deathrattle). Sacrificing it counts as dying.",
                    new[] { "dies", "deathrattle", "death", "on death" },
                    new[] { "fuse_goober", "cellar_rat" },
                    related: new[] { "triggers", "sacrifice", "arrival" }),

                E("equip", "Equip", "Keywords",
                    "<b>Equip X</b>: pay X (Gold first, then mana) to attach this Equipment to a creature you control. Only as one of your actions, with an empty Chain. It's an ability, so it goes on the Chain.",
                    new[] { "equip", "attach", "gear" },
                    new[] { "pulse_blade", "neon_shiv" },
                    new[] { F("equip", "Equipment tucked behind its creature.") },
                    new[] { "equipment" }),

                // ------------------------------------------------------------------ Rules terms
                E("fight", "Fight", "Rules Terms",
                    "Two creatures <b>fight</b>: each deals damage equal to its Power to the other, at the same time. It isn't combat, so Trample doesn't apply. The damage stays, like all damage.",
                    new[] { "fights", "duel", "brawl" },
                    new[] { "primal_clash", "apex_instinct" },
                    related: new[] { "permanent_damage", "trample" }),

                E("sacrifice", "Sacrifice", "Rules Terms",
                    "<b>Sacrifice</b>: put a permanent you control into its graveyard. It can't be prevented, and a sacrificed creature <b>dies</b> (Last Breath triggers).\n\n"
                    + "Some cards ask for a sacrifice as an extra cost.",
                    new[] { "sac", "sacrifice", "extra cost" },
                    new[] { "fling_the_runt", "midnight_ritual" },
                    related: new[] { "last_breath" }),

                E("x_costs", "X Costs", "Rules Terms",
                    "A cost with <b>X</b>: you choose X when you play it (at least 1) and pay the printed cost plus X. Paid like the card: spells and abilities can use Gold, permanents can't.\n\n"
                    + "\"Pay any amount of Gold (X)\" is an X paid only with Gold.",
                    new[] { "x", "variable", "x spell" },
                    new[] { "goober_avalanche", "arc_cascade" },
                    related: new[] { "gold" }),

                E("bank", "Bank", "Rules Terms",
                    "You <b>bank</b> Gold when your unspent mana becomes Gold at the end of the round. Only the Gold you actually gain counts (mana lost to the cap isn't banked).\n\n"
                    + "Cards can say \"Whenever you bank Gold\" or \"Whenever you bank 2 or more Gold\".",
                    new[] { "banking", "bank gold", "end of round" },
                    new[] { "interest_broker", "grizzled_innkeeper" },
                    related: new[] { "gold", "spend_gold" }),

                E("spend_gold", "Spend Gold", "Rules Terms",
                    "You <b>spend</b> Gold when you pay Gold for something: a spell, ability, Invest, X or a tax. \"Whenever you spend Gold\" triggers <b>once per payment</b>, however much it was.\n\n"
                    + "Gold that's lost or stolen isn't spent.",
                    new[] { "spend", "spent", "pay gold" },
                    new[] { "traveling_bard", "coin_juggler" },
                    related: new[] { "gold", "bank" }),

                E("damaged", "Damaged", "Rules Terms",
                    "A <b>damaged</b> creature has damage on it (its Health left is below its max). Because damage is permanent, many cards care about it.\n\n"
                    + "\"Can't be healed\": heal effects do nothing to it, but it can still get bigger.",
                    new[] { "wounded", "injured", "health remaining", "can't be healed" },
                    new[] { "kick_em_while_theyre_down", "blood_price" },
                    related: new[] { "permanent_damage", "healing" }),

                E("gain_control", "Gain Control", "Rules Terms",
                    "You take a creature: it keeps its damage and counters, and it can attack and use Tap abilities for you right away. "
                    + "\"Until end of round\" control ends when the round ends.",
                    new[] { "steal", "control", "mind control", "take" },
                    new[] { "silver_tongued_deal", "hostile_takeover" },
                    related: new[] { "bounce" }),

                E("bounce", "Return to Hand", "Rules Terms",
                    "A permanent returned to its owner's hand comes back as a fresh card: <b>its damage is gone</b>. A token returned to hand just disappears.",
                    new[] { "bounce", "return", "hand", "reset" },
                    new[] { "bounced_check", "golden_parachute" },
                    related: new[] { "tokens", "permanent_damage" }),

                E("may", "\"May\" and \"Up To\"", "Rules Terms",
                    "A card only gives you a choice if it says <b>\"may\"</b> or <b>\"up to\"</b>. Everything else happens whether you like it or not: an Arrival that deals damage has to hit something.",
                    new[] { "optional", "choice", "may", "up to", "forced" },
                    new[] { "ambush_predator", "goober_shaman" },
                    related: new[] { "triggers", "targets" }),

                // ------------------------------------------------------------------ Playing the Game
                E("controls", "Controls", "Playing the Game",
                    "<b>Drag</b> a card from your hand onto the table to play it, or click a glowing card. Drag creatures into the lane to attack, onto attackers to block.\n\n"
                    + "<b>Space</b>: the context button. <b>Esc</b>: cancel targeting, close a zoom, or open Settings. <b>Right-click</b>: cancel targeting, or pin a card's zoom. "
                    + "<b>Ctrl+Z</b>: undo your last action. <b>F1</b>: debug panel. <b>F2</b>: report a bug.",
                    new[] { "keys", "keyboard", "shortcuts", "hotkeys", "mouse", "drag", "undo", "space", "esc", "escape" },
                    related: new[] { "actions", "settings", "bug_report" }),

                E("glows", "Glows and Colours", "Playing the Game",
                    "<b>Blue</b> glow: you can use it. <b>Red</b>: a legal target. <b>Orange</b>: can attack or block. <b>White</b>: picked. "
                    + "<b>Green</b>: your card whose trigger is going off.\n\n"
                    + "Numbers: <b>green</b> = buffed or cheaper than printed, <b>red</b> Health = damaged.",
                    new[] { "glow", "highlight", "colours", "colors", "outline" },
                    frames: new[] { F("target", "Red glows: the legal targets for Spark Snot.") },
                    related: new[] { "targets", "reading_cards" }),

                E("phase_tracker", "Phase Tracker", "Playing the Game",
                    "At the right end of the combat lane, the tracker shows the <b>current phase</b> (Action, Attackers, Blockers, Combat Damage, or the round's start and end) and, smaller under it, the one that comes <b>next</b>. "
                    + "Banners call out \"They attack!\" and \"Block!\" when it's your move in combat.",
                    new[] { "phase", "step", "tracker", "main phase", "combat step" },
                    frames: new[] { F("phases", "Mukk attacks: it's your block, and combat damage comes next.") },
                    related: new[] { "rounds", "attacking", "blocking" }),

                E("tutorial", "Tutorial", "Playing the Game",
                    "<b>Tutorial</b> on the main menu plays three guided rounds against Mukk: playing creatures, attacking, blocking, permanent damage, Instants and the Chain. "
                    + "After round 3 the bot plays for real. Skip it any time.",
                    new[] { "learn", "how to play", "guide", "start here", "new player" },
                    related: new[] { "winning", "table" }),

                E("play_modes", "Playing a Friend or the AI", "Playing the Game",
                    "<b>Play</b> on the main menu: choose for each seat <b>Human</b> or <b>Bot</b>, a deck and its Tavern Dweller, then Battle.\n\n"
                    + "Two humans play <b>hot-seat</b> on one screen: the table covers itself between players so you can't see each other's hands.",
                    new[] { "hot-seat", "hotseat", "multiplayer", "versus", "ai", "bot", "friend", "local" },
                    related: new[] { "deck_building" }),

                E("sounds", "Sounds", "Playing the Game",
                    "Every card makes a sound when it's played, and each kind has its own: a creature lands with a low <b>boom</b> and a chord, "
                    + "an Instant <b>whooshes</b> up into a bright note, a Sorcery <b>swells</b> into a full chord, Equipment rings like muffled metal, "
                    + "a Relic <b>chimes</b> and a Curse sinks. Attacks beat a war drum, blocks thud, and dying creatures dissolve.\n\n"
                    + "Each faction plays its own instrument: warm brass for the Goobers, a wooden marimba for the Wild, celesta for Glitterworld, "
                    + "a dark choir for the Sensationalists and a harp for the Wizards.\n\n"
                    + "Two soft taps mean someone <b>passed</b>; a deep boom starts each round. Change or mute them in Settings, and play every one of them on its <b>Sound board</b>.",
                    new[] { "audio", "sound", "sfx", "music", "volume", "pass sound" },
                    related: new[] { "settings", "actions" }),

                E("settings", "Settings", "Playing the Game",
                    "Open Settings from the main menu or with <b>Esc</b> in a game (the game waits).\n\n"
                    + "<b>Sound</b>: volume (Off, Low, Medium, High), and the pass sound and trigger ticks on or off. "
                    + "<b>Display</b>: animation speed, fullscreen or windowed, resolution, VSync. <b>Help</b>: keyword hint boxes and the coin toss.",
                    new[] { "options", "speed", "resolution", "fullscreen", "vsync", "volume", "sound", "audio", "mute" },
                    frames: new[] { F("settings", "The Settings panel.") },
                    related: new[] { "controls" }),

                E("bug_report", "Reporting a Bug", "Playing the Game",
                    "Something wrong? Press <b>Report bug</b> (top left, on every screen) or <b>F2</b>, write what happened and save.\n\n"
                    + "It saves a folder in <b>BugReports</b> next to the game with a screenshot and the full game state, which replays the game exactly. Send that folder to the developer.",
                    new[] { "bug", "report", "feedback", "issue", "f2", "crash" },
                    frames: new[] { F("bug", "Write what went wrong, then Save report.") },
                    related: new[] { "controls" }),
            };
        }
    }
}
