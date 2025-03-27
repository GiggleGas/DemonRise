using ppCore.Common;
using ppCore.Manager;
using System;
using System.Collections.Generic;

namespace PDR
{
    /// 模块事件管理/派发
    /// 每个事件对应的Register/Dispatch/Unregister参数列表必须一致
    public sealed partial class EventDispatcher
    {
        private Dictionary<int, EventBase> m_listeners = new Dictionary<int, EventBase>();

        public void Reset()
        {
            m_listeners.Clear();
        }

        private int GetRealEventID(EventType eventType, int subEventType)
        {
            int realEventID = (int)eventType * 100000 + subEventType;
            return realEventID;
        }

        ///
        /// 注册事件，0参数
        public void Register(EventType eventType, int subEventType, Action listener, DelGetPriority priDel = null)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugRegister(realEventID, listener.Method.Name);
#endif

            EventBase result = null;
            EEvent ev = null;
            if (!m_listeners.TryGetValue(realEventID, out result))
            {
                ev = new EEvent();
                ev.SetCheckDuplicate(true);
                m_listeners.Add(realEventID, ev);
            }
            else
            {
                ev = result as EEvent;
            }

            if (null != ev)
            {
                ev.AddEvent(listener, priDel);
            }
            else
            {
                // ILog.Error("[EventDispatcher.Register] invalid listener type.");
            }
        }

        ///
        /// 注册事件，1参数
        public void Register<T>(EventType eventType, int subEventType, Action<T> listener, DelGetPriority priDel = null)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugRegister(realEventID, listener.Method.Name);
#endif
            EventBase result = null;
            EEvent<T> ev = null;
            if (!m_listeners.TryGetValue(realEventID, out result))
            {
                ev = new EEvent<T>();
                ev.SetCheckDuplicate(true);
                m_listeners.Add(realEventID, ev);
            }
            else
            {
                ev = result as EEvent<T>;
            }

            if (null != ev)
            {
                ev.AddEvent(listener, priDel);
            }
            else
            {
                // ILog.Error("[EventDispatcher.Register] invalid listener type : " + (int)eventType + " subEventType : " + subEventType);
            }
        }

        ///
        /// 注册事件，2参数
        public void Register<T1, T2>(EventType eventType, int subEventType, Action<T1, T2> listener, DelGetPriority priDel = null)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugRegister(realEventID, listener.Method.Name);
#endif

            EventBase result = null;
            EEvent<T1, T2> ev = null;
            if (!m_listeners.TryGetValue(realEventID, out result))
            {
                ev = new EEvent<T1, T2>();
                ev.SetCheckDuplicate(true);
                m_listeners.Add(realEventID, ev);
            }
            else
            {
                ev = result as EEvent<T1, T2>;
            }

            if (null != ev)
            {
                ev.AddEvent(listener, priDel);
            }
            else
            {
                // ILog.Error("[EventDispatcher.Register] invalid listener type.");
            }
        }

        ///
        /// 注册事件，3参数
        public void Register<T1, T2, T3>(EventType eventType, int subEventType, Action<T1, T2, T3> listener, DelGetPriority priDel = null)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugRegister(realEventID, listener.Method.Name);
#endif

            EventBase result = null;
            EEvent<T1, T2, T3> ev = null;
            if (!m_listeners.TryGetValue(realEventID, out result))
            {
                ev = new EEvent<T1, T2, T3>();
                ev.SetCheckDuplicate(true);
                m_listeners.Add(realEventID, ev);
            }
            else
            {
                ev = result as EEvent<T1, T2, T3>;
            }

            if (null != ev)
            {
                ev.AddEvent(listener, priDel);
            }
            else
            {
                // ILog.Error("[EventDispatcher.Register] invalid listener type.");
            }
        }

        ///
        /// 注册事件，4参数
        public void Register<T1, T2, T3, T4>(EventType eventType, int subEventType, Action<T1, T2, T3, T4> listener, DelGetPriority priDel = null)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugRegister(realEventID, listener.Method.Name);
#endif

            EventBase result = null;
            EEvent<T1, T2, T3, T4> ev = null;
            if (!m_listeners.TryGetValue(realEventID, out result))
            {
                ev = new EEvent<T1, T2, T3, T4>();
                ev.SetCheckDuplicate(true);
                m_listeners.Add(realEventID, ev);
            }
            else
            {
                ev = result as EEvent<T1, T2, T3, T4>;
            }

            if (null != ev)
            {
                ev.AddEvent(listener, priDel);
            }
            else
            {
                // ILog.Error("[EventDispatcher.Register] invalid listener type.");
            }
        }

        ///
        /// 反注册事件，0参数
        public void UnRegister(EventType eventType, int subEventType, Action listener)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugUnRegister(realEventID, listener.Method.Name);
#endif

            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent;
                if (null != action)
                {
                    action.RemoveEvent(listener);
                    if (action.listener == 0)
                    {
                        action.Release();
                        m_listeners.Remove(realEventID);
                    }
                }
                else
                {
                    // ILog.Error("[EventDispatcher.UnRegister] invalid listener type.");
                }
            }
        }

        ///
        /// 反注册事件，1参数
        public void UnRegister<T>(EventType eventType, int subEventType, Action<T> listener)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugUnRegister(realEventID, listener.Method.Name);
