using ppCore.Manager;
using Stateless;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace PDR
{
    [Manager(ManagerPriority.Delay)]
    public partial class BattleManager : ppCore.Common.Singleton<BattleManager>, IManager
    {
    }
}
