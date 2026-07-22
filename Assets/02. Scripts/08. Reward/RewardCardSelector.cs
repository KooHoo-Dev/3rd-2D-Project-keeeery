using System.Collections.Generic;
using UnityEngine;

namespace Arcanist
{
    internal readonly struct RewardGradeWeights
    {
        private readonly int common;
        private readonly int rare;
        private readonly int epic;
        private readonly int legendary;

        public RewardGradeWeights(int common, int rare, int epic, int legendary)
        {
            this.common = Mathf.Max(0, common);
            this.rare = Mathf.Max(0, rare);
            this.epic = Mathf.Max(0, epic);
            this.legendary = Mathf.Max(0, legendary);
        }

        public int Get(CardGrade grade)
        {
            return grade switch
            {
                CardGrade.Common => common,
                CardGrade.Rare => rare,
                CardGrade.Epic => epic,
                CardGrade.Legendary => legendary,
                _ => 0,
            };
        }
    }

    /// <summary>
    /// Selects unique, unowned reward cards using grade weights.
    /// Presentation and granting are intentionally handled elsewhere.
    /// </summary>
    internal sealed class RewardCardSelector
    {
        private readonly IReadOnlyList<CardData> allCards;
        private readonly RewardGradeWeights gradeWeights;
        private readonly CardGrade minimumGrade;

        public RewardCardSelector(
            IReadOnlyList<CardData> allCards,
            RewardGradeWeights gradeWeights,
            CardGrade minimumGrade)
        {
            this.allCards = allCards;
            this.gradeWeights = gradeWeights;
            this.minimumGrade = minimumGrade;
        }

        public List<CardData> CreateRewards(int count, IEnumerable<CardData> excluded)
        {
            List<CardData> rewards = new();
            HashSet<CardData> excludedCards = new();

            if (excluded != null)
            {
                foreach (CardData card in excluded)
                {
                    if (card != null)
                        excludedCards.Add(card);
                }
            }

            int safeCount = Mathf.Max(0, count);
            for (int i = 0; i < safeCount; i++)
            {
                CardData reward = Draw(excludedCards);
                if (reward == null)
                    break;

                rewards.Add(reward);
                excludedCards.Add(reward);
            }

            return rewards;
        }

        private CardData Draw(HashSet<CardData> excludedCards)
        {
            List<CardData> candidates = GetAvailableCards(excludedCards);
            if (candidates.Count == 0)
                return null;

            int totalWeight = 0;
            foreach (CardData card in candidates)
                totalWeight += gradeWeights.Get(card.Grade);

            if (totalWeight <= 0)
                return candidates[Random.Range(0, candidates.Count)];

            int randomWeight = Random.Range(0, totalWeight);
            int accumulatedWeight = 0;

            foreach (CardData card in candidates)
            {
                accumulatedWeight += gradeWeights.Get(card.Grade);
                if (randomWeight < accumulatedWeight)
                    return card;
            }

            return candidates[^1];
        }

        private List<CardData> GetAvailableCards(HashSet<CardData> excludedCards)
        {
            List<CardData> candidates = new();
            if (allCards == null)
                return candidates;

            foreach (CardData card in allCards)
            {
                if (card == null || card.Grade < minimumGrade || excludedCards.Contains(card))
                    continue;

                candidates.Add(card);
            }

            return candidates;
        }
    }
}
