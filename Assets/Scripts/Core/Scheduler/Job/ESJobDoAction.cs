// using ppCore.TinyLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESJobDoAction : ESJob
    {
        public override void Excuse(ESJobContext context)
        {
            if (context.JobDetail.CallBackAction == null /*|| context.JobDetail.CallBackAction.Target == null*/)
            {
                return;
            }

            try
            {
                context.JobDetail.CallBackAction.Invoke();
            }
            catch (System.Exception ex)
            {
                //ILog.Error("Awake try...catch error:", ex.ToString());
            }
        }
    }
}