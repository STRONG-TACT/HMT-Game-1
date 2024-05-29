using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;
using System.Linq;

public class IntegratedGameManager : MonoBehaviour
{
    //public enum GameStatus { GetReady, Player_Pinning, Player_Planning, Player_Moving, Monster_Moving, GameEnd }

    // ======== Managers ========
    [Header("Managers")]
    //protected UIManager uiManager;
    protected GameData gameData;
    protected PinningSystem pinningSystem;


    // ======== Game States ======== 
    [Header("Game States")]
    public Character localChar;
    public GameStatus gameStatus = GameStatus.GetReady;
    public int CurrentRound { get; protected set; } = 0;
    public int goalCount = 0;
    private Queue<Tile> eventQueue = new Queue<Tile>();
    protected Coroutine currentCoroutine = null;
    public int currentLevel = 1;
    public bool isNetworkGame;

    private float lastTurnTimerReset = 0;

    public float TimeRemaining {
        get {
            switch (gameStatus) {
                case GameStatus.Player_Pinning:
                case GameStatus.Player_Planning:
                    return GameData.S.TurntimeLimit - Mathf.RoundToInt(Time.time - lastTurnTimerReset);
                default:
                    return float.PositiveInfinity;
            }
        }
    }


    public List<Character> inSceneCharacters = new List<Character>();
    public List<Monster> inSceneMonsters = new List<Monster>();

    public static IntegratedGameManager S;
    public float excecutionStepTime = 1;

    protected virtual void Awake() {
        if (S) Destroy(this);
        else S = this;
    }

    // Called by map generator to update characters' position at the beginning of the currLevel.
    // Characters are pre created in the scene, since they should always be three of them.
    public virtual void SetupCharacter(int ID, float x, float z, CharacterConfig config = null) {
        Character targetChara = inSceneCharacters[ID];
        int lives = currentLevel == 1 ? gameData.LivesPerCharacter : targetChara.Lives;
        if(config != null) {
            targetChara.SetUpConfig(config, ID, lives);
        }
        else {
            targetChara.SetUpConfig(GameData.S.characterConfigs[ID], ID, lives);
        }
        Vector3 newPosition = new Vector3(x, targetChara.transform.position.y, z);
        targetChara.SetStartPosition(newPosition);
    }



    protected virtual void Start() {        
        gameData = GameData.S;
        pinningSystem = PinningSystem.S;
        goalCount = 0;

        Debug.LogFormat("NetworkMiddleware.S.myCharacterID = {0}", NetworkMiddleware.S.myCharacterID);

#if HMT_BUILD
        if (isNetworkGame) {
            if (CompetitionMiddleware.Instance.overrideAIMode) {
                localChar = inSceneCharacters[NetworkMiddleware.S.myCharacterID];
            }
            else {
                localChar = inSceneCharacters[0]; // AI Agent's just assume the focus character is dwarf because they don't actually need it.
            }   
        }
        else {
            localChar = inSceneCharacters[0];
        }
#else
        localChar = inSceneCharacters[(isNetworkGame) ? NetworkMiddleware.S.myCharacterID : 0];
#endif
        currentLevel = 1;
        StartCoroutine(StartLevel());
    }

    protected virtual void Update() {
        UIManager.S.UpdateTurnTimer();
        if(TimeRemaining<= 0) {
            TimeoutSubmit();
        }
    }

    protected virtual void TimeoutSubmit() {  }

    public void ResetTurnTimer() {
        lastTurnTimerReset = Time.time;
    }

    public virtual IEnumerator StartLevel() {

        IntegratedMapGenerator.Instance.LoadLevel(gameData.levelTextFiles[currentLevel - 1]);

        gameStatus = GameStatus.GetReady;
        CurrentRound = 0;
        goalCount = 0;
        UIManager.S.InitGameUI();
        UIManager.S.ResetTeamActionStatus();
        UIManager.S.ResetTeamGoalStatus();

        yield return new WaitForFixedUpdate();
        
        StartPlayerTurn();
    }


    protected virtual void StartPlayerTurn()
    {
        IntegratedMapGenerator.Instance.UpdateFOWVisuals();
        CurrentRound += 1;

        foreach (Character chara in inSceneCharacters)
        {
            chara.ResetActionPoints();
        }

        CompetitionMiddleware.Instance.LogStartRound(CurrentRound);
        StartPlayerPinningPhase();
    }

