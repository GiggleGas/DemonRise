using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConInnerval : ESCondition
    {
        private long m_innerval = 0;
        private long m_cacheTimeStamp = 0;
        
        private long m_cacheFrame = 0;
        public static ESConInnerval New(float innerval)
        {
            ESConInnerval val = ESPool.Alloc<ESConInnerval>();
            val.m_innerval = (long)(innerval * 1000.0f);
            if (val.m_innerval < 0)
            {
                val.m_innerval = long.MaxValue;
            }
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConInnerval val = ESPool.Alloc<ESConInnerval>();
            val.m_innerval = m_innerval;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConInnerval>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConInnerval refer = other as ESConInnerval;
            if (refer == null)
            {
                return false;
            }
            if (m_innerval != refer.m_innerval)
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
        protected override void OnGetUpdateInfo(ESchedulerContext context, ref long totalTime, ref long restTime, bool max)
        {
            long rest = m_innerval - (context.TimerAjuster.CurrentTime - m_cacheTimeStamp);
            if ((max && rest > restTime) ||
                (!max && rest < restTime))
            {
                totalTime = m_innerval;
                restTime = rest;
            }
        }
        public override void Init(ESchedulerContext context)
        {
            base.Init(context);
            m_cacheTimeStamp = context.TimerAjuster.CurrentTime;
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            if (m_triggered)
            {
                return eConditionState.Close;
            }
            long elapseTime = context.TimerAjuster.CurrentTime - m_cacheTimeStamp;
            if (elapseTime < m_innerval)
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
