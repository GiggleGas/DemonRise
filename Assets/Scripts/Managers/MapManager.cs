using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ͼ������
/// </summary>

namespace PDR
{
    [Manager(ManagerPriority.Delay)]
    public class MapManager : ppCore.Common.Singleton<MapManager>, IManager
    {
        public void OnAwake()
        {
            
        }
    }
}
