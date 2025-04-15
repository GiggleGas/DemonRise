using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;


namespace PDR
{ 
    public class BattleMainView : BaseView
    {
        // UI控件
        private Button diceBtn;
        private Image diceImg;
        private TextMeshProUGUI diceNumTxt;
        private Button quitBtn;
        private Image energyTens;
        private Image energyOnes;
        private Button energyBtn;
        private GameObject endGame;
        private GameObject cardTemplete;
        private GameObject cardContainer;
        private Slider roundSlider;
        private TextMeshProUGUI battleStateTxt;
        private TextMeshProUGUI subStateTxt;
        private TextMeshProUGUI currentSelectCard;
        
        private Dictionary<int, GameObject> _cards;

        protected override void OnAwake()
        {
            base.OnAwake();
            diceBtn = transform.Find("diceBtn").GetComponent<Button>();
            quitBtn = transform.Find("quitBtn").GetComponent<Button>();
            diceImg = transform.Find("diceBtn").GetComponent<Image>();
            diceNumTxt = transform.Find("diceBtn/diceNum").GetComponent<TextMeshProUGUI>();
            energyTens = transform.Find("energy/tens").GetComponent<Image>();
            energyOnes = transform.Find("energy/ones").GetComponent<Image>();
            endGame = transform.Find("EndGame").gameObject;
            cardTemplete = transform.Find("cardTemplete").gameObject;
            cardContainer = transform.Find("cardContainer").gameObject;
            roundSlider = transform.Find("round").GetComponent<Slider>();
            battleStateTxt = transform.Find("battleState").GetComponent<TextMeshProUGUI>();
            subStateTxt = transform.Find("subState").GetComponent<TextMeshProUGUI>();
            subStateTxt = transform.Find("subState").GetComponent<TextMeshProUGUI>();
            currentSelectCard = transform.Find("currentSelectCard").GetComponent<TextMeshProUGUI>();

            diceBtn.onClick.AddListener(OnClickDiceBtn);
            quitBtn.onClick.AddListener(OnClickQuitBtn);

            int randomIndex = UnityEngine.Random.Range(0, 6);
            diceImg.sprite = BattleManager.Instance.diceSprites[randomIndex];
            _cards = new Dictionary<int, GameObject>();

            // EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, ChangeDiceBtnSprite);
            // EventMgr.Instance.Register<bool>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, ChangeDiceBtnState);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, UpdateEnergy);
            // EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_DICE_NUM, UpdateDiceNum);
            EventMgr.Instance.Register<bool>(EventType.EVENT_BATTLE_UI, SubEventType.END_GAME, ShowGameEnd);
            EventMgr.Instance.Register<List<CardBase>>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_HAND_DECK, UpdateCards);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.CANCEAL_SELECT_CARD, OnCancealSelectCard);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.HIDE_USED_CARD, HideUsedCard);
            EventMgr.Instance.Register<BattleState, BattleSubState>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAME_STAGE, ChangeState);
            EventMgr.Instance.Register<float>(EventType.EVENT_BATTLE_UI, SubEventType.START_NEW_ROUND, UpdateRound);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_SELECTED_CARD, UpdateSelectedCard);
        }

        private void OnClickDiceBtn()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE);
            ChangeDiceBtnState(false);
        }

        private void ChangeDiceBtnSprite(int diceSpriteID)
        {
            diceImg.sprite = BattleManager.Instance.diceSprites[diceSpriteID];
        }

        private void ChangeDiceBtnState(bool bValue)
        {
            diceBtn.enabled = bValue;
        }

        private void OnClickQuitBtn()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void UpdateEnergy(int energy)
        {
            int tenNum = energy / 10;
            int oneNum = energy % 10;
            energyTens.sprite = BattleManager.Instance.GetGameEntry().numSprites[tenNum];
            energyOnes.sprite = BattleManager.Instance.GetGameEntry().numSprites[oneNum];
        }

        public void UpdateDiceNum(int diceNum)
        {
            diceNumTxt.text = "Remain Times: " + diceNum.ToString();
        }

        public void ShowGameEnd(bool isVictory)
        {
            string textToShow = isVictory ? "Victory" : "Wasted";
            endGame.SetActive(true);
            endGame.GetComponent<TextMeshProUGUI>().text = textToShow;
        }

        public void UpdateCards(List<CardBase> cards)
        {
            foreach(var iter in _cards) 
            {
                GameObject.Destroy(iter.Value);
            }
            _cards.Clear();
            
            List<string> idString = new List<string>();
            foreach (CardBase card in cards)
            {
                GameObject cardGo = GameObject.Instantiate(cardTemplete, cardContainer.transform);
                cardGo.SetActive(true);
                cardGo.FindInChildren("step").gameObject.SetActive(true);
                cardGo.FindInChildren("icon").gameObject.SetActive(true);
                cardGo.FindInChildren("id").gameObject.SetActive(true);
                cardGo.FindInChildren("step").GetComponent<Image>().sprite = BattleManager.Instance.GetGameEntry().numSprites[card._cost % 10];
                cardGo.FindInChildren("icon").GetComponent<Image>().sprite = BattleManager.Instance.GetGameEntry().spriteConfig[card._typeID];
                cardGo.FindInChildren("id").GetComponent<TextMeshProUGUI>().text = card.GetValueString();
                cardGo.GetComponent<Button>().onClick.RemoveAllListeners();
                cardGo.GetComponent<Button>().onClick.AddListener(() =>
                    OnClickCard(card)
                );
                _cards.Add(card._id, cardGo);
                idString.Add(card._id.ToString());
            }

            string debugString = string.Join(",", idString);
            Debug.Log(debugString);
        }

        public void OnClickCard(CardBase card)
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CLICK_CARD, card);
        }

        public void HideUsedCard(int id)
        {
            if(_cards.ContainsKey(id))
            {
                _cards[id].FindInChildren("step").gameObject.SetActive(false);
                _cards[id].FindInChildren("icon").gameObject.SetActive(false);
                _cards[id].FindInChildren("id").gameObject.SetActive(false);
                _cards[id].GetComponent<Button>().onClick.RemoveAllListeners();
            }
        }

        public void OnCancealSelectCard(int id)
        {
            // todo 做点表现
        }

        public void UpdateRound(float roundPercent)
        {
            roundSlider.value = roundPercent;
        }

        public void ChangeState(BattleState state, BattleSubState subState)
        {
            if(state == BattleState.PlayerTurn)
            {
                battleStateTxt.text = "Player Turn";
            }
            else if(state == BattleState.MonsterTurn)
            {
                battleStateTxt.text = "Enmey Turn";
            }

            if (subState == BattleSubState.WaitingForAction)
            {
                subStateTxt.text = "WaitingForAction";
            }
            else if (subState == BattleSubState.HoldingCard)
            {
                subStateTxt.text = "HoldingCard";

            }
            else if (subState == BattleSubState.UpdatingMove)
            {
                subStateTxt.text = "UpdatingMove";
            }
            else if (subState == BattleSubState.UpdateBattle)
            {
                subStateTxt.text = "UpdateBattle";
            }
        }

        public void UpdateSelectedCard(int id)
        {
            currentSelectCard.text = id.ToString();
        }
    }
}
