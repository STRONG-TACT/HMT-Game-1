using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;
using System.Linq;

public class Monster : MonoBehaviour
{
    private static Dictionary<string, int> MONSTER_IDS_BY_OBJ_KEY = new Dictionary<string, int>();
    private static string GetObjID(string objKey) {
        if (!MONSTER_IDS_BY_OBJ_KEY.ContainsKey(objKey)) {
            MONSTER_IDS_BY_OBJ_KEY[objKey] = 0;
        }
        MONSTER_IDS_BY_OBJ_KEY[objKey]++;
        return objKey + MONSTER_IDS_BY_OBJ_KEY[objKey];
    }


    public string ObjKey {
        get {
            return _objKey;
        }
        set {
            if (_objKey == null) { 
                _objKey = value;
                HMTObjID = GetObjID(_objKey);
            }
        }
    }

    private string _objKey = null;
    public string HMTObjID { get; private set; } = null;

    public MonsterConfig config;
    public int monsterId;
    public GameData gameData;
    public Sprite icon;

    private Vector3 movePoint;
    private Vector3 prevMovePointPos;

    public bool moving = false;
    public int MovesLeftThisTurn { get; private set; } = 0;

    private float stepLength;

    public Tile currentTile;

    private Transform model;
    private Animator animator;
    private CharacterState characterState;

