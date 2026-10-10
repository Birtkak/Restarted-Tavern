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

    /// <summary>A replacement effect changed an event (MTG 614). For logs.</summary>
    public sealed class ReplacedEvent : GameEvent
    {
        public ReplacementEvent Kind;
        public string SourceDefinitionId;
        public PlayerId AffectedPlayer;
        public override string ToString() => SourceDefinitionId + " replaces " + Kind + " (" + AffectedPlayer + ")";
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

    public sealed class RoundStartedEvent : GameEvent
    {
        public PlayerId Leader;
        public int Round;
        public override string ToString() => "Round " + Round + ": " + Leader + " leads";
    }

    public sealed class StepStartedEvent : GameEvent
    {
        public Step Step;
        public override string ToString() => "Step " + Step;
    }

    /// <summary>A player passed (by hand or by auto-pass). The client shows it like LoR's "Pass" callout.</summary>
    public sealed class PriorityPassedEvent : GameEvent
    {
        public PlayerId Player;
        public Step Step;
        /// <summary>Items on the Chain when they passed (0: passing the action on, or ending the round / step).</summary>
        public int ChainCount;
        /// <summary>The engine passed for them: passing was their only legal action.</summary>
        public bool Automatic;
        public override string ToString() => Player + (Automatic ? " auto-passes" : " passes") + " (" + Step + ", Chain " + ChainCount + ")";
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
        /// <summary>"Pay any amount of Gold (X)": the X paid (included in GoldPaid).</summary>
        public int X;
        public override string ToString() => Player + " casts " + DefinitionId + Card + (Targets?.Length > 0 ? " @" + string.Join(",", Targets) : "");
    }

    /// <summary>An activated ability or a Tavern Dweller Power was put on the Chain, with its costs paid.</summary>
    public sealed class AbilityActivatedEvent : GameEvent
    {
        public PlayerId Player;
        public ObjectId Source;
        public string SourceDefinitionId;
        public string Text;
        public bool IsTavernDwellerPower;
        public Target[] Targets;
        public int X;
        public int ManaPaid;
        public int GoldPaid;
        public override string ToString() => Player + " activates " + SourceDefinitionId + Source + (IsTavernDwellerPower ? " (Tavern Dweller Power)" : "");
    }

    /// <summary>An Equipment became attached to a creature (Equip, or an effect that attaches it).</summary>
    public sealed class AttachedEvent : GameEvent
    {
        public ObjectId Equipment;
        public string EquipmentDefinitionId;
        public ObjectId AttachedTo;
        public override string ToString() => EquipmentDefinitionId + Equipment + " attached to " + AttachedTo;
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

    /// <summary>A spell or ability was countered (removed from the Chain without resolving).</summary>
    public sealed class CounteredEvent : GameEvent
    {
        public int ItemId;
        public string SourceDefinitionId;
        public PlayerId Controller;
        public override string ToString() => SourceDefinitionId + " is countered";
    }

    /// <summary>A permanent changed controller (Silver-Tongued Deal, Hostile Takeover, or control ending).</summary>
    public sealed class ControlChangedEvent : GameEvent
    {
        public ObjectId Card;
        public string DefinitionId;
        public PlayerId From;
        public PlayerId To;
        public override string ToString() => DefinitionId + Card + " control " + From + " -> " + To;
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
