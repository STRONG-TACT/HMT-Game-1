using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRoomManager : MonoBehaviour
{
    public void PlayerReady()
    {
        RoomNetwork.S.RegisterPlayerReadyLocal();
    }
}
