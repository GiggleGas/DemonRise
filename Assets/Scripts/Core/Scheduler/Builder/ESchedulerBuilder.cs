using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    class ESchedulerBuilder
    {
        public static EScheduler CreateScheduler(ESTimerAjuster timerAjuster)
        {
            EScheduler scheduler = new EScheduler();
            scheduler.SchedulerContext.TimerAjuster = timerAjuster;

            return scheduler;
        }
    }
}
