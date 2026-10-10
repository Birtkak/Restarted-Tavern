using System.Collections.Generic;
using RestartedTavern.Rules;

namespace RestartedTavern.Client.Logic
{
    /// <summary>How long a beat holds the table. The visual client maps these to seconds (and a speed setting).</summary>
    public enum BeatWeight
    {
        /// <summary>Update the numbers, no pause (mana, Gold, steps).</summary>
        Instant,
        /// <summary>A quick tween (draws, taps, triggers).</summary>
        Short,
        /// <summary>A card being played, damage, a death.</summary>
        Normal,
        /// <summary>Something everyone should notice: a new round, a Tavern Dweller Power, the end of the game.</summary>
        Long,
    }

    /// <summary>One event, ready to animate for one viewer.</summary>
    public sealed class Beat
    {
        public GameEvent Event;
        public BeatWeight Weight;
        /// <summary>The card involved isn't visible to the viewer (an opponent's draw): show a card back.</summary>
        public bool Hidden;

        public override string ToString() => (Hidden ? "[hidden] " : "") + Event;
    }

    /// <summary>
    /// The events of each action, in order, as beats the visual client plays one by one (DEVELOPMENT §1.3:
    /// the client animates from events, never from state diffs). It marks what the viewer may not see,
    /// because the engine's events still carry hidden information.
    ///
    /// Usage: <see cref="Enqueue"/> after every Apply; each frame, if the current beat's animation is done,
    /// <see cref="TryDequeue"/> the next one. When the queue is empty, rebuild the <see cref="TableSnapshot"/>
    /// so the table matches the engine exactly (animations only have to get it close).
    /// </summary>
    public sealed class PresentationQueue
    {
        private readonly Queue<Beat> _beats = new Queue<Beat>();

        public int Count => _beats.Count;
        public bool IsIdle => _beats.Count == 0;

        /// <param name="state">The state after the events (used to find who owns a card that moved).</param>
        public void Enqueue(IEnumerable<GameEvent> events, GameState state, PlayerId viewer)
        {
            foreach (var e in events)
                _beats.Enqueue(new Beat { Event = e, Weight = WeightOf(e), Hidden = IsHiddenFrom(e, state, viewer) });
        }

        public bool TryDequeue(out Beat beat)
        {
            if (_beats.Count == 0) { beat = null; return false; }
            beat = _beats.Dequeue();
            return true;
        }

        /// <summary>Skip the animations (fast-forward, undo, new game).</summary>
        public void Clear() => _beats.Clear();

        public static BeatWeight WeightOf(GameEvent e)
        {
            switch (e)
            {
                case TurnStartedEvent _:
                case GameOverEvent _:
                case PlayerLostEvent _:
                    return BeatWeight.Long;
                case AbilityActivatedEvent a:
                    return a.IsTavernDwellerPower ? BeatWeight.Long : BeatWeight.Normal;
                case SpellCastEvent _:
                case DamageDealtEvent _:
                case HealedEvent _:
                case CreatureDiedEvent _:
                case TokenCreatedEvent _:
                case AttackerDeclaredEvent _:
                case BlockerDeclaredEvent _:
                case CounteredEvent _:
                case FizzledEvent _:
                case ControlChangedEvent _:
                case ChainItemResolvedEvent _:
                    return BeatWeight.Normal;
                case CardDrawnEvent _:
                case ZoneChangedEvent _:
                case AbilityTriggeredEvent _:
                case AttachedEvent _:
                case GoldBankedEvent _:
                case LifeChangedEvent _:
                case MulliganEvent _:
                    return BeatWeight.Short;
                default:
                    return BeatWeight.Instant;
            }
        }

        /// <summary>Draws and moves between a hand and a deck the viewer can't see.</summary>
        public static bool IsHiddenFrom(GameEvent e, GameState state, PlayerId viewer)
        {
            switch (e)
            {
                case CardDrawnEvent d:
                    return d.Player != viewer;
                case ZoneChangedEvent z:
                {
                    // Cards leaving a deck or hand for a public zone are revealed on the way; only deck/hand moves stay private.
                    bool privateMove = (z.From == Zone.Deck || z.From == Zone.Hand) && (z.To == Zone.Deck || z.To == Zone.Hand);
                    if (!privateMove) return false;
                    if (z.From == Zone.Deck && z.To == Zone.Deck) return true;
                    var owner = state.FindObject(z.NewId)?.Owner ?? state.FindObject(z.OldId)?.Owner;
                    return owner != viewer;
                }
                default:
                    return false;
            }
        }
    }
}
