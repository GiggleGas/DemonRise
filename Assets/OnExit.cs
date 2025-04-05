using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PDR;

public class OnExit : StateMachineBehaviour
{
    [SerializeField] 
    private string animation;
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<AnimControlComp>().ChangeAnimation(animation, 0.2f, stateInfo.length);
    }
}
