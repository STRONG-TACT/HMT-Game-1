using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class LocalCharacter : MonoBehaviour
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

    public LocalTile currentTile;

    public GameObject indicator;
    public List<Direction> ActionPlan = new List<Direction>();
    // how many moves that the character left in this turn
    //private int actionPointsLeft;

    //This is only used for AI characters
    public Vector2Int pingCursor;
    private int pinsPlaced = 0;


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

    void Start()
    {
        movePoint = transform.position;
        prevMovePointPos = movePoint;
        characterMask = transform.Find("CharacterMask");
        visibilityMask = transform.Find("VisibleMask");
        health = 3;

        model = transform.Find("Model");
        if(model == null) {
            Debug.LogErrorFormat("Character {0} has no Model child object", gameObject.name);
        }

        animator = GetComponentInChildren<Animator>();
        animator.SetBool("Idle", true);
        State = CharacterState.Idle;
        //State = CharacterState.Walking;
    }

    public void SetUpConfig(CharacterConfig config, int characterId, LocalGameData gameData)
    {
        this.config = config;
        this.maskOn = gameData.maskOn;
        CharacterId = characterId;

        stepLength = gameData.tileSize + gameData.tileGapLength;

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

    public void setStartPos(Vector3 newPosition)
    {
        this.transform.position = newPosition;
        startPos = newPosition;
        movePoint = transform.position;
        prevMovePointPos = movePoint;
    }

    public void PlacePin() {
        pinsPlaced += 1;
        pingCursor = Vector2Int.zero;
        if (ActionPointsRemaining == 0) {
            ReadyForNextPhase = true;
        }
    }

    public void FocusCharacter() {
        MaskControl(true);
        if(LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Planning) {
            indicator.SetActive(true);
        }
    }

    public void UnFocusCharacter() {
        MaskControl(false);
        indicator.SetActive(false);
    }

    public void StartPingPhase() {
        pingCursor = Vector2Int.zero;
        ReadyForNextPhase = dead;
    }

    public void EndPingPhase() {
        pingCursor = Vector2Int.zero;
    }

    public bool AddActionToPlan(Direction direction)
    {
        if (ActionPointsRemaining <= 0) return false;

        Vector3 moveVec = direction switch {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.zero,
        };

        indicator.transform.position += moveVec * stepLength;
        ActionPlan.Add(direction);
        return true;
    }

    public void ResetPlan() {
        ActionPlan.Clear();
    }

    public bool CheckMove(Direction direction) {
        if (direction == Direction.Wait) return true;

        Vector3 moveVec = direction switch {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.forward
        };

        return !Physics.Raycast(indicator.transform.position, moveVec, stepLength, LayerMask.GetMask("Impassible"));
    }

    public void StartPlanningPhase() {
        ResetPlan();
        ReadyForNextPhase = ActionPointsRemaining == 0 || dead;
    }

    public void EndPlanning() {
        indicator.transform.position = this.transform.position;
        indicator.SetActive(false);
    }

    
    public bool UndoPlanStep() {
        if (ReadyForNextPhase) {
            return false;
        }
        Direction lastMove = ActionPlan[ActionPlan.Count - 1];
        ActionPlan.RemoveAt(ActionPlan.Count - 1);
        Vector3 moveVec = lastMove switch {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.zero
        };
        indicator.transform.position += -moveVec * stepLength;

        // TODO: Think about the best way to call ui manager
        FindObjectOfType<LocalUIManager>().UpdateActionPointsRemaining(ActionPointsRemaining);
        return true;
    }

    public IEnumerator TakeNextMove(float stepTime) {
        if(ActionPlan.Count == 0) {
            yield break;
        }
        moving = true;
        float timeStart = Time.time;
        Direction nextMove = ActionPlan[0];
        ActionPlan.RemoveAt(0);
        Vector3 moveVec = nextMove switch {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.zero
        };
        if (moveVec != Vector3.zero) {
            State = CharacterState.Walking;

            Vector3 origin = transform.position;
            Vector3 target = transform.position + moveVec * stepLength;
            Quaternion targetRotation = Quaternion.LookRotation(moveVec, Vector3.up);
            while (Time.time - timeStart < stepTime) {
                float t = (Time.time - timeStart) / stepTime;
                transform.position = Vector3.Lerp(origin, target, t);
                model.rotation = Quaternion.Slerp(model.rotation, targetRotation, t);
                yield return null;
            }
            transform.position = target;
            model.rotation = targetRotation;
        }
        State = CharacterState.Idle;
        moving = false;
    }

    public void Retreat()
    {
        this.transform.position = prevMovePointPos;
        movePoint = prevMovePointPos;
    }

    private void MaskControl(bool mask)
    {
        if (maskOn == true)
        {
            visibilityMask.gameObject.SetActive(mask);
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        //Debug.LogFormat("Character Hit Trigger: {0}", col.gameObject.tag);
        if (col.gameObject.tag == "Goal")
        {
            //Debug.Log("Triggered Goal");
            LocalShrine shrine = col.gameObject.GetComponent<LocalShrine>();
            if (shrine != null && shrine.CheckShrineType(this))
            {
                LocalGameManager.Instance.GoalReached(CharacterId);
            }
        }
    }

    private void OnTriggerStay(Collider col)
    {
        if (col.gameObject.tag == "Door")
        {
            if (LocalGameManager.Instance.goalCount == 3)
            { //take th econditional logic out of the character and move it to the Manager
                LocalGameManager.Instance.NextLevel();
            }
        }
    }

    public void DecrementHealth()
    {
        health -= 1;
        Debug.Log(string.Format("Character {0} health: {1}", config.characterName, health));

        if (health == 0)
        {
            Debug.Log(string.Format("Character {0} Died!", config.characterName));
            StartCoroutine(characterDeath());
        }
    }


    public IEnumerator characterDeath() {
        State = CharacterState.Die;
        float animationLength = 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        animationLength = stateInfo.length;

        // Wait for the duration of the animation
        yield return new WaitForSeconds(animationLength);
        State = CharacterState.Idle;

        dead = true;
        respawnCountdown = 2;
        this.gameObject.SetActive(false);
        this.transform.position = startPos;

        movePoint = startPos;
        prevMovePointPos = movePoint;
    }

    public void RespawnCountdown()
    {
        respawnCountdown -= 1;

        if (respawnCountdown == 0)
        {
            dead = false;
            this.gameObject.SetActive(true);
            health = 3;
        }
    }

    public void QuickRespawn()
    {
        ActionPlan.Clear();
        State = CharacterState.Idle;
        respawnCountdown = 0;
        dead = false;
        this.gameObject.SetActive(true);
        health = 3;
    }

    public bool MovePingCusor(string direction) {
        if(ActionPointsRemaining <= 0) {
            return false;
        }
        else {
            switch(direction) {
                case "up":
                    pingCursor += Vector2Int.up;
                    break;
                case "down":
                    pingCursor += Vector2Int.down;
                    break;
                case "left":
                    pingCursor += Vector2Int.left;
                    break;
                case "right":
                    pingCursor += Vector2Int.right;
                    break;
                default:
                    return false;
            }
            return true;
        }

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

    public JObject HMTStateRep() {
        return new JObject {
            {"name", config.characterName},
            {"characterId", CharacterId },
            {"type", config.type.ToString()},
            {"sightRange", config.sightRange },
            {"monsterDice", config.monsterDice.ToString()},
            {"trapDice", config.trapDice.ToString() },
            {"stoneDice", config.stoneDice.ToString() },
            {"health", health},
            {"dead", dead},
            {"actionPoints", ActionPointsRemaining},
            {"actionPlan", new JArray(ActionPlan.Select(d => d.ToString())) }
        };
    }
}
