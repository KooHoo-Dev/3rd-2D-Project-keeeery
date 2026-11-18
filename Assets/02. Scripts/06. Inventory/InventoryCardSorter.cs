using System.Collections.Generic;

namespace Arcanist
{
    internal static class InventoryCardSorter
    {
        public static List<CardData> SortForCollection(
            IReadOnlyList<CardData> allCards,
            PlayerInventory inventory)
        {
            List<CardData> availableCards = new();
            List<CardData> cardsInDeck = new();
            List<CardData> unownedCards = new();

            if (allCards == null || inventory == null)
                return availableCards;

            foreach (CardData card in allCards)
            {
                if (card == null)
                    continue;

                if (inventory.HasCard(card) == false)
                    unownedCards.Add(card);
                else if (inventory.IsInDeck(card))
                    cardsInDeck.Add(card);
                else
                    availableCards.Add(card);
            }

            availableCards.AddRange(cardsInDeck);
            availableCards.AddRange(unownedCards);
            return availableCards;
        }
    }
}
