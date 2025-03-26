using ppCore.Manager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PDR
{
    public enum InputState
    {
        WaitingForRolling, // 等待摇骰子
        WaitingForPlaying, // 等待局内操作
        Pause,
    }

    [Manager(ManagerPriority.Delay)]
    public class InputManager : ppCore.Common.Singleton<InputManager>, IManager
    {
        private KeyCode CurrentPressKey = KeyCode.None;
        public void OnUpdate()
        {
            if (CurrentPressKey == KeyCode.None)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    CurrentPressKey = KeyCode.Space;
                }
            }
            else if (Input.GetKeyUp(CurrentPressKey))
            {
                if (CurrentPressKey == KeyCode.Space)
                {
                    RollTheDice();
                }
            }
        }

        public void RollTheDice()
        {
            
        }
    }
}