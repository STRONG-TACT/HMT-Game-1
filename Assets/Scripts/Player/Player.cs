using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class Player : MonoBehaviour {
    //public int playerID;

    public Character myCharacter;
    PhotonView photonView;

    public bool MyTurn {
        get {
            return photonView.IsMine && GameManager.Instance.turn == PhotonNetwork.LocalPlayer.ActorNumber;
        }
    }

    // Start is called before the first frame update
    void Start() {
        photonView = this.GetComponent<PhotonView>();

        SceneManager.activeSceneChanged += OnSceneChanged;
        GameData gameData = FindObjectOfType<GameData>();
        myCharacter = gameData.inSceneCharacters[PlayerMapper.Instance.LocalCharacterNumber];

    }

    private void OnSceneChanged(Scene current, Scene next) {
        //TODO may need to wait a frame before grabbing these references

        GameData gameData = FindObjectOfType<GameData>();
        myCharacter = gameData.inSceneCharacters[PlayerMapper.Instance.LocalCharacterNumber];
    }
    
    // Update is called once per frame
    void Update() {
       //if (photonView.IsMine && 
       //     GameManager.Instance.turn == PhotonNetwork.LocalPlayer.ActorNumber && 
       //     !CombatSystem.Instance.isInFight && GameManager.Instance.isFirstLevel &&
       if(MyTurn && myCharacter.State == Character.CharacterState.Idle){
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f) {
                if (Input.GetAxisRaw("Horizontal") < 0) {
                    myCharacter.Move(Character.Direction.Left);
                }
                else if (Input.GetAxisRaw("Horizontal") > 0) {
                    myCharacter.Move(Character.Direction.Right);
                }
            }
            //vertical move
            else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f) {
                if (Input.GetAxisRaw("Vertical") > 0) {
                    myCharacter.Move(Character.Direction.Up);
                }
                else if (Input.GetAxisRaw("Vertical") < 0) {
                    myCharacter.Move(Character.Direction.Down);
                }
            }
        }
    }

    private Character.Direction CheckInputs() {
        
        return Character.Direction.Up;
    }
}