    private List<Character.Direction> movementPlan = new List<Character.Direction>();
    private bool currentDirection = false;

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
                switch (value)
                {
                    case CharacterState.Idle:
                        animator.SetBool("Idle", true);
                        animator.SetBool("Attacking", false);
                        animator.SetBool("Walk", false);
                        break;
                    case CharacterState.Walking:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attacking", false);
                        animator.SetBool("Walk", true);
                        break;
                    case CharacterState.Attacking:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attacking", true);
                        animator.SetBool("Walk", false);
                        break;
                    case CharacterState.Die:
                        animator.SetBool("Idle", false);
                        animator.SetBool("Attacking", false);
                        animator.SetBool("Walk", false);
                        animator.SetBool("Die", true);
                        break;
                }
            }
        }
    }
    
    void Start()
    {
        movePoint = transform.position;
        prevMovePointPos = movePoint;
        animator = GetComponentInChildren<Animator>();
        animator.SetBool("Idle", true);
        State = CharacterState.Idle;

        model = transform.Find("Model");
        if (model == null) {
            Debug.LogErrorFormat("Character {0} has no Model child object", gameObject.name);
        }

        //State = CharacterState.Die;
        animator.speed = Random.Range(0.8f, 1.2f); // Randomize speed within a range
    }
    
    public void SetUpConfig(MonsterConfig config, int MonsterId, GameData data, string code)
    {
        this.config = config;
        monsterId = MonsterId;

        gameData = data;

        stepLength = data.tileSize + data.tileGapLength;
        ObjKey = code;

        if (config.RandomizeInitialDirection) {
            currentDirection = NetworkMiddleware.Instance.NextRandom() > 0.5f;
        }
        else {
            currentDirection = true;
        }
    }
    
    public void MonsterTurnStart() {
        MovesLeftThisTurn = config.movement;
    }

    public List<Character.Direction > PlanNextMove() {
        movementPlan = new List<Character.Direction >();
        if (MovesLeftThisTurn <=0) {
            movementPlan.Add(Character.Direction.Wait);
        }
        else {
            switch (config.movementStyle) {
                case MonsterConfig.MovementStyle.Static:
                    movementPlan.Add(Character.Direction.Wait);
                    break;
                case MonsterConfig.MovementStyle.Horizontal:
                    if (currentDirection) {
                        if (CheckMove(Character.Direction.Right)) {
                            movementPlan.Add(Character.Direction.Right);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                        else {
                            movementPlan.Add(Character.Direction.Left);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                    }
                    else {
                        if (CheckMove(Character.Direction.Left)) {
                            movementPlan.Add(Character.Direction.Left);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                        else {
                            movementPlan.Add(Character.Direction.Right);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                    }
                    break;
                case MonsterConfig.MovementStyle.Vertical:
                    if (currentDirection) {
                        if (CheckMove(Character.Direction.Up)) {
                            movementPlan.Add(Character.Direction.Up);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                        else {
                            movementPlan.Add(Character.Direction.Down);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                    }
                    else {
                        if (CheckMove(Character.Direction.Down)) {
                            movementPlan.Add(Character.Direction.Down);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                        else {
                            movementPlan.Add(Character.Direction.Up);
                            movementPlan.Add(Character.Direction.Wait);
                        }
                    }
                    break;

                case MonsterConfig.MovementStyle.RandomWalk:
                    movementPlan = new List<Character.Direction> {
                        Character.Direction.Up, 
                        Character.Direction.Down,
                        Character.Direction.Left,
                        Character.Direction.Right };
                    movementPlan = movementPlan.Where(x => CheckMove(x)).OrderBy(x => NetworkMiddleware.Instance.NextRandom()).ToList();
                    movementPlan.Add(Character.Direction.Wait);
                    break;
            }
        }
        return movementPlan;
    }

    public Character.Direction NextMove() {
        if (movementPlan.Count > 0) {
            return movementPlan[0];
        }
        return Character.Direction.Wait;
    }

    public Vector2Int NextMoveCoordinates() {
        Vector2Int nextMove = new Vector2Int(currentTile.col, currentTile.row);
        if (movementPlan.Count > 0) {
            switch (movementPlan[0]) {
                case Character.Direction.Up:
                    nextMove += Vector2Int.up;
                    break;
                case Character.Direction.Down:
                    nextMove += Vector2Int.down;
                    break;
                case Character.Direction.Left:
                    nextMove += Vector2Int.left;
                    break;
                case Character.Direction.Right:
                    nextMove += Vector2Int.right;
                    break;
            }
        }
        return nextMove;
    }

    public void ClearPlanMove()
    {
        //moving = false;
        movementPlan.Clear();
        MovesLeftThisTurn = 0;
    }

    public void PopPlanMove() {
        if (movementPlan.Count > 0) {
            movementPlan.RemoveAt(0);
        }
    }
    
    public IEnumerator TakeNextMove(float stepTime) {
        if(MovesLeftThisTurn <= 0) {
            yield break;
        }
        moving = true;
        prevMovePointPos = transform.position;

        Vector3 moveVec = movementPlan[0] switch {
            Character.Direction.Up => Vector3.forward,
            Character.Direction.Down => Vector3.back,
            Character.Direction.Left => Vector3.left,
            Character.Direction.Right => Vector3.right,
            _ => Vector3.zero
        };

        float timeStart = Time.time;
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
            MovesLeftThisTurn -= 1;
            if (MovesLeftThisTurn <= 0) {
                Debug.Log(string.Format("monsterID: {0}, turnFinished", monsterId));
            }
            Debug.Log(string.Format("monsterID: {0}, direction: {1}, StartingActionPoints: {2}, count: {3}", monsterId, movementPlan[0], config.movement, MovesLeftThisTurn));
        }
        State = CharacterState.Idle;
        moving = false;
        yield break;
    }
    
    public IEnumerator moveToTargetLocation(Vector3 target, float stepTime)
    {
        float timeStart = Time.time;

        State = CharacterState.Walking;
        Vector3 origin = transform.position;
        //Vector3 target = transform.position + moveVec * stepLength;
        Vector3 direction = target - origin;
        // Normalize the direction
        direction.Normalize();
        if (direction != Vector3.zero)
        {
            // Create a rotation that looks in the direction of StartingActionPoints
            moving = true;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            model.rotation = targetRotation;
            while (Time.time - timeStart < stepTime)
            {
                float t = (Time.time - timeStart) / stepTime;
                transform.position = Vector3.Lerp(origin, target, t);
                yield return null;
            }
            transform.position = target;
            model.rotation = targetRotation;
        }
        State = CharacterState.Idle;
        moving = false;
    }

    private void OnDestroy() {
        currentTile.RemoveMonster(this);
    }

    public void Kill() {
        Kill(GameManager.Instance.excecutionStepTime);
    }

    public void Kill(float stepTime) {
        State = CharacterState.Die;
        CompetitionMiddleware.Instance.LogChallengeDeath(ObjKey, currentTile.col, currentTile.row);
        StartCoroutine(KillCoroutine(stepTime));
    }

    IEnumerator KillCoroutine(float stepTime) {
        //This code existed in the old MonsterDie function and might be relevant to bring back.
        //AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        //float animationLength = stateInfo.length;
        //yield return new WaitForSeconds(animationLength - 2f);
        yield return new WaitForSeconds(stepTime);
        Destroy(this.gameObject);
        yield break;
    }
    
    /*
    public void Retreat()
    {
        transform.position = prevMovePointPos;
        movePoint = prevMovePointPos;
    }
    */

    public IEnumerator Retreat()
    {
        yield return StartCoroutine(moveToTargetLocation(prevMovePointPos, GameManager.Instance.excecutionStepTime));
        movePoint = prevMovePointPos;
    }

    public bool CheckMove(Character.Direction direction)
    {
        Vector3 moveVec = direction switch
        {
            Character.Direction.Up => Vector3.forward,
            Character.Direction.Down => Vector3.back,
            Character.Direction.Left => Vector3.left,
            Character.Direction.Right => Vector3.right,
            _ => Vector3.forward
        };
        //bool passible = true;
        bool passible = false;
        RaycastHit hit;
        if (Physics.Raycast(this.transform.position, moveVec, out hit, stepLength))
        //if (Physics.Raycast(this.transform.position, moveVec, out hit, stepLength, LayerMask.GetMask("Impassible")))
        {
            if (hit.collider.gameObject.tag == "Rock" || hit.collider.gameObject.tag == "Trap")
            {
                passible = false;
            }
            else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Impassible") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Boundary"))
            {
                passible = false;
            }
            else {
                passible = true;
            }

        }

        //return !Physics.Raycast(this.transform.position, moveVec, stepLength, LayerMask.GetMask("Impassible"));
        return passible;
    }

    public JObject HMTStateRep() {
        return new JObject {
            {"entityType","monster" },
            {"objKey", ObjKey },
            {"id", HMTObjID },
            //{"type", config.configName },
            //{"actionPoints", config.movement },
            {"challengeDie", config.combatDice.ToString() },
            {"x", currentTile.col },
            {"y", currentTile.row }
        };
    }

    public JObject LogStateRep() {
        return new JObject {
            {"entityType","monster" },
            {"objKey", ObjKey },
            {"id", HMTObjID },
            {"type", config.configName },
            {"moveStyle", config.movementStyle.ToString() },
            {"currDirection", currentDirection.ToString() },
            {"actionPoints", MovesLeftThisTurn },
            {"challengeDie", config.combatDice.ToString() },
            {"x", currentTile.col },
            {"y", currentTile.row }
        };
    }
}
