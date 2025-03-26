/************************************************************************
Author：veleonli
Date：2019/03/14 16:36:04
Refer:

Description:时间线调节器，提供时间相关基础属性和接口。如果需要不同的时间计算，需要派生，默认按照本地系统时间计算.内部时间全部以毫秒为单位
************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ppCore.Scheduler
{
    public class ESTimerAjuster
    {
        /// <summary>
        /// 时间调节器是否已经同步，并可以使用
        /// </summary>
        public virtual bool Synced
        {
            get
            {
                return true;
            }
        }
        /// <summary>
        /// 是否支持日期
        /// </summary>
        public virtual bool SupportDate
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// 是否时间戳不稳定，如果返回true，那么每当触发器触发之后需要更新所有触发器的下次触发事件，因为内部是由Heap实现的，而heap又依赖于下次触发时间.
        /// 这个属性，只有在向heap，push的时候使用。
        /// </summary>
        public virtual bool IsStampUnStable
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 时间倍率
        /// </summary>
        protected float m_timeScale = 1.0f;
        public virtual float TimeScale
        {
            get
            {
                return m_timeScale;
            }
            set
            {
                m_timeScale = value;
            }
        }

        /// <summary>
        /// 是否暂停
        /// </summary>
        protected bool m_paused = false;
        public virtual bool Paused
        {
            get
            {
                return m_paused;
            }
            set
            {
                m_paused = value;
            }
        }
        private long m_cacheCurrentTime = 0;
        /// <summary>
        /// 获取当前时间，毫秒为单位
        /// </summary>
        public virtual long CurrentTime
        {
            get
            {
                if (m_cacheCurrentTime == 0 || m_freshCurrentFlag)
                {
                    m_cacheCurrentTime = (System.DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
                }
                return m_cacheCurrentTime;
            }
        }

        public virtual DateTime CurrentDate
        {
            get
            {
                return System.DateTime.Now;
            }
        }

        private long m_lastDeltaTimeStamp = 0;
        private long m_deltaTime = 0;
        /// <summary>
        /// deltaTime
        /// </summary>
        public virtual long DeltaTime
        {
            get
            {
                return m_deltaTime;
            }
        }
        
        private bool m_freshCurrentFlag = false;
        public virtual void FreshDelta()
        {
            m_freshCurrentFlag = true;
            long curTimeStamp = CurrentTime;
            m_deltaTime = curTimeStamp - m_lastDeltaTimeStamp;
            m_lastDeltaTimeStamp = curTimeStamp;
            m_freshCurrentFlag = false;
        }
        /// <summary>
        /// 当前帧数
        /// </summary>
        private long m_currentFrame = 0;
        public virtual long CurrentFrame
        {
            get
            {
                return m_currentFrame;
            }
            set
            {
                m_currentFrame = value;
            }
        }
    }
}
