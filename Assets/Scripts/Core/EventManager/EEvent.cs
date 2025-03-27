// using ppCore.TinyLog;
using System;
using System.Collections.Generic;

namespace PDR
{
    public delegate int DelGetPriority();
    public class EventBase
    {
        public class DelegateWithPriority : IComparable<DelegateWithPriority>
        {
            public Delegate del;
            public DelGetPriority priDel;
            public int fixedPriority; //TODO if needed.

            public DelegateWithPriority(Delegate del, DelGetPriority priDel)
            {
                this.del = del;
                this.priDel = priDel;
            }

            public int CompareTo(DelegateWithPriority other)
            {
                if (priDel == null && other.priDel == null)
                {
                    return 0;
                }

                if (priDel == null)
                {
                    return 1;
                }

                if (other.priDel == null)
                {
                    return -1;
                }

                return priDel() - other.priDel();
            }
        }

        protected EList<DelegateWithPriority> m_dels;
        //protected EList<Delegate> m_dels;
        protected bool m_needClear = false;
        protected bool m_checkDuplicate = false;
        protected int callDepth = 0;
        public EventBase(int capacity)
        {
            //m_dels = new EList<Delegate>();
            m_dels = new EList<DelegateWithPriority>();
            //EventMgr.Instance.RegisterUpdate(this);
        }

        public void Release()
        {
            //EventMgr.Instance.UnregisterUpdate(this);
        }

        public void SetCheckDuplicate(bool check)
        {
            m_checkDuplicate = check;
        }

        protected void Add(Delegate del, DelGetPriority priDel = null)
        {
            if (m_checkDuplicate)
            {
                var ret = m_dels.Find(delegate (DelegateWithPriority dwp)
                {
                    return (dwp != null && dwp.del == del);
                });

                if (null != ret && null != del.Target && null != del.Method)
                {
                    // ILog.Fatal("event delegate duplicate:   target: ", del.Target.ToString(), " method: ", del.Method.ToString());

                    return;
                }
            }

            m_dels.Add(new DelegateWithPriority(del, priDel));
        }

        protected void Remove(Delegate del)
        {
            int index = m_dels.FindIndex(delegate (DelegateWithPriority dwp)
            {
                return (dwp != null && dwp.del == del);
            });
            if (index >= 0)
            {
                m_dels[index] = null;
                m_needClear = true;
            }
        }

        protected void ClearNullEvent()
        {
            if (!m_needClear)
            {
                return;
            }

            if (0 != callDepth)
            {
                return;
            }

            m_needClear = false;
            var c = m_dels.Count;
            for (int i = c - 1; i >= 0; i--)
            {
                if (m_dels[i] == null)
                    m_dels.RemoveFastAt(i);
            }
        }

        public void ClearEvent()
        {
            m_dels.Clear();
        }

        public T Clone<T>()
            where T : EventBase, new()
        {
            var t = new T();
            for (int i = 0; i < m_dels.Count; i++)
            {
                t.m_dels.Add(m_dels[i]);
            }

            return t;
        }

        public int listener
        {
            get
            {
                return m_dels.Count;
            }
        }
    }

    public sealed class EEvent : EventBase
    {
        public EEvent(int capacity = 4) : base(capacity)
        {
        }

        public void AddEvent(Action action, DelGetPriority priDel = null)
        {
            Add(action, priDel);
        }

        public void RemoveEvent(Action action)
        {
            Remove(action);
        }

        public void Invoke()
        {
            callDepth++;
            m_dels.Sort();
            var c = m_dels.Count;
            for (int i = 0; i < c; i++)
            {
                if (c > m_dels.Count)
                {
                    // ILog.Error("EEvent Invoke! c:", c, " count:", m_dels.Count);
                }

                if (m_dels[i] == null || m_dels[i].del == null)
                    continue;
                var a = m_dels[i].del as Action;
                try
                {
                    if (a != null)
                        a();
                }
                catch (Exception e)
                {
                    // ILog.Error("EEvent Invoke exception:", e);
                }
            }

            callDepth--;
            ClearNullEvent();
        }

        public static EEvent operator +(EEvent c1, Action c2)
        {
            c1.Add(c2);
            return c1;
        }

        public static EEvent operator -(EEvent c1, Action c2)
        {
            c1.Remove(c2);
            return c1;
        }
    }

    public sealed class EEvent<T> : EventBase
    {
        public EEvent() : base(4)
        {
        }

        public EEvent(int capacity = 4) : base(capacity)
        {
        }

        public void AddEvent(Action<T> action, DelGetPriority priDel = null)
        {
            Add(action, priDel);
        }

        public void RemoveEvent(Action<T> action)
        {
            Remove(action);
        }

        public void Invoke(T t)
        {
            callDepth++;
            m_dels.Sort();
            var c = m_dels.Count;
            for (int i = 0; i < c; i++)
            {
                if (c > m_dels.Count)
                {
                    // ILog.Error("EEvent<T> Invoke! c:", c, " count:", m_dels.Count);
                }

                if (m_dels[i] == null || m_dels[i].del == null)
                    continue;
                var a = m_dels[i].del as Action<T>;
                try
                {
                    if (a != null)
                        a(t);
                }
                catch (Exception e)
                {
                    // ILog.Error("EEvent Invoke exception:", e);
                }
            }

            callDepth--;
            ClearNullEvent();
        }

