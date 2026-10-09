namespace RestartedTavern.Rules
{
    /// <summary>
    /// Something that happened. The client animates from these, never from state diffs
    /// (DEVELOPMENT §1.3). Events still carry hidden information (e.g. which card was drawn);
    /// filtering them per viewer is a TODO for online play.
    /// </summary>
    public abstract class GameEvent
    {
    }

    public sealed class GameStartedEvent : GameEvent
    {
        public PlayerId StartingPlayer;
        public override string ToString() => "Game started, " + StartingPlayer + " goes first";
    }

    public sealed class MulliganEvent : GameEvent
    {
        public PlayerId Player;
        public int MulligansTaken;
        public override string ToString() => Player + " mulligans (" + MulligansTaken + ")";
    }

    public sealed class TurnStartedEvent : GameEvent
    {
        public PlayerId Player;
        public int Turn;
        public override string ToString() => "Turn " + Turn + ": " + Player;
    }

    public sealed class StepStartedEvent : GameEvent
    {
        public Step Step;
        public override string ToString() => "Step " + Step;
    }

    public sealed class CardDrawnEvent : GameEvent
    {
        public PlayerId Player;
        public ObjectId Card;
        public string DefinitionId;
        public override string ToString() => Player + " draws " + DefinitionId + Card;
    }

    public sealed class ZoneChangedEvent : GameEvent
    {
        public ObjectId OldId;
        /// <summary>None when a token stopped existing.</summary>
        public ObjectId NewId;
        public string DefinitionId;
        public Zone From;
        public Zone To;
        public override string ToString() => DefinitionId + OldId + " " + From + " -> " + To + " " + NewId;
    }

    public sealed class SpellCastEvent : GameEvent
    {
        public PlayerId Player;
        public ObjectId Card;
        public string DefinitionId;
        public Target[] Targets;
        public Target? Target => Targets != null && Targets.Length > 0 ? Targets[0] : (Target?)null;
        public int ManaPaid;
        public int GoldPaid;
        public bool Invested;
        public override string ToString() => Player + " casts " + DefinitionId + Card + (Targets?.Length > 0 ? " @" + string.Join(",", Targets) : "");
    }

    public sealed class AbilityTriggeredEvent : GameEvent
    {
        public PlayerId Controller;
        public ObjectId Source;
        public string SourceDefinitionId;
        public TriggerEvent When;
        public Target? Target;
        public override string ToString() => SourceDefinitionId + Source + " triggers (" + When + ")";
    }

    public sealed class ChainItemResolvedEvent : GameEvent
    {
        public int ItemId;
        public string SourceDefinitionId;
        public override string ToString() => "Resolves " + SourceDefinitionId;
    }

    public sealed class FizzledEvent : GameEvent
    {
        public int ItemId;
        public string SourceDefinitionId;
        public override string ToString() => SourceDefinitionId + " fizzles";
    }

    public sealed class ManaChangedEvent : GameEvent
    {
        public PlayerId Player;
        public int Mana;
        public int MaxMana;
        public override string ToString() => Player + " mana " + Mana + "/" + MaxMana;
    }

    public sealed class GoldChangedEvent : GameEvent
    {
        public PlayerId Player;
        public int OldGold;
        public int NewGold;
        public override string ToString() => Player + " gold " + OldGold + " -> " + NewGold;
    }

    /// <summary>End of turn: unspent mana became Gold. Banked &lt; UnspentMana means the cap wasted some.</summary>
    public sealed class GoldBankedEvent : GameEvent
    {
        public PlayerId Player;
        public int UnspentMana;
        public int Banked;
        public override string ToString() => Player + " banks " + Banked + " of " + UnspentMana + " unspent mana";
    }

    public sealed class DamageDealtEvent : GameEvent
    {
        public ObjectId Source;
        public Target Target;
        public int Amount;
        public bool IsCombat;
        public override string ToString() => Source + " deals " + Amount + " to " + Target + (IsCombat ? " (combat)" : "");
    }

    public sealed class HealedEvent : GameEvent
    {
        public Target Target;
        public int Amount;
        public override string ToString() => Target + " healed " + Amount;
    }

    public sealed class LifeChangedEvent : GameEvent
    {
        public PlayerId Player;
        public int OldLife;
        public int NewLife;
        public override string ToString() => Player + " life " + OldLife + " -> " + NewLife;
    }

    public sealed class TokenCreatedEvent : GameEvent
    {
        public PlayerId Controller;
        public ObjectId Token;
        public string DefinitionId;
        public override string ToString() => Controller + " creates " + DefinitionId + Token;
    }

    public sealed class AttackerDeclaredEvent : GameEvent
    {
        public ObjectId Attacker;
        public PlayerId Defender;
        public override string ToString() => Attacker + " attacks " + Defender;
    }

    public sealed class BlockerDeclaredEvent : GameEvent
    {
        public ObjectId Blocker;
        public ObjectId Attacker;
        public override string ToString() => Blocker + " blocks " + Attacker;
    }

    public sealed class CreatureDiedEvent : GameEvent
    {
        public ObjectId Card;
        public string DefinitionId;
        public PlayerId Controller;
        public override string ToString() => DefinitionId + Card + " dies";
    }

    public sealed class PlayerLostEvent : GameEvent
    {
        public PlayerId Player;
        public string Reason;
        public override string ToString() => Player + " loses (" + Reason + ")";
    }

    public sealed class GameOverEvent : GameEvent
    {
        public PlayerId[] Winners;
        public override string ToString() => "Game over, winners: " + string.Join(", ", Winners);
    }
}
