using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PDR
{
    public enum ControllerType
    {

    }

    public enum ViewType
    {
        BattleMainView
    }

    public enum EventType
    {
        EVENT_BATTLE,
        EVENT_BATTLE_UI,
        EVENT_BLOCKEVENT
    }

    public static class SubEventType
    {
        // EVENT_BATTLE
        public const int GET_PLAYER_INPUT = 1;
        public const int GET_DICE_RESULT = 2;
        public const int UPDATE_GAME_STAGE = 3;

        // EVENT_BATTLE_UI
        public const int OPEN_BATTLE_MAIN_VIEW = 101;
        public const int ROLL_THE_DICE = 102;
        public const int CHANGE_DICE_PIC = 103;
    }
}
