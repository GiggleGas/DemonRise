using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace PDR
{
    public enum UsageType
    {
        SingleUse,  // “ª¥Œ–‘ø®≈∆
        Reusable     // ø…÷ÿ∏¥ π”√ø®≈∆
    }

    public class CardContainer
    {
        public List<CardBase> _cards;
        public List<CardBase> _drawPile = new List<CardBase>();    // Œ¥≥È≈∆ø‚
        public List<CardBase> _handPile = new List<CardBase>();    //  ÷≈∆
        public List<CardBase> _discardPile = new List<CardBase>(); // ∆˙≈∆∂—

        // ≥ı ºªØ≈∆ø‚
        public void InitializeDeck(List<CardBase> cards)
        {
            _drawPile.Clear();
            _drawPile = new List<CardBase>(cards);
            ShuffleDeck();
        }

        public void AddCard(CardBase card)
        {
            _cards.Add(card);
        }

        public void RemoveCard(CardBase card)
        {
            _cards.Remove(card);
        }

        // ≥È≈∆
        public CardBase DrawCard()
        {
            if (_drawPile.Count == 0)
            {
                ReshuffleDiscardPile();
                if (_drawPile.Count == 0) return null;
            }

            CardBase drawnCard = _drawPile[0];
            _drawPile.RemoveAt(0);
            _handPile.Add(drawnCard);
            return drawnCard;
        }

        // ≥ı º≥È≈∆
        public List<CardBase> DrawInitialCards(int count)
        {
            List<CardBase> initialHand = new List<CardBase>();
            for (int i = 0; i < count; i++)
            {
                CardBase card = DrawCard();
                if (card != null) initialHand.Add(card);
            }
            return initialHand;
        }

        // œ¥Œ¥≥È≈∆ø‚
        private void ShuffleDeck()
        {
            int n = _drawPile.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                CardBase value = _drawPile[k];
                _drawPile[k] = _drawPile[n];
                _drawPile[n] = value;
            }
        }

        public List<CardBase> DrawCards(int count)
        {
            while(_handPile.Count < count)
            {
                DrawCard();
            }
            return _handPile;
        }

        private void ReshuffleDiscardPile()
        {
            Debug.Log("÷ÿ÷√≈∆ø‚...");
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            ShuffleDeck();
        }

        //  π”√ø®≈∆∫Ûµƒ¥¶¿Ì
        public void OnCardUsed(CardBase usedCard)
        {
            _handPile.Remove(usedCard);

            if (usedCard._usageType == UsageType.Reusable)
            {
                _discardPile.Add(usedCard);
            }
        }
    }

    public class CardBase
    {
        public int _id;
        public int _step;
        public UsageType _usageType;

        public CardBase(int id, int step, UsageType usageType)
        {
            _id = id;
            _step = step;
            _usageType = usageType;
        }

        public virtual void Click() { }

        public virtual void Hold() { }

        public virtual void Consume() { }
    }

    public class AttackCard : CardBase
    {
        public float _attackValue;
        public AttackCard(int id, int step, float attackValue) : base(id, step, UsageType.Reusable) 
        {
            _attackValue = attackValue;
        }

        public override void Click()
        {

        }
    }

    public class FastMoveCard : CardBase
    {
        public FastMoveCard(int id, int step) : base(id, step, UsageType.Reusable) { }

        public override void Click()
        {

        }

    }
}
