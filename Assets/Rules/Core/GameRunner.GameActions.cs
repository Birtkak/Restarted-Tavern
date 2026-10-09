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

            // A lord/anthem leaving ends its buffs (§7.3: losing a buff can't kill).
            var buffSnapshot = from == Zone.Battlefield && def.Statics.Count > 0 ? SnapshotRemainingHealth() : null;

            if (from == Zone.Battlefield) S.GetPlayer(card.Controller).Battlefield.Remove(card);
            else if (from != Zone.Chain) S.GetPlayer(card.Owner).GetZone(from).Remove(card);

            if (buffSnapshot != null) CapDamageAfterBuffsEnd(buffSnapshot);

            bool dies = from == Zone.Battlefield && to == Zone.Graveyard && def.IsCreature;

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
            }
            if (to == Zone.Battlefield)
                QueueTriggers(def, TriggerEvent.Arrival, moved.Controller, moved.Id);

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
            QueueTriggers(Db.Get(definitionId), TriggerEvent.Arrival, controller, token.Id);
            return token;
        }

        /// <summary>
        /// Damage to a creature stays until healed (§7.3); damage to a player is life loss.
        /// Lifelink: the damage also heals the source's controller (§11).
        /// </summary>
        internal void DealDamage(ObjectId source, Target target, int amount, bool isCombat)
        {
            if (amount <= 0) return;
            if (target.IsPlayer)
            {
                if (S.GetPlayer(target.Player).HasLost) return;
                ChangeLife(target.Player, -amount);
            }
            else
            {
                var creature = S.FindOnBattlefield(target.Object);
                if (creature == null || !Def(creature).IsCreature) return;
                creature.Damage += amount;
            }
            Emit(new DamageDealtEvent { Source = source, Target = target, Amount = amount, IsCombat = isCombat });

            var src = S.FindOnBattlefield(source);
            if (src != null && Stats(src).Has(Keyword.Lifelink))
                Heal(Target.ForPlayer(src.Controller), amount);
        }

        /// <summary>
        /// Heal X (§11.1): remove up to X damage from a creature, or restore a player's life up
        /// to the starting life total.
        /// </summary>
        internal void Heal(Target target, int amount)
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
                if (creature == null) return;
                healed = Math.Min(amount, creature.Damage);
                creature.Damage -= healed;
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

        /// <summary>Gold is clamped to 0..GoldCap; anything above the cap is lost (§5.2).</summary>
        internal void ChangeGold(PlayerId player, int delta)
        {
            var p = S.GetPlayer(player);
            int old = p.Gold;
            p.Gold = Math.Max(0, Math.Min(S.Format.GoldCap, p.Gold + delta));
            if (p.Gold != old) Emit(new GoldChangedEvent { Player = player, OldGold = old, NewGold = p.Gold });
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

        internal void ModifyUntilEndOfTurn(ObjectId creature, int power, int health, Keyword grants)
        {
            if (S.FindOnBattlefield(creature) == null) return;
            S.UntilEndOfTurn.Add(new TemporaryModifier { Target = creature, Power = power, Health = health, Grants = grants });
        }
    }
}
