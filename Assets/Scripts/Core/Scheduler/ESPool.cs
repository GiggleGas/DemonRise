using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESPool
    {
        private static ESPool m_pool;

        private Dictionary<System.Type, List<System.Object>> m_caches = new Dictionary<System.Type, List<System.Object>>();

        private static ESPool Pool
        {
            get
            {
                if (m_pool == null)
                {
                    m_pool = new ESPool();
                }
                return m_pool;
            }
        }

        private ESPool()
        {

        }

        public static T Alloc<T>()
            where T : class, new()
        {
            List<System.Object> rel;
            if (!Pool.m_caches.TryGetValue(typeof(T),out rel))
            {
                rel = new List<System.Object>();
                Pool.m_caches[typeof(T)] = rel;
            }
            if (rel.Count == 0)
            {
                return new T();
            }
            System.Object obj = rel[rel.Count - 1];
            rel.RemoveAt(rel.Count - 1);
            return obj as T;
        }

        public static void Free<T>(T val)
        {
            List<System.Object> rel;
            if (!Pool.m_caches.TryGetValue(typeof(T), out rel))
            {
                rel = new List<System.Object>();
                Pool.m_caches[typeof(T)] = rel;
            }
            rel.Add(val);
        }
    }
}
