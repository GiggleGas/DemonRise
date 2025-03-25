/************************************************************************
Author：veleonli
Date：2019/03/14 19:27:03
Refer:

Description:Job工作细节，封装外部传递的数据
************************************************************************/
using ppCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESJobDetail
    {
        public static string defaultGroup = "default";
        public static ESJobDetail New()
        {
            ESJobDetail val = ESPool.Alloc<ESJobDetail>();
            val.m_identity = ++sCurIdentity;
            if (val.m_identity < 0)
                val.m_identity = 1;
            return val;
        }

        public void Free()
        {
            JobGroup = null;
            m_jobContext.Free();
            m_jobContext = null;
            CallBackAction = null;
            ExtraData = null;
            ESPool.Free<ESJobDetail>(this);
        }
        private int m_group = 0;
        public int Group
        {
            get
            {
                return m_group;
            }
            set
            {
                m_group = value;
            }
        }

        private static long sCurIdentity = 0;
        private long m_identity = 0;
        public long Identity
        {
            get
            {
                return m_identity;
            }
        }

        private ESJobContext m_jobContext;


        public ESJobGroup JobGroup { get; set; }
        public ESJobContext JobContext
        {
            get
            {
                if (m_jobContext == null)
                {
                    m_jobContext = ESJobContext.New();
                    m_jobContext.JobDetail = this;
                }
                return m_jobContext;
            }
        }
        public Action CallBackAction { get; set; }
        public Action<long> CallBackActionEx { get; set; }
        public System.Object ExtraData { get; set; }
    }
}
