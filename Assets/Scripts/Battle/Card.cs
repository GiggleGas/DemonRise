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
        public int _step;
        public int _range;
        public UsageType _usageType;

        public CardBase(int typeID, int step, int range, UsageType usageType)
        {
            _id = CardHelper.currentID++;
            _typeID = typeID;
            _step = step;
            _range = range;
            _usageType = usageType;
        }

        public virtual void Click() { }

        public virtual void Hold() { }

        public void Consume(bool bEnergy) 
        {
            if(bEnergy)
            {
                ConsumeToEnergy();
            }
            else
            {
                ConsumeSkill();
            }
        }

        public void FinishCardSkill()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CARD_USE_FINISH);
        }

        public virtual void ConsumeToEnergy()
        {
            BattleManager.Instance.ModifyEnergy(_step);
            FinishCardSkill();
        }

        public virtual void ConsumeSkill() { }

        public virtual bool IsValidToConsume(BlockInfo playerBlock, BlockInfo targetBlock)
        {
            return true;
        }
    }

    public class AttackCard : CardBase
    {
        public float _attackValue;
        public AttackCard(int typeID, int step, int range, float attackValue) : base(typeID, step, range, UsageType.Reusable) 
        {
            _attackValue = attackValue;
        }

        public override void Click()
        {

        }

        public override bool IsValidToConsume(BlockInfo playerBlock, BlockInfo targetBlock)
        {
            return targetBlock.pawn != null && targetBlock.pawn._teamType == TeamType.Enemy;
        }

        public override void ConsumeSkill()
        {
            base.ConsumeSkill();
            BattleManager.Instance._playerPawn.UpadateAttackTimes(_attackValue, false, false);
            BattleManager.Instance.TryAttack(BattleManager.Instance.pickedBlock);
            EventMgr.Instance.Register<MapPawn, MapPawn>(EventType.EVENT_BATTLE_UI, SubEventType.PLAYER_ATTACK_FINISH, RecoverAttack);
        }

        public void RecoverAttack(MapPawn mapPawnA, MapPawn mapPawnB)
        {
            FinishCardSkill();
        }
    }

    public class FastMoveCard : CardBase
    {
        public FastMoveCard(int typeID, int step, int range) : base(typeID, step, range, UsageType.Reusable) { }

        public override void ConsumeSkill()
        {
            base.ConsumeSkill();
            BattleManager.Instance.ModifyEnergy(_step);
            FinishCardSkill();
        }

    }

    public class DefenceCard : CardBase
    {
        public float _defenceValue;
        public DefenceCard(int typeID, int step, int range, float defenceValue) : base(typeID, step, range, UsageType.Reusable)
        {
            _defenceValue = defenceValue;
        }

        public override void ConsumeSkill()
        {
            base.ConsumeSkill();
            BattleManager.Instance._playerPawn.AddDefence(_defenceValue);
            FinishCardSkill();
        }
    }
}
