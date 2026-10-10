using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>
    /// "[Cost]: [Effect]." (MTG 602). Activating puts the ability on the Chain (GAME_DESIGN §8).
    /// Payment (§5.2): the generic part (<see cref="Cost"/> and X) is paid with Gold first, then
    /// mana; <see cref="GoldCost"/> ("Pay N Gold") is paid only with Gold, like Invest.
    /// Equip and Tavern Dweller Powers are activated abilities with extra rules (see the flags).
    /// </summary>
    public sealed class ActivatedAbility
    {
        /// <summary>Generic cost: Gold first, then mana (§5.2).</summary>
        public int Cost { get; set; }
        /// <summary>"X, ...": X is added to the generic cost, chosen on activation (one action per payable X).</summary>
        public bool HasX { get; set; }
        /// <summary>"Pay N Gold": paid only with Gold (decided 2026-10-09).</summary>
        public int GoldCost { get; set; }
        /// <summary>"Tap: ..." (no summoning sickness, §7.4: usable the round the permanent arrives).</summary>
        public bool TapCost { get; set; }
        /// <summary>"Sacrifice a creature: ..." The creature is chosen as part of the action.</summary>
        public bool SacrificeCreatureCost { get; set; }
        /// <summary>"Pay N life: ..."</summary>
        public int LifeCost { get; set; }

        /// <summary>"Activate only as a sorcery": your own action, empty Chain.</summary>
        public bool SorcerySpeed { get; set; }
        /// <summary>"Activate only if you have N or more Gold" (Auditor Prime), checked before paying. 0 = no condition.</summary>
        public int ActivateOnlyWithGold { get; set; }
        /// <summary>"Activate only once each turn" (MTG: once in each turn, yours or not).</summary>
        public bool OncePerTurn { get; set; }
        /// <summary>Equip X (§10): sorcery speed, target creature you control, Equip cost modifiers apply.</summary>
        public bool IsEquip { get; set; }
        /// <summary>A Tavern Dweller Power (§9.1): once each turn, instant speed.</summary>
        public bool IsTavernDwellerPower { get; set; }

        public List<TargetSlot> Targets { get; set; } = new List<TargetSlot>();
        /// <summary>The source can't be one of its own targets.</summary>
        public bool TargetsExcludeSource { get; set; }

        public List<Effect> Effects { get; set; } = new List<Effect>();
        public string Text { get; set; } = "";

        public bool LimitedPerTurn => OncePerTurn || IsTavernDwellerPower;
        public bool IsSorcerySpeed => SorcerySpeed || IsEquip;
    }
}
