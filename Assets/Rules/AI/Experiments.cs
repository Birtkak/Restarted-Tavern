using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.AI
{
    /// <summary>
    /// The roadmap step 3 balance questions as bot-vs-bot experiments: game length (and how
    /// to even it out), whether damage matters, going second, and the Gold cap.
    /// </summary>
    public static class Experiments
    {
        public sealed class Section
        {
            public string Title;
            public string Question;
            public List<MatchConfig> Configs = new List<MatchConfig>();
            public List<MatchResult> Results = new List<MatchResult>();
        }

        private sealed class Deck
        {
            public string Name;
            public List<string> Cards;
        }

        public static List<Section> Build(int games)
        {
            var decks = new[]
            {
                new Deck { Name = "Goober Mob", Cards = PrototypeCards.GooberMobDeck() },
                new Deck { Name = "Jungle Stampede", Cards = PrototypeCards.JungleStampedeDeck() },
                new Deck { Name = "Zoo Patrol", Cards = PrototypeCards.ZooPatrolDeck() },
                new Deck { Name = "Vesper's Ledger", Cards = PrototypeCards.VespersLedgerDeck() },
                new Deck { Name = "Sparkwrench Scrappers", Cards = PrototypeCards.SparkwrenchScrappersDeck() },
            };
            var sections = new List<Section>();

            MatchConfig M(string name, Deck a, Deck b, Action<FormatConfig> tweak = null, BotStyle styleA = null, BotStyle styleB = null)
            {
                var f = FormatConfig.Standard();
                tweak?.Invoke(f);
                return new MatchConfig
                {
                    Name = name, Format = f, Games = games,
                    DeckAName = a.Name, DeckA = a.Cards, StyleA = styleA ?? BotStyle.Greedy(),
                    DeckBName = b.Name, DeckB = b.Cards, StyleB = styleB ?? BotStyle.Greedy(),
                };
            }

            var roundRobin = new Section
            {
                Title = "Round robin (current rules, Greedy bots)",
                Question = "How long are games, and does damage on creatures decide anything? Every deck against every deck.",
            };
            for (int i = 0; i < decks.Length; i++)
                for (int j = i; j < decks.Length; j++)
                    roundRobin.Configs.Add(i == j
                        ? M(decks[i].Name + " mirror", decks[i], decks[j])
                        : M(decks[i].Name + " vs " + decks[j].Name, decks[i], decks[j]));
            sections.Add(roundRobin);

            var styles = new Section
            {
                Title = "Bot play styles",
                Question = "Do the results hold with a defensive player? Control blocks freely, keeps blockers home and saves Gold. "
                           + "In \"Greedy vs Control\", A win% is the Greedy side.",
            };
            foreach (var d in decks)
                styles.Configs.Add(M(d.Name + " mirror, Control vs Control", d, d, null, BotStyle.Control(), BotStyle.Control()));
            foreach (var d in decks)
                styles.Configs.Add(M(d.Name + " mirror, Greedy vs Control", d, d, null, BotStyle.Greedy(), BotStyle.Control()));
            sections.Add(styles);

            var life = new Section
            {
                Title = "Game length lever: starting life",
                Question = "Can starting life even out game length between fast and slow decks? Compare with the 30-life mirrors in the round robin.",
            };
            foreach (int l in new[] { 25, 35 })
                foreach (var d in decks)
                    life.Configs.Add(M(d.Name + " mirror, " + l + " life", d, d, f => f.StartingLife = l));
            sections.Add(life);

            var damage = new Section
            {
                Title = "Permanent damage (GAME_DESIGN §7.3)",
                Question = "What changes when damage wears off at end of turn like in MTG? Compare with the round-robin mirrors (permanent damage).",
            };
            foreach (var d in decks)
                damage.Configs.Add(M(d.Name + " mirror, damage wears off (MTG)", d, d, f => f.DamageWearsOff = true));
            sections.Add(damage);

            var second = new Section
            {
                Title = "Going second (GAME_DESIGN §3)",
                Question = "First-player win rate in mirrors (50% is fair). Current rule: everyone draws on turn 1; the second player has +1 mana on their first turn.",
            };
            foreach (var d in new[] { decks[0], decks[1], decks[3] })
            {
                second.Configs.Add(M(d.Name + " mirror, current rule", d, d));
                second.Configs.Add(M(d.Name + " mirror, old rule (first player skips draw)", d, d, f => f.FirstPlayerSkipsDraw = true));
                second.Configs.Add(M(d.Name + " mirror, current rule + 1 starting Gold", d, d, f => f.SecondPlayerStartingGold = 1));
            }
            sections.Add(second);

            var cap = new Section
            {
                Title = "Gold cap (GAME_DESIGN §5.2)",
                Question = "Does the cap matter in the decks with the most Gold use?",
            };
            foreach (var d in new[] { decks[3], decks[4] })
                foreach (int c in new[] { 3, 5, 8 })
                    cap.Configs.Add(M(d.Name + " mirror, Gold cap " + c, d, d, f => f.GoldCap = c));
            sections.Add(cap);
            return sections;
        }

        public static void Run(List<Section> sections, CardDatabase db, Action<MatchResult> progress = null)
        {
            foreach (var s in sections)
                s.Results = MatchRunner.RunAll(s.Configs, db, progress);
        }

        public static string ToMarkdown(List<Section> sections, int games, TimeSpan duration)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Simulation Report");
            sb.AppendLine();
            sb.AppendLine("Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). "
                          + "They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.");
            sb.AppendLine();
            sb.AppendLine($"{games} games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time {duration.TotalSeconds:0}s.");
            sb.AppendLine();
            sb.AppendLine("Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · "
                          + "**Turns** = average game length (both players' turns) ± standard deviation · **Long** / **Short** = share of games over "
                          + MatchResult.LongGameTurns + " / under " + MatchResult.ShortGameTurns + " turns · "
                          + "**Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · "
                          + "**Chip→death** = share of *chip damage* (damage a creature carried into a later turn) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · "
                          + "**Wounded** = share of creatures carrying damage at the start of a turn · **Deaths** / **Heal** / **Gold spent** are per game · "
                          + "**Wasted** = share of unspent mana lost to the Gold cap.");
            foreach (var s in sections)
            {
                sb.AppendLine();
                sb.AppendLine("## " + s.Title);
                sb.AppendLine();
                sb.AppendLine("*" + s.Question + "*");
                sb.AppendLine();
                sb.AppendLine("| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Wasted | Draws |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
                foreach (var r in s.Results)
                {
                    sb.Append("| ").Append(r.Config.Name)
                      .Append(" | ").Append(Pct(r.WinRateA))
                      .Append(" | ").Append(Pct(r.FirstPlayerWinRate))
                      .Append(" | ").Append(F(r.AvgTurns)).Append(" ± ").Append(F(r.TurnsStdDev))
                      .Append(" | ").Append(Pct(r.LongGameShare))
                      .Append(" | ").Append(Pct(r.ShortGameShare))
                      .Append(" | ").Append(Pct(r.DamageThatKilledShare))
                      .Append(" | ").Append(Pct(r.ChipThatKilledShare))
                      .Append(" | ").Append(Pct(r.WoundedShare))
                      .Append(" | ").Append(F(r.PerGame(r.CreatureDeaths)))
                      .Append(" | ").Append(F(r.PerGame(r.HealingDone)))
                      .Append(" | ").Append(F(r.PerGame(r.GoldSpent)))
                      .Append(" | ").Append(Pct(r.GoldWastedShare))
                      .Append(" | ").Append(r.Draws)
                      .AppendLine(" |");
                }
            }
            return sb.ToString();
        }

        private static string Pct(double v) => (v * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        private static string F(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
