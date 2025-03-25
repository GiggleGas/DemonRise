/************************************************************************
Author：veleonli
Date：2019/03/14 19:27:48
Refer:

Description:Job 上下文，封装重要数据，在job执行的时候，可以通过上下文获取所需要的数据
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESJobContext
    {
        public static ESJobContext New()
        {
            ESJobContext val = ESPool.Alloc<ESJobContext>();
            return val;
        }

        public void Free()
        {
            JobType = null;
            JobGUID = System.Guid.Empty;
            JobDetail = null;
            Trigger = null;
            ESPool.Free<ESJobContext>(this);
        }

        private System.Type m_jobType;
        public System.Type JobType
        {
            get
            {
                return m_jobType;
            }
            set
            {
                m_jobType = value;
                if (m_jobType == null)
                {
                    JobGUID = System.Guid.Empty;
                }
            }
        }
        public System.Guid JobGUID { get; set; }
        public ESJobDetail JobDetail { get; set; }

        public ESTrigger Trigger { get; set; }


    }
}
