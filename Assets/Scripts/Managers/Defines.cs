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
        BattleMainView,
        GamblingView
    }

    public enum EventType
    {
        EVENT_BATTLE,
        EVENT_BATTLE_UI,
        EVENT_GAMBLING
    }

    public static class SubEventType
    {
        // EVENT_BATTLE
        public const int GET_PLAYER_INPUT = 1;
        public const int GET_DICE_RESULT = 2;
        public const int END_GAME = 3;

        // EVENT_BATTLE_UI
        public const int UPDATE_GAME_STAGE = 101;
        public const int OPEN_BATTLE_MAIN_VIEW = 102;
        public const int CHANGE_DICE_STATE = 103;
        public const int CHANGE_DICE_SPRITE = 104;
        public const int UPDATE_ENERGY = 105;
        public const int UPDATE_PLAYER_PAWN = 106;
        public const int ENTER_GAMBLING = 107;
        public const int UPDATE_GAMBLING_PLAYER_VIEW = 108;
        public const int ANIM_FINISH = 109;
        public const int UPDATE_DICE_NUM = 110;

        public const int ROLL_THE_DICE = 201; // view -> mgr
        public const int GAMBLING_VIEW_FINISH_LOAD = 202;
        public const int DRAW_ROAD = 203;
        public const int CLEAR_ROAD = 204;
        public const int BLOCK_MOUSE_DOWN = 205;
        public const int BLOCK_MOUSE_UP = 206;

        public const int MOVE_PLAYER = 207;
        public const int MOVE_AI = 208;
        public const int PLAYER_MOVE_FINISH = 209;

        public const int PLAYER_ATTACK_FINISH = 210;
        public const int AI_ATTACK_FINISH = 211;

        public const int AI_TURN_FINISH = 212;

        // card
        public const int UPDATE_HAND_DECK = 301;

    }
}
