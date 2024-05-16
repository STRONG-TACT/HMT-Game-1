using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameConstant
{
    public class GlobalConstant
    {
        public const string ROOM_SCENE = "Room_n";
        public const string LOCAL_SCENE = "Local_Animated";
        public const string NETWORK_SCENE = "NetworkGamePlay";
        public const int START_HEALTH = 3;
        public const string GAME_VERSION = "1.0.0";
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