    protected virtual void StartPlayerPinningPhase()
    {
        Debug.Log("Start Pinning Phase.");
        gameStatus = GameStatus.Player_Pinning;
        UIManager.S.UpdateGamePhaseInfo();
        ResetTurnTimer();


        foreach (Character chara in inSceneCharacters) {
            chara.StartPingPhase();
        }

        CompetitionMiddleware.Instance.LogStartPhase("Pinning");

        if (CheckPhaseEnd()) {
            EndPlayerPinningPhase();
        }
        else {
            localChar.FocusCharacter();
            UIManager.S.ShowCharacterPinUI();
            UIManager.S.ShowCommonHUD();
            UIManager.S.ResetTeamActionStatus();
        }
    }

    protected virtual bool CheckPhaseEnd() {
        bool phaseEnd = true;
        foreach (Character character in inSceneCharacters) {
            if (!character.ReadyForNextPhase) {
                phaseEnd = false;
            }
        }
        return phaseEnd;
    }

    // Update params, if all end their pinning, move to planning currPhase
    public virtual void CheckPingPhaseEnd() {
        if(CheckPhaseEnd()) {
            EndPlayerPinningPhase();
        }
    }

    protected virtual void EndPlayerPinningPhase()
    {
        Debug.Log("End Pinning Phase");
        
        foreach (Character chara in inSceneCharacters) {
            chara.EndPingPhase();
        }
        PinningSystem.S.ClosePinWheel();
        UIManager.S.HideCharacterPinUI();
        //Debug.Log("Should start planning currPhase here.");
        StartPlayerPlanningPhase();
    }

    protected virtual void StartPlayerPlanningPhase() 
    {
        Debug.Log("Start Planning Phase.");
        //common operations only, derived classes extend this
        gameStatus = GameStatus.Player_Planning;
        UIManager.S.UpdateGamePhaseInfo();
        ResetTurnTimer();

        foreach (Character chara in inSceneCharacters) {
            chara.StartPlanningPhase();
        }

        CompetitionMiddleware.Instance.LogStartPhase("Planning");

        if (CheckPhaseEnd()) {
            EndPlayerPlanningPhase();
        }
        else {
            UIManager.S.UpdateCommonHUD();
            UIManager.S.ResetTeamActionStatus();
            UIManager.S.ShowCharacterPlanUI();
            localChar.FocusCharacter();
        }
    }

    // Called by LocalPlayer.SubmitPlan(), when player press submit button.
    // Update params, if all submitted their plan, move to moving currPhase
    public virtual void CheckPlanPhaseEnd() {
        if (CheckPhaseEnd()) {
            EndPlayerPlanningPhase();
        }
    }

    protected virtual void EndPlayerPlanningPhase()
    {
        Debug.Log("End Planning Phase");
        UIManager.S.HideCharacterPlanUI();

        foreach (Character chara in inSceneCharacters)
        {
            //chara.UnFocusCharacter();
            chara.EndPlanning();
        }

        StartCharacterMovingPhase();
    }


    public virtual void StartCharacterMovingPhase() 
    {
        Debug.Log("Start Moving Phase.");
        gameStatus = GameStatus.Player_Moving;
        UIManager.S.UpdateGamePhaseInfo();
        UIManager.S.HideCommonHUD();
        UIManager.S.HideCharacterPlanUI();
        UIManager.S.HideCharacterPinUI();
        //moveFinishedCount = 0;       
        eventQueue = new Queue<Tile>();

        CompetitionMiddleware.Instance.LogStartPhase("Player_Movement");

        currentCoroutine = StartCoroutine(CharacterMoveByStep());
    }


