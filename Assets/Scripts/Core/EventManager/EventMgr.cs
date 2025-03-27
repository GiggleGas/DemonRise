using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using ppCore.Common;
using ppCore.Manager;

namespace PDR
{
    /// 模块事件管理/派发
    /// 每个事件对应的Register/Dispatch/Unregister参数列表必须一致
    [Manager(ManagerPriority.Advance)]
    public sealed partial class EventMgr : Singleton<EventMgr>, IManager
    {
        private EventDispatcher m_eventDispatcher = new EventDispatcher();
#if __DEBUG__
        public static int MainThreadId = -1;
#endif
        public void OnAwake()
        {
#if __DEBUG__
            MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
#endif
            //m_listeners.Clear();
        }

        private void CheckMainThread()
        {
#if __DEBUG__
            if (MainThreadId != -1 && System.Threading.Thread.CurrentThread.ManagedThreadId != MainThreadId)
            {
                ILog.Error("EventMgr:CheckMainThread:Not in main thread");
                return;
            }
#endif
        }

        public void OnDestroy()
        {
            OnReset();
        }

        public void OnReset()
        {
            m_eventDispatcher.Reset();
        }

        ///
        /// 注册事件，0参数
        public void Register(EventType eventType, int subEventType, Action listener, DelGetPriority priDel = null)
        {
            CheckMainThread();
            m_eventDispatcher.Register(eventType, subEventType, listener, priDel);
        }

        ///
        /// 注册事件，1参数
        public void Register<T>(EventType eventType, int subEventType, Action<T> listener, DelGetPriority priDel = null)
        {
            CheckMainThread();
            m_eventDispatcher.Register<T>(eventType, subEventType, listener, priDel);
        }

        ///
        /// 注册事件，2参数
        public void Register<T1, T2>(EventType eventType, int subEventType, Action<T1, T2> listener, DelGetPriority priDel = null)
        {
            CheckMainThread();
            m_eventDispatcher.Register<T1, T2>(eventType, subEventType, listener, priDel);
        }

        ///
        /// 注册事件，3参数
        public void Register<T1, T2, T3>(EventType eventType, int subEventType, Action<T1, T2, T3> listener, DelGetPriority priDel = null)
        {
            CheckMainThread();
            m_eventDispatcher.Register<T1, T2, T3>(eventType, subEventType, listener, priDel);
        }

        ///
        /// 注册事件，4参数
        public void Register<T1, T2, T3, T4>(EventType eventType, int subEventType, Action<T1, T2, T3, T4> listener, DelGetPriority priDel = null)
        {
            CheckMainThread();
            m_eventDispatcher.Register<T1, T2, T3, T4>(eventType, subEventType, listener, priDel);
        }

        ///
        /// 反注册事件，0参数
        public void UnRegister(EventType eventType, int subEventType, Action listener)
        {
            CheckMainThread();
            m_eventDispatcher.UnRegister(eventType, subEventType, listener);
        }

        ///
        /// 反注册事件，1参数
        public void UnRegister<T>(EventType eventType, int subEventType, Action<T> listener)
        {
            CheckMainThread();
            m_eventDispatcher.UnRegister<T>(eventType, subEventType, listener);
        }

        ///
        /// 反注册事件，2参数
        public void UnRegister<T1, T2>(EventType eventType, int subEventType, Action<T1, T2> listener)
        {
            CheckMainThread();
            m_eventDispatcher.UnRegister<T1, T2>(eventType, subEventType, listener);
        }

        ///
        /// 反注册事件，3参数
        public void UnRegister<T1, T2, T3>(EventType eventType, int subEventType, Action<T1, T2, T3> listener)
        {
            CheckMainThread();
            m_eventDispatcher.UnRegister<T1, T2, T3>(eventType, subEventType, listener);
        }

        ///
        /// 反注册事件，4参数
        public void UnRegister<T1, T2, T3, T4>(EventType eventType, int subEventType, Action<T1, T2, T3, T4> listener)
        {
            CheckMainThread();
            m_eventDispatcher.UnRegister<T1, T2, T3, T4>(eventType, subEventType, listener);
        }

        ///
        /// 派发事件，0参数
        public void Dispatch(EventType eventType, int subEventType)
        {
            CheckMainThread();
            m_eventDispatcher.Dispatch(eventType, subEventType);
        }

        ///
        /// 派发事件，1参数
        public void Dispatch<T>(EventType eventType, int subEventType, T arg)
        {
            CheckMainThread();
            m_eventDispatcher.Dispatch<T>(eventType, subEventType, arg);
        }

        ///
        /// 派发事件，2参数
        public void Dispatch<T1, T2>(EventType eventType, int subEventType, T1 arg1, T2 arg2)
        {
            CheckMainThread();
            m_eventDispatcher.Dispatch<T1, T2>(eventType, subEventType, arg1, arg2);
        }

        ///
        /// 派发事件，3参数
        public void Dispatch<T1, T2, T3>(EventType eventType, int subEventType, T1 arg1, T2 arg2, T3 arg3)
        {
            CheckMainThread();
            m_eventDispatcher.Dispatch<T1, T2, T3>(eventType, subEventType, arg1, arg2, arg3);
        }

        ///
        /// 派发事件，4参数
        public void Dispatch<T1, T2, T3, T4>(EventType eventType, int subEventType, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            CheckMainThread();
            m_eventDispatcher.Dispatch<T1, T2, T3, T4>(eventType, subEventType, arg1, arg2, arg3, arg4);
        }

        public Dictionary<int, List<string>> GetCurrentRegisters()
        {
#if __DEBUG__
            return m_eventDispatcher.GetCurrentRegister();
#else
            return null;
#endif
        }
    }
}