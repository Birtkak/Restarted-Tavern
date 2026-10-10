using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using RestartedTavern.Rules.Cards;

// Bug hunt (DEVELOPMENT §7.1): every prototype deck x Tavern Dweller pairing with the greedy bot, random play with
// the prototype decks, and random play with random decks from the whole card pool. After every action it checks the
// engine invariants and the client logic (context button, snapshot, picker, Chain bubbles); at the end of each game,
// replay determinism. A watchdog reports games that take longer than 15 s (hangs, exploding choices). Exit code 1
// when anything failed.
//
//   dotnet run -c Release --project Tools/BugHunt -- [-seeds 3] [-random 3000] [-randomDecks 3000] [-seed N] [-data path]
//   -seed N: only that game, with a line for every combat damage choice (for digging into a failure).
static class BugHunt
{
    static readonly object Lock = new object();
    static readonly List<string> Failures = new List<string>();
    static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
    static int Games, Actions;
    static bool Probe;
    static readonly Dictionary<string, (DateTime start, int n, string what)> Running = new Dictionary<string, (DateTime, int, string)>();
    // Profiling: ticks per phase, the slowest games, the biggest legal-action list.
    static long TEngine, TClient, TLegal, TApply, TReplay;
    static int MaxLegal; static string MaxLegalWhere;
    static readonly List<(double sec, int actions, string where)> Slow = new List<(double, int, string)>();
    static long Now => System.Diagnostics.Stopwatch.GetTimestamp();
    static void Add(ref long field, long since) => System.Threading.Interlocked.Add(ref field, Now - since);

    static void Fail(string where, string what)
    {
        lock (Lock)
        {
            string key = what.Length > 90 ? what.Substring(0, 90) : what;
            Counts.TryGetValue(key, out int n);
            Counts[key] = n + 1;
            if (n < 3) Failures.Add(where + ": " + what);
        }
    }

    static int Main(string[] a)
    {
        string Arg(string name, string fallback)
        {
            int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }
        if (Arg("-data", null) != null) CardPool.DataRoot = Arg("-data", null);
        int seeds = int.Parse(Arg("-seeds", "3"));
        int randomGames = int.Parse(Arg("-random", "3000"));
        var db = CardPool.CreateDatabase();
        var decks = CardPool.PrototypeDecks();
        var jobs = new List<(int d1, string t1, int d2, string t2, ulong seed, bool random)>();
        for (int d1 = 0; d1 < decks.Count; d1++)
            for (int d2 = 0; d2 < decks.Count; d2++)
                foreach (var t1 in MatchSetup.TavernDwellersFor(decks[d1], db))
                    foreach (var t2 in MatchSetup.TavernDwellersFor(decks[d2], db).Take(1))
                        for (int s = 1; s <= seeds; s++)
                            jobs.Add((d1, t1, d2, t2, (ulong)(s * 1000 + d1 * 37 + d2), false));
        var rng = new Random(12345);
        for (int i = 0; i < randomGames; i++)
        {
            int d1 = rng.Next(decks.Count), d2 = rng.Next(decks.Count);
            var o1 = MatchSetup.TavernDwellersFor(decks[d1], db);
            var o2 = MatchSetup.TavernDwellersFor(decks[d2], db);
            jobs.Add((d1, o1[rng.Next(o1.Count)], d2, o2[rng.Next(o2.Count)], (ulong)(500000 + i), true));
        }
        int randomDecks = int.Parse(Arg("-randomDecks", "3000"));
        for (int i = 0; i < randomDecks; i++) jobs.Add((-1, null, -1, null, (ulong)(900000 + i), true));
        if (Arg("-seed", null) != null)
        {
            Probe = true;
            ulong only = ulong.Parse(Arg("-seed", null));
            jobs = jobs.Where(j => j.seed == only).ToList();
            if (jobs.Count == 0) jobs.Add((-1, null, -1, null, only, true)); // a random-deck game by seed
        }
        Console.WriteLine(jobs.Count + " games");
        var t0 = DateTime.Now;
        var watchdog = new System.Threading.Thread(() =>
        {
            var reported = new HashSet<string>();
            while (true)
            {
                System.Threading.Thread.Sleep(5000);
                lock (Lock)
                    foreach (var kv in Running)
                        if ((DateTime.Now - kv.Value.start).TotalSeconds > 15 && reported.Add(kv.Key + kv.Value.n))
                            Console.WriteLine($"SLOW/HUNG {(DateTime.Now - kv.Value.start).TotalSeconds:0}s at action {kv.Value.n}: {kv.Key} | {kv.Value.what}");
            }
        }) { IsBackground = true };
        watchdog.Start();
        Parallel.ForEach(jobs, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, j =>
        {
            string where = j.d1 < 0 ? $"random-deck seed {j.seed}" : $"{(j.random ? "random" : "greedy")} decks {j.d1}/{j.d2} dwellers {j.t1}/{j.t2} seed {j.seed}";
            try { Play(j.d1, j.t1, j.d2, j.t2, j.seed, j.random, where); }
            catch (Exception e) { Fail(where, "EXCEPTION " + e.GetType().Name + ": " + e.Message + " @ " + e.StackTrace?.Split('\n').FirstOrDefault()?.Trim()); }
        });
        Console.WriteLine($"{Games} games, {Actions} actions, {(DateTime.Now - t0).TotalSeconds:0}s");
        double sf = 1.0 / System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine($"cpu seconds: engine checks {TEngine * sf:0.0}, client checks {TClient * sf:0.0}, legal {TLegal * sf:0.0}, apply {TApply * sf:0.0}, replay {TReplay * sf:0.0}");
        Console.WriteLine($"biggest legal list: {MaxLegal} ({MaxLegalWhere})");
        foreach (var g in Slow.OrderByDescending(x => x.sec).Take(8)) Console.WriteLine($"  slow: {g.sec:0.00}s {g.actions} actions  {g.where}");
        Console.WriteLine(Counts.Count == 0 ? "NO FAILURES" : Counts.Count + " distinct failures:");
        foreach (var kv in Counts.OrderByDescending(k => k.Value)) Console.WriteLine($"  x{kv.Value}  {kv.Key}");
        Console.WriteLine();
        foreach (var f in Failures) Console.WriteLine(f);
        return Counts.Count == 0 ? 0 : 1;
    }

