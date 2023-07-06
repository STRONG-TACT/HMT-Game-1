using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Pun.Demo.PunBasics;

/// <summary>
/// A goal is that this script knows nothing about dwarves, giants, or humans.
/// </summary>
public class Character : MonoBehaviour {
    public enum Direction {
        Left, Right, Up, Down
    }

    public enum CharacterState {
        Idle, Walking, Attacking
    }

    PhotonView photonView;

    public float speed;
    private Vector3 movePoint;
    private Vector3 prevMovePointPos;

    public int playerId;

    private CharacterState characterState;

    public CharacterState State {
        get { return characterState; }
        set {
            if (value != characterState) {
                characterState = value;
                switch (value) {
                    case CharacterState.Idle:
                        animator.SetBool("Idle", true);
                        animator.SetBool("Attack", false);
                        animator.SetBool("Walk", false);
                        break;
                    case CharacterState.Walking:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attack", false);
                        animator.SetBool("Walk", true);
                        break;
                    case CharacterState.Attacking:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attack", true);
                        animator.SetBool("Walk", false);
                        break;
                }
            }
        }
    }
    //public bool[] movable; // detecting walls. index 0: left, 1: right, 2: front, 3: back 

    public int moveCount;

    public CharacterConfig config;
    public int CharacterId;

    private Transform characterRig;
    public Transform characterMask { get; private set; }
    public Transform visibilityMask { get; private set; }

    //Health
    public int Health { get { return health; } }
    private int health;

    

    private Animator animator;

    public CameraManager cameraManager;
    private UIManager uiManager;
    public GameData gameData;

    void Awake() {
        photonView = GetComponent<PhotonView>();
        characterRig = transform.GetChild(0);

    }
    private void Start() {

        speed = 3.0f;
        cameraManager = FindObjectOfType<CameraManager>();
        gameData = FindObjectOfType<GameData>();
        uiManager = FindObjectOfType<UIManager>();

        movePoint = transform.position;
        prevMovePointPos = movePoint;

        moveCount = 0;

        animator = GetComponentInChildren<Animator>();
        animator.SetBool("Idle", true);
        State = CharacterState.Idle;

        health = 3;
    }

    void Update() {
        //if its our turn the camera is centered then move
        //if (GameManager.Instance.CurrentTurnPlayerNum == PhotonNetwork.LocalPlayer.ActorNumber) {
        //    if (gameData.differentCameraView) {
        //        if (cameraManager.cameraCentered) {
        //            PlayerMovement();
        //        }
        //    }
        //    else {
        //        PlayerMovement();
        //    }
        //}
        //else {
        //    PlayerMovement();
        //}

        ////if its not out turn then just move?
        //if (GameManager.Instance.CurrentTurnPlayerNum == PhotonNetwork.LocalPlayer.ActorNumber && 
        //    !CombatSystem.Instance.isInFight && 
        //    GameManager.Instance.isFirstLevel) {
        //    if (!gameData.differentCameraView) {
        //        PlayerMovement();
        //    }
        //    else if (cameraManager.cameraCentered) {
        //        PlayerMovement();
        //    }
        //}

        switch (State) {
            case CharacterState.Walking:
                transform.position = Vector3.MoveTowards(transform.position, movePoint, speed * Time.deltaTime);
                if (Vector3.Distance(transform.position, movePoint) == 0f) {
                    GameManager.Instance.CallUpdateActionPoints(config.movement - moveCount);

                    prevMovePointPos = movePoint;
                    if (moveCount == config.movement) {
                        moveCount = 0;
                        GameManager.Instance.EndTurn();
                    }
                    State = CharacterState.Idle;
                }
                break;
            case CharacterState.Idle:
            case CharacterState.Attacking:
            default: break;
        }
    }

    public void SetUpConfig(CharacterConfig config, int characterId, GameData gameData) {
        this.config = config;
        CharacterId = characterId;

        characterMask = transform.Find("CharacterMask");
        visibilityMask = transform.Find("VisibleMask");
       
        Vector3 cellScale = new Vector3( gameData.tileSize + 2 * gameData.tileGapLength, 
                                         0, 
                                         gameData.tileSize + 2 * gameData.tileGapLength);
        characterMask.localScale = cellScale;
        visibilityMask.localScale = cellScale * config.sightRange;
        
    }

    public void CallDoMove(Vector3 targetPos, Quaternion orientation) {
        photonView.RPC("DoMove", RpcTarget.All, targetPos, orientation);
    }

    [PunRPC]
    public void DoMove(Vector3 targetPos, Quaternion orientation) {
        movePoint = targetPos;
        if (characterRig.rotation != orientation) {
            characterRig.rotation = orientation;
        }
        moveCount++;
        State = CharacterState.Walking;
    }

