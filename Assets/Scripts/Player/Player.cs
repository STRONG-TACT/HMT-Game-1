using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Player : MonoBehaviour
{
    PhotonView view;

    [HideInInspector] public float speed;
    [HideInInspector] public int playerId;
    [HideInInspector] public Transform movePoint;
    [HideInInspector] public Vector3 prevMovePointPos;
    public bool[] movable; // detecting walls. index 0: left, 1: right, 2: front, 3: back 
    [HideInInspector] public GameObject wallDetectors;

    [HideInInspector] public int moveCount;

    [HideInInspector] public GameData.CharacterConfig config;
    public static bool changeTurn;

    private Animator animator;

    public CameraManager cameraManager;
    public GameData gameData;

    private bool isReset;

    void Awake()
    {
        view = GetComponent<PhotonView>();
        isReset = false;

    }
    private void Start()
    {
        playerId = GameManager.instance.playerIDs.IndexOf(this.gameObject.GetPhotonView().ViewID);
        speed = 3.0f;
        cameraManager = FindObjectOfType<CameraManager>();
        gameData = FindObjectOfType<GameData>();

        movePoint = this.transform.GetChild(1);
        movePoint.parent = null;
        prevMovePointPos = movePoint.position;

        moveCount = 0;
        changeTurn = false;
        movable = new bool[4] { true, true, true, true};

        animator = this.transform.GetChild(0).GetComponent<Animator>();
        animator.SetBool("Idle", true);

        wallDetectors = this.transform.GetChild(6).gameObject;
    }

    void Update()
    {
        if (view.IsMine && GameManager.instance.turn == PhotonNetwork.LocalPlayer.ActorNumber && !CombatSystem.instance.isInFight && GameManager.instance.isGameStart)
        {
            if (!gameData.differentCameraView)
            {
                PlayerMovement();
            }
            else if(cameraManager.cameraIsSet)
            {
                PlayerMovement();
            }
        }
    }

    private void PlayerMovement()
    {
        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, movePoint.position) == 0f)
        {
            ResetWallDetector();
            wallDetectors.SetActive(true);
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            {
                GameManager.instance.CallMoveLeft( gameData.dwarfMovecount - moveCount);
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
            {
                GameManager.instance.CallMoveLeft(gameData.giantMovecount - moveCount);
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 3)
            {
                GameManager.instance.CallMoveLeft(gameData.humanMovecount - moveCount);
            }

            prevMovePointPos = movePoint.position;
            if (CheckMoveCount()) 
            {
                moveCount = 0;
                changeTurn = true;
            }
            else if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
            {
                if (Input.GetAxisRaw("Horizontal") < 0 && movable[0])  //left
                {
                    movePoint.position += new Vector3(Input.GetAxisRaw("Horizontal") * 3 - gameData.tileGapLength, 0f, 0f);
                    if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 270, 0)) //rotate
                    {
                        this.transform.GetChild(0).rotation = Quaternion.Euler(0, 270, 0);
                    }
                    moveCount++;
                }
                else if(Input.GetAxisRaw("Horizontal") > 0 && movable[1]) //right
                {
                    movePoint.position += new Vector3(Input.GetAxisRaw("Horizontal") * 3 + gameData.tileGapLength, 0f, 0f);
                    if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 90, 0)) //rotate
                    {
                        this.transform.GetChild(0).rotation = Quaternion.Euler(0, 90, 0);
                    }
                    moveCount++;
                }
            }
            else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
            {
                if (Input.GetAxisRaw("Vertical") < 0 && movable[3])  // back
                {
                    movePoint.position += new Vector3(0f, 0f, Input.GetAxisRaw("Vertical") * 3 - gameData.tileGapLength);
                    if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 180, 0)) //rotate
                    {
                        this.transform.GetChild(0).rotation = Quaternion.Euler(0, 180, 0);
                    }
                    moveCount++;
                }
                else if (Input.GetAxisRaw("Vertical") > 0 && movable[2])  // front
                {
                    movePoint.position += new Vector3(0f, 0f, Input.GetAxisRaw("Vertical") * 3 + gameData.tileGapLength);
                    if (this.transform.GetChild(0).rotation != Quaternion.Euler(0, 0, 0)) //rotate
                    {
                        this.transform.GetChild(0).rotation = Quaternion.Euler(0, 0, 0);
                    }
                    moveCount++;
                }
            }
            
            animator.SetBool("Walk", false);
            animator.SetBool("Attack", false);
            animator.SetBool("Idle", true);
        }
        else
        {
            isReset = false;
            wallDetectors.SetActive(false);
            animator.SetBool("Idle", false);
            animator.SetBool("Attack", false);
            animator.SetBool("Walk", true);
        }
    }
    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Goal"))
        {
            //Debug.Log("Triggered Goal");
            if (CheckRightGoal(col.gameObject))
            {
                if (view.IsMine)
                {
                    GameManager.instance.CallGoalCount(PhotonNetwork.LocalPlayer.ActorNumber);
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(col.gameObject);
                }
            }
        }

        if (col.gameObject.CompareTag("Door"))
        {
            //Debug.Log("Triggered Door");
            if (view.IsMine && GameManager.instance.goalCount == 3)  //After collect all the goal, the door can be stepped and end game
            {
                GameManager.instance.CallNextLevel();
            }
        }

        if (col.gameObject.CompareTag("Rock"))
        {
            if (view.IsMine)
            {
                PlayAttactAnimation();
            }
            //Debug.Log("Triggered Rock");
            CombatSystem.instance.isInFight = true;
            CombatSystem.instance.StartFight(col.gameObject, 0, this.gameObject);
        }
        else if (col.gameObject.CompareTag("Trap"))
        {
            if (view.IsMine)
            {
                PlayAttactAnimation();
            }
            //Debug.Log("Triggered Trap");
            CombatSystem.instance.isInFight = true;
            CombatSystem.instance.StartFight(col.gameObject, 1, this.gameObject);
        }
        else if (col.gameObject.CompareTag("Monster"))
        {
            if (view.IsMine)
            {
                PlayAttactAnimation();
            }
            //Debug.Log("Triggered Monster");
            CombatSystem.instance.isInFight = true;
            CombatSystem.instance.StartFight(col.gameObject, 2, this.gameObject);
        }
    }

    private bool CheckRightGoal(GameObject goal)
    {
        if (playerId == 0 && goal.name == "DwarfGoal")
        {
            return true;
        }
        else if (playerId == 1 && goal.name == "GiantGoal")
        {
            return true;
        }
        else if (playerId == 2 && goal.name == "HumanGoal")
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void PlayAttactAnimation()
    {
        animator.SetBool("Idle", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Attack", true);
    }

    private bool CheckMoveCount()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1 && moveCount == gameData.dwarfMovecount) { 
            return true;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 2 && moveCount == gameData.giantMovecount) 
        {
            return true;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 3 && moveCount == gameData.humanMovecount)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void ResetWallDetector()
    {
        if (!isReset)
        {
            isReset = true;
            for (int i = 0; i < 4; i++)
            {
                movable[i] = true;
            }
        }
    }        
}
