using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    /// <summary>
    /// 全局动画更新comp
    /// </summary>
    public class AnimControlComp : MonoBehaviour
    {
        public void RegisterEvents()
        {
            EventMgr.Instance.Register<MapPawn, string>(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_CONTINUOUS_ANIMATION, PlayContinuousAnimation);
            EventMgr.Instance.Register<MapPawn, string, float>(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_ONCE_ANIMATION, PlayOnceAnimation);
        }

        /// <summary>
        /// 播放持续性动画 播了就行，停的时候其他地方停
        /// </summary>
        /// <param name="mapPawn"></param>
        /// <param name="animation"></param>
        public void PlayContinuousAnimation(MapPawn mapPawn, string animation)
        {
            if(mapPawn is EnemyPawn) 
            {
                Debug.Log("continue animation " + animation); 
            }
            mapPawn._animator.CrossFade(animation, 0.2f);
        }

        /// <summary>
        /// 播animation time时长后触发事件
        /// </summary>
        /// <param name="mapPawn"></param>
        /// <param name="animation"></param>
        /// <param name="afterAnimation"></param>
        /// <param name="time"></param>
        /// <param name="crossFade"></param>
        public void PlayOnceAnimation(MapPawn mapPawn, string animation, float time = 1.0f)
        {
            if (time > 0)
            {
                if (mapPawn is EnemyPawn)
                {
                    Debug.Log("Once animation " + animation);
                }
                mapPawn._animator.CrossFade(animation, 0.2f);
                StartCoroutine(Wait());
            }

            IEnumerator Wait()
            {
                yield return new WaitForSeconds(time - 0.2f);
                EndAnimation();
            }

            void EndAnimation()
            {
                if (mapPawn is EnemyPawn)
                {
                    Debug.Log("Once animation " + animation +" End");
                }
                mapPawn._animator.CrossFade("Idle", 0.2f);
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_ANIMATION_FINISH, mapPawn, animation);
            }
        }
    }
}