    public bool CheckMove(Direction direction) {
        Vector3 moveVec = direction switch {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.forward
        } ;

        return !Physics.Raycast(transform.position, moveVec, gameData.tileSize + gameData.tileGapLength, LayerMask.GetMask("Impassible"));
    }

    public bool Move(Direction direction) {
        //Debug.LogFormat("Calling Move on: {0} direction: {1}", name, direction.ToString());
        Vector3 moveVec = Vector3.zero;
        Quaternion rot = Quaternion.identity;


        switch(direction) {
            case Direction.Up:
                moveVec = Vector3.forward;
                rot = Quaternion.Euler(0, 0, 0);
                break;

            case Direction.Down:
                moveVec = Vector3.back;
                rot = Quaternion.Euler(0, 180, 0);
                break;

            case Direction.Left:
                moveVec = Vector3.left;
                rot = Quaternion.Euler(0, 270, 0);
                break;
                
            case Direction.Right:
                moveVec = Vector3.right;
                rot = Quaternion.Euler(0, 90, 0);
                break;

            default:
                goto case Direction.Up;
        }

        Debug.DrawLine(transform.position, transform.position + moveVec * (gameData.tileSize + gameData.tileGapLength), Color.red, 1f, false);

        if (Physics.Raycast(transform.position, moveVec, out RaycastHit hit, gameData.tileSize + gameData.tileGapLength, LayerMask.GetMask("Impassible"))) {
            Debug.LogErrorFormat("{0} attempted {1} move but hit {2}", name, direction.ToString(), hit.collider.gameObject.name);
            return false;
        }
        else {
            movePoint += moveVec * (gameData.tileSize + gameData.tileGapLength);

            if (characterRig.rotation != rot) {
                characterRig.rotation = rot;
            }
            moveCount++;
            State = CharacterState.Walking;
            return true;
        }
    }

    public void CompleteMove() {
        State = CharacterState.Walking;
    }

    public void ResetPosition() {
        movePoint = prevMovePointPos;
        State = CharacterState.Walking;
    }

    private void OnTriggerEnter(Collider col) {
        //Debug.LogFormat("Character Hit Trigger: {0}", col.gameObject.tag);
        switch (col.gameObject.tag) {
            case "Goal":
                //Debug.Log("Triggered Goal");
                if (CheckRightGoal(col.gameObject)) {
                    if (PhotonNetwork.IsMasterClient) {
                        GameManager.Instance.CallGoalReached(config.type.ToString());
                        PhotonNetwork.Destroy(col.gameObject);
                    }
                }
                break;

            case "Door":
                //Debug.Log("Triggered Door");
                //After collect all the goal, the door can be stepped and end game
                if (PhotonNetwork.IsMasterClient && GameManager.Instance.goalCount == 3) { //take th econditional logic out of the character and move it to the Manager
                    GameManager.Instance.CallNextLevel();
                }
                break;

            case "Rock":
            case "Trap":
            case "Monster":
                if (GameManager.Instance.CurrentTurnPlayerNum == PhotonNetwork.LocalPlayer.ActorNumber) {
                    GameManager.Instance.CallStartFight(photonView.ViewID, col.gameObject.GetComponent<PhotonView>().ViewID);
                }
                //CombatSystem.Instance.StartFight(col.gameObject, CombatSystem.FightType.Rock, this);
                break;

            case "VisableArea":
            case "VisibleArea":
                break;

            default:
                Debug.LogFormat("Character Hit Trigger: {0}", col.gameObject.tag);
                break;
        }
    }

    private bool CheckRightGoal(GameObject goal) {
        if (config.type == CharacterConfig.CharacterType.Dwarf  && goal.name == "DwarfGoal") {
            return true;
        }
        else if (config.type == CharacterConfig.CharacterType.Giant && goal.name == "GiantGoal") {
            return true;
        }
        else if (config.type == CharacterConfig.CharacterType.Human && goal.name == "HumanGoal") {
            return true;
        }
        else {
            return false;
        }
    }

    //private void PlayAttackAnimation() {
    //    animator.SetBool("Idle", false);
    //    animator.SetBool("Walk", false);
    //    animator.SetBool("Attack", true);
    //}

   /* private bool CheckMoveCount() {
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1 && moveCount == gameData.dwarfMovecount) {
            return true;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 2 && moveCount == gameData.giantMovecount) {
            return true;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 3 && moveCount == gameData.humanMovecount) {
            return true;
        }
        else {
            return false;
        }
    }*/

    public void Damage(int amount) {
        health -= amount;
        if (CharacterId == GameManager.Instance.CurrentTurnCharacterId) {
            uiManager.UpdateHealthUI(health);
        }
    }
}
