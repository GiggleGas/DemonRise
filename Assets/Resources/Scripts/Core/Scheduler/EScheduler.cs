/************************************************************************
Author：veleonli
Date：2019/03/14 20:30:59
Refer:

Description:任务调度系统入口，控制整个任务调度系统的管理。基于指定的TimerAjuster，按条件出发任务
************************************************************************/
// using ppCore.TinyLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class EScheduler
    {
        private ESchedulerContext m_Context = new ESchedulerContext();
        private Dictionary<int, ESJobGroup> m_jobGroups = new Dictionary<int, ESJobGroup>();
        private Dictionary<ESJobDetail, ESTrigger> m_cacheAdd = new Dictionary<ESJobDetail, ESTrigger>();
        private List<long> m_cacheDel = new List<long>();
        private List<int> m_cacheDelGroup = new List<int>();
        private EHeap<ESTrigger> m_jobTriggers = new EHeap<ESTrigger>();
        private EHeap<ESTrigger> m_catchjobTriggers = new EHeap<ESTrigger>();
        private LinkedList<ESTrigger> m_jobTriggersEx = new LinkedList<ESTrigger>();
        private Dictionary<long, ESJobDetail> m_jobDetails = new Dictionary<long, ESJobDetail>();
        private List<ESTrigger> m_waitDestroy = new List<ESTrigger>();
        public ESchedulerContext SchedulerContext
        {
            get
            {
                return m_Context;
            }
        }

        public EScheduler()
        {
            m_Context.Scheduler = this;
        }

        public void Start()
        {
        }

        public void Shutdown()
        {
        }

        private void PreUpdate()
        {
            m_Context.TimerAjuster.CurrentFrame += 1;
            m_Context.TimerAjuster.FreshDelta();
            if (!m_Context.TimerAjuster.Synced)
            {
                return;
            }

            for (int i = 0; i < m_cacheDelGroup.Count; ++i)
            {
                _StopGroup(m_cacheDelGroup[i]);
            }

            m_cacheDelGroup.Clear();
            //必须先添加，在删除，在添加里面判断一次是否待删除，如果是标记删除的跳过添加逻辑
            foreach (var pair in m_cacheAdd)
            {
                RealAddJob(pair.Key, pair.Value);
            }

            m_cacheAdd.Clear();
            //后删除
            for (int i = 0; i < m_cacheDel.Count; ++i)
            {
                _StopJob(m_cacheDel[i]);
            }

            m_cacheDel.Clear();
        }

        private void PostUpdate()
        {
            for (int i = 0; i < m_waitDestroy.Count; ++i)
            {
                m_jobTriggersEx.Remove(m_waitDestroy[i]);
                m_waitDestroy[i].Free();
            }

            m_waitDestroy.Clear();
            for (int i = 0; i < m_cacheDelGroup.Count; ++i)
            {
                _StopGroup(m_cacheDelGroup[i]);
            }

            m_cacheDelGroup.Clear();
            //必须先添加，在删除，在添加里面判断一次是否待删除，如果是标记删除的跳过添加逻辑
            foreach (var pair in m_cacheAdd)
            {
                RealAddJob(pair.Key, pair.Value);
            }

            m_cacheAdd.Clear();
            //后删除
            for (int i = 0; i < m_cacheDel.Count; ++i)
            {
                _StopJob(m_cacheDel[i]);
            }

            m_cacheDel.Clear();
        }

        public void Update()
        {
            PreUpdate();
            if (m_catchjobTriggers.Length > 0)
            {
                // ILog.Debug("m_catchjobTriggers Length > 0", m_catchjobTriggers.Length);
            }

            while (m_jobTriggers.Length > 0)
            {
                ESTrigger trigger = m_jobTriggers.Head();
                eConditionState state = TickTrigger(trigger);
                if (state == eConditionState.Close)
                {
                    m_jobTriggers.Pop();
                    foreach (var detail in trigger.JobDetails)
                    {
                        detail.JobGroup.RemoveJobDetail(detail);
                        m_jobDetails.Remove(detail.Identity);
                        detail.Free();
                    }

                    trigger.Clear();
                    trigger.Free();
                    continue;
                }
                else if (state == eConditionState.Normal)
                {
                    break;
                }

                m_jobTriggers.Pop();
                if (trigger.CheckCondition(SchedulerContext) == eConditionState.Close)
                {
                    foreach (var detail in trigger.JobDetails)
                    {
                        detail.JobGroup.RemoveJobDetail(detail);
                        m_jobDetails.Remove(detail.Identity);
                        detail.Free();
                    }

                    trigger.Clear();
                    trigger.Free();
                    continue;
                }

                trigger.FreshNextTriggerTime(m_Context);
                if (trigger.CanOptimize)
                {
                    if (m_Context.TimerAjuster.IsStampUnStable)
                    {
                        FreshAllRestTirggerTime();
                    }

                    //m_jobTriggers.Push(trigger);
                    m_catchjobTriggers.Push(trigger);
                }
                else
                {
                    m_jobTriggersEx.AddLast(trigger);
                }
            }

            while (m_catchjobTriggers.Length > 0)
            {
                var catchTrigger = m_catchjobTriggers.Pop();
                m_jobTriggers.Push(catchTrigger);
            }

            foreach (var trigger in m_jobTriggersEx)
            {
                eConditionState state = TickTrigger(trigger);
                if (state == eConditionState.Close)
                {
                    foreach (var detail in trigger.JobDetails)
                    {
                        detail.JobGroup.RemoveJobDetail(detail);
                        m_jobDetails.Remove(detail.Identity);
                        detail.Free();
                    }

                    trigger.Clear();
                    m_waitDestroy.Add(trigger);
                    continue;
                }
                else if (state == eConditionState.Normal)
                {
                    continue;
                }

                if (trigger.CheckCondition(SchedulerContext) == eConditionState.Close)
                {
                    foreach (var detail in trigger.JobDetails)
                    {
                        detail.JobGroup.RemoveJobDetail(detail);
                        m_jobDetails.Remove(detail.Identity);
                        detail.Free();
                    }

                    trigger.Clear();
                    m_waitDestroy.Add(trigger);
                    continue;
                }

                trigger.FreshNextTriggerTime(m_Context);
                if (trigger.CanOptimize)
                {
                    if (m_Context.TimerAjuster.IsStampUnStable)
                    {
                        FreshAllRestTirggerTime();
                    }

                    m_jobTriggers.Push(trigger);
                    m_waitDestroy.Add(trigger);
                }
            }

            PostUpdate();
        }

        private void FreshAllRestTirggerTime()
        {
            for (int i = 0; i < m_jobTriggers.Length; ++i)
            {
                m_jobTriggers.Datas[i].FreshNextTriggerTime(m_Context);
            }
        }

        public eConditionState TickTrigger(ESTrigger trigger)
        {
            if (trigger.IsEmpty())
            {
                return eConditionState.Close;
            }

            eConditionState state = trigger.CheckCondition(SchedulerContext);
            if (state == eConditionState.Open)
            {
                trigger.Fire(SchedulerContext);
            }

            return state;
        }

        public void Clear()
        {
            m_cacheAdd.Clear();
            m_cacheDel.Clear();
            m_cacheDelGroup.Clear();
            m_waitDestroy.Clear();
            foreach (var pair in m_jobGroups)
            {
                pair.Value.Clear();
            }

            m_jobGroups.Clear();
            for (int i = 0; i < m_jobTriggers.Length; ++i)
            {
                m_jobTriggers.Datas[i].Clear();
                m_jobTriggers.Datas[i].Free();
            }

            m_jobTriggers.Clear();
            for (int i = 0; i < m_catchjobTriggers.Length; ++i)
            {
                m_catchjobTriggers.Datas[i].Clear();
                m_catchjobTriggers.Datas[i].Free();
            }

            m_catchjobTriggers.Clear();
            foreach (var trigger in m_jobTriggersEx)
            {
                trigger.Clear();
                trigger.Free();
            }

            m_jobTriggersEx.Clear();
            foreach (var pair in m_jobDetails)
            {
                pair.Value.Free();
            }

            m_jobDetails.Clear();
        }

        /// <summary>
        /// 添加任务需要向三个容器添加，group,trigger,以及scheduler全局容器。
        /// </summary>
        /// <param name="detail"></param>
        /// <param name="trigger"></param>
        public void AddJob(ESJobDetail detail, ESTrigger trigger)
        {
            m_cacheAdd[detail] = trigger;
        }

        /// <summary>
        /// 删除任务也需要把三个容器里面，该任务依次删除
        /// </summary>
        /// <param name="id"></param>
        public void StopJob(long id)
        {
            m_cacheDel.Add(id);
        }

        private void _StopJob(long id)
        {
            ESJobDetail detail;
            if (!m_jobDetails.TryGetValue(id, out detail))
            {
                return;
            }

            detail.JobContext.Trigger.RemoveJobDetail(detail);
            detail.JobGroup.RemoveJobDetail(detail);
            m_jobDetails.Remove(id);
            detail.Free();
        }

        public void RestartJob(long id)
        {
            ESJobDetail detail;
            if (!m_jobDetails.TryGetValue(id, out detail))
            {
                return;
            }

            ESTrigger trigger = detail.JobContext.Trigger.Clone();
            StopJob(id);
            m_cacheAdd[detail] = trigger;
        }

        public void StopGroup(int group)
        {
            List<ESJobDetail> waitRemove = new List<ESJobDetail>();
            foreach (var pair in m_cacheAdd)
            {
                if (pair.Key.Group != group)
                {
                    continue;
                }

                waitRemove.Add(pair.Key);
            }

            for (int i = 0; i < waitRemove.Count; ++i)
            {
                m_cacheAdd.Remove(waitRemove[i]);
            }

            waitRemove.Clear();
            m_cacheDelGroup.Add(group);
        }

        private void _StopGroup(int group)
        {
            ESJobGroup jobGroup = GetJobGroup(group);
            foreach (var pair in jobGroup.JobDetails)
            {
                m_jobDetails.Remove(pair.Key);
                pair.Value.JobContext.Trigger.RemoveJobDetail(pair.Value);
                pair.Value.Free();
            }

            jobGroup.JobDetails.Clear();
        }

        public ESJobGroup GetJobGroup(int group)
        {
            ESJobGroup jobGroup;
            if (m_jobGroups.TryGetValue(group, out jobGroup))
            {
                return jobGroup;
            }

            jobGroup = new ESJobGroup();
            jobGroup.Group = group;
            m_jobGroups[group] = jobGroup;
            return jobGroup;
        }

        public ESTrigger MergeTrigger(ESTrigger referTrigger)
        {
            referTrigger.InitCondition(SchedulerContext);
            for (int i = 0; i < m_jobTriggers.Length; ++i)
            {
                if (!m_jobTriggers.Datas[i].Equals(referTrigger))
                {
                    continue;
                }

                referTrigger.Free();
                return m_jobTriggers.Datas[i];
            }

            foreach (var trigger in m_jobTriggersEx)
            {
                if (!trigger.Equals(referTrigger))
                {
                    continue;
                }

                referTrigger.Free();
                return trigger;
            }

            referTrigger.FreshNextTriggerTime(SchedulerContext);
            if (referTrigger.CanOptimize)
            {
                if (m_Context.TimerAjuster.IsStampUnStable)
                {
                    FreshAllRestTirggerTime();
                }

                m_jobTriggers.Push(referTrigger);
            }
            else
            {
                m_jobTriggersEx.AddLast(referTrigger);
            }

            return referTrigger;
        }

        private void RealAddJob(ESJobDetail detail, ESTrigger trigger)
        {
            //if (m_cacheDel.Contains(detail.Identity))
            //{
            //    m_cacheDel.Remove(detail.Identity);
            //    return;
            //}
            ESTrigger ownTrigger = MergeTrigger(trigger);
            ownTrigger.AddJobDetail(detail);
            ESJobGroup group = GetJobGroup(detail.Group);
            group.AddJobDetail(detail);
            m_jobDetails[detail.Identity] = detail;
        }

        public bool CheckDelJob(long id)
        {
            return m_cacheDel.Contains(id);
        }

        public bool CheckDelGroup(int group)
        {
            return m_cacheDelGroup.Contains(group);
        }

        public bool Dump(StringBuilder sb)
        {
            foreach (var pair in m_jobDetails)
            {
                ESJobDetail detail = pair.Value;
                sb.AppendLine(string.Format("{0} {1}", detail.CallBackAction.Method.ReflectedType.FullName, detail.CallBackAction.Method.ToString()));
            }

            return true;
        //for (int i = 0; i < m_jobTriggers.Length; ++i)
        //{
        //    m_jobTriggers.Datas[i].Dump(sb);
        //}
        }
    }
}