    static void Play(int d1, string t1, int d2, string t2, ulong seed, bool random, string where)
    {
        var setup = d1 >= 0 ? MatchSetup.Duel(d1, t1, d2, t2, SeatKind.Human, SeatKind.Human, seed) : RandomSetup(seed);
        var s = new MatchSession(setup);
        var rng = new Random((int)seed);
        var db = s.Engine.Cards;
        int n = 0;
        long start = Now;
        lock (Lock) Running[where] = (DateTime.Now, 0, "");
        while (!s.State.IsGameOver)
        {
            if (s.HandoffPending) s.AcknowledgeHandoff();
            if (++n > 6000)
            {
                var last = s.History.Skip(s.History.Count - 6).Select(x => x.Kind + " " + (x.Card.IsNone ? "" : s.Text.Name(s.State, x.Card)) + (x.Targets != null && x.Targets.Length > 0 ? " -> " + string.Join(",", x.Targets.Select(t => s.Text.Name(s.State, t))) : ""));
                Fail(where, "game did not end in 6000 actions; last: " + string.Join(" | ", last));
                return;
            }
            lock (Lock) Running[where] = (Running[where].start, n, "R" + s.State.RoundNumber + " " + s.State.Step + " chain " + s.State.Chain.Count + " pending " + s.State.Pending?.Kind + " last " + (s.History.Count > 0 ? s.History[s.History.Count - 1].ToString() : ""));
            var st = s.State;
            if (Probe && st.Pending?.Kind == DecisionKind.AssignCombatDamage)
            {
                var dealer = st.FindOnBattlefield(st.Pending.Card);
                var w0 = System.Diagnostics.Stopwatch.StartNew();
                int count = s.Engine.GetLegalActions(st, st.Pending.Player).Count;
                Console.WriteLine($"#{n} {dealer?.DefinitionId} deals {st.Pending.Count} to {st.Pending.Choices.Count}: "
                    + string.Join(", ", st.Pending.Choices.Select(id => { var c = st.FindOnBattlefield(id); return c == null ? "gone" : c.DefinitionId + " health " + CharacteristicsCalculator.Compute(st, db, c).RemainingHealth; }))
                    + $" -> {count} options in {w0.ElapsedMilliseconds} ms");
            }
            long t = Now;
            CheckEngine(st, db, where, n);
            Add(ref TEngine, t);
            t = Now;
            CheckClient(s, rng, where, n);
            Add(ref TClient, t);

            t = Now;
            var legal = s.LegalForViewer();
            Add(ref TLegal, t);
            if (legal.Count > MaxLegal) lock (Lock) if (legal.Count > MaxLegal) { MaxLegal = legal.Count; MaxLegalWhere = where + " #" + n + " " + legal[legal.Count - 1].Kind; }
            if (legal.Count == 0) { Fail(where, "human to act has no legal actions"); return; }
            t = Now;
            if (random)
            {
                var pick = legal.Count > 1 && rng.Next(4) != 0 ? legal[1 + rng.Next(legal.Count - 1)] : legal[rng.Next(legal.Count)];
                s.Submit(pick);
            }
            else s.AutoStep();
            Add(ref TApply, t);
        }
        lock (Lock) { Running.Remove(where); Games++; Actions += n; Slow.Add(((Now - start) / (double)System.Diagnostics.Stopwatch.Frequency, n, where)); }

        // Replay determinism (what bug reports rely on).
        long r = Now;
        var again = new MatchSession(setup);
        foreach (var act in s.History) again.Engine.Apply(again.State, act);
        if (again.State.Fingerprint() != s.State.Fingerprint()) Fail(where, "replay of the history gives a different game");
        Add(ref TReplay, r);
    }

