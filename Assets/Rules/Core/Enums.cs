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
        Haste = 1 << 1,
        Flying = 1 << 2,
        Lifelink = 1 << 3,
        Reach = 1 << 4,
        CantBlock = 1 << 5,
    }

    /// <summary>
    /// GAME_DESIGN §6, split into MTG-style steps. Start covers untap + upkeep,
    /// Cleanup covers discard-to-hand-size and the mana → Gold conversion.
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
        Main2,
        End,
        Cleanup,
        GameOver,
    }
}
