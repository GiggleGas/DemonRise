using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public static class AnimUtil
    {
        public static void ForceCrossFade(this Animator animator, string name, float transitionDuration, int layer = 0, float normalizedTime = float.NegativeInfinity)
        {
            animator.Update(0);

            if (animator.GetNextAnimatorStateInfo(layer).fullPathHash == 0)
            {
                animator.CrossFade(name, transitionDuration, layer, normalizedTime);
            }
            else
            {
                animator.Play(animator.GetNextAnimatorStateInfo(layer).fullPathHash, layer);
                animator.Update(0);
                animator.CrossFade(name, transitionDuration, layer, normalizedTime);
            }
        }
    }
    /// <summary>
    /// 动画更新comp
    /// </summary>
    public class AnimControlComp : MonoBehaviour
    {
        private MapPawn _mapPawn;
        private Animator _animator;

        public void Init(MapPawn mapPawn)
        {
            _mapPawn = mapPawn;
            _animator = GetComponent<Animator>();
        }

        public void PlayAnimation(string animation)
        {
            _animator.Play(animation);
            //AnimUtil.ForceCrossFade(_animator, animation, 0.2f);
        }

        public void OnAttackEnd()
        {
            _mapPawn.OnAnimFinish("Attack");
        }
    }
}
