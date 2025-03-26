
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ppCore.Manager
{
    public interface IManager
    {
        void OnAwake() { }
        void OnStart() { }
        void OnUpdate() { }
        void OnLateUpdate() { }
        void OnFixedUpdate() { }
        void Destroy() { }
    }

    public class ManagerSortData
    {
        public ManagerSortData(IManager manager, int priority)
        {
            Manager = manager; ;
            Priority = priority;
        }
        public IManager Manager;
        public int Priority;
    }
    public class ManagerListSort : IComparer<ManagerSortData>
    {
        public int Compare(ManagerSortData x, ManagerSortData y)
        {
            return PriorityCompare(x, y);
        }
        static int PriorityCompare(ManagerSortData x, ManagerSortData y)
        {
            int xPriority = (int)x.Priority;
            int yPriority = (int)y.Priority;

            if (xPriority > yPriority)
            {
                return 1;
            }
            else if (xPriority == yPriority)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }
}