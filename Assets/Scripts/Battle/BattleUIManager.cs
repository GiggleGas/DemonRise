using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;


namespace PDR
{
    public enum DiceStateChange
    {
        Restart,
        Change,
        GetResult
    }

    [Manager(ManagerPriority.Delay)]
    public class BattleUIManager : ppCore.Common.Singleton<BattleUIManager>, IManager
    {
        private Sprite[] diceSprites; // 存储骰子图片的数组

        private BattleMainView mainView;

        public void OnAwake()
        {
            ViewManager.Instance.Register(ViewType.BattleMainView, new ViewInfo()
            {
                viewName = "BattleMainView",
                parentTransform = ViewManager.Instance.canvasTransform,
                order = 0,
            });

            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.OPEN_BATTLE_MAIN_VIEW, OpenBattleMainView);
            EventMgr.Instance.Register(EventType.EVENT_BATTLE_UI, SubEventType.ROLL_THE_DICE, BeginRolling);
            EventMgr.Instance.Register<BattleStage>(EventType.EVENT_BATTLE, SubEventType.UPDATE_GAME_STAGE, OnGameStageChange);
        }

        public void OnUpdate()
        {
            if(bIsRolling)
            {
                RollTheDice();
                return;
            }
        }

        private void OpenBattleMainView()
        {
            ViewManager.Instance.Open(ViewType.BattleMainView);
            mainView = ViewManager.Instance.GetView<BattleMainView>(ViewType.BattleMainView);
        }

        private void OnGameStageChange(BattleStage newBattleStage)
        {
            if(newBattleStage == BattleStage.WaitingForRolling)
            {
                UpdateDiceState(DiceStateChange.Restart);
            }
            else if(newBattleStage == BattleStage.WaitingForAction)
            {
                UpdateEnergy();
            }
        }

        // DICE
        public float rollDuration = 0.5f; // 骰子滚动的持续时间
        public float rollInterval = 0.02f; // 骰子图片切换的时间间隔
        public float rollEnd = 0.0f; // 骰子最终时间
        public float lastRollTime = 0.0f; // 上次切换时间
        private bool bIsRolling = false;

        private void UpdateDiceState(DiceStateChange diceStateChange)
        {
            if(diceStateChange == DiceStateChange.GetResult || diceStateChange == DiceStateChange.Restart)
            {
                rollEnd = 0.0f;
                lastRollTime = 0.0f;
                bIsRolling = false;
            }
            EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.CHANGE_DICE_PIC, diceStateChange);
        }

        private void RollTheDice()
        {
            if (Time.time > rollEnd)
            {
                UpdateDiceState(DiceStateChange.GetResult);
            }
            else if (Time.time - lastRollTime > rollInterval)
            {
                UpdateDiceState(DiceStateChange.Change);
                lastRollTime = Time.time;
            }
        }

        private void BeginRolling()
        {
            if(bIsRolling)
            {
                return;
            }
            bIsRolling = true;
            rollEnd = Time.time + rollDuration;
            lastRollTime = Time.time;
        }

        /// Energy
        private void UpdateEnergy()
        {
            mainView.UpdateEnergy(BattleManager.Instance.currentEnergy);
        }
    }
}
