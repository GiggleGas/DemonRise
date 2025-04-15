using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace PDR
{
    public class CardHelper
    {
        public static int currentID = 0;
    }


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
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.HIDE_USED_CARD, OnCardUsed);
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
        public void OnCardUsed(int id)
        {
            Debug.Log("Try Use " + id.ToString());
            CardBase usedCard = null;
            foreach (CardBase card in _handPile)
            {
                if(card!=null && id == card._id)
                {
                    usedCard = card; 
                    break;
                }
            }
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
        public int _typeID;
        public int _cost;
        public int _range;
        public UsageType _usageType;

        public CardBase(int typeID, int cost, int range, UsageType usageType)
        {
            _id = CardHelper.currentID++;
            _typeID = typeID;
            _cost = cost;
            _range = range;
            _usageType = usageType;
        }

        public virtual void Click() { }

        public virtual void Hold() { }

        public void Consume() 
        {
            ConsumeSkill();
        }

        public void FinishCardSkill()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CARD_USE_FINISH);
        }

        public virtual void ConsumeSkill() { }

        public virtual bool IsValidToConsume(BlockInfo playerBlock, BlockInfo targetBlock)
        {
            return true;
        }

        public virtual string GetValueString() { return ""; }
    }

    public class AttackCard : CardBase
    {
        public float _attackValue;
        public AttackCard(int typeID, int cost, int range, float attackValue) : base(typeID, cost, range, UsageType.Reusable) 
        {
            _attackValue = attackValue;
        }

        public override void Click()
        {

        }

        public override void ConsumeSkill()
        {
            base.ConsumeSkill();
            BattleManager.Instance._playerPawn.UpadateAttackTimes(_attackValue, false, true);
            //FinishCardSkill();
        }

        public override string GetValueString()
        {
            return "Attack+" + _attackValue.ToString();
        }
    }

    public class FastMoveCard : CardBase
    {
        private int _moveValue;
        public FastMoveCard(int typeID, int cost, int range, int moveValue) : base(typeID, cost, range, UsageType.Reusable) 
        {
            _moveValue = moveValue;
        }

        public override void ConsumeSkill()
        {
            base.ConsumeSkill();
            BattleManager.Instance.ModifyEnergy(_moveValue);
            //FinishCardSkill();
        }

        public override string GetValueString()
        {
            return "Energy+" + _moveValue.ToString();
        }
    }

    public class DefenceCard : CardBase
    {
        public float _defenceValue;
        public DefenceCard(int typeID, int cost, int range, float defenceValue) : base(typeID, cost, range, UsageType.Reusable)
        {
            _defenceValue = defenceValue;
        }

        public override void ConsumeSkill()
        {
            base.ConsumeSkill();
            BattleManager.Instance._playerPawn.AddDefence(_defenceValue);
            //FinishCardSkill();
        }

        public override string GetValueString()
        {
            return "Defence+" + _defenceValue.ToString();
        }
    }

    public class FireBallCard : CardBase
    {
        public float _fireAttack;
        public FireBallCard(int typeID, int cost, int range, float fireAttack) : base(typeID, cost, range, UsageType.Reusable)
        {
            _fireAttack = fireAttack;
        }
        public override void ConsumeSkill()
        {
            base.ConsumeSkill();
            BattleManager.Instance.pickedBlock.pawn.TakeDamage(BattleManager.Instance._playerPawn, BattleManager.Instance._playerPawn.GetAttackValue() + _fireAttack);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.SPAWN_FIRE);
            //FinishCardSkill();
        }
        public override string GetValueString()
        {
            return "Attack+" + _fireAttack.ToString();
        }
    }
}
