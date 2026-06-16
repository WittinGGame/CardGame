using System.Collections.Generic;

namespace CardBattle.Core
{
    public class RewardSession
    {
        private readonly List<CardData> internalChoices = new List<CardData>();

        public int GoldAmount { get; }
        public bool GoldGranted { get; internal set; }

        public IReadOnlyList<CardData> CardChoices => internalChoices;

        public bool CardChoiceResolved { get; internal set; }
        public bool WasCardSkipped { get; internal set; }
        public CardData SelectedCard { get; internal set; }

        public bool IsComplete =>
            GoldGranted &&
            CardChoiceResolved;

        public int ChoiceCount => internalChoices.Count;
        public bool HasCardChoices => internalChoices.Count > 0;

        public RewardSession(int goldAmount, IEnumerable<CardData> cardChoices)
        {
            GoldAmount = goldAmount < 0 ? 0 : goldAmount;

            if (cardChoices != null)
            {
                foreach (CardData card in cardChoices)
                {
                    if (card == null)
                        continue;

                    internalChoices.Add(card);
                }
            }

            GoldGranted = false;
            CardChoiceResolved = false;
            WasCardSkipped = false;
            SelectedCard = null;
        }
    }
}
