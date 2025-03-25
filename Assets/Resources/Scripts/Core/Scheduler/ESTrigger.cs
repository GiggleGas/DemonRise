/************************************************************************
Author：veleonli
Date：2019/03/14 19:15:04
Refer:

Description:job触发器，组合多个condition，来控制job的执行
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESTrigger : IComparable<ESTrigger>,IEquatable<ESTrigger>
    {
        private ESCondition m_condition;
        private LinkedList<ESJobDetail> m_jobDetails = new LinkedList<ESJobDetail>();


        private long m_nextTriggerTime = long.MaxValue;

        public bool CanOptimize
        {
            get
            {
                return m_nextTriggerTime != long.MaxValue;
            }
        }
        public LinkedList<ESJobDetail> JobDetails
        {
            get
            {
                return m_jobDetails;
            }
        }

        public static ESTrigger New()
        {
            return ESPool.Alloc<ESTrigger>();
        }

        public ESTrigger Clone()
        {
            ESTrigger trigger = ESPool.Alloc<ESTrigger>();


            return trigger;
        }
        public void Free()
        {
            m_condition.Free();
            m_condition = null;
            ESPool.Free<ESTrigger>(this);
        }
        /// <summary>
        /// 相等比较，暂时空出来，预留未来trigger合并，用于优化
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ESTrigger other)
        {
            return m_nextTriggerTime.CompareTo(other.m_nextTriggerTime);
        }
        public bool Equals(ESTrigger other)
        {
            return m_condition.Equals(other.m_condition);
        }
        /// <summary>
        /// 设置条件
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        public ESCondition Condition(ESCondition con)
        {
            m_condition = con;
            return m_condition;
        }
        
        public void AddJobDetail(ESJobDetail detail)
        {
            detail.JobContext.Trigger = this;
            m_jobDetails.AddLast(detail);
        }

        public void RemoveJobDetail(ESJobDetail detail)
        {
            m_jobDetails.Remove(detail);
        }

        public void Clear()
        {
            m_jobDetails.Clear();
        }

        public bool IsEmpty()
        {
            return (m_jobDetails.Count == 0);
        }
        public void InitCondition(ESchedulerContext context)
        {
            if (m_condition != null)
            {
                m_condition.Init(context);
            }
        }

        public void Fire(ESchedulerContext context)
        {
            foreach (ESJobDetail detail in m_jobDetails)
            {
                if (context.Scheduler.CheckDelJob(detail.Identity)||
                    context.Scheduler.CheckDelGroup(detail.Group))
                {
                    continue;
                }
                ESJob job = context.GetRegistryJob(detail.JobContext.JobType,detail.JobContext.JobGUID);
                if (job == null)
                {
                    continue;
                }
                job.Excuse(detail.JobContext);
            }

            if (m_condition != null)
            {
                m_condition.PostTriggerFired(context);
            }
        }

        public eConditionState CheckCondition(ESchedulerContext context)
        {
            if (m_condition == null)
            {
                return eConditionState.Close;
            }
            return m_condition.Check(context);
        }

        public void FreshNextTriggerTime(ESchedulerContext context)
        {
            if (m_condition == null)
            {
                return;
            }
            long totalTime = long.MaxValue;
            long restTime = long.MaxValue;

            m_nextTriggerTime = long.MaxValue;

            m_condition.GetUpdateInfo(context, ref totalTime, ref restTime, false);
            if (restTime != long.MaxValue)
            {
                m_nextTriggerTime = context.TimerAjuster.CurrentTime + restTime;
            }
            //foreach (ESJobDetail detail in m_jobDetails)
            //{
            //    if (detail.UpdateCallBack == null)
            //    {
            //        continue;
            //    }
            //    detail.UpdateCallBack.Invoke(totalTime, restTime);
            //}
        }


        public void Dump(StringBuilder sb)
        {
            foreach (ESJobDetail detail in m_jobDetails)
            {
                sb.AppendLine(string.Format("{0} {1}", detail.CallBackAction.Method.ReflectedType.FullName, detail.CallBackAction.Method.ToString()));
            }
        }
    }
}
