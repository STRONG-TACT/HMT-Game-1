using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;

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
    protected int remainingCharacterCount = 3;
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

    // Called by map generator to update characters' position at the beginning of the level.
    // Characters are pre created in the scene, since they should always be three of them.
    public virtual void setCharaPosition(int ID, float x, float z)
    {
        Character targetChara = inSceneCharacters[ID];
        Vector3 newPosition = new Vector3(x, targetChara.transform.position.y, z);
        targetChara.setStartPos(newPosition);
    }



    protected virtual void Start() {        
        gameData = GameData.S;
        pinningSystem = PinningSystem.S;
        goalCount = 0;

        Debug.LogFormat("NetworkMiddleware.S.myCharacterID = {0}", NetworkMiddleware.S.myCharacterID);

#if HMT_BUILD
        localChar = inSceneCharacters[0]; //Agent'd don't care about the localChar
#else
        localChar = inSceneCharacters[(isNetworkGame) ? NetworkMiddleware.S.myCharacterID : 0];
#endif
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
        gameStatus = GameStatus.GetReady;
        CurrentRound = 0;
        UIManager.S.InitGameUI();
        UIManager.S.ResetTeamActionStatus();
        yield return new WaitForFixedUpdate();
        CompetitionMiddleware.Instance.LogStartLevel(IntegratedMapGenerator.Instance.CurrentLevelName);
        StartPlayerTurn();
    }


    protected virtual void StartPlayerTurn()
    {
        IntegratedMapGenerator.Instance.UpdateFOWVisuals();
        CurrentRound += 1;
        remainingCharacterCount = 3;

        foreach (Character chara in inSceneCharacters)
        {
            chara.ResetActionPoints();
            if (chara.ActionPointsRemaining == 0)
            {
                remainingCharacterCount -= 1;
            }
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

        if (remainingCharacterCount > 0) {
            localChar.FocusCharacter();
            UIManager.S.ShowCharacterPinUI();
            UIManager.S.ShowCommonHUD();
            UIManager.S.ResetTeamActionStatus();
        }
        else {
            EndPlayerPinningPhase();
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

    // Update params, if all end their pinning, move to planning phase
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
        //Debug.Log("Should start planning phase here.");
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

        if (remainingCharacterCount <= 0 || CheckPhaseEnd()) {
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
    // Update params, if all submitted their plan, move to moving phase
    public virtual void CheckPlanPhaseEnd() {
        if (remainingCharacterCount <= 0 || CheckPhaseEnd()) {
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

        //// A flag, whether this round of step triggers a combat
        //bool hasCombat = false;
        bool allCharactersDone = false;
        while (!allCharactersDone)
        {
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
            Debug.Log("Moving phase ended.");
            pinningSystem.ClearCurrentTurnPins();
        }
        StartMonsterTurn();
    }

    // Start monster moving phase
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

    // Monsters moving step by step
    // Same with chara move by step, when events happened, deal with them and come back
    protected virtual IEnumerator MonsterMoveByStep()
    {
        bool allMonstersDone = false;


        while (!allMonstersDone)
        {
            foreach (Monster m in inSceneMonsters)
            {
                if (!m.turnFinished)
                {
                    StartCoroutine(m.TakeNextMove(excecutionStepTime));
                }
            }
            bool doneMoving;
            do
            {
                doneMoving = true;
                foreach (Monster m in inSceneMonsters)
                {
                    if (m.moving)
                    {
                        doneMoving = false;
                    }
                }
                yield return null;
            } while (!doneMoving);

            if (eventQueue.Count != 0)
            {
                yield return ExecuteCombatOneByOne();
                break;
            }

            //after all combat done, if multiple monster oppcuies the same tile, they move until every tile contains at most 1 monster
            foreach (Monster mon in inSceneMonsters)
            {
                Tile currentTile = mon.currentTile;
                if (currentTile.enemyList.Count > 1)
                {
                    int startRow = currentTile.row;
                    int startCol = currentTile.col;
                    bool foundSpot = false;
                    //move monster to nearest avaiable tile where there is no character and no other monster on it
                    for (int distance = 1; distance < Math.Max(IntegratedMapGenerator.Instance.Map.GetLength(0), IntegratedMapGenerator.Instance.Map.GetLength(1)); distance++)
                    {
                        if (!foundSpot)
                        {
                            List<Tile> availablePos = new List<Tile>();
                            for (int rowOffset = distance * -1; rowOffset < distance + 1; rowOffset++)
                            {
                                for (int colOffset = distance * -1; colOffset < distance + 1; colOffset++)
                                {
                                    int targetRow = startRow + rowOffset;
                                    int targetCol = startCol + colOffset;
                                    if (IntegratedMapGenerator.Instance.InMap(targetRow, targetCol))
                                    {
                                        Tile targetTile = IntegratedMapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                        if (targetTile.tileType == Tile.ObstacleType.None && targetTile.enemyList.Count == 0
                                            && targetTile.charaList.Count == 0 && targetTile.gameObject.tag != "Rock" && targetTile.gameObject.tag != "Trap" && targetTile.gameObject.layer != LayerMask.NameToLayer("Impassible"))
                                        {
                                            availablePos.Add(targetTile);
                                        }
                                    }
                                }
                            }
                            if (availablePos.Count > 0)
                            {
                                foundSpot = true;
                                //select a random tile in the avaiable positions to move
                                int randomIndex = UnityEngine.Random.Range(0, availablePos.Count);
                                Tile selectedTile = availablePos[randomIndex];
                                yield return StartCoroutine(mon.moveToTargetLocation(selectedTile.transform.position, excecutionStepTime));
                            }
                        }
                    }
                }
            }

            allMonstersDone = true;
            foreach (Monster m in inSceneMonsters)
            {
                if (!m.turnFinished)
                {
                    allMonstersDone = false;
                    break;
                }
            }
        }

        Debug.Log("Monster moving phase ended.");
        foreach (Character chara in inSceneCharacters)
        {
            if (chara.dead)
            {
                chara.RespawnCountdown();
                //update life status ui if character respawns
                if (!chara.dead) {
                    UIManager.S.UpdateCharacterLifeStatus(chara.CharacterId, true);
                }
            }
        }
        StartPlayerTurn();
    }

    // Execute all the events happened within one step time
    // Combat.ExecuteCombat() is the actual combat function
    protected virtual IEnumerator ExecuteCombatOneByOne()
    {
        Debug.LogFormat("Exectuing {0} events in queue.", eventQueue.Count);

        while (eventQueue.Count != 0)
        {
            bool win = false;
            Tile t = eventQueue.Dequeue();

            Debug.LogFormat("Processing Event at {0}, {1}", t.row, t.col);
            Tile.FogOfWarState fow_state = t.fogOfWarDictionary[localChar.CharacterId];
            bool visibility;
            if (fow_state == Tile.FogOfWarState.Visible)
            {
                visibility = true;
            }
            else
            {
                visibility = false;
            }
            switch (t.tileType)
            {
                case Tile.ObstacleType.None:
                    win = Combat.ExecuteCombat(Combat.FightType.Monster, t, visibility);
                    break;
                case Tile.ObstacleType.Trap:
                    win = Combat.ExecuteCombat(Combat.FightType.Trap, t, visibility);
                    break;
                case Tile.ObstacleType.Rock:
                    win = Combat.ExecuteCombat(Combat.FightType.Rock, t, visibility);
                    break;
            }
            //play attack animation for all characters and monster on the tile
            //make a copy of the characters and monsters that are originally in the tile. So that if a character or monster moves elsewhere, we can still find it
            List<Character> copiedCharacters = new List<Character>(t.charaList);
            List<Monster> copiedEnemies = new List<Monster>(t.enemyList);
            foreach (Character c in copiedCharacters)
            {
                c.State = Character.CharacterState.Attacking;
            }
            foreach (Monster mo in copiedEnemies)
            {
                mo.State = Monster.CharacterState.Attacking;
            }

            if (win)
            {
                // if the character(s) won the battle, destory the enemies
                Debug.Log("Character won.");
                switch (t.tileType)
                {
                    case Tile.ObstacleType.None:
                        foreach (Monster m in t.enemyList)
                        {
                            m.Kill(excecutionStepTime);
                            inSceneMonsters.Remove(m);
                        }
                        t.enemyList.Clear();
                        break;
                    case Tile.ObstacleType.Trap:
                    case Tile.ObstacleType.Rock:
                        var copy = new Dictionary<int, Tile.FogOfWarState>();
                        foreach (KeyValuePair<int, Tile.FogOfWarState> entry in t.fogOfWarDictionary)
                        {
                           copy.Add(entry.Key, entry.Value);
                        }
                        GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                        Tile newTile = opentile.GetComponent<Tile>();
                        newTile.fogOfWarDictionary = copy;
                        newTile.row = t.row;
                        newTile.col = t.col;
                        IntegratedMapGenerator.Instance.SetTileAt(newTile.row, newTile.col, newTile);
                        Destroy(t.gameObject);
                        break;
                }

                //problem -> need to fix
                IntegratedMapGenerator.Instance.UpdateFOWVisuals();
            }
            else
            {
                // If not, reduce health except rock
                // If character's turn, all remaining steps should be cleared.
                Debug.Log("Enemy won.");

                List<Character> deadChara = new List<Character>();
                List<Character> aliveChara = new List<Character>();
                switch (t.tileType)
                {
                    case Tile.ObstacleType.None:
                        reduceCharacterHealth(t.charaList, deadChara, aliveChara);
                        if (gameStatus == GameStatus.Player_Moving)
                        {
                            clearCharacterMoves(t.charaList);
                        }

                        // if player got defeated by monster on a shrine tile
                        // and the player correspond to that shrine tile
                        // then we should "unreach" the shrine
                        if (t.gameObject.CompareTag("Goal"))
                        {
                            Shrine shrine = t.gameObject.GetComponentInChildren<Shrine>();
                            foreach (var character in t.charaList)
                            {
                                if (shrine.CheckShrineType(character))
                                {
                                    GoalUnReached(character.CharacterId);
                                    shrine.ReturnOrb();
                                }
                            }
                        }
                        break;
                    case Tile.ObstacleType.Trap:
                        reduceCharacterHealth(t.charaList, deadChara, aliveChara);
                        clearCharacterMoves(t.charaList);
                        Dictionary<int, Tile.FogOfWarState> fogOfWarDictionary = new Dictionary<int, Tile.FogOfWarState>();
                        foreach (KeyValuePair<int, Tile.FogOfWarState> entry in t.fogOfWarDictionary)
                        {
                            fogOfWarDictionary.Add(entry.Key, entry.Value);
                        }
                        GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                        Tile newTile = opentile.GetComponent<Tile>();
                        newTile.fogOfWarDictionary = fogOfWarDictionary;
                        newTile.row = t.row;
                        newTile.col = t.col;
                        IntegratedMapGenerator.Instance.SetTileAt(newTile.row, newTile.col, newTile);
                        Destroy(t.gameObject);
                        break;
                    case Tile.ObstacleType.Rock:
                        foreach (Character c in t.charaList)
                        {
                            aliveChara.Add(c);
                        }

                        clearCharacterMoves(t.charaList);
                        break;
                }

                foreach (Character c in deadChara)
                {
                    t.charaList.Remove(c);
                }
                if (gameStatus == GameStatus.Player_Moving)
                {
                    foreach (Character c in aliveChara)
                    {
                        c.Retreat();
                    }
                }
                else if (gameStatus == GameStatus.Monster_Moving && aliveChara.Count > 0)
                {
                    foreach (Monster m in t.enemyList)
                    {
                        m.Retreat();
                    }
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
            c.DecrementHealth();

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


    // When chara fail a combat, clear all the remaining moves in queue this round
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
        UIManager.S.UpdateCharacterGoalStatus(charaID);
        goalCount += 1;
    }

    public virtual void GoalUnReached(int charaID)
    {
        UIManager.S.UpdateCharacterGoalStatus(charaID, false);
        goalCount -= 1;
        Debug.Log("refunded");
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
    // "Move" to next level by reset all relevant constants, delete monsters and tiles (tiles done by map generator) this level, and reset chara status
    public virtual void NextLevel()
    {
        StartCoroutine(PrepareForNextLevel());
    }

    protected virtual IEnumerator PrepareForNextLevel()
    {

        while (inSceneMonsters.Count != 0)
        {
            Monster m = inSceneMonsters[0];
            inSceneMonsters.Remove(m);
            Destroy(m.gameObject);
        }

        UIManager.S.LoadLevelEndUI();
        foreach (Character c in inSceneCharacters)
        {
            c.State = Character.CharacterState.Cheering;
        }

        eventQueue.Clear();
        UIManager.S.HideCharacterPinUI();
        yield return new WaitForSeconds(5f);
        foreach (Character c in inSceneCharacters)
        {
            c.State = Character.CharacterState.Idle;
        }


        Debug.Log("Moving to next level.");
        currentLevel += 1;

        if (currentLevel <= gameData.levelTextFiles.Length)
        {
            goalCount = 0;
            remainingCharacterCount = 3;
            eventQueue.Clear();
            StopAllCoroutines();

            foreach (Character c in inSceneCharacters)
            {
                c.StopAllCoroutines();
                c.QuickRespawn();
                UIManager.S.UpdateCharacterLifeStatus(c.CharacterId, true);
            }

            while (inSceneMonsters.Count != 0)
            {
                Monster m = inSceneMonsters[0];
                inSceneMonsters.Remove(m);
                Destroy(m.gameObject);
            }

            UIManager.S.ResetTeamGoalStatus();
            UIManager.S.ResetTeamActionStatus();
            IntegratedMapGenerator.Instance.LoadLevel(gameData.levelTextFiles[currentLevel - 1]);

            StartCoroutine(StartLevel());
        }
        else
        {
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
