/************************************************************************
Author：veleonli
Date：2019/03/14 17:09:03
Refer:

Description:任务调度上下文，作为函数调用的参数，提供其他类需要访问的接口。
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESchedulerContext
    {
        private ESTimerAjuster m_timerAjuster;
        private Dictionary<System.Guid, ESJob> m_registerJobs = new Dictionary<System.Guid, ESJob>();

        public EScheduler Scheduler { get; set; }
        public ESTimerAjuster TimerAjuster
        {
            get
            {
                return m_timerAjuster;
            }
            set
            {
                m_timerAjuster = value;
            }
        }


        public ESJob GetRegistryJob(System.Type type, System.Guid guid)
        {
            ESJob job;
            if (m_registerJobs.TryGetValue(guid, out job))
            {
                return job;
            }
            job = Activator.CreateInstance(type) as ESJob;
            m_registerJobs[guid] = job;
            return job;
        }

    }
}
