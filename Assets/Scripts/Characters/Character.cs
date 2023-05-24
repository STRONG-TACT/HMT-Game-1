using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

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
        private set {
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

    private Transform characterRig;

    //Health
    public int Health { get { return health; } }
    private int health;
    private GameObject[] hearts;
    private GameObject[] brokenHearts;

    

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
        if (photonView.IsMine && 
            GameManager.Instance.turn == PhotonNetwork.LocalPlayer.ActorNumber && 
            !CombatSystem.Instance.isInFight && 
            GameManager.Instance.isFirstLevel) {
            if (!gameData.differentCameraView) {
                PlayerMovement();
            }
            else if (cameraManager.cameraCentered) {
                PlayerMovement();
            }
        }
    }

    private void PlayerMovement() {
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

    public bool Move(Direction direction) {
        Vector3 moveVec = Vector3.zero;
        Quaternion rot = Quaternion.identity;


        switch(direction) {
            case Direction.Up:
                moveVec = Vector3.forward;
                rot = Quaternion.Euler(0, 180, 0);
                break;

            case Direction.Down:
                moveVec = Vector3.back;
                rot = Quaternion.Euler(0, 0, 0);
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

        if (Physics.Raycast(transform.position, moveVec, out RaycastHit hit, gameData.tileSize + gameData.tileGapLength, LayerMask.GetMask("Impassible"))) {
            Debug.Log("Impassible space that direction, preventing move");
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

    public void ResetPosition() {
        movePoint = prevMovePointPos;
    }

    private void OnTriggerEnter(Collider col) {

        switch (col.gameObject.tag) {
            case "Goal":
                //Debug.Log("Triggered Goal");
                if (CheckRightGoal(col.gameObject)) {
                    if (photonView.IsMine) {
                        GameManager.Instance.CallGoalCount(PhotonNetwork.LocalPlayer.ActorNumber);
                    }

                    if (PhotonNetwork.IsMasterClient) {
                        PhotonNetwork.Destroy(col.gameObject);
                    }
                }
                break;

            case "Door":
                //Debug.Log("Triggered Door");
                //After collect all the goal, the door can be stepped and end game
                if (photonView.IsMine && GameManager.Instance.goalCount == 3) { //take th econditional logic out of the character and move it to the Manager
                    GameManager.Instance.CallNextLevel();
                }
                break;

            case "Rock":
                if (photonView.IsMine) {
                    PlayAttackAnimation();
                }
                //Debug.Log("Triggered Rock");
                //CombatSystem.Instance.isInFight = true;
                CombatSystem.Instance.StartFight(col.gameObject, CombatSystem.FightType.Rock, this);
                break;

            case "Trap":
                if (photonView.IsMine) {
                    PlayAttackAnimation();
                }
                //Debug.Log("Triggered Trap");
                //CombatSystem.Instance.isInFight = true;
                CombatSystem.Instance.StartFight(col.gameObject, CombatSystem.FightType.Trap, this);
                break;

            case "Monster":
                if (photonView.IsMine) {
                    PlayAttackAnimation();
                }
                //Debug.Log("Triggered Monster");
                //CombatSystem.Instance.isInFight = true;
                CombatSystem.Instance.StartFight(col.gameObject, CombatSystem.FightType.Monster, this);
                break;

            default:
                Debug.LogFormat("Triggered: {0}", col.gameObject.tag);
                break;
        }
    }

    private bool CheckRightGoal(GameObject goal) {
        if (playerId == 0 && goal.name == "DwarfGoal") {
            return true;
        }
        else if (playerId == 1 && goal.name == "GiantGoal") {
            return true;
        }
        else if (playerId == 2 && goal.name == "HumanGoal") {
            return true;
        }
        else {
            return false;
        }
    }

    private void PlayAttackAnimation() {
        animator.SetBool("Idle", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Attack", true);
    }

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
        uiManager.UpdateHealthUI(health);
    }
}
