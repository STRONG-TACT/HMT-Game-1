using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class RoomManager : MonoBehaviour
{
    public Text waitingTxt;
    private int playerLeft;

    private bool startGame;

    private void Start()
    {
        startGame = false;
    }
    private void Update()
    {
        playerLeft = 3 - PhotonNetwork.CurrentRoom.Players.Count;
        waitingTxt.text = "Wait for " + playerLeft.ToString() + " characters to start the game";
/*        if (PhotonNetwork.IsMasterClient && !startGame)
        {
            PhotonNetwork.LoadLevel("Game");
            startGame = true;
        }*/
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.Players.Count == 3 && !startGame)
        {
            //PhotonNetwork.LoadLevel("Level_1");
            PhotonNetwork.LoadLevel("Level_1");
            startGame = true;
        }
    }
}
