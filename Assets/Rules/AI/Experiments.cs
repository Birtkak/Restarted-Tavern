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
            public string TavernDweller;
        }

        public static List<Section> Build(int games)
        {
            var decks = new[]
            {
                new Deck { Name = "Goober Mob", Cards = CardPool.GooberMobDeck(), TavernDweller = CardPool.GooberMobTavernDweller },
                new Deck { Name = "Jungle Stampede", Cards = CardPool.JungleStampedeDeck(), TavernDweller = CardPool.JungleStampedeTavernDweller },
                new Deck { Name = "Zoo Patrol", Cards = CardPool.ZooPatrolDeck(), TavernDweller = CardPool.ZooPatrolTavernDweller },
                new Deck { Name = "Vesper's Ledger", Cards = CardPool.VespersLedgerDeck(), TavernDweller = CardPool.VespersLedgerTavernDweller },
                new Deck { Name = "Sparkwrench Scrappers", Cards = CardPool.SparkwrenchScrappersDeck(), TavernDweller = CardPool.SparkwrenchScrappersTavernDweller },
                new Deck { Name = "Auditor's Arsenal", Cards = CardPool.AuditorsArsenalDeck(), TavernDweller = CardPool.AuditorsArsenalTavernDweller },
            };
            var sections = new List<Section>();

            MatchConfig M(string name, Deck a, Deck b, Action<FormatConfig> tweak = null, BotStyle styleA = null, BotStyle styleB = null)
            {
                var f = FormatConfig.Standard();
                tweak?.Invoke(f);
                return new MatchConfig
                {
                    Name = name, Format = f, Games = games,
                    DeckAName = a.Name, DeckA = a.Cards, TavernDwellerA = a.TavernDweller, StyleA = styleA ?? BotStyle.Greedy(),
                    DeckBName = b.Name, DeckB = b.Cards, TavernDwellerB = b.TavernDweller, StyleB = styleB ?? BotStyle.Greedy(),
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
                Question = "First-player win rate in mirrors (50% is fair). Current rule (MTG default): the first player skips their turn-1 draw, no other compensation.",
            };
            foreach (var d in new[] { decks[0], decks[1], decks[3] })
            {
                second.Configs.Add(M(d.Name + " mirror, current rule (MTG)", d, d));
                second.Configs.Add(M(d.Name + " mirror, everyone draws + 2nd player +1 mana on turn 1", d, d,
                    f => { f.FirstPlayerSkipsDraw = false; f.SecondPlayerFirstTurnBonusMana = 1; }));
                second.Configs.Add(M(d.Name + " mirror, everyone draws + 2nd player +1 mana and 1 Gold", d, d,
                    f => { f.FirstPlayerSkipsDraw = false; f.SecondPlayerFirstTurnBonusMana = 1; f.SecondPlayerStartingGold = 1; }));
            }
            sections.Add(second);

            var mana = new Section
            {
                Title = "Mana model: round pool (GAME_DESIGN §5)",
                Question = "Does the round pool (everyone refills when a round starts, mana usable on any turn of the round, unspent mana "
                           + "banked at the end of the round) take away the first player's edge? Watch 1st win% (50% is fair).",
            };
            foreach (var d in decks)
            {
                mana.Configs.Add(M(d.Name + " mirror, mana per turn (current)", d, d));
                mana.Configs.Add(M(d.Name + " mirror, round pool", d, d, f => f.ManaPerRound = true));
                mana.Configs.Add(M(d.Name + " mirror, round pool + Gold first off-turn", d, d,
                    f => { f.ManaPerRound = true; f.GoldFirstOffTurn = true; }));
                mana.Configs.Add(M(d.Name + " mirror, rotating first player", d, d, f => f.RotateRoundLeader = true));
                mana.Configs.Add(M(d.Name + " mirror, rotating first player + round pool + Gold first", d, d,
                    f => { f.RotateRoundLeader = true; f.ManaPerRound = true; f.GoldFirstOffTurn = true; }));
                mana.Configs.Add(M(d.Name + " mirror, 1st player skips first mana", d, d, f => f.FirstPlayerSkipsFirstMana = true));
                mana.Configs.Add(M(d.Name + " mirror, 1st player skips first mana + round pool + Gold first", d, d,
                    f => { f.FirstPlayerSkipsFirstMana = true; f.ManaPerRound = true; f.GoldFirstOffTurn = true; }));
            }
            sections.Add(mana);

            var lor = new Section
            {
                Title = "Runeterra-style mana",
                Question = "Legends of Runeterra's mana in full turns: everyone refills when a round starts, unspent mana becomes Gold "
                           + "(spell mana) at the end of the round, spells and abilities spend Gold first, the round leader alternates "
                           + "and only they may attack (attack token), creatures can attack the turn they arrive. Watch Turns, Off-turn and 1st win%.",
            };
            foreach (var d in decks)
            {
                lor.Configs.Add(M(d.Name + " mirror, today's rules", d, d));
                lor.Configs.Add(M(d.Name + " mirror, Runeterra (Gold cap 3)", d, d, f => Copy(FormatConfig.Runeterra(3), f)));
                lor.Configs.Add(M(d.Name + " mirror, Runeterra (Gold cap 5)", d, d, f => Copy(FormatConfig.Runeterra(5), f)));
                lor.Configs.Add(M(d.Name + " mirror, Runeterra with summoning sickness", d, d,
                    f => { Copy(FormatConfig.Runeterra(3), f); f.NoSummoningSickness = false; }));
                lor.Configs.Add(M(d.Name + " mirror, Runeterra without the attack token", d, d,
                    f => { Copy(FormatConfig.Runeterra(3), f); f.AttackToken = false; f.NoSummoningSickness = false; }));
            }
            sections.Add(lor);

            var tavernDwellers = new Section
            {
                Title = "Tavern Dwellers (GAME_DESIGN §9)",
                Question = "What do Tavern Dwellers (passives and Powers, the universal Gold sink) change? Each mirror with and without Tavern Dwellers. "
                           + "Watch Wasted (mana lost to the Gold cap), Gold spent and game length.",
            };
            foreach (var d in decks)
            {
                tavernDwellers.Configs.Add(M(d.Name + " mirror, with Tavern Dwellers", d, d));
                tavernDwellers.Configs.Add(M(d.Name + " mirror, no Tavern Dwellers", d, d, f => f.TavernDwellersEnabled = false));
            }
            sections.Add(tavernDwellers);

            var cap = new Section
            {
                Title = "Gold cap (GAME_DESIGN §5.2)",
                Question = "Does the cap matter now that Tavern Dweller Powers and Equip spend Gold?",
            };
            foreach (var d in new[] { decks[2], decks[3], decks[4], decks[5] })
                foreach (int c in new[] { 3, 5, 8 })
                    cap.Configs.Add(M(d.Name + " mirror, Gold cap " + c, d, d, f => f.GoldCap = c));
            sections.Add(cap);
            return sections;
        }

        /// <summary>Copies every format setting from <paramref name="from"/> onto <paramref name="to"/>.</summary>
        private static void Copy(FormatConfig from, FormatConfig to)
        {
            foreach (var prop in typeof(FormatConfig).GetProperties())
                if (prop.CanWrite) prop.SetValue(to, prop.GetValue(from));
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
                          + "**Powers** = Tavern Dweller Powers used per game (in brackets: share used on an opponent's turn) · **Abil.** = other activated abilities per game (Equip, Tap abilities...) · **Off-turn** = spells cast and abilities/Powers used on an opponent's turn, per game · "
                          + "**Wasted** = share of unspent mana lost to the Gold cap.");
            foreach (var s in sections)
            {
                sb.AppendLine();
                sb.AppendLine("## " + s.Title);
                sb.AppendLine();
                sb.AppendLine("*" + s.Question + "*");
                sb.AppendLine();
                sb.AppendLine("| Matchup | A win% | 1st win% | Turns | Long | Short | Dmg→death | Chip→death | Wounded | Deaths | Heal | Gold spent | Powers | Abil. | Off-turn | Wasted | Draws |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
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
