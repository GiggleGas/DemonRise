using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConFrame : ESCondition
    {
        private long m_delayFrame = 1;
        private long m_cacheFrame = 0;
        public static ESConFrame New(long delayFrame)
        {
            ESConFrame val = ESPool.Alloc<ESConFrame>();
            val.m_delayFrame = delayFrame;
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConFrame val = ESPool.Alloc<ESConFrame>();
            val.m_delayFrame = m_delayFrame;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConFrame>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConFrame refer = other as ESConFrame;
            if (refer == null)
            {
                return false;
            }
            if (m_delayFrame != refer.m_delayFrame)
            {
                return false;
            }
            return true;
        }
        public override void Init(ESchedulerContext context)
        {
            base.Init(context);
            m_cacheFrame = context.TimerAjuster.CurrentFrame;
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            if (m_triggered)
            {
                return eConditionState.Close;
            }
            if (m_cacheFrame == 0)
            {
                return eConditionState.Close;
            }
            if (m_cacheFrame + m_delayFrame >= context.TimerAjuster.CurrentFrame)
            {
                return eConditionState.Normal;
            }
            return eConditionState.Open;
        }
        public override void OnPostTriggerFired(ESchedulerContext context)
        {
            base.OnPostTriggerFired(context);
            m_cacheFrame = 0;
        }
    }
}
