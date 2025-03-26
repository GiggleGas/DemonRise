using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESConBlock : ESCondition
    {
        public static ESConBlock New()
        {
            ESConBlock val = ESPool.Alloc<ESConBlock>();
            return val;
        }
        protected override ESCondition OnClone()
        {
            ESConBlock val = ESPool.Alloc<ESConBlock>();
            return val;
        }
        public override void Free()
        {
            base.Free();
            ESPool.Free<ESConBlock>(this);
        }
        protected override bool IsEqual(ESCondition other)
        {
            if (other == null)
            {
                return false;
            }
            ESConBlock refer = other as ESConBlock;
            if (refer == null)
            {
                return false;
            }
            return true;
        }
        public override void Init(ESchedulerContext context)
        {
            base.Init(context);
        }
        public override eConditionState OnCheck(ESchedulerContext context)
        {
            return eConditionState.Open;
        }
    }
}
