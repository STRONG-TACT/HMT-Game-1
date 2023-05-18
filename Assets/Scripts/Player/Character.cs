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
    public Transform movePoint;
    public Vector3 prevMovePointPos;
    public Dictionary<Direction, bool> movable;

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

    public GameObject wallDetectors;

    public int moveCount;

    public GameData.CharacterConfig config;

    //Health
    public int Health { get { return health; } }
    private int health;
    private GameObject[] hearts;
    private GameObject[] brokenHearts;

    private Animator animator;

    public CameraManager cameraManager;
    private UIManager uiManager;
    public GameData gameData;

    private bool isReset;

    void Awake() {
        photonView = GetComponent<PhotonView>();
        isReset = false;

    }
    private void Start() {

        speed = 3.0f;
        cameraManager = FindObjectOfType<CameraManager>();
        gameData = FindObjectOfType<GameData>();
        uiManager = FindObjectOfType<UIManager>();

        movePoint = transform.Find("Character Move Point");
        movePoint.parent = null;
        prevMovePointPos = movePoint.position;

        moveCount = 0;
        movable = new Dictionary<Direction, bool> { { Direction.Left, true }, { Direction.Right, true }, { Direction.Up, true }, { Direction.Down, true } };
        //    new bool[4] { true, true, true, true};

        animator = GetComponentInChildren<Animator>();
        animator.SetBool("Idle", true);
        State = CharacterState.Idle;
        wallDetectors = transform.Find("WallDetector").gameObject;

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
                transform.position = Vector3.MoveTowards(transform.position, movePoint.position, speed * Time.deltaTime);
                if (Vector3.Distance(transform.position, movePoint.position) == 0f) {
                    //ResetWallDetector();
                    //wallDetectors.SetActive(true);
                    GameManager.Instance.CallUpdateActionPoints(config.movement - moveCount);

                    prevMovePointPos = movePoint.position;
                    if (moveCount == config.movement) {
                        moveCount = 0;
                        GameManager.Instance.EndTurn();
                    }
                    State = CharacterState.Idle;
                }
                else {
                    isReset = false;
                    //wallDetectors.SetActive(false);
                }
                break;
            case CharacterState.Idle:
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f) {
                    if (Input.GetAxisRaw("Horizontal") < 0 && movable[Direction.Left]) {
                        Move(Direction.Left);
                    }
                    else if (Input.GetAxisRaw("Horizontal") > 0 && movable[Direction.Right]) {
                        Move(Direction.Right);
                    }
                }
                //vertical move
                else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f) {
                    if (Input.GetAxisRaw("Vertical") > 0 && movable[Direction.Up]) {
                        Move(Direction.Up);
                    }
                    else if (Input.GetAxisRaw("Vertical") < 0 && movable[Direction.Down]) {
                        Move(Direction.Down);
                    }
                }
                break;

            case CharacterState.Attacking:
                //if we're in a fight then roll a die if we can
                break;

            default: break;
        }
    }

    private void Move(Direction direction) {
        switch(direction) {
            case Direction.Up:
                movePoint.position += Vector3.forward * (gameData.tileSize + gameData.tileGapLength);

                if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 180, 0)) {
                    this.transform.GetChild(0).rotation = Quaternion.Euler(0, 180, 0);
                }
                moveCount++;
                State = CharacterState.Walking;
                break;
            case Direction.Down:
                movePoint.position += Vector3.back * (gameData.tileSize + gameData.tileGapLength);
                if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 0, 0)) {
                    this.transform.GetChild(0).rotation = Quaternion.Euler(0, 0, 0);
                }
                moveCount++;
                State = CharacterState.Walking;
                break;
            case Direction.Left:
                movePoint.position += Vector3.left * (gameData.tileSize + gameData.tileGapLength);
                if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 270, 0)) {
                    this.transform.GetChild(0).rotation = Quaternion.Euler(0, 270, 0);
                }
                moveCount++;
                State = CharacterState.Walking;
                break;
            case Direction.Right:
                movePoint.position += Vector3.right * (gameData.tileSize + gameData.tileGapLength);
                if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 90, 0)) {
                    this.transform.GetChild(0).rotation = Quaternion.Euler(0, 90, 0);
                }
                moveCount++;
                State = CharacterState.Walking;
                break;

        }
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

    private void ResetWallDetector() {
        if (!isReset) {
            isReset = true;
            movable[Direction.Left] = true;
            movable[Direction.Right] = true;
            movable[Direction.Up] = true;
            movable[Direction.Down] = true;
        }
    }

    public void Damage(int amount) {
        health -= amount;
        uiManager.UpdateHealthUI(health);
    }
}
