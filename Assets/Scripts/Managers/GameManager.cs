using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;
using System.Linq;
using Photon.Pun;
using Random = UnityEngine.Random;
using System.IO;

public class GameManager : MonoBehaviour
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
    public GameStatus gameStatus = GameStatus.LevelStart;
    public int CurrentRound { get; protected set; } = 0;
    private List<Tile> eventQueue = new List<Tile>();
    protected Coroutine currentCoroutine = null;
    public int currentLevel = 1;
    public bool isNetworkGame;
    public int readyForPlayerTurnCount = 0;
    public int CombatResultSyncedCount = 0;

    private float lastTurnTimerReset = 0;

    protected HMT.ArgParser ArgParser = new HMT.ArgParser();

    public bool[] characterDied;

    public float TimeRemaining {
        get {
            switch (gameStatus) {
                case GameStatus.Player_Pinning:
                case GameStatus.Player_Planning:
                    return GameData.Instance.TurntimeLimit - (Time.time - lastTurnTimerReset);
                default:
                    return float.PositiveInfinity;
            }
        }
    }

    public int GoalCount {
        get {
            var ret = 0;
            foreach (var shrine in InSceneShrines) {
                if (shrine.Reached) {
                    ret++;
                }
            }
            return ret;
        }
    }

    public List<Shrine> InSceneShrines = new List<Shrine> { null, null, null };
    public List<Character> inSceneCharacters = new List<Character>();
    public List<Monster> inSceneMonsters = new List<Monster>();

    public static GameManager Instance { get; protected set; } = null;
    public float excecutionStepTime = 1;

    protected virtual void Awake() {
        if (Instance) Destroy(this);
        else Instance = this;
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
            targetChara.SetUpConfig(GameData.Instance.characterConfigs[ID], ID, lives);
        }
        Vector3 newPosition = new Vector3(x, targetChara.transform.position.y, z);
        targetChara.SetStartPosition(newPosition);
    }



    protected virtual void Start() {        
        gameData = GameData.Instance;
        pinningSystem = PinningSystem.Instance;
        characterDied = new bool[3];

        Debug.LogFormat("NetworkMiddleware.S.myCharacterID = {0}", NetworkMiddleware.Instance.myCharacterID);

        ArgParser.AddArg("levelSpec", HMT.ArgParser.ArgType.One);
        ArgParser.ParseArgs();

        if (ArgParser.CheckFlag("levelSpec")) {
            try {
                StreamReader sr = new StreamReader(ArgParser.GetArgValue("levelSpec"));
                string levelSpec = sr.ReadToEnd();
                sr.Close();
                MapGenerator.Instance.ParseLevelSpec(levelSpec);
            }
            catch(Exception e) {
                Debug.LogError("Problem parsing spec file " + e + " Reverting to in-build levels.");
                MapGenerator.Instance.levelSpecs = null;
            }
        }

        if(MapGenerator.Instance.levelSpecs == null) {
            MapGenerator.Instance.ParseLevelSpec(gameData.levelTextFiles);
        }

#if HMT_BUILD
        if (isNetworkGame) {
            if (CompetitionMiddleware.Instance.overrideAIMode) {
                localChar = inSceneCharacters[NetworkMiddleware.Instance.myCharacterID];
            }
            else {
                localChar = inSceneCharacters[0]; // AI Agent's just assume the focus character is dwarf because they don't actually need it.
            }   
        }
        else {
            localChar = inSceneCharacters[0];
        }
#else
        localChar = inSceneCharacters[(isNetworkGame) ? NetworkMiddleware.Instance.myCharacterID : 0];
#endif
        //currentLevel = 1;
        StartCoroutine(StartLevel());
    }

    protected virtual void Update() {
        UIManager.Instance.UpdateTurnTimer();
        if(TimeRemaining<= 0) {
            TimeoutSubmit();
        }
    }

    protected virtual void TimeoutSubmit() {  }

    public void ResetTurnTimer() {
        lastTurnTimerReset = Time.time;
    }

    public virtual IEnumerator StartLevel() {
        InSceneShrines = new List<Shrine> { null, null, null }; 
        MapGenerator.Instance.LoadLevel(currentLevel - 1);

        gameStatus = GameStatus.LevelStart;
        CurrentRound = 0;
        characterDied = new bool[3];
        UIManager.Instance.InitGameUI();
        UIManager.Instance.ResetTeamActionStatus();
        UIManager.Instance.ResetTeamGoalStatus();

        yield return new WaitForFixedUpdate();

        NetworkMiddleware.Instance.CallSyncStartPlayerturn();
        // Wait until all clients are ready
        while (readyForPlayerTurnCount < 3)
        {
            yield return null;
        }
        readyForPlayerTurnCount = 0;
        CheckLoseCondition();
        StartPlayerTurn();
    }


    public virtual void StartPlayerTurn()
    {
        //Debug.Log("Goal Count at start of level: " + goalCount.ToString());
        MapGenerator.Instance.UpdateFOWVisuals();
        CurrentRound += 1;

        foreach (Character chara in inSceneCharacters)
        {
            chara.ResetActionPoints();
            if (!chara.dead)
            {
                chara.State = Character.CharacterState.Idle;
            }
        }

        CompetitionMiddleware.Instance.LogStartRound(CurrentRound);
        StartPlayerPinningPhase();
    }

    protected virtual void StartPlayerPinningPhase()
    {
        Debug.Log("Start Pinning Phase.");
        gameStatus = GameStatus.Player_Pinning;
        UIManager.Instance.UpdateGamePhaseInfo();
        ResetTurnTimer();


        foreach (Character chara in inSceneCharacters) {
            chara.StartPingPhase();
            if (chara.dead == false)
            {
                chara.State = Character.CharacterState.Idle;
            }
        }

        CompetitionMiddleware.Instance.LogStartPhase("Pinning");

        if (CheckPhaseEnd()) {
            EndPlayerPinningPhase();
        }
        else {
            localChar.FocusCharacter();
            UIManager.Instance.ShowCharacterPinUI();
            UIManager.Instance.ShowCommonHUD();
            UIManager.Instance.ResetTeamActionStatus();
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

    public virtual void GotoNextPhase() {
        switch (gameStatus) {
            //Note: the "EndXPhase" functions always call the StartX+1Phase function
            case GameStatus.Player_Pinning:
                EndPlayerPinningPhase();
                break;
            case GameStatus.Player_Planning:
                EndPlayerPlanningPhase();
                break;
            case GameStatus.Player_Moving:
            case GameStatus.Monster_Moving:
                //These should be automatic?
                break;
            default:
                Debug.LogFormat("Don't know what to do with gameStatus {0}", gameStatus);
                break;
        }
    }

    // Update params, if all end their pinning, move to planning currPhase
    public virtual void CheckPingPhaseEnd() {
        if(CheckPhaseEnd()) {
            NetworkMiddleware.Instance.CallGotoNextPhase();

            //EndPlayerPinningPhase();
        }
    }

    protected virtual void EndPlayerPinningPhase()
    {
        Debug.Log("End Pinning Phase");
        
        foreach (Character chara in inSceneCharacters) {
            chara.EndPingPhase();
        }
        PinningSystem.Instance.ClosePinWheel();
        UIManager.Instance.HideCharacterPinUI();
        //Debug.Log("Should start planning currPhase here.");
        StartPlayerPlanningPhase();
    }

    protected virtual void StartPlayerPlanningPhase() 
    {
        Debug.Log("Start Planning Phase.");
        //common operations only, derived classes extend this
        gameStatus = GameStatus.Player_Planning;
        UIManager.Instance.UpdateGamePhaseInfo();
        ResetTurnTimer();

        foreach (Character chara in inSceneCharacters) {
            chara.StartPlanningPhase();
        }

        CompetitionMiddleware.Instance.LogStartPhase("Planning");

        if (CheckPhaseEnd()) {
            EndPlayerPlanningPhase();
        }
        else {
            UIManager.Instance.UpdateCommonHUD();
            UIManager.Instance.ResetTeamActionStatus();
            UIManager.Instance.ShowCharacterPlanUI();
            localChar.FocusCharacter();
        }
    }

    // Called by LocalPlayer.SubmitPlan(), when player press submit button.
    // Update params, if all submitted their plan, move to moving currPhase
    public virtual void CheckPlanPhaseEnd() {
        if (CheckPhaseEnd()) {
            NetworkMiddleware.Instance.CallGotoNextPhase();
            //EndPlayerPlanningPhase();
        }
    }

    protected virtual void EndPlayerPlanningPhase()
    {
        Debug.Log("End Planning Phase");
        UIManager.Instance.HideCharacterPlanUI();

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
        UIManager.Instance.UpdateGamePhaseInfo();
        UIManager.Instance.HideCommonHUD();
        UIManager.Instance.HideCharacterPlanUI();
        UIManager.Instance.HideCharacterPinUI();
        //moveFinishedCount = 0;       
        eventQueue = new List<Tile>();

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

        CheckWinCondition();
        StartMonsterTurn();
    }

    // Start monster moving currPhase
    protected virtual void StartMonsterTurn()
    {
        Debug.Log("Start Monster Turn.");
        gameStatus = GameStatus.Monster_Moving;
        UIManager.Instance.UpdateGamePhaseInfo();
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
            //Manually clear all conflicted monster's move plan if conflict cannot be solved
            /*
            if(countflictLoops > 100)
            {
                foreach (List<Monster> m_list in conflicts.Values)
                {
                    foreach (Monster m in m_list) {
                        m.ClearPlanMove();
                    }
                }
                break;
            }
            */
            countflictLoops++;
            //Debug.Log("Deconflict Loop: " + countflictLoops++);
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
                    Tile t = MapGenerator.Instance.GetTileAt(loc);
                    if (t != null && t.IsOccupiedByPlayer) {
                        continue;
                    }
                    else {
                        conflicted = true;
                        conflicts[loc].OrderBy(m => m.config.movementStyle);
                        //conflicts[loc][0].PopPlanMove();
                        conflicts[loc][0].ClearPlanMove();
                        conflicts[loc].RemoveAt(0);
                    }
                }
            }
        }
    }

    // Monsters moving step by step
    // Same with chara move by step, when events happened, deal with them and come back
    protected virtual IEnumerator MonsterMoveByStep() {
        Random.InitState(NetworkMiddleware.Instance.randomSeed);
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
            //Debug.Log("Test Game Frozen -----------------");
            //Deconflict function is causing game frozen and out of sync issue. Disable this for now
            //DeconflictMonsterPlans(monstersMoving);
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
            Random.InitState(NetworkMiddleware.Instance.randomSeed);
            //wait 0.5 seconds for collision to register properly
            //yield return new WaitForSeconds(0.5f);
            yield return new WaitForFixedUpdate();
            yield return StartCoroutine(SeparateDuplicateMonster());
            

            monstersMoving.Clear();
            foreach (Monster m in inSceneMonsters) {
                if (m.MovesLeftThisTurn > 0) {
                    monstersMoving.Add(m);
                }
            }
        }
        Random.InitState(NetworkMiddleware.Instance.randomSeed);
        //This function call here is necessary, otherwise monster won't separate if combat happens
        //wait 0.5 seconds for collision to register properly
        //yield return new WaitForSeconds(0.5f);
        yield return new WaitForFixedUpdate();
        yield return StartCoroutine(SeparateDuplicateMonster());
        Debug.Log("Monster moving currPhase ended.");
        //StartPlayerTurn();
        NetworkMiddleware.Instance.CallSyncStartPlayerturn();
        // Wait until all clients are ready
        while (readyForPlayerTurnCount < 3)
        {
            yield return null;
        }
        readyForPlayerTurnCount = 0;
        
        //if (isNetworkGame && PhotonNetwork.IsMasterClient) LogLevelResult();
        
        StartPlayerTurn();
    }

    //after all combats are done, if multiple monster oppcuies the same tile, they move until every tile contains at most 1 monster
    protected virtual IEnumerator SeparateDuplicateMonster()
    {
        foreach (Monster mon in inSceneMonsters.OrderByDescending(m => m.config.movementStyle))
        {
            Tile currentTile = mon.currentTile;
            if (currentTile.MultipleMonsters)
            {
                int startRow = currentTile.row;
                int startCol = currentTile.col;
                bool foundSpot = false;
                //move monster to nearest avaiable tile where there is no character and no other monster on it
                for (int distance = 1; distance < Math.Max(MapGenerator.Instance.Map.GetLength(0), MapGenerator.Instance.Map.GetLength(1)); distance++)
                {
                    if (!foundSpot)
                    {
                        List<Tile> availablePos = new List<Tile>();
                        switch (mon.config.movementStyle)
                        {
                            case MonsterConfig.MovementStyle.Horizontal:
                                for (int colOffset = distance * -1; colOffset < distance + 1; colOffset++)
                                {
                                    int targetRow = startRow;
                                    int targetCol = startCol + colOffset;
                                    if (MapGenerator.Instance.InMap(targetRow, targetCol))
                                    {
                                        Tile targetTile = MapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                        if (targetTile.tileType == Tile.ObstacleType.None && !targetTile.IsOccupied)
                                        {
                                            availablePos.Add(targetTile);
                                        }
                                    }
                                }
                                break;
                            case MonsterConfig.MovementStyle.Vertical:
                                for (int rowOffset = distance * -1; rowOffset < distance + 1; rowOffset++)
                                {
                                    int targetRow = startRow + rowOffset;
                                    int targetCol = startCol;
                                    if (MapGenerator.Instance.InMap(targetRow, targetCol))
                                    {
                                        Tile targetTile = MapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                        if (targetTile.tileType == Tile.ObstacleType.None && !targetTile.IsOccupied)
                                        {
                                            availablePos.Add(targetTile);
                                        }
                                    }
                                }
                                break;
                            case MonsterConfig.MovementStyle.RandomWalk:
                                for (int rowOffset = distance * -1; rowOffset < distance + 1; rowOffset++)
                                {
                                    for (int colOffset = distance * -1; colOffset < distance + 1; colOffset++)
                                    {
                                        int targetRow = startRow + rowOffset;
                                        int targetCol = startCol + colOffset;
                                        if (MapGenerator.Instance.InMap(targetRow, targetCol))
                                        {
                                            Tile targetTile = MapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                            if (targetTile.tileType == Tile.ObstacleType.None && !targetTile.IsOccupied)
                                            {
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

                        if (availablePos.Count > 0)
                        {
                            foundSpot = true;
                            //select a random tile in the avaiable positions to move
                            int randomIndex = NetworkMiddleware.Instance.NextRandomInt(0, availablePos.Count);
                            Tile selectedTile = availablePos[randomIndex];
                            yield return StartCoroutine(mon.moveToTargetLocation(selectedTile.transform.position, excecutionStepTime));
                        }
                    }
                }
            }
        }
    }



    // Execute all the events happened within one step time
    // Combat.ExecuteCombat() is the actual combat function
    protected virtual IEnumerator ExecuteCombatOneByOne() {
        Debug.LogFormat("Exectuing {0} events in queue.", eventQueue.Count);
        eventQueue.Sort();
        while (eventQueue.Count != 0) {
            bool win = false;
            Tile t = eventQueue[0];
            eventQueue.RemoveAt(0);
            Debug.LogFormat("Processing Event at {0}, {1}", t.row, t.col);            
            
            bool visibility = t.fogOfWarDictionary[localChar.CharacterId] == Tile.FogOfWarState.Visible;
            Combat.FightType challengeType;

            switch (t.tileType) {
                case Tile.ObstacleType.None:
                    challengeType = Combat.FightType.Monster;
                    //win = Combat.S.ExecuteCombat(Combat.FightType.Monster, t, visibility);
                    NetworkMiddleware.Instance.CallSyncExecuteCombat(Combat.FightType.Monster, t, visibility);
                    while(CombatResultSyncedCount < 3)
                    {
                        //Debug.Log("Combat Result ready Count: " + CombatResultSyncedCount);
                        yield return null;
                    }
                    CombatResultSyncedCount = 0;
                    Random.InitState(NetworkMiddleware.Instance.randomSeed);
                    win = Combat.Instance.result;
                    //UIManager.S.ShowCombatUI(Combat.FightType.Monster, Combat.S.charaIDs, Combat.S.charaDiceStats, Combat.S.enemyDiceStats, Combat.S.charaScores, Combat.S.enemyScores,
                    //    Combat.S.charaScore, Combat.S.enemyScore, Combat.S.result, visibility);
                    break;
                case Tile.ObstacleType.Trap:
                    challengeType = Combat.FightType.Trap;
                    //win = Combat.S.ExecuteCombat(Combat.FightType.Trap, t, visibility);

                    NetworkMiddleware.Instance.CallSyncExecuteCombat(Combat.FightType.Trap, t, visibility);
                    while (CombatResultSyncedCount < 3)
                    {
                        //Debug.Log("Combat Result ready Count: " + CombatResultSyncedCount);
                        yield return null;
                    }
                    CombatResultSyncedCount = 0;
                    Random.InitState(NetworkMiddleware.Instance.randomSeed);
                    win = Combat.Instance.result;
                    //UIManager.S.ShowCombatUI(Combat.FightType.Trap, Combat.S.charaIDs, Combat.S.charaDiceStats, Combat.S.enemyDiceStats, Combat.S.charaScores, Combat.S.enemyScores, 
                    //    Combat.S.charaScore, Combat.S.enemyScore, Combat.S.result, visibility);
                    break;
                case Tile.ObstacleType.Rock:
                    challengeType = Combat.FightType.Rock;
                    //win = Combat.S.ExecuteCombat(Combat.FightType.Rock, t, visibility);
                    NetworkMiddleware.Instance.CallSyncExecuteCombat(Combat.FightType.Rock, t, visibility);
                    while (CombatResultSyncedCount < 3)
                    {
                        //Debug.Log("Combat Result ready Count: " + CombatResultSyncedCount);
                        yield return null;
                    }
                    CombatResultSyncedCount = 0;
                    Random.InitState(NetworkMiddleware.Instance.randomSeed);
                    win = Combat.Instance.result;
                    //UIManager.S.ShowCombatUI(Combat.FightType.Rock, Combat.S.charaIDs, Combat.S.charaDiceStats, Combat.S.enemyDiceStats, Combat.S.charaScores, Combat.S.enemyScores,
                    //    Combat.S.charaScore, Combat.S.enemyScore, Combat.S.result, visibility);
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
            yield return UIManager.Instance.CombatUICoroutine(challengeType, Combat.Instance, visibility);

            if (win) {
                // if the character(s) won the battle, destory the enemies
                Debug.Log("Character won.");
                switch (challengeType) {
                    case Combat.FightType.Monster:
                        t.ClearMonsters();
                        break;
                    case Combat.FightType.Trap:
                    case Combat.FightType.Rock:
                        MapGenerator.Instance.ClearTile(t.col, t.row);
                        break;
                }

                //problem -> need to fix
                MapGenerator.Instance.UpdateFOWVisuals();
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
                                    ShrineUnReached(character.CharacterId);
                                    shrine.ReturnOrb();
                                }
                            }
                        }
                        break;
                    case Tile.ObstacleType.Trap:
                        foreach(Character chara in t.CharacterList) {
                            chara.TakeDamage();
                        }
                        MapGenerator.Instance.ClearTile(t.col, t.row);
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
                            yield return c.Retreat();
                        }
                        break;
                    case GameStatus.Monster_Moving:
                        foreach (Monster m in t.MonsterList) {
                            yield return m.Retreat();
                        }
                        break;
                }
                MapGenerator.Instance.UpdateFOWVisuals();
            }

            //TODO this should probably be waiting for a button click in the future.
            yield return WaitForExecutionSteps(2);
            foreach (Character c in copiedCharacters)
            {
                if (c != null && !c.dead)
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
            UIManager.Instance.HideCombatUI();
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
                UIManager.Instance.UpdateCharacterDeathCounter(c);
                UIManager.Instance.UpdateCharacterLifeStatus(c.CharacterId, false);
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
    public virtual void ShrineReached(int charaID)
    {
        Tile tile = inSceneCharacters[charaID].currentTile;
        CompetitionMiddleware.Instance.LogClearShrine(charaID, tile.col, tile.row);
        UIManager.Instance.UpdateCharacterGoalStatus(charaID);
        InSceneShrines[charaID].SetReached(true);
    }

    public virtual void ShrineUnReached(int charaID)
    {
        Tile tile = inSceneCharacters[charaID].currentTile;
        CompetitionMiddleware.Instance.LogRevokeShrine(charaID, tile.col, tile.row);
        UIManager.Instance.UpdateCharacterGoalStatus(charaID, false);
        InSceneShrines[charaID].SetReached(false);
        Debug.Log("refunded");
    }

    public virtual void CharacterDied(int charaID) {
        UIManager.Instance.UpdateCharacterLifeStatus(charaID, false);
        UIManager.Instance.UpdateCommonHUD();
        MapGenerator.Instance.UpdateFOWVisuals();
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
            eventQueue.Add(tile);
        }
    }

    protected virtual void CheckWinCondition() {
        if(gameStatus== GameStatus.LevelEnd || gameStatus == GameStatus.GameEnd) {
            return;
        }
        bool shrinesReached = true;
        foreach (Shrine shrine in InSceneShrines) {
            if (!shrine.Reached) {
                shrinesReached = false;
                break;
            }
        }

        bool onDoor = false;
        Character doorCharacter = null;
        foreach (Character character in inSceneCharacters) {
            if(character.currentTile == MapGenerator.Instance.DoorTile) {
                onDoor = true;
                doorCharacter = character;
                break;
            }
        }

        if (shrinesReached && onDoor) {
            CompetitionMiddleware.Instance.LogClearGoal(doorCharacter.CharacterId, MapGenerator.Instance.DoorTile.col, MapGenerator.Instance.DoorTile.row);
            if (isNetworkGame && PhotonNetwork.IsMasterClient) LogLevelResult();
            StopAllCoroutines();
            StartCoroutine(PrepareForNextLevel());
        }
    }


    // Called by Character.OnTriggerEnter(), when all three goals fetched and one character collide with the door after that
    // "Move" to next currLevel by reset all relevant constants, delete monsters and tiles (tiles done by map generator) this currLevel, and reset chara status
    public virtual void CheckGoalReached(int charaID) {
        /*bool level_finished = true;
        foreach (Shrine shrine in InSceneShrines) {
            if (!shrine.Reached) {
                level_finished = false;
                break;
            }
        }
        if (level_finished) {
            Tile tile = inSceneCharacters[charaID].currentTile;
            CompetitionMiddleware.Instance.LogClearGoal(charaID, tile.col, tile.row);
            if (isNetworkGame && PhotonNetwork.IsMasterClient) LogLevelResult();
            StopAllCoroutines();
            StartCoroutine(PrepareForNextLevel());
        }*/
        CheckWinCondition();
    }

    protected virtual IEnumerator PrepareForNextLevel() {
        //temporarily disable all physical interactions
        Physics.autoSimulation = false;
        gameStatus = GameStatus.LevelEnd;
        while (inSceneMonsters.Count != 0) {
            Monster m = inSceneMonsters[0];
            inSceneMonsters.Remove(m);
            Destroy(m.gameObject);
        }

        UIManager.Instance.LoadLevelEndUI();
        foreach (Character c in inSceneCharacters) {
            c.State = Character.CharacterState.Cheering;
        }

        eventQueue.Clear();
        UIManager.Instance.HideCharacterPinUI();
        //yield return new WaitForSeconds(5f);
        /*
        foreach (Character c in inSceneCharacters) {
            c.State = Character.CharacterState.Idle;
        }
        */


        Debug.Log("Moving to next currLevel.");
        currentLevel += 1;

        if (currentLevel <= MapGenerator.Instance.levelSpecs.Count) {

            eventQueue.Clear();
            //StopAllCoroutines();

            yield return WaitForExecutionSteps(5);
            foreach (Character c in inSceneCharacters)
            {
                c.State = Character.CharacterState.Idle;
            }
            foreach (Character c in inSceneCharacters) {
                c.StopAllCoroutines();
                c.QuickRespawn();
                UIManager.Instance.UpdateCharacterLifeStatus(c.CharacterId, true);
            }
            Physics.autoSimulation = true;
            StartCoroutine(StartLevel());
        }
        else {
            Physics.autoSimulation = true;
            Debug.Log("Game ends.");
            yield return WaitForExecutionSteps(5);
            CompetitionMiddleware.Instance.LogEndGame("Win");
            UIManager.Instance.DisplayVictoryScreen();
            gameStatus = GameStatus.GameEnd;
        }
    }

    public virtual void CheckLoseCondition()
    {
        int deadPlayerCount = 0;
        foreach (var character in inSceneCharacters)
        {
            if (character.dead)
            {
                deadPlayerCount++;
                //if any player exhausts all the lives and hasn't reach the shrine, the game loses
                if(character.Lives <= 0)
                {
                    if (!InSceneShrines[character.CharacterId].Reached)
                    {
                        Lose();
                    }
                }
            }
        }

        //if all players are dead, game loses
        if (deadPlayerCount >= 3)
        {
            Lose();
        }

    }
    protected virtual void Lose()
    {
        CompetitionMiddleware.Instance.LogEndGame("Loss");
        UIManager.Instance.DisplayLossScreen();
    }

    public virtual void SwitchCharacter(int index)
    {
    }

    protected void LogLevelResult()
    {
        Dictionary<string, Dictionary<string, string>>
            playerInfo = new Dictionary<string, Dictionary<string, string>>();
        for (int charID = 0; charID < 3; charID++)
        {
            playerInfo[inSceneCharacters[charID].config.characterName.ToLower()] = new Dictionary<string, string>
            {
                { "player_type", CompetitionMiddleware.Instance.RegisteredAgents.ContainsKey(charID) ? "ai" : "human" },
                { "player_id", CompetitionMiddleware.Instance.charID2PlayerID[charID] },
                { "health", inSceneCharacters[charID].Health.ToString() },
                { "max_health", inSceneCharacters[charID].config.StartingHealth.ToString() },
                { "lives", inSceneCharacters[charID].Lives.ToString() },
                { "max_lives", 3.ToString() },
                { "respawned", characterDied[charID].ToString() }
            };
        }
        
        NetworkMiddleware.Instance.CallLogLevelResult(playerInfo);
    }

    private void OnDestroy()
    {
        if(gameStatus != GameStatus.LevelStart)
        {
            Debug.Log("Game manager OnDestory getting called, destroying NetworkMiddleware, current gamestatus: " + gameStatus);
            Destroy(NetworkMiddleware.Instance.gameObject);
        }
    }

    /// <summary>
    /// Waits for a number of execution steps to pass before continuing.
    /// 
    /// This assumes the executionStepTime is usually 1, but in instant mode it is 0.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public IEnumerator WaitForExecutionSteps(float time) {
        if (excecutionStepTime <= 0) {
            yield break;
        }
        else {
            yield return new WaitForSeconds(time * excecutionStepTime);
        }
    }

    public bool InstantMode {
        get {
            return excecutionStepTime <= 0;
        }
    }
}
