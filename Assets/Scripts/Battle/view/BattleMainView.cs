using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


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
        private GameObject endGame;
        private GameObject cardTemplet;
        private GameObject cardContainer;

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
            cardTemplet = transform.Find("cardTemplet").gameObject;
            cardContainer = transform.Find("cardContainer").gameObject;

            diceBtn.onClick.AddListener(OnClickDiceBtn);
            quitBtn.onClick.AddListener(OnClickQuitBtn);

            int randomIndex = UnityEngine.Random.Range(0, 6);
            diceImg.sprite = BattleManager.Instance.diceSprites[randomIndex];
            
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, ChangeDiceBtnSprite);
            EventMgr.Instance.Register<bool>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, ChangeDiceBtnState);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, UpdateEnergy);
            // EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_DICE_NUM, UpdateDiceNum);
            EventMgr.Instance.Register<bool>(EventType.EVENT_BATTLE_UI, SubEventType.END_GAME, ShowGameEnd);
            EventMgr.Instance.Register<PlayerPawn>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_PLAYER_PAWN, UpdatePlayerState);
            EventMgr.Instance.Register<List<CardBase>>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_HAND_DECK, UpdateCards);

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

        public void UpdatePlayerState(PlayerPawn player)
        {
            //healthBarSlider.value = player._health / player._maxHealth;
            //playerHealthTxt.text = player._health.ToString() + " " + player._maxHealth.ToString();
            //playerExperience.text = player._experience.ToString();
            //playerLevel.text = player._level.ToString();
        }

        public void UpdateCards(List<CardBase> cards)
        {
            foreach (CardBase card in cards)
            {
                GameObject cardGo = GameObject.Instantiate(cardTemplet, cardContainer.transform);
                cardGo.SetActive(true);
                cardGo.FindInChildren("step").GetComponent<TextMeshProUGUI>().text = card._step.ToString();

            }
        }
    }
}
