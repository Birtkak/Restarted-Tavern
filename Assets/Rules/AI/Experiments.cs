using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RestartedTavern.Rules.Cards;

namespace RestartedTavern.Rules.AI
{
    /// <summary>
    /// The roadmap step 3 balance questions, as bot-vs-bot experiments:
    /// the Gold cap, the going-second compensation and the impact of permanent damage.
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

        private const string Goobers = "Goober Mob";
        private const string Jungle = "Jungle Stampede";
        private const string Zoo = "Zoo Patrol";

        public static List<Section> Build(int games)
        {
            var g = PrototypeCards.GooberMobDeck();
            var j = PrototypeCards.JungleStampedeDeck();
            var z = PrototypeCards.ZooPatrolDeck();
            var sections = new List<Section>();

            MatchConfig M(string name, string an, List<string> a, string bn, List<string> b, Action<FormatConfig> tweak = null)
            {
                var f = FormatConfig.Standard();
                tweak?.Invoke(f);
                return new MatchConfig { Name = name, Format = f, DeckAName = an, DeckA = a, DeckBName = bn, DeckB = b, Games = games };
            }

            var baseline = new Section
            {
                Title = "Baseline",
                Question = "How do the two prototype decks do against each other with the current rules?",
            };
            baseline.Configs.Add(M("Goober Mob vs Jungle Stampede", Goobers, g, Jungle, j));
            baseline.Configs.Add(M("Goober Mob mirror", Goobers, g, Goobers, g));
            baseline.Configs.Add(M("Jungle Stampede mirror", Jungle, j, Jungle, j));
            baseline.Configs.Add(M("Zoo Patrol vs Goober Mob", Zoo, z, Goobers, g));
            baseline.Configs.Add(M("Zoo Patrol vs Jungle Stampede", Zoo, z, Jungle, j));
            baseline.Configs.Add(M("Zoo Patrol mirror", Zoo, z, Zoo, z));
            sections.Add(baseline);

            var second = new Section
            {
                Title = "Going-second compensation (GAME_DESIGN §3)",
                Question = "How much should going second be compensated? Look at the first-player win rate in mirrors (50% is fair). "
                           + "Current rule (since 2026-10-09): the first player skips their first draw, and the second player has +1 mana on their first turn.",
            };
            var options = new (string label, Action<FormatConfig> tweak)[]
            {
                ("no compensation besides the draw skip", f => f.SecondPlayerFirstTurnBonusMana = 0),
                ("1 starting Gold (old rule)", f => { f.SecondPlayerFirstTurnBonusMana = 0; f.SecondPlayerStartingGold = 1; }),
                ("+1 mana on first turn (current rule)", f => { }),
                ("+1 mana on first turn and 1 Gold", f => f.SecondPlayerStartingGold = 1),
                ("+2 mana on first turn", f => f.SecondPlayerFirstTurnBonusMana = 2),
                ("+1 card and +1 mana on first turn", f => f.SecondPlayerExtraCards = 1),
            };
            foreach (var (label, tweak) in options)
            {
                second.Configs.Add(M("Goober mirror, 2nd player: " + label, Goobers, g, Goobers, g, tweak));
                second.Configs.Add(M("Jungle mirror, 2nd player: " + label, Jungle, j, Jungle, j, tweak));
            }
            sections.Add(second);

            var cap = new Section
            {
                Title = "Gold cap (GAME_DESIGN §5.2)",
                Question = "What does the cap of 5 do? Compare wasted mana, Gold spent and instant-speed play.",
            };
            foreach (int c in new[] { 3, 5, 7, 10 })
                cap.Configs.Add(M("Goober Mob vs Jungle Stampede, Gold cap " + c, Goobers, g, Jungle, j, f => f.GoldCap = c));
            sections.Add(cap);

            var damage = new Section
            {
                Title = "Permanent damage (GAME_DESIGN §7.3)",
                Question = "What does permanent damage change compared to MTG-style damage that wears off at end of turn?",
            };
            foreach (bool wearsOff in new[] { false, true })
            {
                string label = wearsOff ? "damage wears off (MTG)" : "permanent damage";
                damage.Configs.Add(M("Goober Mob vs Jungle Stampede, " + label, Goobers, g, Jungle, j, f => f.DamageWearsOff = wearsOff));
                damage.Configs.Add(M("Goober mirror, " + label, Goobers, g, Goobers, g, f => f.DamageWearsOff = wearsOff));
                damage.Configs.Add(M("Jungle mirror, " + label, Jungle, j, Jungle, j, f => f.DamageWearsOff = wearsOff));
                damage.Configs.Add(M("Zoo Patrol vs Jungle Stampede, " + label, Zoo, z, Jungle, j, f => f.DamageWearsOff = wearsOff));
                damage.Configs.Add(M("Zoo Patrol mirror, " + label, Zoo, z, Zoo, z, f => f.DamageWearsOff = wearsOff));
            }
            sections.Add(damage);
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
            sb.AppendLine("Bot-vs-bot results for roadmap step 3 (DEVELOPMENT §5). Both players are `GreedyBot`: a simple, deterministic, rule-based player. "
                          + "It plays like a careful beginner, so **treat these numbers as hints about the rules, not as card balance**. "
                          + "Human playtests on the debug table decide.");
            sb.AppendLine();
            sb.AppendLine($"{games} games per row · decks swap seats every game, and who goes first is random · games over 120 turns count as draws · run time {duration.TotalSeconds:0}s.");
            sb.AppendLine();
            sb.AppendLine("Columns: **A win%** = the first-named deck's win rate · **1st win%** = how often the player who went first won · **Turns** = average game length (both players' turns) · "
                          + "**Wasted** = the share of unspent mana lost to the Gold cap · **Gold spent** and **Opp-turn casts** are per game · "
                          + "**Wounded** = the share of creatures carrying damage at the start of a turn · **Deaths** = creature deaths per game · **Heal** = healing per game.");
            foreach (var s in sections)
            {
                sb.AppendLine();
                sb.AppendLine("## " + s.Title);
                sb.AppendLine();
                sb.AppendLine("*" + s.Question + "*");
                sb.AppendLine();
                sb.AppendLine("| Matchup | A win% | 1st win% | Draws | Turns | Wasted | Gold spent | Opp-turn casts | Wounded | Deaths | Heal |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
                foreach (var r in s.Results)
                {
                    sb.Append("| ").Append(r.Config.Name)
                      .Append(" | ").Append(Pct(r.WinRateA))
                      .Append(" | ").Append(Pct(r.FirstPlayerWinRate))
                      .Append(" | ").Append(r.Draws)
                      .Append(" | ").Append(F(r.AvgTurns))
                      .Append(" | ").Append(Pct(r.GoldWastedShare))
                      .Append(" | ").Append(F(r.PerGame(r.GoldSpent)))
                      .Append(" | ").Append(F(r.PerGame(r.InstantsOnOpponentsTurn)))
                      .Append(" | ").Append(Pct(r.WoundedShare))
                      .Append(" | ").Append(F(r.PerGame(r.CreatureDeaths)))
                      .Append(" | ").Append(F(r.PerGame(r.HealingDone)))
                      .AppendLine(" |");
                }
            }
            return sb.ToString();
        }

        private static string Pct(double v) => (v * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        private static string F(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);
    }
}
