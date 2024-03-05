using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoomManager : MonoBehaviour
{
    [SerializeField]
    private Button _readyButton;
    public void PlayerReady()
    {
        RoomNetwork.S.RegisterPlayerReadyLocal();
        _readyButton.interactable = false;
    }
}
