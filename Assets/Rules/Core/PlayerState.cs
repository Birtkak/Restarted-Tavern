using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    public sealed class PlayerState
    {
        public PlayerId Id { get; set; }
        /// <summary>Seat index = turn order position.</summary>
        public int Seat { get; set; }
        /// <summary>Every player gets a team from the start (GAME_DESIGN §13.1). Free-for-all: one team per player.</summary>
        public int TeamId { get; set; }
        public string PatronId { get; set; }

        public int Life { get; set; }
        public int MaxMana { get; set; }
        /// <summary>Only filled during this player's own turn (GAME_DESIGN §5.2).</summary>
        public int Mana { get; set; }
        public int Gold { get; set; }

        public bool HasLost { get; set; }
        /// <summary>Set when a draw from an empty deck was attempted; checked as a state-based action.</summary>
        public bool DrewFromEmptyDeck { get; set; }

        public int MulligansTaken { get; set; }

        /// <summary>Index 0 is the top of the deck.</summary>
        public List<CardInstance> Deck { get; set; } = new List<CardInstance>();
        public List<CardInstance> Hand { get; set; } = new List<CardInstance>();
        /// <summary>Permanents this player <b>controls</b>.</summary>
        public List<CardInstance> Battlefield { get; set; } = new List<CardInstance>();
        /// <summary>Index 0 is the bottom; the newest card is last.</summary>
        public List<CardInstance> Graveyard { get; set; } = new List<CardInstance>();
        public List<CardInstance> Exile { get; set; } = new List<CardInstance>();

        public List<CardInstance> GetZone(Zone zone)
        {
            switch (zone)
            {
                case Zone.Deck: return Deck;
                case Zone.Hand: return Hand;
                case Zone.Battlefield: return Battlefield;
                case Zone.Graveyard: return Graveyard;
                case Zone.Exile: return Exile;
                default: throw new ArgumentException("Players have no " + zone + " zone.");
            }
        }

        public PlayerState Clone()
        {
            var p = (PlayerState)MemberwiseClone();
            p.Deck = CloneList(Deck);
            p.Hand = CloneList(Hand);
            p.Battlefield = CloneList(Battlefield);
            p.Graveyard = CloneList(Graveyard);
            p.Exile = CloneList(Exile);
            return p;
        }

        internal static List<CardInstance> CloneList(List<CardInstance> list)
        {
            var copy = new List<CardInstance>(list.Count);
            foreach (var c in list) copy.Add(c.Clone());
            return copy;
        }

        public override string ToString() => Id.ToString();
    }
}
