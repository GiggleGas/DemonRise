using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConRepeat : ESCondition
    {
        private int m_count;
        private int m_current;
        public static ESConRepeat New(int count)
        {
            ESConRepeat val = ESPool.Alloc<ESConRepeat>();
            val.m_count = count;
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConRepeat val = ESPool.Alloc<ESConRepeat>();
            val.m_count = m_count;
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConRepeat>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConRepeat refer = other as ESConRepeat;
            if (refer == null)
            {
                return false;
            }
            if (m_count != refer.m_count && m_current != refer.m_current)
            {
                return false;
            }
            return true;
        }
        public override void Init(ESchedulerContext context)
        {
            base.Init(context);
            m_current = 0;
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            if (m_current >= m_count)
            {
                return eConditionState.Close;
            }
            return eConditionState.Open;
        }
        public override void OnPostTriggerFired(ESchedulerContext context)
        {
            ++m_current;
        }
    }
}
