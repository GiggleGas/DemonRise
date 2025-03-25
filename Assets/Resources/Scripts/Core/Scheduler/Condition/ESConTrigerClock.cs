using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConTrigerClock : ESCondition
    {
        private long m_clockTime = 60000;
        private long m_clockCnt = 0;
        private long trigerTime = 0;

        private static long s_startStamp = 0;
        private static long StartTime
        {
            get
            {
                if (s_startStamp > 0)
                {
                    return s_startStamp;
                }
                DateTime date = new DateTime(1970,1,1,0,0,0);
                s_startStamp = (date.ToUniversalTime().Ticks - 621355968000000000) / 10000; ;
                return s_startStamp;
            }
        }
        public static ESConTrigerClock New(long clockMilliSeconds)
        {
            ESConTrigerClock val = ESPool.Alloc<ESConTrigerClock>();
            val.m_clockTime = clockMilliSeconds;
            if (val.m_clockTime < 0)
            {
                val.m_clockTime = long.MaxValue;
            }
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConTrigerClock val = ESPool.Alloc<ESConTrigerClock>();
            val.m_clockTime = m_clockTime;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConTrigerClock>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConTrigerClock refer = other as ESConTrigerClock;
            if (refer == null)
            {
                return false;
            }
            if (m_clockTime == refer.m_clockTime)
            {
                return true;
            }
            return false;
        }

        protected override void OnGetUpdateInfo(ESchedulerContext context, ref long totalTime, ref long restTime, bool max)
        {
            long cnt = (context.TimerAjuster.CurrentTime - StartTime - trigerTime) / m_clockTime;
            long rest = (cnt+1)*m_clockTime - (context.TimerAjuster.CurrentTime - StartTime - trigerTime);
            if ((max && rest > restTime) ||
                (!max && rest < restTime))
            {
                restTime = rest;
            }
        }
        public override void Init(ESchedulerContext context)
        {
            base.Init(context);
            trigerTime = (context.TimerAjuster.CurrentTime - StartTime);
            m_clockCnt = 0;
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            if (m_triggered)
            {
                return eConditionState.Close;
            }
            long cnt = (context.TimerAjuster.CurrentTime - StartTime - trigerTime) / m_clockTime;
            if (cnt == m_clockCnt)
            {
                return eConditionState.Normal;
            }
            return eConditionState.Open;
        }
        public override void OnPostTriggerFired(ESchedulerContext context)
        {
            long cnt = (context.TimerAjuster.CurrentTime - StartTime - trigerTime) / m_clockTime;
            m_clockCnt = cnt;
            base.OnPostTriggerFired(context);
        }
    }
}
