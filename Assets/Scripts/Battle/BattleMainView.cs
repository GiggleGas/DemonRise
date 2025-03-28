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
        private Button diceBtn;
        private Image diceImg;
        private Button quitBtn;
        private TextMeshProUGUI energyText;

        private Sprite[] diceSprites; // 存储骰子图片的数组

        protected override void OnAwake()
        {
            base.OnAwake();
            diceBtn = transform.Find("diceBtn").GetComponent<Button>();
            quitBtn = transform.Find("quitBtn").GetComponent<Button>();
            diceImg = transform.Find("diceBtn").GetComponent<Image>();
            energyText = transform.Find("energy").GetComponent<TextMeshProUGUI>();


            diceBtn.onClick.AddListener(OnClickDiceBtn);
            quitBtn.onClick.AddListener(OnClickQuitBtn);

            diceSprites = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                diceSprites[i] = Resources.Load<Sprite>($"Pics/diceRed{i + 1}");
            }

            int randomIndex = UnityEngine.Random.Range(0, 6);
            diceImg.sprite = diceSprites[randomIndex];
            
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_SPRITE, ChangeDiceBtnSprite);
            EventMgr.Instance.Register<bool>(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_STATE, ChangeDiceBtnState);
            EventMgr.Instance.Register<int>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_ENERGY, UpdateEnergy);
        }

        private void OnClickDiceBtn()
        {
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE);
            diceBtn.enabled = false;
        }

        private void ChangeDiceBtnSprite(int diceSpriteID)
        {
            diceImg.sprite = diceSprites[diceSpriteID];
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
    }
}