#endif

            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T>;
                if (null != action)
                {
                    action.RemoveEvent(listener);
                    if (action.listener == 0)
                    {
                        action.Release();
                        m_listeners.Remove(realEventID);
                    }
                }
                else
                {
                    // ILog.Error("[EventDispatcher.UnRegister] invalid listener type.");
                }
            }
        }

        ///
        /// 反注册事件，2参数
        public void UnRegister<T1, T2>(EventType eventType, int subEventType, Action<T1, T2> listener)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugUnRegister(realEventID, listener.Method.Name);
#endif

            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T1, T2>;
                if (null != action)
                {
                    action.RemoveEvent(listener);
                    if (action.listener == 0)
                    {
                        action.Release();
                        m_listeners.Remove(realEventID);
                    }
                }
                else
                {
                    // ILog.Error("[EventDispatcher.UnRegister] invalid listener type.");
                }
            }
        }

        ///
        /// 反注册事件，3参数
        public void UnRegister<T1, T2, T3>(EventType eventType, int subEventType, Action<T1, T2, T3> listener)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugUnRegister(realEventID, listener.Method.Name);
#endif

            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T1, T2, T3>;
                if (null != action)
                {
                    action.RemoveEvent(listener);
                    if (action.listener == 0)
                    {
                        action.Release();
                        m_listeners.Remove(realEventID);
                    }
                }
                else
                {
                    // ILog.Error("[EventDispatcher.UnRegister] invalid listener type.");
                }
            }
        }

        ///
        /// 反注册事件，4参数
        public void UnRegister<T1, T2, T3, T4>(EventType eventType, int subEventType, Action<T1, T2, T3, T4> listener)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
#if __DEBUG__
            DebugUnRegister(realEventID, listener.Method.Name);
#endif
            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T1, T2, T3, T4>;
                if (null != action)
                {
                    action.RemoveEvent(listener);
                    if (action.listener == 0)
                    {
                        m_listeners.Remove(realEventID);
                    }
                }
                else
                {
                    // ILog.Error("[EventDispatcher.UnRegister] invalid listener type.");
                }
            }
        }

        ///
        /// 派发事件，0参数
        public void Dispatch(EventType eventType, int subEventType)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent;
                if (null != action)
                {
                    action.Invoke();
                }
                else
                {
                    // ILog.Error("[EventDispatcher.Dispatch] invalid arg Type.");
                }
            }
        }

        ///
        /// 派发事件，1参数
        public void Dispatch<T>(EventType eventType, int subEventType, T arg)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T>;
                if (null != action)
                {
                    action.Invoke(arg);
                }
                else
                {
                    // ILog.Error("[EventDispatcher.Dispatch] invalid arg Type.");
                }
            }
        }

        ///
        /// 派发事件，2参数
        public void Dispatch<T1, T2>(EventType eventType, int subEventType, T1 arg1, T2 arg2)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T1, T2>;
                if (null != action)
                {
                    action.Invoke(arg1, arg2);
                }
                else
                {
                    // ILog.Error("[EventDispatcher.Dispatch<T1, T2>] invalid arg Type.");
                }
            }
        }

        ///
        /// 派发事件，3参数
        public void Dispatch<T1, T2, T3>(EventType eventType, int subEventType, T1 arg1, T2 arg2, T3 arg3)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T1, T2, T3>;
                if (null != action)
                {
                    action.Invoke(arg1, arg2, arg3);
                }
                else
                {
                    // ILog.Error("[EventDispatcher.Dispatch<T1, T2, T3>] invalid arg Type.");
                }
            }
        }

        ///
        /// 派发事件，4参数
        public void Dispatch<T1, T2, T3, T4>(EventType eventType, int subEventType, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            int realEventID = GetRealEventID(eventType, subEventType);
            EventBase result;
            if (m_listeners.TryGetValue(realEventID, out result))
            {
                var action = result as EEvent<T1, T2, T3, T4>;
                if (null != action)
                {
                    action.Invoke(arg1, arg2, arg3, arg4);
                }
                else
                {
                    // ILog.Error("[EventDispatcher.Dispatch<T1, T2, T3, T4>] invalid arg Type.");
                }
            }
        }


#if __DEBUG__
        private Dictionary<int, List<string>> debugs = new Dictionary<int, List<string>>();


        public Dictionary<int, List<string>> GetCurrentRegister()
        {
            return debugs; 
        }
        private void DebugRegister(int realEventID,string methodName)
        {
            if (!debugs.ContainsKey(realEventID))
            {
                debugs.Add(realEventID, new List<string>());
            }
            debugs[realEventID].Add(methodName);
        }

        private void DebugUnRegister(int realEventID, string methodName)
        {
            if (!debugs.ContainsKey(realEventID))
            {
                return;
            }
            debugs[realEventID].Remove(methodName);
        }

        public void PrintDebugInfoAndClear(SceneLogic from)
        {
            string sceneName = "";
            if (null!=from)
            {
                sceneName = from.ToString();
            }
            var enumerator3 = debugs.GetEnumerator();
            while (enumerator3.MoveNext())
            {
                var kv = enumerator3.Current;
                int realEventID = kv.Key;
                EventType eventType = (EventType)(realEventID / 10000);
                int subEventType = realEventID % 10000;

                List<string> methods = kv.Value;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    LOG.LogWarning("[EventDispatcher.PrintDebugInfoAndClear] 场景", sceneName, "退出 事件 ", eventType, ", subEventType:", subEventType, " 未反注册:", kv.Value[i]);
                }
            }

            debugs.Clear();
        }
#endif
    }
}