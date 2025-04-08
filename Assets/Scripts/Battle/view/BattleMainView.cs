using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting;
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

        private Dictionary<int, GameObject> _cards;
        private GameObject hiddenCard = null;

        protected override void OnAwake()
        {
            base.OnAwake();
            diceBtn = transform.Find("diceBtn").GetComponent<Button>();
            quitBtn = transform.Find("quitBtn").GetComponent<Button>();
            diceImg = transform.Find("diceBtn").GetComponent<Image>();
            diceNumTxt = transform.Find("diceBtn/diceNum").GetComponent<TextMeshProUGUI>();
            energyTens = transform.Find("energy/tens").GetComponent<Image>();
            energyOnes = transform.Find("energy/ones").GetComponent<Image>();
            energyBtn = transform.Find("energy").GetComponent<Button>();
            endGame = transform.Find("EndGame").gameObject;
            cardTemplete = transform.Find("cardTemplete").gameObject;
            cardContainer = transform.Find("cardContainer").gameObject;

            diceBtn.onClick.AddListener(OnClickDiceBtn);
            quitBtn.onClick.AddListener(OnClickQuitBtn);
            energyBtn.onClick.AddListener(OnClickEnergyBtn);

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

        private void OnClickEnergyBtn()
        {
            // todo 通知选择步数
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CARD_TO_ENERGY);
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
            if(_cards.Count <= 0)
            {
                foreach (CardBase card in cards)
                {
                    GameObject cardGo = GameObject.Instantiate(cardTemplete, cardContainer.transform);
                    cardGo.SetActive(true);
                    cardGo.FindInChildren("step").GetComponent<Image>().sprite = BattleManager.Instance.GetGameEntry().numSprites[card._step % 10];
                    cardGo.FindInChildren("icon").GetComponent<Image>().sprite = BattleManager.Instance.GetGameEntry().spriteConfig[card._typeID];
                    cardGo.GetComponent<Button>().onClick.AddListener(() => 
                        OnClickCard(card)
                    );
                    _cards.Add(card._id, cardGo);
                }
            }
            else
            {
                foreach(CardBase card in cards)
                {
                    if(card == null || _cards.ContainsKey(card._id))
                    {
                        continue;
                    }
                    if(hiddenCard == null)
                    {
                        hiddenCard = GameObject.Instantiate(cardTemplete, cardContainer.transform);
                    }
                    hiddenCard.SetActive(true);
                    hiddenCard.FindInChildren("step").gameObject.SetActive(true);
                    hiddenCard.FindInChildren("icon").gameObject.SetActive(true);
                    hiddenCard.FindInChildren("step").GetComponent<Image>().sprite = BattleManager.Instance.GetGameEntry().numSprites[card._step % 10];
                    hiddenCard.FindInChildren("icon").GetComponent<Image>().sprite = BattleManager.Instance.GetGameEntry().spriteConfig[card._typeID];
                    hiddenCard.GetComponent<Button>().onClick.AddListener(() =>
                        OnClickCard(card)
                    );
                    _cards.Add(card._id, hiddenCard);

                }
            }
        }

        public void OnClickCard(CardBase card)
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CLICK_CARD, card);
        }

        public void HideUsedCard(int id)
        {
            if(_cards.ContainsKey(id))
            {
                hiddenCard = _cards[id];
                _cards.Remove(id);
                hiddenCard.FindInChildren("step").gameObject.SetActive(false);
                hiddenCard.FindInChildren("icon").gameObject.SetActive(false);
            }
        }

        public void OnCancealSelectCard(int id)
        {
            // todo 做点表现
        }
    }
}
