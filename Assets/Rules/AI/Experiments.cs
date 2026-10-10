using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.AI
{
    /// <summary>
    /// The simulation report (playtest/SIMULATION_REPORT.md): every prototype deck against every deck under the
    /// Standard rules, with Greedy and Control bots.
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

        public sealed class Deck
        {
            public string Name;
            public List<string> Cards;
            public string TavernDweller;
        }

        /// <summary>The six prototype decks, each with its Tavern Dweller.</summary>
        public static Deck[] PrototypeDecks() => new[]
        {
            new Deck { Name = "Goober Mob", Cards = CardPool.GooberMobDeck(), TavernDweller = CardPool.GooberMobTavernDweller },
            new Deck { Name = "Jungle Stampede", Cards = CardPool.JungleStampedeDeck(), TavernDweller = CardPool.JungleStampedeTavernDweller },
            new Deck { Name = "Zoo Patrol", Cards = CardPool.ZooPatrolDeck(), TavernDweller = CardPool.ZooPatrolTavernDweller },
            new Deck { Name = "Vesper's Ledger", Cards = CardPool.VespersLedgerDeck(), TavernDweller = CardPool.VespersLedgerTavernDweller },
            new Deck { Name = "Sparkwrench Scrappers", Cards = CardPool.SparkwrenchScrappersDeck(), TavernDweller = CardPool.SparkwrenchScrappersTavernDweller },
            new Deck { Name = "Auditor's Arsenal", Cards = CardPool.AuditorsArsenalDeck(), TavernDweller = CardPool.AuditorsArsenalTavernDweller },
        };

        public static List<Section> Build(int games)
        {
            var decks = PrototypeDecks();
            var sections = new List<Section>();

            MatchConfig M(string name, Deck a, Deck b, BotStyle styleA = null, BotStyle styleB = null)
            {
                return new MatchConfig
                {
                    Name = name, Format = FormatConfig.Standard(), Games = games,
                    DeckAName = a.Name, DeckA = a.Cards, TavernDwellerA = a.TavernDweller, StyleA = styleA ?? BotStyle.Greedy(),
                    DeckBName = b.Name, DeckB = b.Cards, TavernDwellerB = b.TavernDweller, StyleB = styleB ?? BotStyle.Greedy(),
                };
            }

            var roundRobin = new Section
            {
                Title = "Round robin (Greedy bots)",
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
                styles.Configs.Add(M(d.Name + " mirror, Control vs Control", d, d, BotStyle.Control(), BotStyle.Control()));
            foreach (var d in decks)
                styles.Configs.Add(M(d.Name + " mirror, Greedy vs Control", d, d, BotStyle.Greedy(), BotStyle.Control()));
            sections.Add(styles);
            return sections;
        }

        public static void Run(List<Section> sections, CardDatabase db, Action<MatchResult> progress = null)
        {
            // One pool for all sections: see MatchRunner.RunAll.
            var all = MatchRunner.RunAll(sections.SelectMany(s => s.Configs), db, progress);
            int next = 0;
            foreach (var s in sections)
            {
                s.Results = all.GetRange(next, s.Configs.Count);
                next += s.Configs.Count;
            }
        }

        public static string ToMarkdown(List<Section> sections, int games, TimeSpan duration)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Simulation Report");
            sb.AppendLine();
            sb.AppendLine("Bot-vs-bot results under the Standard rules (Legends of Runeterra rounds). Players are `GreedyBot`s in two styles: **Greedy** (develops and races) and **Control** (blocks, holds back, saves Gold). "
                          + "They play like careful beginners, so **treat these numbers as hints about the rules, not as card balance**. Human playtests on the debug table decide.");
            sb.AppendLine();
            sb.AppendLine($"{games} games per row · decks swap seats every game, and who goes first is random · games over 120 rounds count as draws · run time {duration.TotalSeconds:0}s.");
            sb.AppendLine();
            sb.AppendLine("Columns: **A win%** = the first-named side's win rate · **1st win%** = how often the player who went first won · "
                          + "**Rounds** = average game length ± standard deviation · **Long** / **Short** = share of games over "
                          + MatchResult.LongGameRounds + " / under " + MatchResult.ShortGameRounds + " rounds · "
                          + "**Dmg→death** = share of all damage dealt to creatures that was still on a creature when it died (includes killing blows) · "
                          + "**Chip→death** = share of *chip damage* (damage a creature carried into a later round) that was still on it when it died; the rest was healed away or sat on survivors, so it **never decided anything** · "
                          + "**Wounded** = share of creatures carrying damage at the start of a round · **Deaths** / **Heal** / **Gold spent** are per game · "
                          + "**Powers** = Tavern Dweller Powers used per game (in brackets: share used while an opponent had the action) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Off-turn** = spells cast and abilities/Powers used while an opponent had the action, per game · "
                          + "**Wasted** = share of unspent mana lost to the Gold cap.");
            foreach (var s in sections)
            {
                sb.AppendLine();
                sb.AppendLine("## " + s.Title);
                sb.AppendLine();
                sb.AppendLine("*" + s.Question + "*");
                sb.AppendLine();
                sb.AppendLine("| Matchup | A win% | 1st win% | Rounds | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
                foreach (var r in s.Results)
                {
                    sb.Append("| ").Append(r.Config.Name)
                      .Append(" | ").Append(Pct(r.WinRateA))
                      .Append(" | ").Append(Pct(r.FirstPlayerWinRate))
                      .Append(" | ").Append(F(r.AvgRounds)).Append(" ± ").Append(F(r.RoundsStdDev))
                      .Append(" | ").Append(Pct(r.LongGameShare))
                      .Append(" | ").Append(Pct(r.ShortGameShare))
                      .Append(" | ").Append(Pct(r.DamageThatKilledShare))
                      .Append(" | ").Append(Pct(r.ChipThatKilledShare))
                      .Append(" | ").Append(Pct(r.WoundedShare))
                      .Append(" | ").Append(F(r.PerGame(r.CreatureDeaths)))
                      .Append(" | ").Append(F(r.PerGame(r.HealingDone)))
                      .Append(" | ").Append(F(r.PerGame(r.GoldSpent)))
                      .Append(" | ").Append(F(r.PerGame(r.PowersUsed)))
                      .Append(r.PowersUsed > 0 ? " (" + Pct((double)r.PowersOnOpponentsTurn / r.PowersUsed) + ")" : "")
                      .Append(" | ").Append(F(r.PerGame(r.AbilitiesActivated)))
                      .Append(" | ").Append(F(r.PerGame(r.InstantsOnOpponentsTurn + r.AbilitiesOnOpponentsTurn)))
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
