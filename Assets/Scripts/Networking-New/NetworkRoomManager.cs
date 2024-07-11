using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoomManager : MonoBehaviour
{
    [SerializeField]
    private Button _readyButton;

    [SerializeField]
    private Button _launchWithAIsButton;

    [SerializeField]
    private Text _launchAIText;


    public void PlayerReady()
    {
        RoomNetwork.S.RegisterPlayerReadyLocal();
        _readyButton.interactable = false;
        CompetitionMiddleware.Instance.LogReadyUp();
    }

    public void LaunchGameWithAIs() {
        RoomNetwork.S.LaunchGameWithAIsLocal();
        _readyButton.interactable = false;
        _launchWithAIsButton.interactable = false;
    }

    private void Update() {
        UpdateButtonLabel(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    public void UpdateButtonLabel(int playerCount) {
       switch(playerCount) {
            case 0:
                _launchAIText.text = "Launch Game with 3 AIs";
                break;
           case 1:
               _launchAIText.text = "Launch Game with 2 AIs";
               break;
           case 2:
               _launchAIText.text = "Launch Game with 1 AI";
               break;
           default:
                _launchAIText.text = "Lobby Full";
                _launchWithAIsButton.interactable = false;
               break;
       }
    }

}

