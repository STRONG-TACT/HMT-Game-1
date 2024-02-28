using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;

public class NetworkCharacter : MonoBehaviour
{
    public enum Direction
    {
        Up = 1, Down = 2, Left = 3, Right = 4, Wait = 0
    }

    private Vector3 movePoint;
    private Vector3 prevMovePointPos;
    private Vector3 startPos;

    private float stepLength;

    public int playerId;

    public CharacterConfig config;
    public int CharacterId;
    private bool maskOn;

    public NetworkTile currentTile;

    private Vector3 indicator_offset;
    private float path_indicator_offset;
    public GameObject path_indicator;
    public GameObject indicator;
    public GameObject combat_indicator;
    private Stack<GameObject> combat_indicator_list = new Stack<GameObject>();
    private Stack<GameObject> path_indicator_list = new Stack<GameObject>();
    private List<Vector3> path_indicator_positions = new List<Vector3>();
    public List<Direction> ActionPlan = new List<Direction>();
    // how many moves that the character left in this turn
    //private int actionPointsLeft;

    //This is only used for AI characters
    public Vector2Int pingCursor;
    private int pinsPlaced = 0;
    
    private Vector3 RoundPosition(Vector3 position, float precision)
    {
        float x = Mathf.Round(position.x / precision) * precision;
        float y = Mathf.Round(position.y / precision) * precision;
        float z = Mathf.Round(position.z / precision) * precision;
        return new Vector3(x, y, z);
    }

    public int ActionPointsRemaining {
        get {
            return config.movement - ActionPlan.Count - pinsPlaced;
        }
    }

    public bool ReadyForNextPhase = false;

    public Transform characterMask { get; private set; }
    public Transform visibilityMask { get; private set; }

    //Health
    public int Health { get { return health; } }
    private int health;

    //Death and death round count down
    public bool dead = false;
    public int respawnCountdown = 0;

    private Transform model;
    private Animator animator;
    private CharacterState characterState;

    public enum CharacterState
    {
        Idle, Walking, Attacking, Die
    }


    public CharacterState State
    {
        get { return characterState; }
        set
        {
            if (value != characterState)
            {
                characterState = value;
                if (animator != null) {
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
                        case CharacterState.Die:
                            animator.SetBool("Idle", false);
                            animator.SetBool("Attack", false);
                            animator.SetBool("Walk", false);
                            animator.SetBool("Die", true);
                            break;
                    }
                }
            }
        }
    }


    public bool moving = false;
    
    public void setStartPos(Vector3 newPosition)
    {
        this.transform.position = newPosition;
        startPos = newPosition;
        movePoint = transform.position;
        prevMovePointPos = movePoint;
    }
    
    public void ResetActionPoints() {
        if (dead) {
            ActionPlan.Clear();
            for(int i = 0; i <config.movement; i++) {
                ActionPlan.Add(Direction.Wait);
            }
        }
        else {
            pinsPlaced = 0;
            ActionPlan.Clear();
        }
    }
    
    public void StartPingPhase() {
        pingCursor = Vector2Int.zero;
        ReadyForNextPhase = dead;
    }

    public void PlacePin()
    {
        
    }
    
    private void MaskControl(bool mask)
    {
        if (maskOn == true)
        {
            visibilityMask.gameObject.SetActive(mask);
        }
    }
    
    public void FocusCharacter() {
        MaskControl(true);
        if(NetworkGameManager.S.gameStatus == GameStatus.Player_Planning) {
            indicator.SetActive(true);
            foreach (GameObject one_path_indicator in path_indicator_list)
            {
                one_path_indicator.SetActive(true); 
            }
            foreach (GameObject one_combat_indicator in combat_indicator_list)
            {
                one_combat_indicator.SetActive(true);
            }
        }
    }
    
    public void SetUpConfig(CharacterConfig config, int characterId, NetworkGameData gameData)
    {
        this.config = config;
        this.maskOn = gameData.maskOn;
        CharacterId = characterId;
        path_indicator_offset = gameData.tileSize * 0.15f;
        stepLength = gameData.tileSize + gameData.tileGapLength;
        
        indicator_offset = new Vector3(0.1f, 0.5f, -0.1f) * gameData.tileSize;
        Debug.Log(indicator_offset);
        indicator.transform.position += indicator_offset;
        characterMask = transform.Find("CharacterMask");
        visibilityMask = transform.Find("VisibleMask");
        ResetActionPoints();
        
        Vector3 cellScale = new Vector3(gameData.tileSize + 2 * gameData.tileGapLength,
            0,
            gameData.tileSize + 2 * gameData.tileGapLength);
        Vector3 globalCellScale = new Vector3(cellScale.x / transform.lossyScale.x, cellScale.y / transform.lossyScale.y, cellScale.z / transform.lossyScale.z);

        characterMask.localScale = globalCellScale;
        visibilityMask.localScale = globalCellScale*(config.sightRange*2f+1f-0.1f);
        //characterMask.localScale = cellScale;
        //visibilityMask.localScale = cellScale * config.sightRange;
    }
}
