using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConBeginTime : ESCondition
    {
        private long m_beginTime = 0;
        

        public static ESConBeginTime New(DateTime date)
        {
            ESConBeginTime val = ESPool.Alloc<ESConBeginTime>();
            val.m_beginTime = (date.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConBeginTime val = ESPool.Alloc<ESConBeginTime>();
            val.m_beginTime = m_beginTime;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConBeginTime>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConBeginTime refer = other as ESConBeginTime;
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
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            if (m_triggered)
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
