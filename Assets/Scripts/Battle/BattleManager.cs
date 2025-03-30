using ppCore.Manager;
using Stateless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using TMPro.EditorUtilities;
using Unity.VisualScripting;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace PDR
{
    [Manager(ManagerPriority.Delay)]
    public class BattleManager : ppCore.Common.Singleton<BattleManager>, IManager
    {
        // private BattleStage battleStage;

        public int currentEnergy = 0;

        public void OnAwake()
        {
        }

        public void OnStart()
        {
        }


        /*protected void OnGetDiceResult(int DiceResult)
        {
            currentEnergy = DiceResult;
            UpdateBattleStage(BattleStage.WaitingForAction);
            ///
            UpdateBattleStage(BattleStage.WaitingForRolling);
        }

        public void UpdateBattleStage(BattleStage newBattleStage)
        {
            //battleStage = newBattleStage;
            //EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE, SubEventType.UPDATE_GAME_STAGE, newBattleStage);
        }*/
    }
}
