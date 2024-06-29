using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace GameConstant
{
    public class GlobalConstant
    {
        public const string LOBBY_SCENE = "Lobby_n";
        public const string ROOM_SCENE = "Room_n";
        public const string LOCAL_SCENE = "Local_Animated";
        public const string SURVEY_SCENE = "Survey_scene";
        public const string NETWORK_SCENE = "NetworkGamePlay";
        public const string GAME_VERSION = "1.0.0";
    }

    public class MatchMakingParameter
    {
        public const float TWO_PERSON_GAME_CHANCE = 1.0f;
        public const string NUM_PERSON_KEY = "NPK";
        public const int ROOM_NAME_RANGE = 999999999;
        public const float TIMEOUT_LIMIT = 3.0f;
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