    /// <summary>Two random legal decks from the whole pool (15 cards x 4 for a random Tavern Dweller), like the soak test.</summary>
    static MatchSetup RandomSetup(ulong seed)
    {
        var db = CardPool.CreateDatabase();
        var rng = new Random((int)(seed % int.MaxValue));
        var dwellers = db.All.Where(c => c.IsTavernDweller).OrderBy(c => c.Id).ToList();
        var setup = new MatchSetup { Seed = seed, Seats = { SeatKind.Human, SeatKind.Human } };
        for (int i = 0; i < 2; i++)
        {
            var d = dwellers[rng.Next(dwellers.Count)];
            var pool = db.All.Where(c => !c.IsToken && !c.IsTavernDweller && (c.Faction == "neutral" || d.TavernDwellerFactions.Contains(c.Faction)))
                .Select(c => c.Id).OrderBy(x => rng.Next()).Take(15).ToList();
            var deck = new CardPool.DeckList { Id = "random" + i, Name = "Random " + i, TavernDweller = d.Id };
            foreach (var id in pool) deck.Cards.AddRange(Enumerable.Repeat(id, 4));
            setup.Decks.Add(deck);
        }
        return setup;
    }

    static void CheckEngine(GameState s, CardDatabase db, string game, int n)
    {
        string where = game + " #" + n;
        foreach (var p in s.Players)
        {
            if (p.Mana < 0) Fail(where, "negative mana");
            if (p.Gold < 0 || p.Gold > GoldRules.Cap(s, db, p.Id)) Fail(where, "Gold out of range");
            int owned = s.Players.Sum(q => q.Battlefield.Count(c => c.Owner == p.Id && !c.IsToken))
                        + p.Deck.Count + p.Hand.Count + p.Graveyard.Count + p.Exile.Count
                        + s.Chain.Count(i => i.Card != null && i.Card.Owner == p.Id);
            if (owned != s.Format.DeckSize) Fail(where, "cards created or lost");
            if (p.TavernDwellerZone.Count != 1) Fail(where, "Tavern Dweller zone count");
        }
        // One source, one trigger (Decision Log 2026-10-10): while triggers are being put on the Chain (ordering them, choosing
        // their targets) they're already merged, unless they target or are Separate.
        bool puttingOnChain = s.Pending != null && (s.Pending.Kind == DecisionKind.OrderTriggers || s.Pending.Kind == DecisionKind.ChooseTriggerTarget);
        for (int i = 0; puttingOnChain && i < s.PendingTriggers.Count; i++)
            for (int j = 0; j < i; j++)
            {
                var a = s.PendingTriggers[i];
                var b = s.PendingTriggers[j];
                if (ReferenceEquals(a.Ability, b.Ability) && a.SourceId == b.SourceId && a.Ability.Target == TargetSpec.None && !a.Ability.Separate)
                    Fail(where, "same trigger waiting twice, not merged (" + a.SourceDefinitionId + ")");
            }
        if (s.PriorityPlayer.HasValue && !s.IsGameOver)
            foreach (var c in s.AllPermanents())
            {
                var def = db.Get(c.DefinitionId);
                if (def.IsCreature && CharacteristicsCalculator.Compute(s, db, c).RemainingHealth <= 0) Fail(where, "dead creature on battlefield (" + c.DefinitionId + ")");
                if (def.Type == CardType.Equipment && !c.AttachedToObject.IsNone && s.FindOnBattlefield(c.AttachedToObject) == null)
                    Fail(where, "Equipment attached to something gone (" + c.DefinitionId + ")");
            }
    }

