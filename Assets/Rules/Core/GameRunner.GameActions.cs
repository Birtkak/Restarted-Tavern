using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>The low-level game actions every rule and effect is built from.</summary>
    internal sealed partial class GameRunner
    {
        private CardInstance NewObject(string definitionId, PlayerId owner, Zone zone) => new CardInstance
        {
            Id = new ObjectId(S.NextObjectId++),
            DefinitionId = definitionId,
            Owner = owner,
            Controller = owner,
            Zone = zone,
            Timestamp = S.NextTimestamp++,
        };

        /// <summary>
        /// Moves a card to another zone. It becomes a new object with a new id (MTG 400.7), and
        /// loses damage, counters, tapped state and attachments. Tokens that leave the
        /// battlefield stop existing (MTG 111.7); this returns null for them.
        /// The caller removes a spell from the Chain before moving its card.
        /// </summary>
        internal CardInstance MoveCard(CardInstance card, Zone to, PlayerId? controller = null, bool toBottom = false)
        {
            var from = card.Zone;
            var def = Def(card);
            var lastController = card.Controller;
            bool dies = from == Zone.Battlefield && to == Zone.Graveyard && def.IsCreature;
            int lastPower = dies ? Stats(card).Power : 0; // last known information (MTG 608.2h)

            // A lord/anthem leaving ends its buffs (§7.3: losing a buff can't kill).
            var buffSnapshot = from == Zone.Battlefield && def.Statics.Count > 0 ? SnapshotRemainingHealth() : null;

            if (from == Zone.Battlefield) S.GetPlayer(card.Controller).Battlefield.Remove(card);
            else if (from != Zone.Chain) S.GetPlayer(card.Owner).GetZone(from).Remove(card);

            if (buffSnapshot != null) CapDamageAfterBuffsEnd(buffSnapshot);

            CardInstance moved = null;
            if (card.IsToken && to != Zone.Battlefield && to != Zone.Chain)
            {
                Emit(new ZoneChangedEvent { OldId = card.Id, NewId = ObjectId.None, DefinitionId = def.Id, From = from, To = to });
            }
            else
            {
                moved = NewObject(card.DefinitionId, card.Owner, to);
                moved.IsToken = card.IsToken;
                moved.Controller = to == Zone.Battlefield || to == Zone.Chain ? controller ?? card.Owner : card.Owner;
                if (to == Zone.Battlefield) moved.SummoningSick = true;

                switch (to)
                {
                    case Zone.Chain:
                        break; // held by its ChainItem
                    case Zone.Battlefield:
                        S.GetPlayer(moved.Controller).Battlefield.Add(moved);
                        break;
                    case Zone.Deck:
                        var deck = S.GetPlayer(moved.Owner).Deck;
                        if (toBottom) deck.Add(moved);
                        else deck.Insert(0, moved);
                        break;
                    default:
                        S.GetPlayer(moved.Owner).GetZone(to).Add(moved);
                        break;
                }
                Emit(new ZoneChangedEvent { OldId = card.Id, NewId = moved.Id, DefinitionId = def.Id, From = from, To = to });
            }

            if (dies)
            {
                Emit(new CreatureDiedEvent { Card = card.Id, DefinitionId = def.Id, Controller = lastController });
                QueueTriggers(def, TriggerEvent.LastBreath, lastController, card.Id);
                QueueWatcherTriggers(TriggerEvent.CreatureDies, lastController,
                    (t, watcher) => (t.MinPower <= 0 || lastPower >= t.MinPower)
                                    && (t.SubjectSubtype == null || def.HasSubtype(t.SubjectSubtype))
                                    && (!t.OthersOnly || watcher.Id != card.Id)
                                    && (!t.OnlyEnchantedPlayer || watcher.AttachedToPlayer == lastController),
                    eventObject: card.Id, eventPlayer: lastController);
            }
            if (from == Zone.Battlefield && to == Zone.Graveyard && def.Type == CardType.Curse)
                QueueWatcherTriggers(TriggerEvent.CurseToGraveyard, lastController);
            if (to == Zone.Battlefield)
            {
                ApplyEntersWithCounters(moved);
                QueueTriggers(moved, TriggerEvent.Arrival);
                QueueCreatureEnters(moved);
            }

            return moved;
        }

        internal void Draw(PlayerId player, int count)
        {
            var p = S.GetPlayer(player);
            for (int i = 0; i < count; i++)
            {
                if (p.Deck.Count == 0)
                {
                    p.DrewFromEmptyDeck = true; // §12: checked as a state-based action
                    return;
                }
                var drawn = MoveCard(p.Deck[0], Zone.Hand);
                Emit(new CardDrawnEvent { Player = player, Card = drawn.Id, DefinitionId = drawn.DefinitionId });
            }
        }

        internal CardInstance CreateToken(PlayerId controller, string definitionId)
        {
            var token = NewObject(definitionId, controller, Zone.Battlefield);
            token.IsToken = true;
            token.SummoningSick = true;
            S.GetPlayer(controller).Battlefield.Add(token);
            Emit(new TokenCreatedEvent { Controller = controller, Token = token.Id, DefinitionId = definitionId });
            ApplyEntersWithCounters(token);
            QueueTriggers(token, TriggerEvent.Arrival);
            QueueCreatureEnters(token);
            return token;
        }

        /// <summary>"Whenever another creature with 5 or more Power enters under your control" (Herd Matriarch).</summary>
        private void QueueCreatureEnters(CardInstance permanent)
        {
            if (!Def(permanent).IsCreature) return;
            int power = Stats(permanent).Power; // as it exists on the battlefield, counters included (MTG 603.6a)
            QueueWatcherTriggers(TriggerEvent.CreatureEnters, permanent.Controller,
                (t, source) => (!t.OthersOnly || source.Id != permanent.Id) && (t.MinPower <= 0 || power >= t.MinPower),
                eventObject: permanent.Id, eventPlayer: permanent.Controller);
        }

        /// <summary>
        /// "Your creatures with 5 or more Health enter with a +1/+1 counter" (Keeper Z-00). A
        /// replacement effect (MTG 614.1c): Health is checked as the creature exists on the
        /// battlefield, so static buffs count (MTG 614.12).
        /// </summary>
        private void ApplyEntersWithCounters(CardInstance permanent)
        {
            if (!Def(permanent).IsCreature) return;
            var p = S.GetPlayer(permanent.Controller);
            int counters = 0;
            int health = Stats(permanent).MaxHealth;
            foreach (var list in new[] { p.TavernDwellerZone, p.Battlefield })
                foreach (var source in list)
                    foreach (var st in Def(source).Statics)
                        if (st is EntersWithCountersAbility e && health >= e.MinHealth) counters += e.Counters;
            permanent.PlusOneCounters += counters;
        }

        /// <summary>
        /// Attach an Equipment to a creature (Equip, MTG 701.3). Nothing happens if either is gone,
        /// isn't the right type, or it's already attached there. Moving it away from another
        /// creature ends that creature's bonus, which can't kill it (§7.3).
        /// </summary>
        internal void Attach(CardInstance equipment, CardInstance creature)
        {
            if (equipment == null || creature == null) return;
            if (S.FindOnBattlefield(equipment.Id) == null || S.FindOnBattlefield(creature.Id) == null) return;
            if (Def(equipment).Type != CardType.Equipment || !Def(creature).IsCreature) return;
            if (equipment.AttachedToObject == creature.Id) return;

            var old = equipment.AttachedToObject;
            var snapshot = old.IsNone ? null : SnapshotRemainingHealth();
            equipment.AttachedToObject = creature.Id;
            if (snapshot != null) CapDamageAfterBuffsEnd(snapshot);

            Emit(new AttachedEvent { Equipment = equipment.Id, EquipmentDefinitionId = equipment.DefinitionId, AttachedTo = creature.Id });
            if (!old.IsNone) QueueWatcherTriggers(TriggerEvent.EquipmentUnattached, equipment.Controller);
            QueueTriggers(creature, TriggerEvent.EquipmentAttachedToThis);
        }

        /// <summary>
        /// Damage to a creature stays until healed (§7.3); damage to a player is life loss.
        /// Lifelink: the damage also heals the source's controller (§11). Returns the damage actually
        /// dealt: "can't be dealt more than N damage each turn" (Hardlight Aegis) prevents the rest.
        /// </summary>
        internal int DealDamage(ObjectId source, Target target, int amount, bool isCombat)
        {
            if (amount <= 0) return 0;
            if (target.IsPlayer)
            {
                if (S.GetPlayer(target.Player).HasLost) return 0;
                Emit(new DamageDealtEvent { Source = source, Target = target, Amount = amount, IsCombat = isCombat });
                ChangeLife(target.Player, -amount);
                var dealer = isCombat ? S.FindOnBattlefield(source) : null;
                if (dealer != null)
                {
                    QueueTriggers(dealer, TriggerEvent.DealsCombatDamageToPlayer, target.Player);
                    var dealerDef = Def(dealer);
                    QueueWatcherTriggers(TriggerEvent.CreatureDealsCombatDamageToPlayer, dealer.Controller,
                        (t, watcher) => (!t.OthersOnly || watcher.Id != dealer.Id)
                                        && (t.SubjectSubtype == null || dealerDef.HasSubtype(t.SubjectSubtype)),
                        amount, dealer.Id, target.Player);
                }
            }
            else
            {
                var creature = S.FindOnBattlefield(target.Object);
                if (creature == null || !Def(creature).IsCreature) return 0;

                // Damage dealt to each creature this turn, for "can't be dealt more than N damage each turn".
                string key = "damage:" + creature.Id.Value;
                S.UsesThisTurn.TryGetValue(key, out int taken);
                int cap = CharacteristicsCalculator.MaxDamageEachTurn(S, Db, creature);
                if (cap < int.MaxValue) amount = Math.Min(amount, Math.Max(0, cap - taken));
                if (amount <= 0) return 0;
                S.UsesThisTurn[key] = taken + amount;

                Emit(new DamageDealtEvent { Source = source, Target = target, Amount = amount, IsCombat = isCombat });
                creature.Damage += amount;
                int remaining = Stats(creature).RemainingHealth;
                QueueWatcherTriggers(TriggerEvent.CreatureDealtDamage, creature.Controller,
                    (t, watcher) => (!t.OnlyAttachedCreature || watcher.AttachedToObject == creature.Id)
                                    && (!t.OnlySelf || watcher.Id == creature.Id)
                                    && (!t.OnlyIfSurvives || remaining > 0)
                                    && (!t.MaxRemainingHealth.HasValue || remaining <= t.MaxRemainingHealth.Value),
                    amount, creature.Id, creature.Controller);
            }

            var src = S.FindOnBattlefield(source);
            if (src != null && Stats(src).Has(Keyword.Lifelink))
                Heal(Target.ForPlayer(src.Controller), amount);
            return amount;
        }

        /// <summary>
        /// Fight (§11.1): each creature deals damage equal to its Power to the other, at the same
        /// time. Not combat, so no Trample. If either isn't on the battlefield, nothing happens (MTG 701.14b).
        /// </summary>
        internal void Fight(CardInstance a, CardInstance b)
        {
            if (a == null || b == null || a.Id == b.Id) return;
            if (S.FindOnBattlefield(a.Id) == null || S.FindOnBattlefield(b.Id) == null) return;
            int powerA = Stats(a).Power, powerB = Stats(b).Power;
            DealDamage(a.Id, Target.ForObject(b.Id), powerA, false);
            DealDamage(b.Id, Target.ForObject(a.Id), powerB, false);
        }

        /// <summary>
        /// Heal X (§11.1): remove up to X damage from a creature, or restore a player's life up
        /// to the starting life total. A creature that "can't be healed" keeps its damage.
        /// <paramref name="healer"/> is the player whose effect heals ("whenever you heal a creature").
        /// </summary>
        internal void Heal(Target target, int amount, PlayerId? healer = null)
        {
            if (amount <= 0) return;
            int healed;
            if (target.IsPlayer)
            {
                var p = S.GetPlayer(target.Player);
                if (p.HasLost) return;
                healed = Math.Max(0, Math.Min(amount, S.Format.StartingLife - p.Life));
                if (healed > 0) ChangeLife(p.Id, healed);
            }
            else
            {
                var creature = S.FindOnBattlefield(target.Object);
                if (creature == null || CharacteristicsCalculator.CantBeHealed(S, Db, creature)) return;
                healed = Math.Min(amount, creature.Damage);
                creature.Damage -= healed;
                if (healed > 0 && healer.HasValue)
                    QueueWatcherTriggers(TriggerEvent.CreatureHealed, healer.Value, null, healed, creature.Id, creature.Controller);
            }
            if (healed > 0) Emit(new HealedEvent { Target = target, Amount = healed });
        }

        internal void ChangeLife(PlayerId player, int delta)
        {
            var p = S.GetPlayer(player);
            int old = p.Life;
            p.Life += delta;
            if (p.Life != old) Emit(new LifeChangedEvent { Player = player, OldLife = old, NewLife = p.Life });
        }

        /// <summary>
        /// Gold is clamped to 0..the player's Gold cap; anything gained above the cap is lost (§5.2).
        /// Losing Gold never raises it, even if the player is somehow above the cap.
        /// </summary>
        internal void ChangeGold(PlayerId player, int delta)
        {
            var p = S.GetPlayer(player);
            int old = p.Gold;
            int cap = delta > 0 ? Math.Max(p.Gold, GoldRules.Cap(S, Db, player)) : int.MaxValue;
            p.Gold = Math.Max(0, Math.Min(cap, p.Gold + delta));
            if (p.Gold != old) Emit(new GoldChangedEvent { Player = player, OldGold = old, NewGold = p.Gold });
        }

        /// <summary>Tap a permanent (Spilled Drink). Tapping an attacking creature doesn't remove it from combat (MTG 506.4).</summary>
        internal void Tap(CardInstance permanent)
        {
            if (permanent == null || S.FindOnBattlefield(permanent.Id) == null || permanent.Tapped) return;
            permanent.Tapped = true;
        }

        internal void AddCounters(ObjectId creature, int count)
        {
            var c = S.FindOnBattlefield(creature);
            if (c != null) c.PlusOneCounters += count;
        }

        /// <summary>Remaining Health of every creature on the battlefield, before buffs change.</summary>
        private Dictionary<ObjectId, int> SnapshotRemainingHealth()
        {
            var snapshot = new Dictionary<ObjectId, int>();
            foreach (var c in S.AllPermanents())
                if (Def(c).IsCreature) snapshot[c.Id] = Stats(c).RemainingHealth;
            return snapshot;
        }

        /// <summary>
        /// GAME_DESIGN §7.3 (deviation from MTG): when a Health buff ends, a creature that was
        /// alive keeps at least 1 Health. Its damage is lowered instead of the buff killing it.
        /// </summary>
        private void CapDamageAfterBuffsEnd(Dictionary<ObjectId, int> before)
        {
            foreach (var c in S.AllPermanents())
            {
                if (!before.TryGetValue(c.Id, out int had) || had <= 0) continue;
                var now = Stats(c);
                if (now.RemainingHealth > 0) continue;
                c.Damage = Math.Max(0, now.MaxHealth - 1);
            }
        }

        /// <summary>
        /// Counter a spell or ability (MTG 701.5): it leaves the Chain without resolving, and a countered
        /// spell goes to its owner's graveyard. A countered Tavern Dweller Power still counts as used this turn.
        /// </summary>
        internal void Counter(ChainItem item)
        {
            if (!S.Chain.Remove(item)) return;
            Emit(new CounteredEvent { ItemId = item.Id, SourceDefinitionId = item.SourceDefinitionId, Controller = item.Controller });
            if (item.Card != null) MoveCard(item.Card, Zone.Graveyard);
        }

        /// <summary>
        /// Gain control of a permanent (MTG 613.1b, layer 2). It's the same object, damage and counters stay.
        /// It can't attack or use Tap abilities until its new controller's next turn (MTG 302.6), and leaves
        /// combat (MTG 506.4). "Until end of turn": control goes back in the cleanup step.
        /// </summary>
        internal void GainControl(CardInstance permanent, PlayerId to, bool untilEndOfTurn)
        {
            if (permanent == null || S.FindOnBattlefield(permanent.Id) == null || S.GetPlayer(to).HasLost) return;
            var from = permanent.Controller;
            // A newer control effect wins over an older one (layer 2 timestamps).
            S.ControlUntilEndOfTurn.RemoveAll(t => t.Object == permanent.Id);
            if (from == to) return;
            if (untilEndOfTurn) S.ControlUntilEndOfTurn.Add(new TemporaryControl { Object = permanent.Id, ReturnTo = from });
            MoveControl(permanent, to);
        }

        private void MoveControl(CardInstance permanent, PlayerId to)
        {
            var from = permanent.Controller;
            S.GetPlayer(from).Battlefield.Remove(permanent);
            S.GetPlayer(to).Battlefield.Add(permanent);
            permanent.Controller = to;
            permanent.SummoningSick = true;
            S.Combat?.Remove(permanent.Id);
            Emit(new ControlChangedEvent { Card = permanent.Id, DefinitionId = permanent.DefinitionId, From = from, To = to });
        }

        /// <summary>Cleanup step: "gain control until end of turn" effects end (MTG 514.2).</summary>
        private void EndTemporaryControl()
        {
            var ending = new List<TemporaryControl>(S.ControlUntilEndOfTurn);
            S.ControlUntilEndOfTurn.Clear();
            foreach (var t in ending)
            {
                var permanent = S.FindOnBattlefield(t.Object);
                if (permanent != null && permanent.Controller != t.ReturnTo && !S.GetPlayer(t.ReturnTo).HasLost)
                    MoveControl(permanent, t.ReturnTo);
            }
        }

        /// <summary>Set up a delayed trigger "at the end of your turn" about <paramref name="obj"/> (MTG 603.7).</summary>
        internal void AddDelayedTrigger(TriggeredAbility ability, PlayerId controller, ObjectId source, string sourceDefinitionId, ObjectId obj) =>
            S.DelayedTriggers.Add(new DelayedTrigger
            {
                Ability = ability, Controller = controller, SourceId = source, SourceDefinitionId = sourceDefinitionId, EventObject = obj,
            });

        /// <summary>
        /// "Create a 1/1 Goober that's tapped and attacking" (Grakka). It attacks the player the controller's
        /// first attacker attacks (or the next opponent). It was never declared, so "whenever this attacks" doesn't trigger (MTG 508.4).
        /// </summary>
        internal void CreateAttackingToken(PlayerId controller, string tokenId)
        {
            var token = CreateToken(controller, tokenId);
            if (S.Combat == null || S.ActivePlayer != controller) return;
            token.Tapped = true;
            PlayerId? defender = null;
            foreach (var attack in S.Combat.Attacks)
            {
                var attacker = S.FindOnBattlefield(attack.Attacker);
                if (attacker != null && attacker.Controller == controller) { defender = attack.Defender; break; }
            }
            if (defender == null)
                foreach (var p in S.LivingPlayersFrom(controller))
                    if (S.AreOpponents(controller, p.Id)) { defender = p.Id; break; }
            if (defender.HasValue) S.Combat.Attacks.Add(new AttackDeclaration { Attacker = token.Id, Defender = defender.Value });
        }

        internal void Untap(CardInstance permanent)
        {
            if (permanent != null && S.FindOnBattlefield(permanent.Id) != null) permanent.Tapped = false;
        }

        internal void ModifyUntilEndOfTurn(ObjectId creature, int power, int health, Keyword grants)
        {
            if (S.FindOnBattlefield(creature) == null) return;
            S.UntilEndOfTurn.Add(new TemporaryModifier { Target = creature, Power = power, Health = health, Grants = grants });
        }
    }
}
