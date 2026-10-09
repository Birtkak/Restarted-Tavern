using System;
using System.Collections.Generic;

namespace RestartedTavern.Rules
{
    /// <summary>All card definitions the engine knows about, looked up by id.</summary>
    public sealed class CardDatabase
    {
        private readonly Dictionary<string, CardDefinition> _cards = new Dictionary<string, CardDefinition>();

        public CardDatabase() { }

        public CardDatabase(IEnumerable<CardDefinition> cards)
        {
            foreach (var c in cards) Add(c);
        }

        public void Add(CardDefinition card)
        {
            if (string.IsNullOrEmpty(card.Id)) throw new ArgumentException("Card needs an id.");
            if (_cards.ContainsKey(card.Id)) throw new ArgumentException("Duplicate card id: " + card.Id);
            _cards.Add(card.Id, card);
        }

        public CardDefinition Get(string id)
        {
            if (id != null && _cards.TryGetValue(id, out var card)) return card;
            throw new KeyNotFoundException("Unknown card id: " + id);
        }

        public bool Contains(string id) => id != null && _cards.ContainsKey(id);

        public IEnumerable<CardDefinition> All => _cards.Values;
    }
}
