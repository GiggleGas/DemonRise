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
        // UI¿Ø¼þ
        private Button diceBtn;
        private Image diceImg;
        private Button quitBtn;
        private TextMeshProUGUI energyText;
        private Slider healthBarSlider;
        private TextMeshProUGUI playerHealthTxt;
        private TextMeshProUGUI playerExperience;
        private TextMeshProUGUI playerLevel;

        protected override void OnAwake()
        {
            base.OnAwake();
            diceBtn = transform.Find("diceBtn").GetComponent<Button>();
            quitBtn = transform.Find("quitBtn").GetComponent<Button>();
            diceImg = transform.Find("diceBtn").GetComponent<Image>();
            energyText = transform.Find("energy").GetComponent<TextMeshProUGUI>();
            healthBarSlider = transform.Find("playerState/health").GetComponent<Slider>();
            playerHealthTxt = transform.Find("playerState/health/healthTxt").GetComponent<TextMeshProUGUI>();
            playerExperience = transform.Find("playerState/goldenBG/playerExperience").GetComponent<TextMeshProUGUI>();
            playerLevel = transform.Find("playerState/playerIcon/playerLevel").GetComponent<TextMeshProUGUI>();

            diceBtn.onClick.AddListener(OnClickDiceBtn);
            quitBtn.onClick.AddListener(OnClickQuitBtn);

            int randomIndex = UnityEngine.Random.Range(0, 6);
            diceImg.sprite = BattleManager.Instance.diceSprites[randomIndex];
            
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, ChangeDiceBtnSprite);
            EventMgr.Instance.Register<bool>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, ChangeDiceBtnState);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, UpdateEnergy);
            EventMgr.Instance.Register<PlayerPawn>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_PLAYER_PAWN, UpdatePlayerState);
        }

        private void OnClickDiceBtn()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE);
            diceBtn.enabled = false;
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
            // todo
        }

        public void UpdateEnergy(int energy)
        {
            energyText.text = energy.ToString();
        }

        public void UpdatePlayerState(PlayerPawn player)
        {
            healthBarSlider.value = player._health / player._maxHealth;
            playerHealthTxt.text = player._health.ToString() + " " + player._maxHealth.ToString();
            playerExperience.text = player._experience.ToString();
            playerLevel.text = player._level.ToString();
        }
    }
}
