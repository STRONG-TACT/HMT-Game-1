using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameConstant
{
    public class GlobalConstant
    {
        public const int LOCAL_TEST_LEVEL = 1;
        public const int ROOM_LEVEL = 2;
        public const int GAME_LEVEL = 3;
        public const int START_HEALTH = 3;
    }
    
    public enum OnBoardingState
    {
        ChooseGameMode, 
        ChooseJoinRoomMode, 
        CreateRoom, 
        JoinRoom, 
        CreateOrJoinRoom, 
        JoinRandomRoom, 
        Loading, 
        Disconnected
    };

    public enum GameStatus
    {
        GetReady, 
        Player_Pinning, 
        Player_Planning, 
        Player_Moving, 
        Monster_Moving, 
        Animation_Pause, 
        GameEnd
    }
}