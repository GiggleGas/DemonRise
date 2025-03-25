/************************************************************************
Author：veleonli
Date：2019/03/14 19:24:29
Refer:

Description:Job基类，提供具体职责实现
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESJob
    {
        public virtual void Excuse(ESJobContext context)
        {

        }
    }
}
