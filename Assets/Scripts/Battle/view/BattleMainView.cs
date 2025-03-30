using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
        private TextMeshProUGUI playerHealth;
        private TextMeshProUGUI playerMaxHealth;
        private TextMeshProUGUI playerAttack;
        private TextMeshProUGUI playerExperience;
        private TextMeshProUGUI playerLevel;

        protected override void OnAwake()
        {
            base.OnAwake();
            diceBtn = transform.Find("diceBtn").GetComponent<Button>();
            quitBtn = transform.Find("quitBtn").GetComponent<Button>();
            diceImg = transform.Find("diceBtn").GetComponent<Image>();
            energyText = transform.Find("energy").GetComponent<TextMeshProUGUI>();
            playerHealth = transform.Find("playerHealth").GetComponent<TextMeshProUGUI>();
            playerMaxHealth = transform.Find("playerMaxHealth").GetComponent<TextMeshProUGUI>();
            playerAttack = transform.Find("playerAttack").GetComponent<TextMeshProUGUI>();
            playerExperience = transform.Find("playerExperience").GetComponent<TextMeshProUGUI>();
            playerLevel = transform.Find("playerLevel").GetComponent<TextMeshProUGUI>();

            diceBtn.onClick.AddListener(OnClickDiceBtn);
            quitBtn.onClick.AddListener(OnClickQuitBtn);

            int randomIndex = UnityEngine.Random.Range(0, 6);
            diceImg.sprite = BattleUIManager.Instance.diceSprites[randomIndex];
            
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, ChangeDiceBtnSprite);
            EventMgr.Instance.Register<bool>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, ChangeDiceBtnState);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, UpdateEnergy);
            EventMgr.Instance.Register<PlayerState>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_PLAYERSTATE, UpdatePlayerState);
        }

        private void OnClickDiceBtn()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE);
            diceBtn.enabled = false;
        }

        private void ChangeDiceBtnSprite(int diceSpriteID)
        {
            diceImg.sprite = BattleUIManager.Instance.diceSprites[diceSpriteID];
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

        public void UpdatePlayerState(PlayerState player)
        {
            playerHealth.text = player._health.ToString();
            playerMaxHealth.text = player._maxHealth.ToString();
            playerAttack.text = player._attack.ToString();
            playerExperience.text = player._experience.ToString();
            playerLevel.text = player._level.ToString();
        }
    }
}
