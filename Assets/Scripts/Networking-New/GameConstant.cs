using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameConstant
{
    public class GlobalConstant
    {
        public const int LOCAL_TEST_LEVEL = 1;
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
}