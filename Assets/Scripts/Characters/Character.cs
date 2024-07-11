using GameConstant;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 4/5/24 refactor change this older version of character to a integrated version
/// used by both local and network version of the game
/// </summary>
public class Character : MonoBehaviour {


    public enum Direction
    {
        Up = 1, Down = 2, Left = 3, Right = 4, Wait = 0
    }
    
    public enum CharacterState
    {
        Idle, Walking, Attacking, Die, Cheering
    }

    private Vector3 movePoint;
    private Vector3 prevMovePointPos;
    private Vector3 startPos;

    private float stepLength;

    public int playerId;

    public CharacterConfig config;
    public int CharacterId { get; private set; }
    public Tile currentTile;

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
    public bool moving = false;

    public bool Focused { get; private set; } = false;

    //This is only used for AI characters
    public Vector2Int pingCursor;
    private int pinsPlaced = 0;
    
    public bool ReadyForNextPhase = false;


    public Transform characterMask { get; private set; }
    public Transform visibilityMask { get; private set; }

    //Health
    public int Health { get; private set; } = 3;
    public int Lives { get; private set; } = 3;
    public int Deaths { get; private set; } = 0;

    //Death and death currRound count down
    public bool dead = false;
    public int respawnCountdown = 0;

    private Transform model;
    private Animator animator;
    private CharacterState characterState;
    
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
                            animator.SetBool("Cheer", false);
                            animator.SetBool("Die", false);
                            break;
                        case CharacterState.Walking:
                            animator.SetBool("Idle", false);
                            animator.SetBool("Attack", false);
                            animator.SetBool("Walk", true);
                            animator.SetBool("Cheer", false);
                            animator.SetBool("Die", false);
                            break;
                        case CharacterState.Attacking:
                            animator.SetBool("Idle", false);
                            animator.SetBool("Attack", true);
                            animator.SetBool("Walk", false);
                            animator.SetBool("Cheer", false);
                            animator.SetBool("Die", false);
                            break;
                        case CharacterState.Cheering:
                            animator.SetBool("Idle", false);
                            animator.SetBool("Attack", false);
                            animator.SetBool("Walk", false);
                            animator.SetBool("Cheer", true);
                            animator.SetBool("Die", false);
                            break;
                        case CharacterState.Die:
                            animator.SetBool("Idle", false);
                            animator.SetBool("Attack", false);
                            animator.SetBool("Walk", false);
                            animator.SetBool("Cheer", false);
                            animator.SetBool("Die", true);
                            break;
                    }
                }
            }
        }
    }
    
    private Vector3 RoundPosition(Vector3 position, float precision)
    {
        float x = Mathf.Round(position.x / precision) * precision;
        float y = Mathf.Round(position.y / precision) * precision;
        float z = Mathf.Round(position.z / precision) * precision;
        return new Vector3(x, y, z);
    }

    public int ActionPointsRemaining {
        get {
            return config.StartingActionPoints - ActionPlan.Count - pinsPlaced;
        }
    }

    #region Setup

    private void Start()
    {
        movePoint = transform.position;
        prevMovePointPos = movePoint;
        characterMask = transform.Find("CharacterMask");
        visibilityMask = transform.Find("VisibleMask");
        
        
        model = transform.Find("Model");
        if(model == null) {
            Debug.LogErrorFormat("Character {0} has no Model child object", gameObject.name);
        }
        
        animator = GetComponentInChildren<Animator>();
        animator.SetBool("Idle", true);
        State = CharacterState.Idle;
    }
    
    public void SetStartPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
        startPos = newPosition;
        movePoint = transform.position;
        prevMovePointPos = movePoint;
    }

    public void SetUpConfig(CharacterConfig config, int characterId, int lives) {
        this.config = config;
        CharacterId = characterId;
        GameData gameData = GameData.S;
        path_indicator_offset = gameData.tileSize * 0.15f;
        stepLength = gameData.tileSize + gameData.tileGapLength;
        Health = config.StartingHealth;
        Lives = lives;

        indicator_offset = new Vector3(0.1f, 0.5f, -0.1f) * gameData.tileSize;
        //Debug.Log(indicator_offset);
        indicator.transform.position += indicator_offset;
        characterMask = transform.Find("CharacterMask");
        visibilityMask = transform.Find("VisibleMask");
        ResetActionPoints();

        float maskScale = gameData.tileSize * (config.sightRange * 2f + 1f) + gameData.tileGapLength * (config.sightRange * 2f);
        characterMask.localScale = new Vector3(gameData.tileSize, 0, gameData.tileSize);
        visibilityMask.localScale = new Vector3(maskScale, 0, maskScale);
        //characterMask.localScale = cellScale;
        //visibilityMask.localScale = cellScale * config.sightRange;
    }

    #endregion

    public void ResetActionPoints() {
        if (dead) {
            ActionPlan.Clear();
            for(int i = 0; i <config.StartingActionPoints; i++) {
                ActionPlan.Add(Direction.Wait);
            }
        }
        else {
            pinsPlaced = 0;
            ActionPlan.Clear();
        }

        moving = false;
    }

    #region Focus Management

    public void FocusCharacter() {
        //MaskControl(true);
        Focused = true;
        IntegratedMapGenerator.Instance.UpdateFOWVisuals();
        if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning) {
            indicator.SetActive(true);
            foreach (GameObject one_path_indicator in path_indicator_list) {
                one_path_indicator.SetActive(true);
            }
            foreach (GameObject one_combat_indicator in combat_indicator_list) {
                one_combat_indicator.SetActive(true);
            }
        }
    }

    public void UnFocusCharacter() {
        //MaskControl(false);
        Focused = false;
        indicator.SetActive(false);
        foreach (GameObject one_path_indicator in path_indicator_list) {
            one_path_indicator.SetActive(false);
        }
        foreach (GameObject one_combat_indicator in combat_indicator_list) {
            one_combat_indicator.SetActive(false);
        }
    }

    #endregion

    #region Pinning Phase

    public void StartPingPhase() {
        pingCursor = Vector2Int.zero;
        if (dead) {
            RespawnCheck();
        }
        NetworkMiddleware.S.CallReadyForNextPhase(CharacterId, dead);
    }
    
    public void EndPingPhase() {
        pingCursor = Vector2Int.zero;
        ReadyForNextPhase = false;
    }

    public bool MovePingCursor(Character.Direction direction) {
        if (ActionPointsRemaining <= 0) {
            return false;
        }
        else {
            switch (direction) {
                case Character.Direction.Up:
                    if (pingCursor.y < IntegratedMapGenerator.Instance.Map.GetLength(0)) {
                        pingCursor += Vector2Int.up;
                    }
                    break;
                case Character.Direction.Down:
                    if (pingCursor.y > 0) {
                        pingCursor += Vector2Int.down;
                    }
                    break;
                case Character.Direction.Left:
                    if (pingCursor.x > 0) {
                        pingCursor += Vector2Int.left;
                    }
                    break;
                case Character.Direction.Right:
                    if (pingCursor.x < IntegratedMapGenerator.Instance.Map.GetLength(1)) {
                        pingCursor += Vector2Int.right;
                    }
                    break;
                case Character.Direction.Wait:
                    return false;
            }
            return true;
        }
    }

    public void PinPlaced() {
        pinsPlaced += 1;
        pingCursor = Vector2Int.zero;
        if (ActionPointsRemaining == 0) {
            NetworkMiddleware.S.CallReadyForNextPhase(CharacterId, true);
        }
    }

    #endregion

    #region Planning Phase

    public void ResetPlan() {
        ActionPlan.Clear();
    }
    
    public void StartPlanningPhase() {
        ResetPlan();
       
        NetworkMiddleware.S.CallReadyForNextPhaseAuto(CharacterId, ActionPointsRemaining == 0 || dead);
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
        RaycastHit hit;
        if (Physics.Raycast(indicator.transform.position, moveVec, out hit, stepLength, LayerMask.GetMask("Impassible") | LayerMask.GetMask("Boundary")))
        {
            Tile getTile = hit.collider.gameObject.GetComponent<Tile>();
            if (getTile != null && getTile.fogOfWarDictionary[CharacterId] == Tile.FogOfWarState.Unseen)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }
        return true;
        //return !Physics.Raycast(indicator.transform.position, moveVec, stepLength, LayerMask.GetMask("Impassible"));
    }
    
    public void AddActionToPlan(Direction direction)
    {
        if (ActionPointsRemaining <= 0 || !CheckMove(direction)) 
            return;

        Vector3 moveVec = direction switch {
            Direction.Up => Vector3.forward,
            Direction.Down => Vector3.back,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            _ => Vector3.zero,
        };
        if (direction != Direction.Wait)
        {

            Vector3 old_indicator_position = indicator.transform.position;
            indicator.transform.position += moveVec * stepLength;
            Vector3 midpoint = (old_indicator_position + indicator.transform.position) / 2 - indicator_offset;
            Vector3 path_indicator_direction = (indicator.transform.position - old_indicator_position).normalized;
            midpoint = RoundPosition(midpoint, 0.001f);
            //midpoint -= (Vector3.Cross(path_indicator_direction, Vector3.up).normalized) * 0.4f*path_indicator_offset;
            //midpoint += (Vector3.Cross(path_indicator_direction, Vector3.back).normalized)  * path_indicator_offset;
            while (path_indicator_positions.Contains(midpoint))
            {
                midpoint -= (Vector3.Cross(path_indicator_direction, Vector3.up).normalized) * path_indicator_offset;
            }

            RaycastHit hit;
            // Raycast downwards from the indicator's position
            if (Physics.Raycast(indicator.transform.position, -Vector3.up, out hit))
            {
                if (hit.collider.gameObject.tag == "Monster" )
                {
                    //if the tile is visible to player, drop a combat indicator
                    if (hit.collider.gameObject.GetComponent<Monster>().currentTile.fogOfWarDictionary[CharacterId] == Tile.FogOfWarState.Visible)
                    {
                        Vector3 combat_indicator_position = indicator.transform.position + indicator_offset;
                        GameObject new_combat_indicator = Instantiate(combat_indicator, combat_indicator_position, Quaternion.identity);
                        new_combat_indicator.SetActive(Focused);
                        combat_indicator_list.Push(new_combat_indicator);
                    }

                }
                if (hit.collider.gameObject.tag == "Trap" || hit.collider.gameObject.tag == "Rock") {
                    Debug.Log("Trap or Rock");
                    if (hit.collider.gameObject.GetComponent<Tile>().fogOfWarDictionary[CharacterId] == Tile.FogOfWarState.Visible) {
                        Vector3 combat_indicator_position = indicator.transform.position + indicator_offset;
                        GameObject new_combat_indicator = Instantiate(combat_indicator, combat_indicator_position, Quaternion.identity);
                        new_combat_indicator.SetActive(Focused);
                        combat_indicator_list.Push(new_combat_indicator);
                    }
                }

            }

            path_indicator_positions.Add(midpoint);
            GameObject new_path_indicator = Instantiate(path_indicator, midpoint, Quaternion.LookRotation(path_indicator_direction));

            //new_path_indicator.transform.Rotate(0, -180, 0);
            new_path_indicator.transform.position = midpoint;
            new_path_indicator.SetActive(Focused);
            path_indicator_list.Push(new_path_indicator);
        }

        ActionPlan.Add(direction);
    }
    
    public void UndoPlanStep() {
        if (ReadyForNextPhase || ActionPlan.Count == 0) {
            return;
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
        
        if (lastMove != Character.Direction.Wait)
        {
            GameObject one_path_indicator = path_indicator_list.Pop();
            //Vector3 one_path_indicator_position = RoundPosition(one_path_indicator.transform.position, 0.001f);
            path_indicator_positions.Remove(one_path_indicator.transform.position);
            Destroy(one_path_indicator);
            RaycastHit hit;
            if (Physics.Raycast(indicator.transform.position, -Vector3.up, out hit))
            {
                if (hit.collider.gameObject.tag == "Monster")
                {
                    if (hit.collider.gameObject.GetComponent<Monster>().currentTile.fogOfWarDictionary[CharacterId] == Tile.FogOfWarState.Visible)
                    {
                        GameObject one_combat_indicator = combat_indicator_list.Pop();
                        Destroy(one_combat_indicator);
                    }
                }
                if (hit.collider.gameObject.tag == "Trap" || hit.collider.gameObject.tag == "Rock")
                {
                    if (hit.collider.gameObject.GetComponent<Tile>().fogOfWarDictionary[CharacterId] == Tile.FogOfWarState.Visible)
                    {
                        GameObject one_combat_indicator = combat_indicator_list.Pop();
                        Destroy(one_combat_indicator);
                    }
                }
            }
        }
        indicator.transform.position += -moveVec * stepLength;
    }
    
    public void EndPlanning() {
        indicator.transform.position = transform.position;
        indicator.transform.position += indicator_offset;
        indicator.SetActive(false);
        while (path_indicator_list.Count > 0)
        {
            GameObject one_path_indicator = path_indicator_list.Pop(); 
            Destroy(one_path_indicator); 
        }
        while (combat_indicator_list.Count > 0)
        {
            GameObject one_combat_indicator = combat_indicator_list.Pop();
            Destroy(one_combat_indicator);
        }
        path_indicator_positions.Clear();
    }

    #endregion

    #region Movement Phase


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

    public IEnumerator TakeNextMove(float stepTime) {
        if(ActionPlan.Count == 0) {
            yield break;
        } 
        prevMovePointPos = transform.position;
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
        //if next step is an impassible terrain, cancel all action
        RaycastHit hit;
        if (Physics.Raycast(indicator.transform.position, moveVec, out hit, stepLength, LayerMask.GetMask("Impassible"))) 
        {
            ActionPlan.Clear();
            //Debug.Log("Impassible!");

            //State = CharacterState.Idle;
            yield break;
        }
        moving = true;
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

        IntegratedMapGenerator.Instance.UpdateFOWVisuals();

        State = CharacterState.Idle;
        moving = false;
    }

    /*
    public void Retreat() {
        transform.position = prevMovePointPos;
        IntegratedMapGenerator.Instance.UpdateFOWVisuals();
        movePoint = prevMovePointPos;
    }
    */

    public IEnumerator Retreat()
    {
        yield return StartCoroutine(moveToTargetLocation(prevMovePointPos, IntegratedGameManager.S.excecutionStepTime));
        IntegratedMapGenerator.Instance.UpdateFOWVisuals();
        movePoint = prevMovePointPos;
    }


    #endregion

    #region Shrine and Goal Checks

    private void OnTriggerEnter(Collider col) {
        if (col.gameObject.tag == "Goal") {
            Shrine shrine = col.gameObject.GetComponent<Shrine>();
            if (shrine != null && shrine.CheckShrineType(this)) {
                IntegratedGameManager.S.GoalReached(CharacterId);
            }
        }
    }
    
    private void OnTriggerStay(Collider col) {
        if (col.gameObject.tag == "Door") {
            IntegratedGameManager.S.CheckGoalReached(CharacterId);
        }
    }

    #endregion

    #region Death and Respawn
    public void TakeDamage(int damageAmount = 1) {
        Health -= damageAmount;
        if(IntegratedGameManager.S.gameStatus == GameStatus.Player_Moving) {
            ResetPlan();
        }
        //Debug.Log(string.Format("Character {0} health: {1}", config.characterName, health));

        if (Health == 0) {
            Debug.Log(string.Format("Character {0} Died!", config.characterName));
            Die();
        }
    }

    /*
     * When a Character dies:
     * 1. Set their state to Die
     * 2. Immediately set their death flag to true
     * 3. Set their respawn counter
     * 4. Leave their body where it is
     */ 
    
    public void Die() {
        if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Moving) {
            transform.position = prevMovePointPos;
        }
        State = CharacterState.Die;
        Lives -= 1;
        Deaths += 1;
        dead = true;
        respawnCountdown = GameData.S.RespawnDelay+1;
        ResetPlan();
        IntegratedGameManager.S.CharacterDied(CharacterId);
        UIManager.S.UpdateDeathCounterPanel();

        CompetitionMiddleware.Instance.LogPlayerDeath(CharacterId, currentTile.col, currentTile.row);

        //// Wait for the duration of the animation
        
        //State = CharacterState.Idle;

        //dead = true;
        //respawnCountdown = 2;
        //gameObject.SetActive(false);
        //transform.position = startPos;

        //movePoint = startPos;
        //prevMovePointPos = movePoint;

        IntegratedGameManager.S.characterDied[CharacterId] = true;
    }
    
    public void RespawnCheck() {
        if (Lives > 0) {
            respawnCountdown -= 1;

            if (respawnCountdown == 0) {
                Debug.Log("Respawn in RespawnCheck");
                UIManager.S.UpdateCharacterLifeStatus(CharacterId, true);
                ActionPlan.Clear();
                dead = false;
                Health = config.StartingHealth;
               // transform.position = startPos;
               // transform.rotation = Quaternion.identity;
                //TODO this might need to wait a beat for collision checks
                IntegratedMapGenerator.Instance.UpdateFOWVisuals();

                //Manually turn on the animator and renderer of the character
                Animator[] char_animators = this.GetComponentsInChildren<Animator>(true);
                foreach (Animator char_animator in char_animators)
                {
                    if (char_animator != null)
                    {

                        char_animator.enabled = true;
                    }
                }
                Renderer[] char_renderers = this.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer char_renderer in char_renderers)
                {
                    if (char_renderer != null)
                    {
                        if (char_renderer.gameObject.tag != "TileGround" && char_renderer.gameObject.tag != "VisibleMask")
                        {
                            char_renderer.enabled = true;
                        }
                    }
                }
                State = CharacterState.Idle;
            }
        }
    }
    
    public void QuickRespawn() {
        Debug.Log("Respawn in QuickRespawn");
        ActionPlan.Clear();
        respawnCountdown = 0;
        dead = false;
        Health = config.StartingHealth;
        //Manually turn on the animator and renderer of the character
        Animator[] char_animators = this.GetComponentsInChildren<Animator>(true);
        foreach (Animator char_animator in char_animators)
        {
            if (char_animator != null)
            {

                char_animator.enabled = true;
            }
        }
        Renderer[] char_renderers = this.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer char_renderer in char_renderers)
        {
            if (char_renderer != null)
            {
                if (char_renderer.gameObject.tag != "TileGround" && char_renderer.gameObject.tag != "VisibleMask")
                {
                    char_renderer.enabled = true;
                }
            }
        }
        State = CharacterState.Idle;
    }

    #endregion

    #region HMT State Representation

    public enum StateRepLevel {
        Full = 0,
        TeamVisible = 1,
        TeamUnseen = 2
    }
    
    public JObject HMTStateRep(StateRepLevel level = StateRepLevel.Full) {
        JObject ret = new JObject {
            {"entityType", "Character" },
            {"objKey", "C"+(CharacterId+1) },
            {"id", "C"+(CharacterId+1)+"1" },
            {"sightRange", config.sightRange },
            {"monsterDie", config.monsterDice.ToString()},
            {"trapDie", config.trapDice.ToString() },
            {"stoneDie", config.stoneDice.ToString() },
            {"ready", ReadyForNextPhase },      //This is visible in the TeamStatus UI so should always be here
            {"dead", dead},                     //This is visible in the TeamStatus UI so should always be here
            {"lives", Lives },
        };

        switch (level) {
            case StateRepLevel.Full:
                ret["health"] = Health;
                ret["respawnCounter"] = respawnCountdown;
                ret["actionPoints"] = ActionPointsRemaining;
                ret["actionPlan"] = new JArray(ActionPlan.Select(d => d.ToString()));
                if (currentTile == null) {
                    ret["pinCursorX"] = pingCursor.x + 0;
                    ret["pinCursorY"] = pingCursor.y + 0;
                }
                else {
                    ret["pinCursorX"] = pingCursor.x + currentTile.col;
                    ret["pinCursorY"] = pingCursor.y + currentTile.row;
                }
                goto case StateRepLevel.TeamVisible;
            case StateRepLevel.TeamVisible:
                if (currentTile == null) {
                    ret["x"] = -1;
                    ret["y"] = -1;
                }
                else {
                    ret["x"] = currentTile.col;
                    ret["y"] = currentTile.row;
                }
                break;
            case StateRepLevel.TeamUnseen:
                break;
            default:
                Debug.LogWarningFormat("Unknown StateRepLevel: {0}", level);
                goto case StateRepLevel.Full;
        }
        return ret;
    }

    #endregion


}

