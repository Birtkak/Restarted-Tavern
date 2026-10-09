using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// State-based actions (MTG CR 704), checked whenever a player would receive priority.
    /// They repeat until none apply; then waiting triggers go on the Chain.
    /// </summary>
    internal sealed partial class GameRunner
    {
        private void CheckStateBasedActionsAndTriggers()
        {
            while (!S.IsGameOver && ApplyStateBasedActionsOnce()) { }
            if (S.IsGameOver) return;
            PutPendingTriggersOnChain();
        }

        /// <summary>Performs every applicable state-based action at once. Returns true if any happened.</summary>
        private bool ApplyStateBasedActionsOnce()
        {
            var losers = new List<(PlayerState player, string reason)>();
            foreach (var p in S.Players)
            {
                if (p.HasLost) continue;
                if (p.Life <= 0) losers.Add((p, "life"));                          // 704.5a, GAME_DESIGN §12
                else if (p.DrewFromEmptyDeck) losers.Add((p, "drew from empty deck")); // 704.5b
            }

            var toGraveyard = new List<CardInstance>();
            var toUnattach = new List<CardInstance>();
            foreach (var p in S.Players)
            {
                foreach (var c in p.Battlefield)
                {
                    var def = Def(c);
                    // 704.5f/g: permanent damage model — dies when its remaining Health is 0 or less (§7.3).
                    if (def.IsCreature && Stats(c).RemainingHealth <= 0)
                        toGraveyard.Add(c);
                    // 704.5m: a Curse that isn't attached to anything legal goes to the graveyard (§10).
                    else if (def.Type == CardType.Curse && !CurseAttachmentIsLegal(c))
                        toGraveyard.Add(c);
                    // 704.5n: Equipment attached to something that's gone stays, unattached (§10).
                    else if (def.Type == CardType.Equipment && !c.AttachedToObject.IsNone
                             && S.FindOnBattlefield(c.AttachedToObject) == null)
                        toUnattach.Add(c);
                }
            }

            // 704.5j Legendary rule: one per name per controller.
            // TODO: the controller should choose which to keep; for now the newest stays.
            foreach (var p in S.Players)
            {
                var newestByName = new Dictionary<string, CardInstance>();
                foreach (var c in p.Battlefield)
                {
                    var def = Def(c);
                    if (!def.IsLegendary || toGraveyard.Contains(c)) continue;
                    if (!newestByName.TryGetValue(def.Name, out var kept))
                    {
                        newestByName[def.Name] = c;
                        continue;
                    }
                    bool cIsNewer = c.Timestamp > kept.Timestamp;
                    toGraveyard.Add(cIsNewer ? kept : c);
                    if (cIsNewer) newestByName[def.Name] = c;
                }
            }

            // §5.2 (decided 2026-10-09): when a player's Gold cap goes down, Gold above it is lost at once.
            var overCap = new List<(PlayerState player, int cap)>();
            foreach (var p in S.Players)
            {
                if (p.HasLost) continue;
                int cap = GoldRules.Cap(S, Db, p.Id);
                if (p.Gold > cap) overCap.Add((p, cap));
            }

            if (losers.Count == 0 && toGraveyard.Count == 0 && toUnattach.Count == 0 && overCap.Count == 0) return false;

            foreach (var (player, cap) in overCap) ChangeGold(player.Id, cap - player.Gold);

            foreach (var c in toUnattach)
            {
                c.AttachedToObject = ObjectId.None;
                QueueWatcherTriggers(TriggerEvent.EquipmentUnattached, c.Controller);
            }
            foreach (var c in toGraveyard) MoveCard(c, Zone.Graveyard);
            foreach (var (player, reason) in losers) Lose(player, reason);
            CheckGameOver();
            return true;
        }

        private bool CurseAttachmentIsLegal(CardInstance curse)
        {
            if (curse.AttachedToPlayer.HasValue) return !S.GetPlayer(curse.AttachedToPlayer.Value).HasLost;
            return S.FindOnBattlefield(curse.AttachedToObject) != null;
        }

        /// <summary>
        /// GAME_DESIGN §13 elimination: the player leaves the game with everything they own, and
        /// their spells and abilities leave the Chain.
        /// TODO: control-change effects and "until end of turn" effects they controlled; a
        /// player losing in the middle of their own turn in multiplayer.
        /// </summary>
        private void Lose(PlayerState player, string reason)
        {
            player.HasLost = true;
            Emit(new PlayerLostEvent { Player = player.Id, Reason = reason });

            if (S.LivingPlayerCount <= 1) return; // the game ends anyway

            // Control of anything of someone else's that they controlled ends (§13): it goes back to its owner.
            foreach (var c in new List<CardInstance>(player.Battlefield))
            {
                var owner = S.GetPlayer(c.Owner);
                if (c.Owner == player.Id || owner.HasLost) continue;
                player.Battlefield.Remove(c);
                c.Controller = c.Owner;
                owner.Battlefield.Add(c);
            }
            S.ControlUntilEndOfTurn.RemoveAll(t => t.ReturnTo == player.Id);
            S.DelayedTriggers.RemoveAll(t => t.Controller == player.Id);

            foreach (var p in S.Players)
                p.Battlefield.RemoveAll(c => c.Owner == player.Id);
            S.Chain.RemoveAll(item => item.Controller == player.Id || (item.Card != null && item.Card.Owner == player.Id));
            S.PendingTriggers.RemoveAll(t => t.Controller == player.Id);
            S.Combat?.Attacks.RemoveAll(a => a.Defender == player.Id);
            if (S.PriorityPlayer == player.Id) S.PriorityPlayer = NextLivingPlayer(player.Id).Id;
        }

        /// <summary>The game ends when only one team is left (§12–13).</summary>
        private void CheckGameOver()
        {
            var teams = new HashSet<int>();
            foreach (var p in S.Players)
                if (!p.HasLost) teams.Add(p.TeamId);
            if (teams.Count > 1) return;

            S.IsGameOver = true;
            S.Step = Step.GameOver;
            S.PriorityPlayer = null;
            S.Pending = null;
            S.Winners.Clear();
            foreach (var p in S.Players)
                if (!p.HasLost) S.Winners.Add(p.Id);
            Emit(new GameOverEvent { Winners = S.Winners.ToArray() });
        }
    }
}
