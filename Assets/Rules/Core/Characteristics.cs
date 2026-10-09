namespace RestartedTavern.Rules
{
    /// <summary>A creature's current, computed stats (after continuous effects).</summary>
    public struct Characteristics
    {
        public int Power;
        public int MaxHealth;
        public Keyword Keywords;
        /// <summary>MaxHealth − Damage. The creature dies at 0 or less.</summary>
        public int RemainingHealth;

        public bool Has(Keyword k) => (Keywords & k) != 0;
    }

    /// <summary>
    /// An always-on ability of a permanent on the battlefield (MTG static ability).
    /// Applied when computing characteristics; see <see cref="CharacteristicsCalculator"/>.
    /// </summary>
    public abstract class StaticAbility
    {
        public string Text { get; set; } = "";

        /// <summary>Change <paramref name="ch"/> if this ability affects <paramref name="affected"/>.</summary>
        public abstract void Apply(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            ref Characteristics ch);
    }

    /// <summary>
    /// "Your [other] [Subtype] creatures get +P/+H [and have Keyword]." (lords and anthems).
    /// </summary>
    public sealed class AnthemAbility : StaticAbility
    {
        /// <summary>Null means every creature you control.</summary>
        public string Subtype { get; set; }
        public bool OthersOnly { get; set; }
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        public override void Apply(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            ref Characteristics ch)
        {
            if (affected.Controller != source.Controller) return;
            if (OthersOnly && affected.Id == source.Id) return;
            var def = db.Get(affected.DefinitionId);
            if (!def.IsCreature) return;
            if (Subtype != null && !def.HasSubtype(Subtype)) return;
            ch.Power += Power;
            ch.MaxHealth += Health;
            ch.Keywords |= Grants;
        }
    }

    /// <summary>
    /// "Enchanted / equipped creature gets +P/+H [and Keyword]": affects whatever the source
    /// (a Curse or Equipment) is attached to. Negative values for Curses (Hex of Frailty: -1/-1).
    /// </summary>
    public sealed class AttachedCreatureModifier : StaticAbility
    {
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        public override void Apply(GameState state, CardDatabase db, CardInstance source, CardInstance affected,
            ref Characteristics ch)
        {
            if (source.AttachedToObject.IsNone || affected.Id != source.AttachedToObject) return;
            ch.Power += Power;
            ch.MaxHealth += Health;
            ch.Keywords |= Grants;
        }
    }

    /// <summary>
    /// Computes current characteristics, following the MTG layer order (CR 613) for the
    /// parts the engine supports today:
    ///   layer 6 (abilities) and layer 7c (P/T modifiers), in timestamp order:
    ///   printed values → static abilities → +1/+1 counters → until-end-of-turn effects.
    /// Only additive effects exist so far, so the order inside 7c can't change the result.
    /// Characteristic-setting effects (7a/7b) and type-changing effects come later.
    /// </summary>
    public static class CharacteristicsCalculator
    {
        public static Characteristics Compute(GameState state, CardDatabase db, CardInstance card)
        {
            var def = db.Get(card.DefinitionId);
            var ch = new Characteristics
            {
                Power = def.Power,
                MaxHealth = def.Health,
                Keywords = def.Keywords,
            };

            if (card.Zone == Zone.Battlefield)
            {
                foreach (var player in state.Players)
                {
                    foreach (var source in player.Battlefield)
                    {
                        var sourceDef = db.Get(source.DefinitionId);
                        foreach (var st in sourceDef.Statics)
                            st.Apply(state, db, source, card, ref ch);
                    }
                }

                ch.Power += card.PlusOneCounters;
                ch.MaxHealth += card.PlusOneCounters;

                foreach (var mod in state.UntilEndOfTurn)
                {
                    if (mod.Target != card.Id) continue;
                    ch.Power += mod.Power;
                    ch.MaxHealth += mod.Health;
                    ch.Keywords |= mod.Grants;
                }
            }

            ch.RemainingHealth = ch.MaxHealth - card.Damage;
            return ch;
        }
    }

    /// <summary>"... until end of turn" on one object. Removed in the cleanup step.</summary>
    public sealed class TemporaryModifier
    {
        public ObjectId Target { get; set; }
        public int Power { get; set; }
        public int Health { get; set; }
        public Keyword Grants { get; set; }

        public TemporaryModifier Clone() => (TemporaryModifier)MemberwiseClone();
    }
}
