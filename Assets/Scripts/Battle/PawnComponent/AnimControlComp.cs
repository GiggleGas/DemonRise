using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public class AnimControlComp : MonoBehaviour
    {
        public void RegisterEvents()
        {
            EventMgr.Instance.Register<MapPawn, string, float, float>(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_ANIMATION, ChangeAnimation);
        }

        public void ChangeAnimation(MapPawn mapPawn, string animation, float time, float crossFade)
        {
            if (time > 0)
            {
                StartCoroutine(Wait());
            }
            else
            {
                EndAnimation();
            }

            IEnumerator Wait()
            {
                yield return new WaitForSeconds(time - crossFade);
                EndAnimation();
            }

            void EndAnimation()
            {
                mapPawn._animator.CrossFade(animation, crossFade);
                EventMgr.Instance.Dispatch(EventType.EVENT_BATTLE_UI, SubEventType.PAWN_PLAY_ANIMATION_FINISH, mapPawn, animation);
            }
        }
    }
}
