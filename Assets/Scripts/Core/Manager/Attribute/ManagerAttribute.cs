using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace ppCore.Manager
{
    public enum ManagerPriority
    {
        Advance = -1,
        Default = 0,
        Delay = 1,
        AfterDelay = 2,
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagerAttribute : Attribute
    {
        public int Priority { get; set; }
        public ManagerAttribute(int priority = 0)
        {
            Priority = priority;
        }

        public ManagerAttribute(ManagerPriority priority)
        {
            Priority = (int)priority;
        }
    }
}