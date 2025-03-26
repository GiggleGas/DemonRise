using ppCore.Common;
// using ppCore.TinyLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace ppCore.Manager
{
    public class ManagerLauncher : Singleton<ManagerLauncher>
    {
        private List<ManagerSortData> m_managerList = new List<ManagerSortData>();

        private ManagerListSort m_managerListSort = new ManagerListSort();
        private bool m_managerInited = false;

        public void Init()
        {
            var types = AssemblyManager.Instance.GetTypes();
            foreach (var t in types)
            {
                var atts = t.GetCustomAttributes(typeof(ManagerAttribute), false);
                if (atts != null && atts.Length > 0)
                {
                    ManagerAttribute ma = (ManagerAttribute)atts[0];
                    var prop = t.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    if (prop != null)
                    {
                        var inst = prop.GetValue(null, null);
                        if (inst != null)
                        {
                            IManager manager = (IManager)inst;
                            if (manager != null)
                                m_managerList.Add(new ManagerSortData(manager, ma.Priority));
                        }
                    }


                }
            }

            m_managerList.Sort(m_managerListSort);

            foreach (var manager in m_managerList)
            {
                if (manager != null)
                    manager.Manager.OnAwake();
            }
            m_managerInited = true;
        }

        public void Start()
        {
            foreach (var manager in m_managerList)
            {
                if (manager != null)
                    manager.Manager.OnStart();
            }
        }
        public void Update()
        {
            foreach (var manager in m_managerList)
            {
                if (manager != null)
                    manager.Manager.OnUpdate();
            }
        }
        public void LateUpdate()
        {
            foreach (var manager in m_managerList)
            {
                if (manager != null)
                    manager.Manager.OnLateUpdate();
            }
        }
        public void FixedUpdate()
        {
            foreach (var manager in m_managerList)
            {
                if (manager != null)
                    manager.Manager.OnFixedUpdate();
            }
        }
        public void Destroy()
        {
            foreach (var manager in m_managerList)
            {
                if (manager != null)
                    manager.Manager.Destroy();
            }
        }
    }
}