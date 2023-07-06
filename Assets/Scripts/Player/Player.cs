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
            return GameManager.Instance.CurrentTurnPlayerNum == PhotonNetwork.LocalPlayer.ActorNumber;
        }
    }

    // Start is called before the first frame update
    IEnumerator Start() {
        photonView = this.GetComponent<PhotonView>();
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;
        GameData gameData = FindObjectOfType<GameData>();
        while (!PlayerMapper.Instance.Inititialized) {
            yield return null;
        }
        myCharacter = gameData.inSceneCharacters[PlayerMapper.Instance.LocalCharacterNumber];
        yield break;
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
        if (MyTurn) {
            switch (myCharacter.State) {
                case Character.CharacterState.Idle:
                    IdleUpdate();
                    break;
                case Character.CharacterState.Walking:
                    break;
                case Character.CharacterState.Attacking:
                    AttackingUpdate();
                    break;
                default:
                    break;
            }
        }

        void IdleUpdate() {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f) {
                if (Input.GetAxisRaw("Horizontal") < 0 && myCharacter.CheckMove(Character.Direction.Left)) {
                    GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Left);
                }
                else if (Input.GetAxisRaw("Horizontal") > 0 && myCharacter.CheckMove(Character.Direction.Right)) {
                    GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Right);
                }
            }
            //vertical move
            else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f) {
                if (Input.GetAxisRaw("Vertical") > 0 && myCharacter.CheckMove(Character.Direction.Up)) {
                    GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Up);
                }
                else if (Input.GetAxisRaw("Vertical") < 0 && myCharacter.CheckMove(Character.Direction.Down)) {
                    GameManager.Instance.CallMoveCharacter(PlayerMapper.Instance.LocalCharacterNumber, Character.Direction.Down);
                }
            }
        }

        void AttackingUpdate() {
            if (CombatSystem.Instance.State == CombatSystem.FightState.Waiting) {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)) {
                    GameManager.Instance.CallRollDie();

                    //GameManager.Instance.CallAttack(PlayerMapper.Instance.LocalCharacterNumber);
                }
            }
        }
    }
}
