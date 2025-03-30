using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace PDR
{
    [Manager(ManagerPriority.Delay)]
    public class GamblingController : ppCore.Common.Singleton<GamblingController>, IManager
    {
        public void OnAwake()
        {
            EventMgr.Instance.Register<BlockInfo>(EventType.EVENT_BATTLE_UI, SubEventType.ENTER_GAMBLING, OnEnterGambling);
        }

        public void OnEnterGambling(BlockInfo blockInfo)
        {
            OpenGamblingView();

            InitPlayerData();
            InitEnemyData();
            InitUI();
        }

        private void OpenGamblingView()
        {
            ViewManager.Instance.Open(ViewType.GamblingView);
        }

        private void InitPlayerData()
        {

        }

        private void InitEnemyData()
        {

        }

        private void InitUI()
        {

        }
    }
}