    // Characters move step by step
    // If events happen, deal with all the events and then back to moving
    protected virtual IEnumerator CharacterMoveByStep()
    {

        //// A flag, whether this currRound of step triggers a combat
        //bool hasCombat = false;
        bool allCharactersDone = false;
        while (!allCharactersDone)
        {
            CompetitionMiddleware.Instance.LogPlayerMoveStep();
            foreach (Character chara in inSceneCharacters)
            {
                StartCoroutine(chara.TakeNextMove(excecutionStepTime));
            }
            bool doneMoving;
            do
            {
                doneMoving = true;
                foreach (Character chara in inSceneCharacters)
                {
                    if (chara.moving)
                    {
                        doneMoving = false;
                    }
                }
                yield return null;
            } while (!doneMoving);

            if (eventQueue.Count != 0)
            {
                yield return ExecuteCombatOneByOne();
                //hasCombat = true;
            }

            allCharactersDone = true;
            foreach (Character character in inSceneCharacters)
            {
                if (character.ActionPlan.Count > 0)
                {
                    allCharactersDone = false;
                    break;
                }
            }
            Debug.Log("Moving currPhase ended.");
            pinningSystem.ClearCurrentTurnPins();
        }
        StartMonsterTurn();
    }

    // Start monster moving currPhase
    protected virtual void StartMonsterTurn()
    {
        Debug.Log("Start Monster Turn.");
        gameStatus = GameStatus.Monster_Moving;
        UIManager.S.UpdateGamePhaseInfo();
        //moveFinishedCount = 0;

        foreach (Monster m in inSceneMonsters)
        {
            m.MonsterTurnStart();
        }

        CompetitionMiddleware.Instance.LogStartPhase("Monster_Movement");

        currentCoroutine = StartCoroutine(MonsterMoveByStep());
    }


    protected void DeconflictMonsterPlans (List<Monster> monstersMoving) {
        Dictionary<Vector2Int, List<Monster>> conflicts = new Dictionary<Vector2Int, List<Monster>>();
        bool conflicted = true;
        int countflictLoops = 0;
        while (conflicted) {
            Debug.Log("Deconflict Loop: " + countflictLoops++);
            foreach (Monster m in monstersMoving) {
                if (conflicts.ContainsKey(m.NextMoveCoordinates())) {
                    conflicts[m.NextMoveCoordinates()].Add(m);
                }
                else {
                    conflicts[m.NextMoveCoordinates()] = new List<Monster> { m };
                }
            }

            conflicted = false;

            foreach (Vector2Int loc in conflicts.Keys) {
                if (conflicts[loc].Count > 1 ) {
                    Tile t = IntegratedMapGenerator.Instance.GetTileAt(loc);
                    if (t != null && t.IsOccupiedByPlayer) {
                        continue;
                    }
                    else {
                        conflicted = true;
                        conflicts[loc].OrderBy(m => m.config.movementStyle);
                        conflicts[loc][0].PopPlanMove();
                    }
                }
            }
        }
    }

