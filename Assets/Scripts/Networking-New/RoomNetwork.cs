using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class RoomNetwork : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        Debug.Log("My UserId: " + PhotonNetwork.LocalPlayer.UserId);
        Debug.Log("Am I the master: " + PhotonNetwork.IsMasterClient);
    }
}
