using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PDR
{
    public struct DiceStruct
    {
        public Button diceBtn;
        public Image diceImg;
    }

    public struct SkillIconView
    {
        public Button skillBtn;
        public Image skillImg;
        public TextMeshProUGUI value;
        public TextMeshProUGUI cost;
    }

    public class PlayerView
    {
        public Slider hpSlider;
        public TextMeshProUGUI hpText;
        public List<SkillIconView> skillIcons;
        public List<GameObject> selectedIcons;
    }

    public class GamblingView : BaseView
    {
        private PlayerView playerView;
        private List<DiceStruct> dices;
        private PlayerView enemyView;
        private TextMeshProUGUI _energy;

        protected override void OnAwake()
        {
            _energy = GameObject.Find("Energy").GetComponent<TextMeshProUGUI>();
            _energy.text = "CurrentEnergy:" + 0;

            InitDice(2);
            InitPlayerGo("GamblingPlayer", out playerView);
            InitPlayerGo("GamblingEnemy", out enemyView);
            EventMgr.Instance.Register<PlayerPawn>(EventType.EVENT_BATTLE_UI, SubEventType.UPDATE_GAMBLING_PLAYER_VIEW, UpdatePlayerGo);
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.GAMBLING_VIEW_FINISH_LOAD);
        }

        protected void InitDice(int num)
        {
            // 未来根据角色情况初始化
            dices = new List<DiceStruct>();
            dices.Add(new DiceStruct
            {
                diceBtn = GameObject.Find("diceBtn0").GetComponent<Button>(),
                diceImg = GameObject.Find("diceBtn0").GetComponent<Image>()
            });
            dices.Add(new DiceStruct
            {
                diceBtn = GameObject.Find("diceBtn1").GetComponent<Button>(),
                diceImg = GameObject.Find("diceBtn1").GetComponent<Image>()
            });

        }

        #region player
        public void InitPlayerGo(string rootName, out PlayerView inPlayerView)
        {
            inPlayerView = new PlayerView();
            // init hp bar
            inPlayerView.hpSlider = GameObject.Find($"{rootName}/playerImage/HPBar").GetComponent<Slider>();
            inPlayerView.hpText = GameObject.Find($"{rootName}/playerImage/HPBar/hpText").GetComponent<TextMeshProUGUI>();

            // init skill icons
            inPlayerView.skillIcons = new List<SkillIconView>();
            for (int i = 1; i <= 5; i++) 
            {
                SkillIconView skillIconView = new SkillIconView();
                skillIconView.skillBtn = GameObject.Find($"{rootName}/skillIcons/t_skillIcon{i}").GetComponent<Button>();
                skillIconView.skillImg = GameObject.Find($"{rootName}/skillIcons/t_skillIcon{i}/Icon").GetComponent<Image>();
                skillIconView.value = GameObject.Find($"{rootName}/skillIcons/t_skillIcon{i}/value").GetComponent<TextMeshProUGUI>();
                skillIconView.cost = GameObject.Find($"{rootName}/skillIcons/t_skillIcon{i}/cost").GetComponent<TextMeshProUGUI>();
                inPlayerView.skillIcons.Add(skillIconView);
            }
            foreach (SkillIconView skillIcon in inPlayerView.skillIcons)
            {
                skillIcon.skillBtn.enabled = false;
                skillIcon.skillImg.enabled = false;
                skillIcon.value.enabled = false;
                skillIcon.cost.enabled = false;
            }

            inPlayerView.selectedIcons = new List<GameObject>(); ;
            for (int i = 0; i <= 11; i++)
            {
                inPlayerView.selectedIcons.Add(GameObject.Find($"{rootName}/selectArray/selectedIcon{i}").gameObject);
            }
            foreach(GameObject obj in inPlayerView.selectedIcons)
            {
                obj.SetActive(false);
            }
        }

        public void UpdatePlayerGo(PlayerPawn playerState)
        {
            playerView.hpSlider.value = playerState._health / playerState._maxHealth;
            playerView.hpText.text = playerState._health.ToString() + "/" + playerState._maxHealth.ToString();
        }
        #endregion
    }
}
