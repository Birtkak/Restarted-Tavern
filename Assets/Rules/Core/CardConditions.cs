using System;

namespace RestartedTavern.Rules
{
    public enum ConditionKind
    {
        None,
        /// <summary>"if this has no damage" (Sproutling).</summary>
        SourceHasNoDamage,
        /// <summary>"if you have N or more Gold" (Velvet Embezzler).</summary>
        YouHaveGoldAtLeast,
        /// <summary>"if each opponent has N or less life" (Closing Bell).</summary>
        EachOpponentHasLifeAtMost,
    }

    /// <summary>
    /// An intervening "if" on a triggered ability (MTG 603.4), written as data so cards can live in data files. Checked
    /// when it would trigger and again when it resolves.
    /// </summary>
    public sealed class TriggerCondition
    {
        public ConditionKind Kind { get; set; }
        public int Amount { get; set; }

        public bool Holds(GameState s, CardDatabase db, PlayerId controller, ObjectId source)
        {
            switch (Kind)
            {
                case ConditionKind.SourceHasNoDamage:
                    return s.FindOnBattlefield(source)?.Damage == 0;
                case ConditionKind.YouHaveGoldAtLeast:
                    return s.GetPlayer(controller).Gold >= Amount;
                case ConditionKind.EachOpponentHasLifeAtMost:
                    foreach (var p in s.Players)
                        if (!p.HasLost && s.AreOpponents(controller, p.Id) && p.Life > Amount) return false;
                    return true;
                case ConditionKind.None:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null);
            }
        }
    }

    public enum CountKind
    {
        None,
        /// <summary>Equipment its controller controls (Archon Lumen).</summary>
        EquipmentYouControl,
        /// <summary>Creature cards in its controller's graveyard (Hex of Hollow Bones).</summary>
        CreatureCardsInYourGraveyard,
    }

    /// <summary>"for each [thing]" counted as data, seen from the source's controller, optionally capped ("up to -4/-4").</summary>
    public sealed class DynamicCount
    {
        public CountKind Kind { get; set; }
        /// <summary>The most it can be. 0 = no cap.</summary>
        public int Max { get; set; }

        public int Of(GameState s, CardDatabase db, CardInstance source)
        {
            var p = s.GetPlayer(source.Controller);
            int n = 0;
            switch (Kind)
            {
                case CountKind.EquipmentYouControl:
                    foreach (var c in p.Battlefield)
                        if (db.Get(c.DefinitionId).Type == CardType.Equipment) n++;
                    break;
                case CountKind.CreatureCardsInYourGraveyard:
                    foreach (var c in p.Graveyard)
                        if (db.Get(c.DefinitionId).IsCreature) n++;
                    break;
                case CountKind.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null);
            }
            return Max > 0 ? Math.Min(Max, n) : n;
        }
    }
}
