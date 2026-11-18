using TMPro;
using UnityEngine;

namespace Arcanist
{
    internal sealed class DeckConditionView
    {
        private const string DefaultWarning = "카드 수와 마력 용량 조건을 만족해야 합니다.";

        private readonly TMP_Text allCardCountText;
        private readonly TMP_Text deckCardCountText;
        private readonly TMP_Text deckManaCostText;
        private readonly InventoryGuideMessage guideMessage;

        private bool isWarningVisible;

        public DeckConditionView(
            TMP_Text allCardCountText,
            TMP_Text deckCardCountText,
            TMP_Text deckManaCostText,
            InventoryGuideMessage guideMessage)
        {
            this.allCardCountText = allCardCountText;
            this.deckCardCountText = deckCardCountText;
            this.deckManaCostText = deckManaCostText;
            this.guideMessage = guideMessage;
        }

        public void Refresh(PlayerInventory inventory, int allCardCount)
        {
            int ownedCount = inventory != null ? inventory.OwnedCards.Count : 0;

            if (allCardCountText != null)
                allCardCountText.text = $"{ownedCount} / {allCardCount}";

            RefreshDeckCount(inventory);
            RefreshManaCost(inventory);
            RefreshWarning(inventory);
        }

        public void ShowWarning(PlayerInventory inventory)
        {
            guideMessage?.ShowPersistent(GetWarningMessage(inventory));
            isWarningVisible = true;
        }

        public void HideWarning(bool immediate)
        {
            isWarningVisible = false;
            guideMessage?.Hide(immediate);
        }

        private void RefreshDeckCount(PlayerInventory inventory)
        {
            if (deckCardCountText == null)
                return;

            deckCardCountText.text = inventory == null
                ? "0 / 0"
                : $"카드  현재 {inventory.DeckCardCount} / 최소 {inventory.MinDeckCardCount}";
            deckCardCountText.color = inventory != null && inventory.HasEnoughDeckCards()
                ? Color.white
                : Color.red;
        }

        private void RefreshManaCost(PlayerInventory inventory)
        {
            if (deckManaCostText == null)
                return;

            int manaCost = inventory != null ? inventory.DeckManaCost : 0;
            deckManaCostText.text = inventory == null
                ? "0 / 0"
                : $"마력  {manaCost} / {inventory.MinDeckManaCost}~{inventory.MaxDeckManaCost}";
            deckManaCostText.color = inventory != null && inventory.IsDeckManaCostValid()
                ? Color.white
                : Color.red;
        }

        private void RefreshWarning(PlayerInventory inventory)
        {
            if (inventory == null || inventory.CanCompleteDeckEdit() == false)
            {
                ShowWarning(inventory);
                return;
            }

            if (isWarningVisible)
                HideWarning(true);
        }

        private static string GetWarningMessage(PlayerInventory inventory)
        {
            if (inventory == null)
                return DefaultWarning;

            int manaCost = inventory.DeckManaCost;

            if (inventory.HasEnoughDeckCards() == false)
                return $"카드 덱에는 최소 {inventory.MinDeckCardCount}장의 카드가 필요합니다.";
            if (manaCost < inventory.MinDeckManaCost)
                return $"마력 용량이 부족합니다. ({manaCost} / {inventory.MinDeckManaCost}~{inventory.MaxDeckManaCost})";
            if (manaCost > inventory.MaxDeckManaCost)
                return $"마력 용량을 초과했습니다. ({manaCost} / {inventory.MinDeckManaCost}~{inventory.MaxDeckManaCost})";

            return DefaultWarning;
        }
    }
}