    static void CheckClient(MatchSession s, Random rng, string game, int n)
    {
        string where = game + " #" + n;
        var snap = s.Snapshot();
        var legal = s.LegalForViewer();
        var legalSet = new HashSet<PlayerAction>(legal);
        var b = TableControls.Main(s);
        if (b.Hint == null) Fail(where, "button hint is null");
        if (string.IsNullOrEmpty(b.Label)) Fail(where, "button label empty (" + b.Mode + ")");
        if (b.Action != null && !legalSet.Contains(b.Action)) Fail(where, "button action not legal: " + b.Mode + " " + b.Action);
        if (s.HumanToAct && !b.Enabled && b.Mode != ButtonMode.ChooseOnTable && b.Mode != ButtonMode.GameOver)
            Fail(where, "button disabled on the human's call: " + b.Mode);
        if (b.Mode == ButtonMode.Waiting && s.HumanToAct) Fail(where, "Waiting while human to act");
        var choices = TableControls.Choices(s);
        foreach (var c in choices)
            if (!legalSet.Contains(c.Action)) Fail(where, "choice button not legal");

        // Every target the player may pick is something the table draws (or it's offered as a button).
        var drawn = new HashSet<ObjectId>();
        foreach (var p in snap.Players)
        {
            foreach (var c in p.Battlefield.Concat(p.Hand).Concat(p.Graveyard).Concat(p.Exile)) drawn.Add(c.Id);
            if (p.TavernDweller != null) drawn.Add(p.TavernDweller.Id);
        }
        foreach (var item in snap.Chain)
        {
            if (item.ObjectId.IsNone) Fail(where, "Chain item without object id");
            drawn.Add(item.ObjectId);
            if (item.SourceDefinitionId == null) Fail(where, "Chain item without source card (bubble has no face): " + item.Kind);
        }
        foreach (var a in legal)
            if (a.Targets != null)
                foreach (var t in a.Targets)
                    if (!t.IsPlayer && !drawn.Contains(t.Object)) Fail(where, "legal target not in the snapshot: " + a.Kind + " " + s.Text.Describe(s.State, a));

        // The picker reaches only legal actions: walk a random source to the end.
        var picker = new ActionPicker(legal);
        var sources = picker.Sources.ToList();
        if (sources.Count > 0)
        {
            var src = sources[rng.Next(sources.Count)];
            if (!picker.Begin(src)) { Fail(where, "picker can't begin a listed source"); return; }
            int guard = 0;
            while (picker.Ready == null && picker.Prompt != null && guard++ < 20)
            {
                var opts = picker.Prompt.Options;
                if (opts.Count == 0) { Fail(where, "picker prompt with no options (" + picker.Prompt.Dimension + ")"); return; }
                picker.Choose(opts[rng.Next(opts.Count)]);
            }
            if (picker.Ready != null && !legalSet.Contains(picker.Ready)) Fail(where, "picker built an illegal action");
            if (picker.Ready == null && picker.Prompt == null) Fail(where, "picker stuck without prompt");
        }
    }
}