    // Monsters moving step by step
    // Same with chara move by step, when events happened, deal with them and come back
    protected virtual IEnumerator MonsterMoveByStep() {
        List<Monster> monstersMoving = new List<Monster>();
        foreach(Monster m in inSceneMonsters) {
            if(m.MovesLeftThisTurn > 0) {
                monstersMoving.Add(m);
            }
        }
        int monsterMoveSteps = 0;
        while (monstersMoving.Count > 0) {
            monsterMoveSteps++;
            Debug.Log("Monster Move Step: " + monsterMoveSteps);
            foreach(Monster m in monstersMoving) {
                m.PlanNextMove();
            }

            DeconflictMonsterPlans(monstersMoving);
            CompetitionMiddleware.Instance.LogMonsterMoveStep(monstersMoving);
            foreach (Monster m in monstersMoving) {
                StartCoroutine(m.TakeNextMove(excecutionStepTime));
            }
            bool doneMoving;
            do {
                doneMoving = true;
                foreach (Monster m in monstersMoving) {
                    if (m.moving) {
                        doneMoving = false;
                    }
                }
                yield return null;
            } while (!doneMoving);

            if (eventQueue.Count != 0) {
                yield return ExecuteCombatOneByOne();
                break;
            }

            //after all combat done, if multiple monster oppcuies the same tile, they move until every tile contains at most 1 monster
            foreach (Monster mon in inSceneMonsters.OrderByDescending(m => m.config.movementStyle)) {
                Tile currentTile = mon.currentTile;
                if (currentTile.MultipleMonsters) {
                    int startRow = currentTile.row;
                    int startCol = currentTile.col;
                    bool foundSpot = false;
                    //move monster to nearest avaiable tile where there is no character and no other monster on it
                    for (int distance = 1; distance < Math.Max(IntegratedMapGenerator.Instance.Map.GetLength(0), IntegratedMapGenerator.Instance.Map.GetLength(1)); distance++)
                    {
                        if (!foundSpot)
                        {
                            List<Tile> availablePos = new List<Tile>();
                            switch (mon.config.movementStyle) {
                                case MonsterConfig.MovementStyle.Horizontal:
                                    for (int colOffset = distance * -1; colOffset < distance + 1; colOffset++) {
                                        int targetRow = startRow;
                                        int targetCol = startCol + colOffset;
                                        if (IntegratedMapGenerator.Instance.InMap(targetRow, targetCol)) {
                                            Tile targetTile = IntegratedMapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                            if (targetTile.tileType == Tile.ObstacleType.None && !targetTile.IsOccupied) {
                                                availablePos.Add(targetTile);
                                            }
                                        }
                                    }
                                    break;
                                case MonsterConfig.MovementStyle.Vertical:
                                    for (int rowOffset = distance * -1; rowOffset < distance + 1; rowOffset++) {
                                        int targetRow = startRow + rowOffset;
                                        int targetCol = startCol;
                                        if (IntegratedMapGenerator.Instance.InMap(targetRow, targetCol)) {
                                            Tile targetTile = IntegratedMapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                            if (targetTile.tileType == Tile.ObstacleType.None && !targetTile.IsOccupied) {
                                                availablePos.Add(targetTile);
                                            }
                                        }
                                    }
                                    break;
                                case MonsterConfig.MovementStyle.RandomWalk:
                                    for (int rowOffset = distance * -1; rowOffset < distance + 1; rowOffset++) {
                                        for (int colOffset = distance * -1; colOffset < distance + 1; colOffset++) {
                                            int targetRow = startRow + rowOffset;
                                            int targetCol = startCol + colOffset;
                                            if (IntegratedMapGenerator.Instance.InMap(targetRow, targetCol)) {
                                                Tile targetTile = IntegratedMapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                                if (targetTile.tileType == Tile.ObstacleType.None && !targetTile.IsOccupied) {
                                                    availablePos.Add(targetTile);
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case MonsterConfig.MovementStyle.Static:
                                    Debug.LogError("Static monster should not be in the same tile");
                                    break;
                            }

                            if (availablePos.Count > 0) {
                                foundSpot = true;
                                //select a random tile in the avaiable positions to move
                                int randomIndex = NetworkMiddleware.S.NextRandomInt(0, availablePos.Count);
                                Tile selectedTile = availablePos[randomIndex];
                                yield return StartCoroutine(mon.moveToTargetLocation(selectedTile.transform.position, excecutionStepTime));
                            }
                        }
                    }
                }
            }

            monstersMoving.Clear();
            foreach (Monster m in inSceneMonsters) {
                if (m.MovesLeftThisTurn > 0) {
                    monstersMoving.Add(m);
                }
            }
        }

        Debug.Log("Monster moving currPhase ended.");
        StartPlayerTurn();
    }

    // Execute all the events happened within one step time
    // Combat.ExecuteCombat() is the actual combat function
    protected virtual IEnumerator ExecuteCombatOneByOne() {
        Debug.LogFormat("Exectuing {0} events in queue.", eventQueue.Count);

        while (eventQueue.Count != 0) {
            bool win = false;
            Tile t = eventQueue.Dequeue();
            Debug.LogFormat("Processing Event at {0}, {1}", t.row, t.col);            
            
            bool visibility = t.fogOfWarDictionary[localChar.CharacterId] == Tile.FogOfWarState.Visible;
            Combat.FightType challengeType;

            switch (t.tileType) {
                case Tile.ObstacleType.None:
                    challengeType = Combat.FightType.Monster;
                    win = Combat.ExecuteCombat(Combat.FightType.Monster, t, visibility);
                    break;
                case Tile.ObstacleType.Trap:
                    challengeType = Combat.FightType.Trap;
                    win = Combat.ExecuteCombat(Combat.FightType.Trap, t, visibility);
                    break;
                case Tile.ObstacleType.Rock:
                    challengeType = Combat.FightType.Rock;
                    win = Combat.ExecuteCombat(Combat.FightType.Rock, t, visibility);
                    break;
                default:
                    Debug.LogWarning("Unknown Combat Type Encountered");
                    continue;
            }
            //play attack animation for all characters and monster on the tile
            //make a copy of the characters and monsters that are originally in the tile. So that if a character or monster moves elsewhere, we can still find it
            List<Character> copiedCharacters = t.CharacterList;
            List<Monster> copiedEnemies = t.MonsterList;
            foreach (Character c in copiedCharacters) {
                c.State = Character.CharacterState.Attacking;
            }
            foreach (Monster mo in copiedEnemies) {
                mo.State = Monster.CharacterState.Attacking;
            }

            //wait for animation to play
            yield return new WaitForSeconds(UIManager.S.animationDuration * 3);

            if (win) {
                // if the character(s) won the battle, destory the enemies
                Debug.Log("Character won.");
                switch (challengeType) {
                    case Combat.FightType.Monster:
                        t.ClearMonsters();
                        break;
                    case Combat.FightType.Trap:
                    case Combat.FightType.Rock:
                        IntegratedMapGenerator.Instance.ClearTile(t.col, t.row);
                        break;
                }

                //problem -> need to fix
                IntegratedMapGenerator.Instance.UpdateFOWVisuals();
            }
            else {
                // If not, reduce health except rock
                // If character's turn, all remaining steps should be cleared.
                Debug.Log("Enemy won.");

                switch (t.tileType) {
                    case Tile.ObstacleType.None:
                        foreach(Character chara in t.CharacterList) {
                            chara.TakeDamage();
                        }
                        
                        // if player got defeated by monster on a shrine tile
                        // and the player correspond to that shrine tile
                        // then we should "unreach" the shrine
                        if (t.gameObject.CompareTag("Goal")) {
                            Shrine shrine = t.gameObject.GetComponentInChildren<Shrine>();
                            foreach (var character in t.CharacterList) {
                                if (shrine.CheckShrineType(character)) {
                                    GoalUnReached(character.CharacterId);
                                    shrine.ReturnOrb();
                                }
                            }
                        }
                        break;
                    case Tile.ObstacleType.Trap:
                        foreach(Character chara in t.CharacterList) {
                            chara.TakeDamage();
                        }
                        IntegratedMapGenerator.Instance.ClearTile(t.col, t.row);
                        break;
                    case Tile.ObstacleType.Rock:
                        foreach (Character c in t.CharacterList) {
                            c.ResetPlan();
                        }
                        break;
                }

                switch(gameStatus) {
                    case GameStatus.Player_Moving:
                        foreach (Character c in t.CharacterList) {
                            c.Retreat();
                        }
                        break;
                    case GameStatus.Monster_Moving:
                        foreach (Monster m in t.MonsterList) {
                            m.Retreat();
                        }
                        break;
                }
                IntegratedMapGenerator.Instance.UpdateFOWVisuals();
            }

            //TODO this should probably be waiting for a button click in the future.
            yield return new WaitForSeconds(2 * excecutionStepTime);
            foreach (Character c in copiedCharacters)
            {
                if (c != null)
                {
                    c.State = Character.CharacterState.Idle;
                }
            }
            foreach (Monster mo in copiedEnemies)
            {
                if (mo != null)
                {
                    mo.State = Monster.CharacterState.Idle;
                }
            }
            UIManager.S.HideCombatUI();
        }
        yield break;
        // This should only be called as a sub-coroutine of the main moving one so it
        // doesn't need to restart them, it should just yield break
        //if (gameStatus == GameStatus.Player_Moving)
        //{
        //    StartCoroutine(CharacterMoveByStep());
        //}else if (gameStatus == GameStatus.Monster_Moving)
        //{
        //    StartCoroutine(MonsterMoveByStep());
        //}
    }


    protected virtual void reduceCharacterHealth(List<Character> charaList, List<Character> deadChara, List<Character> aliveChara)
    {
        foreach (Character c in charaList)
        {
            c.TakeDamage();

            if (c.dead)
            {
                UIManager.S.UpdateCharacterLifeStatus(c.CharacterId, false);
                deadChara.Add(c);
            }
            else
            {
                aliveChara.Add(c);
            }
        }
    }


    // When chara fail a combat, clear all the remaining moves in queue this currRound
    protected virtual void clearCharacterMoves(List<Character> charaList)
    {
        foreach (Character c in charaList)
        {
            //Debug.LogFormat("Clear plan: {0}", c.name);
            c.ActionPlan.Clear();
        }
    }

    // Called by Character.OnTriggerEnter(), when a character collide with its goal
    public virtual void GoalReached(int charaID)
    {
        Tile tile = inSceneCharacters[charaID].currentTile;
        CompetitionMiddleware.Instance.LogClearShrine(charaID, tile.col, tile.row);
        UIManager.S.UpdateCharacterGoalStatus(charaID);
        goalCount += 1;
    }

    public virtual void GoalUnReached(int charaID)
    {
        Tile tile = inSceneCharacters[charaID].currentTile;
        CompetitionMiddleware.Instance.LogRevokeShrine(charaID, tile.col, tile.row);
        UIManager.S.UpdateCharacterGoalStatus(charaID, false);
        goalCount -= 1;
        Debug.Log("refunded");
    }

    public virtual void CharacterDied(int charaID) {
        UIManager.S.UpdateCharacterLifeStatus(charaID, false);
        UIManager.S.UpdateCommonHUD();
        IntegratedMapGenerator.Instance.UpdateFOWVisuals();
        CheckLoseCondition();
    }

    // Called by Tile.OnTriggerEnter(), when an event happens at the newTile
    // The same newTile (where an event happens) will only appear in queue once
    public void updateEventQueue(Tile tile)
    {
        if (gameStatus != GameStatus.Player_Moving && gameStatus != GameStatus.Monster_Moving)
        {
            Debug.LogWarningFormat("Event Generated during the {0} Phase, ignoring it.", gameStatus);
            return;
        }

        Debug.LogFormat("Event generated at {0}, {1}, of type {2}", tile.row, tile.col, tile.tileType);
        if (!eventQueue.Contains(tile))
        {
            eventQueue.Enqueue(tile);
        }
    }

    // Called by Character.OnTriggerEnter(), when all three goals fetched and one character collide with the door after that
    // "Move" to next currLevel by reset all relevant constants, delete monsters and tiles (tiles done by map generator) this currLevel, and reset chara status
    public virtual void CheckGoalReached(int charaID)
    {
        if (goalCount >= 3) { //TODO: take th conditional logic out of the character and move it to the Manager
            Tile tile = inSceneCharacters[charaID].currentTile;
            CompetitionMiddleware.Instance.LogClearGoal(charaID, tile.col, tile.row);
            StartCoroutine(PrepareForNextLevel());
        }
    }

    protected virtual IEnumerator PrepareForNextLevel() {

        while (inSceneMonsters.Count != 0) {
            Monster m = inSceneMonsters[0];
            inSceneMonsters.Remove(m);
            Destroy(m.gameObject);
        }

        UIManager.S.LoadLevelEndUI();
        foreach (Character c in inSceneCharacters) {
            c.State = Character.CharacterState.Cheering;
        }

        eventQueue.Clear();
        UIManager.S.HideCharacterPinUI();
        yield return new WaitForSeconds(5f);
        foreach (Character c in inSceneCharacters) {
            c.State = Character.CharacterState.Idle;
        }


        Debug.Log("Moving to next currLevel.");
        currentLevel += 1;

        if (currentLevel <= gameData.levelTextFiles.Length) {

            eventQueue.Clear();
            StopAllCoroutines();

            foreach (Character c in inSceneCharacters) {
                c.StopAllCoroutines();
                c.QuickRespawn();
                UIManager.S.UpdateCharacterLifeStatus(c.CharacterId, true);
            }

            StartCoroutine(StartLevel());
        }
        else {
            Debug.Log("Game ends.");
            UIManager.S.DisplayVictoryScreen();
            gameStatus = GameStatus.GameEnd;
        }
    }

    public virtual void CheckLoseCondition()
    {
        int deadPlayerCount = 0;
        foreach (var character in inSceneCharacters)
        {
            if (character.dead) deadPlayerCount++;
        }

        if (deadPlayerCount >= 3)
        {
            Lose();
        }
    }
    protected virtual void Lose()
    {
        UIManager.S.DisplayLossScreen();
    }

    public virtual void SwitchCharacter(int index)
    {
    }
}
