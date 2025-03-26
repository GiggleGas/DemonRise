using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    [Manager(ManagerPriority.Delay)]
    public class BattleManager : ppCore.Common.Singleton<BattleManager>, IManager
    {
        public void OnAwake()
        {

        }

        public void OnUpdate()
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {

            }
        }
    }
}
