using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConDateClock : ESCondition
    {
        private long m_beginTime = 0;

        private long m_lastTime = 0;
        public static ESConDateClock New(DateTime date)
        {
            ESConDateClock val = ESPool.Alloc<ESConDateClock>();
            val.m_beginTime = (date.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConDateClock val = ESPool.Alloc<ESConDateClock>();
            val.m_beginTime = m_beginTime;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConDateClock>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConDateClock refer = other as ESConDateClock;
            if (refer == null)
            {
                return false;
            }
            if (m_beginTime == refer.m_beginTime)
            {
                return true;
            }
            return false;
        }

        protected override void OnGetUpdateInfo(ESchedulerContext context, ref long totalTime, ref long restTime, bool max)
        {
            long rest = m_beginTime - context.TimerAjuster.CurrentTime;
            if ((max && rest > restTime) ||
                (!max && rest < restTime))
            {
                restTime = rest;
            }
        }
        public override void Init(ESchedulerContext context)
        {
            base.Init(context);
            m_lastTime = context.TimerAjuster.CurrentTime;
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            if (m_triggered)
            {
                return eConditionState.Close;
            }
            if (m_beginTime < m_lastTime)
            {
                return eConditionState.Close;
            }
            if (m_beginTime > context.TimerAjuster.CurrentTime)
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
