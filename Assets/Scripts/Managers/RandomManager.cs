using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    [Manager(ManagerPriority.Delay)]
    public class RandomManager : ppCore.Common.Singleton<RandomManager>, IManager
    {
        private int _gameSeed = 0;
        public RandomManager() 
        {
            
        }
    }
}
