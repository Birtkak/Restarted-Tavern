using System;

namespace RestartedTavern.Rules
{
    /// <summary>GAME_DESIGN §4. Chain holds spells while they wait to resolve (MTG: the stack).</summary>
    public enum Zone
    {
        Deck,
        Hand,
        Battlefield,
        Graveyard,
        Exile,
        Chain,
        /// <summary>Public zone holding each player's Tavern Dweller (GAME_DESIGN §4, §9.1). Tavern Dwellers never leave it in v0.1.</summary>
        TavernDweller,
    }

    /// <summary>GAME_DESIGN §10.</summary>
    public enum CardType
    {
        Creature,
        Sorcery,
        Instant,
        Equipment,
        Relic,
        Curse,
        /// <summary>The player's face (GAME_DESIGN §9). Lives in the Tavern Dweller zone, is never cast and never a permanent.</summary>
        TavernDweller,
    }

    /// <summary>CARD_DESIGN rarities. Legendary cards also follow the Legendary rule.</summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
    }

    /// <summary>GAME_DESIGN §11. CantBlock is a rules flag used by card text ("Can't block.").</summary>
    [Flags]
    public enum Keyword
    {
        None = 0,
        Trample = 1 << 0,
        Flying = 1 << 2,
        Lifelink = 1 << 3,
        Reach = 1 << 4,
        CantBlock = 1 << 5,
        /// <summary>Attacking doesn't tap it (MTG 702.20), so it can still block in the opponent's attack round.</summary>
        Vigilance = 1 << 6,
    }

    /// <summary>
    /// GAME_DESIGN §6, split into MTG-style steps. Start covers refill + untap + upkeep, Main1 is the action
    /// phase, the combat steps run when the attack token holder attacks, and Cleanup covers discard-to-hand-size
    /// and the mana → Gold conversion.
    /// </summary>
    public enum Step
    {
        Mulligan,
        Start,
        Draw,
        Main1,
        BeginCombat,
        DeclareAttackers,
        DeclareBlockers,
        CombatDamage,
        End,
        Cleanup,
        GameOver,
    }
}