        public static EEvent<T> operator +(EEvent<T> c1, Action<T> c2)
        {
            c1.Add(c2);
            return c1;
        }

        public static EEvent<T> operator -(EEvent<T> c1, Action<T> c2)
        {
            c1.Remove(c2);
            return c1;
        }
    }

    public sealed class EEvent<T1, T2> : EventBase
    {
        public EEvent(int capacity = 4) : base(capacity)
        {
        }

        public void AddEvent(Action<T1, T2> action, DelGetPriority priDel = null)
        {
            Add(action, priDel);
        }

        public void RemoveEvent(Action<T1, T2> action)
        {
            Remove(action);
        }

        public void Invoke(T1 t1, T2 t2)
        {
            callDepth++;
            m_dels.Sort();
            var c = m_dels.Count;
            for (int i = 0; i < c; i++)
            {
                if (c > m_dels.Count)
                {
                    // ILog.Error("EEvent<T1, T2> Invoke! c:", c, " count:", m_dels.Count);
                }

                if (m_dels[i] == null || m_dels[i].del == null)
                    continue;
                var a = m_dels[i].del as Action<T1, T2>;
                try
                {
                    if (a != null)
                        a(t1, t2);
                }
                catch (Exception e)
                {
                    // ILog.Error("EEvent Invoke exception:", e);
                }
            }

            callDepth--;
            ClearNullEvent();
        }

        public static EEvent<T1, T2> operator +(EEvent<T1, T2> c1, Action<T1, T2> c2)
        {
            c1.Add(c2);
            return c1;
        }

        public static EEvent<T1, T2> operator -(EEvent<T1, T2> c1, Action<T1, T2> c2)
        {
            c1.Remove(c2);
            return c1;
        }
    }

    public sealed class EEvent<T1, T2, T3> : EventBase
    {
        public EEvent(int capacity = 4) : base(capacity)
        {
        }

        public void AddEvent(Action<T1, T2, T3> action, DelGetPriority priDel = null)
        {
            Add(action, priDel);
        }

        public void RemoveEvent(Action<T1, T2, T3> action)
        {
            Remove(action);
        }

        public void Invoke(T1 t1, T2 t2, T3 t3)
        {
            callDepth++;
            m_dels.Sort();
            var c = m_dels.Count;
            for (int i = 0; i < c; i++)
            {
                if (c > m_dels.Count)
                {
                    // ILog.Error("EEvent<T1, T2, T3> Invoke! c:", c, " count:", m_dels.Count);
                }

                if (m_dels[i] == null || m_dels[i].del == null)
                    continue;
                var a = m_dels[i].del as Action<T1, T2, T3>;
                try
                {
                    if (a != null)
                        a(t1, t2, t3);
                }
                catch (Exception e)
                {
                    // ILog.Error("EEvent Invoke exception:", e);
                }
            }

            callDepth--;
            ClearNullEvent();
        }

        public static EEvent<T1, T2, T3> operator +(EEvent<T1, T2, T3> c1, Action<T1, T2, T3> c2)
        {
            c1.Add(c2);
            return c1;
        }

        public static EEvent<T1, T2, T3> operator -(EEvent<T1, T2, T3> c1, Action<T1, T2, T3> c2)
        {
            c1.Remove(c2);
            return c1;
        }
    }

    public sealed class EEvent<T1, T2, T3, T4> : EventBase
    {
        public EEvent(int capacity = 4) : base(capacity)
        {
        }

        public void AddEvent(Action<T1, T2, T3, T4> action, DelGetPriority priDel = null)
        {
            Add(action, priDel);
        }

        public void RemoveEvent(Action<T1, T2, T3, T4> action)
        {
            Remove(action);
        }

        public void Invoke(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            callDepth++;
            m_dels.Sort();
            var c = m_dels.Count;
            for (int i = 0; i < c; i++)
            {
                if (c > m_dels.Count)
                {
                    // ILog.Error("EEvent<T1, T2, T3, T4> Invoke! c:", c, " count:", m_dels.Count);
                }

                if (m_dels[i] == null || m_dels[i].del == null)
                    continue;
                var a = m_dels[i].del as Action<T1, T2, T3, T4>;
                try
                {
                    if (a != null)
                        a(t1, t2, t3, t4);
                }
                catch (Exception e)
                {
                    // ILog.Error("EEvent Invoke exception:", e);
                }
            }

            callDepth--;
            ClearNullEvent();
        }

        public static EEvent<T1, T2, T3, T4> operator +(EEvent<T1, T2, T3, T4> c1, Action<T1, T2, T3, T4> c2)
        {
            c1.Add(c2);
            return c1;
        }

        public static EEvent<T1, T2, T3, T4> operator -(EEvent<T1, T2, T3, T4> c1, Action<T1, T2, T3, T4> c2)
        {
            c1.Remove(c2);
            return c1;
        }
    }
}