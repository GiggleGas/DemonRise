using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConEndTime : ESCondition
    {
        private long m_endTime = 0;
        
        public static ESConEndTime New(DateTime date)
        {
            ESConEndTime val = ESPool.Alloc<ESConEndTime>();
            val.m_endTime = (date.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConEndTime val = ESPool.Alloc<ESConEndTime>();
            val.m_endTime = m_endTime;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConEndTime>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConEndTime refer = other as ESConEndTime;
            if (refer == null)
            {
                return false;
            }
            if (m_endTime == refer.m_endTime)
            {
                return true;
            }
            return false;
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
            if (context.TimerAjuster.CurrentTime >= m_endTime)
            {
                return eConditionState.Close;
            }
            return eConditionState.Open;
        }
        public override void OnPostTriggerFired(ESchedulerContext context)
        {
            base.OnPostTriggerFired(context);
        }
    }
}
