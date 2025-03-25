/************************************************************************
Author：veleonli
Date：2019/03/14 20:14:49
Refer:

Description:Job分组管理
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESJobGroup
    {
        private Dictionary<long,ESJobDetail> m_jobDetails = new Dictionary<long, ESJobDetail>();

        public Dictionary<long, ESJobDetail> JobDetails
        {
            get
            {
                return m_jobDetails;
            }
        }
        private int m_group = 0;
        public int Group
        {
            get
            {
                return m_group;
            }
            set
            {
                m_group = value;
            }
        }

        public void AddJobDetail(ESJobDetail jobDetail)
        {
            jobDetail.JobGroup = this;
            if (m_jobDetails.ContainsKey(jobDetail.Identity))
            {
                throw new Exception("AddJob Error! Identity has been Conflict!");
            }
            m_jobDetails[jobDetail.Identity] = jobDetail;
        }

        public void RemoveJobDetail(ESJobDetail jobDetail)
        {
            m_jobDetails.Remove(jobDetail.Identity);
        }

        public void Clear()
        {
            m_jobDetails.Clear();
        }
    }
}
