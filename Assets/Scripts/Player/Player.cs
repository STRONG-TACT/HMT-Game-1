using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class Player : MonoBehaviour {
    public int playerID;

    public Character myCharacter;
    PhotonView photonView;

    GameManager gameManager;
    CombatSystem combatSystem;
    CameraManager cameraManager;

    public bool MyTurn {
        get {
            return photonView.IsMine && GameManager.Instance.turn == PhotonNetwork.LocalPlayer.ActorNumber;
        }
    }

    // Start is called before the first frame update
    void Start() {
        photonView = this.GetComponent<PhotonView>();
        playerID = GameManager.Instance.playerIDs.IndexOf(photonView.ViewID);


        SceneManager.activeSceneChanged += OnSceneChanged;

        
        
        gameManager = GameManager.Instance;
        combatSystem = CombatSystem.Instance;
        cameraManager = CameraManager.Instance;
    }

    private void OnSceneChanged(Scene current, Scene next) {
        throw new NotImplementedException();
    }





    // Update is called once per frame
    void Update() {
/*        if (photonView.IsMine && GameManager.Instance.turn == PhotonNetwork.LocalPlayer.ActorNumber && !CombatSystem.Instance.isInFight && GameManager.Instance.isFirstLevel) {
            if (!gameData.differentCameraView) {
                PlayerMovement();
            }
            else if (cameraManager.cameraCentered) {
                PlayerMovement();
            }
        }*/
    }
}
