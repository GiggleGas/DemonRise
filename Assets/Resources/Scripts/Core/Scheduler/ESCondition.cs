/************************************************************************
Author：veleonli
Date：2019/03/14 19:15:45
Refer:

Description:触发器条件,条件是整个任务调度系统最复杂的模块。条件有并列条件和或条件，还有复位条件。并列条件全部满足这个条件才是open状态，或条件只有一个满足
就open。复位条件，是当条件满足触发一次任务的时候，吧当前条件重置，进行重新检测，一般用于循环任务，常用的复位条件有repeat, block。
    为了优化，条件节点缓存了当前检测的节点，这个缓存节点只存在根节点。当条件open之后，需要缓存next作为当前检测节点，如果节点close之后，需要设置pre节点作为
当前节点。
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public enum eConditionState
    {
        Normal,
        Open,
        Close,
    }
    public class ESCondition : IEquatable<ESCondition>
    {
        private List<ESCondition> m_andCons = new List<ESCondition>();  //并列条件
        private List<ESCondition> m_orCons = new List<ESCondition>();   //或条件
        private ESCondition m_preCon;        //前一个条件
        private ESCondition m_nextCon;      //下一个条件
        private ESCondition m_recoveryCon;  //复位条件

        private ESCondition m_cacheCon;   //当前在检测的节点，为了优化

        private eConditionState m_currentState = eConditionState.Normal;    //缓存当前条件状态

        protected bool m_triggered = false;

        public ESCondition Clone()
        {
            ESCondition con = OnClone();
            if (con == null)
            {
                return null;
            }
            for (int i = 0; i < m_andCons.Count; ++i)
            {
                ESCondition conTmp = m_andCons[i].Clone();
                if (conTmp == null)
                {
                    continue;
                }
                con.m_andCons.Add(conTmp);
            }
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                ESCondition conTmp = m_orCons[i].Clone();
                if (conTmp == null)
                {
                    continue;
                }
                con.m_orCons.Add(conTmp);
            }
            if (m_recoveryCon != null)
            {
                con.m_recoveryCon = m_recoveryCon.Clone();
            }
            return con;
        }
        protected virtual ESCondition OnClone()
        {
            return null;
        }
        public virtual void Free()
        {
            for (int i = 0; i < m_andCons.Count; ++i)
            {
                m_andCons[i].Free();
            }
            m_andCons.Clear();
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                m_orCons[i].Free();
            }
            m_orCons.Clear();
            m_preCon = null;
            if (m_nextCon != null)
            {
                m_nextCon.Free();
            }
            m_nextCon = null;
            if (m_recoveryCon != null)
            {
                m_recoveryCon.Free();
            }
            m_recoveryCon = null;
            m_cacheCon = null;
            m_currentState = eConditionState.Normal;
        }
        public bool Equals(ESCondition other)
        {
            if (!IsEqual(other))
            {
                return false;
            }
            if (m_andCons.Count != other.m_andCons.Count)
            {
                return false;
            }
            if (m_orCons.Count != other.m_orCons.Count)
            {
                return false;
            }
            for (int i = 0; i < m_andCons.Count; ++i)
            {
                if (!m_andCons[i].IsEqual(other.m_andCons[i]))
                {
                    return false;
                }
            }
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                if (!m_orCons[i].IsEqual(other.m_orCons[i]))
                {
                    return false;
                }
            }
            if (m_recoveryCon != null)
            {
                if (!m_recoveryCon.IsEqual(other.m_recoveryCon))
                {
                    return false;
                }
            }
            else if (other.m_recoveryCon != null)
            {
                return false;
            }
            if (m_nextCon != null)
            {
                return m_nextCon.Equals(other.m_nextCon);
            }
            return true;
        }
        protected virtual bool IsEqual(ESCondition other)
        {
            return false;
        }
        public ESCondition And(ESCondition con)
        {
            m_andCons.Add(con);
            return this;
        }

        public ESCondition Or(ESCondition con)
        {
            m_orCons.Add(con);
            return this;
        }
        public ESCondition Next(ESCondition con)
        {
            m_nextCon = con;
            m_nextCon.m_preCon = this;
            return m_nextCon;
        }

        public ESCondition Recovery(ESCondition con)
        {
            m_recoveryCon = con;
            return this;
        }

        /// <summary>
        /// 更新任务触发剩余时间信息，只有少数条件才能更新这个信息。比如Delay，BeginTime。其他条件返回最大时间
        /// </summary>
        /// <param name="context"></param>
        /// <param name="totalTime"></param>
        /// <param name="restTime"></param>
        /// <param name="max"></param>
        public void GetUpdateInfo(ESchedulerContext context, ref long totalTime, ref long restTime,bool max = false)
        {
            if (m_cacheCon != null)
            {
                m_cacheCon.GetUpdateInfo(context, ref totalTime, ref restTime, max);
                return;
            }
            //自己先获取最小时间值
            OnGetUpdateInfo(context, ref totalTime, ref restTime, max);
            //并列条件获取最大值
            for (int i = 0; i < m_andCons.Count; ++i)
            {
                m_andCons[i].GetUpdateInfo(context, ref totalTime, ref restTime, true);
            }

            //或条件获取最小值
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                m_orCons[i].GetUpdateInfo(context, ref totalTime, ref restTime, false);
            }
        }

        protected virtual void OnGetUpdateInfo(ESchedulerContext context,ref long totalTime, ref long restTime, bool max)
        {
            totalTime = long.MaxValue;
            restTime = long.MaxValue;
        }
        public virtual void Init(ESchedulerContext context)
        {
            m_triggered = false;
            for (int i = 0; i < m_andCons.Count; ++i)
            {
                m_andCons[i].Init(context);
            }
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                m_orCons[i].Init(context);
            }
        }

        public void ReStart(ESchedulerContext context)
        {
            m_currentState = eConditionState.Normal;
            Init(context);
            for (int i = 0; i < m_andCons.Count; ++i)
            {
                m_andCons[i].ReStart(context);
            }
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                m_orCons[i].ReStart(context);
            }
            if (m_nextCon != null)
            {
                m_nextCon.ReStart(context);
            }
        }

        /// <summary>
        /// 检测条件是否open，检测思路是：先检测并列条件，如果都open，则open；其次检测或条件；如果上一次检测是open，这一次检测是close，则执行重置逻辑；
        /// 如果连重置也返回close，那么整个条件就返回close。需要回溯父条件，继续检测，知道根条件。
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public eConditionState Check(ESchedulerContext context)
        {
            eConditionState state = eConditionState.Normal;
            if (m_preCon != null || m_cacheCon == null)
            {
                m_currentState = NormalCheck(context);

                return m_currentState;
            }
            else
            {
                state = m_cacheCon.Check(context);
                while (state == eConditionState.Close)
                {
                    if (this == m_cacheCon.m_preCon)
                    {
                        m_cacheCon = null;
                        break;
                    }
                    m_cacheCon = m_cacheCon.m_preCon;
                    state = m_cacheCon.Check(context);
                }
                return state;
            }
        }

        private ESCondition GetRoot()
        {
            ESCondition root = this;
            while (root.m_preCon != null)
            {
                root = root.m_preCon;
            }
            return root;
        }
        private void TryChangeCacheDown(ESchedulerContext context)
        {
            if (m_nextCon != null)
            {
                m_nextCon.Init(context);

                if (m_nextCon.m_recoveryCon != null)
                {
                    m_nextCon.m_recoveryCon.Init(context);
                }
                GetRoot().m_cacheCon = m_nextCon;
                return;
            }

        }

        private eConditionState NormalCheck(ESchedulerContext context)
        {
            bool tmp = false;
            //首先判断并列条件
            bool closed = false;
            var state = eConditionState.Close;
            try
            {
                state = OnCheck(context);
            }
            catch(Exception)
            {
                state = eConditionState.Close;
            }
            if (state == eConditionState.Open)
            {
                tmp = false;
                for (int i = 0; i < m_andCons.Count; ++i)
                {
                    state = m_andCons[i].Check(context);
                    if (state == eConditionState.Open)
                    {
                        continue;
                    }
                    else if (state == eConditionState.Close)
                    {
                        closed = true;
                    }
                    tmp = true;
                }
                if (!tmp)
                {
                    return state;
                }
            }
            else if (state == eConditionState.Close)
            {
                closed = true;
            }
            //判断或条件
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                state = m_orCons[i].Check(context);
                if (state != eConditionState.Open)
                {
                    if (state == eConditionState.Normal)
                    {
                        closed = false;
                    }
                    continue;
                }
                closed = false;
                GetRoot().m_cacheCon = m_nextCon;
                return eConditionState.Open;
            }
            if (closed)
            {
                return eConditionState.Close;
            }
            return eConditionState.Normal;
        }
        private void TryRecovery(ESchedulerContext context)
        {
            //这里执行重置逻辑
            if (m_recoveryCon == null)
            {
                m_currentState = eConditionState.Close;
                return;
            }
            eConditionState state = m_recoveryCon.Check(context);
            if (state == eConditionState.Open)
            {
                ReStart(context);
            }
            else if (state == eConditionState.Close)
            {
                m_currentState = eConditionState.Close;
                return;
            }
            m_currentState = eConditionState.Normal;
        }

        //private Exception m_exception = new Exception("triggered");
        public virtual eConditionState OnCheck(ESchedulerContext context)
        {
            //if (m_triggered)
            //{
            //    throw new Exception("triggered");
            //}

            return eConditionState.Normal;
        }

        public void PostTriggerFired(ESchedulerContext context)
        {
            if (m_cacheCon != null)
            {
                m_cacheCon.PostTriggerFired(context);
                return;
            }
            for (int i = 0; i < m_andCons.Count; ++i)
            {
                m_andCons[i].PostTriggerFired(context);
            }
            for (int i = 0; i < m_orCons.Count; ++i)
            {
                m_orCons[i].PostTriggerFired(context);
            }
            if (m_recoveryCon != null)
            {
                m_recoveryCon.PostTriggerFired(context);
            }
            OnPostTriggerFired(context);

            //这里重置缓存节点
            eConditionState state = NormalCheck(context);
            if (state == eConditionState.Normal)
            {
                return;
            }
            else if(state == eConditionState.Close)
            {
                TryRecovery(context);
            }

            if (m_currentState != eConditionState.Normal)
            {
                TryChangeCacheDown(context);
            }
        }
        public virtual void OnPostTriggerFired(ESchedulerContext context)
        {
            m_triggered = true;
        }
    }
}
