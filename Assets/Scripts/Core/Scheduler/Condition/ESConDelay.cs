using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConDelay : ESCondition
    {
        private long m_delay = 0;

        private long m_cacheTimeStamp = 0;

        private long m_cacheFrame = 0;
        

        public static ESConDelay New(float delay)
        {
            ESConDelay val = ESPool.Alloc<ESConDelay>();
            val.m_delay = (long)(delay * 1000.0f);
            if (val.m_delay < 0)
            {
                val.m_delay = long.MaxValue;
            }
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConDelay val = ESPool.Alloc<ESConDelay>();
            val.m_delay = m_delay;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConDelay>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConDelay refer = other as ESConDelay;
            if (refer == null)
            {
                return false;
            }
            if (m_delay != refer.m_delay)
            {
                return false;
            }
            if (m_cacheTimeStamp == refer.m_cacheTimeStamp)
            {
                return true;
            }
            if (m_cacheFrame == refer.m_cacheFrame)
            {
                return true;
            }
            return false;
        }
        protected override void OnGetUpdateInfo(ESchedulerContext context,ref long totalTime, ref long restTime, bool max)
        {
            long rest = m_delay - (context.TimerAjuster.CurrentTime - m_cacheTimeStamp);
            if ((max && rest > restTime)||
                (!max && rest < restTime))
            {
                totalTime = m_delay;
                restTime = rest;
            }
        }

        public override void Init(ESchedulerContext context)
        {
            base.Init(context);
            m_cacheTimeStamp = context.TimerAjuster.CurrentTime;
            m_cacheFrame = context.TimerAjuster.CurrentFrame;
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            if (m_triggered)
            {
                return eConditionState.Close;
            }
            long elapseTime = context.TimerAjuster.CurrentTime - m_cacheTimeStamp;
            if (elapseTime < m_delay)
            {
                return eConditionState.Normal;
            }
            return eConditionState.Open;
        }
        public override void OnPostTriggerFired(ESchedulerContext context)
        {
            base.OnPostTriggerFired(context);
        }
    }
}
