using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;

public class NetworkGameManager : MonoBehaviour
{
    // ======== Managers ========
    [Header("Managers")]
    public NetworkUIManager uiManager;
    public NetworkGameData gameData;
    public NetworkPlayer player;
    public NetworkPinningSystem pinningSystem;
    
    // ======== Game States ======== 
    [Header("Game States")]
    public GameStatus gameStatus = GameStatus.GetReady;
    public int goalCount = 0;
    private int remainingCharacterCount = 3;
    private int pinningSubmittedCount = 0;
    private int planSubmittedCount = 0;
    private Queue<NetworkTile> eventQueue = new Queue<NetworkTile>();
    private Coroutine currentCoroutine = null;
    public int currentLevel = 1;
    
    // ======== Pointer to In Game Prefabs ========
    [Header("In Game Prefabs")]
    [Tooltip("Set Manually")]
    public List<NetworkCharacter> inSceneCharacters = new List<NetworkCharacter>();
    [Tooltip("Set automatically by MapGenerator")]
    public List<NetworkMonster> inSceneMonsters = new List<NetworkMonster>();
    // Pointer to the character that local client controls
    [HideInInspector] public NetworkCharacter localChar;

    public static NetworkGameManager S;
    
    public float excecutionStepTime = 1;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
        
        goalCount = 0;
        localChar = inSceneCharacters[NetworkMiddleware.S.myCharacterID];
        player.myCharacter = localChar;
        localChar.FocusCharacter();
        
        // RPC to know presence
    }
    
    // Called by map generator to update characters' position at the beginning of the level.
    // Characters are pre created in the scene, since they should always be three of them.
    public void setCharaPosition(int ID, float x, float z)
    {
        NetworkCharacter targetChara = inSceneCharacters[ID];
        Vector3 newPosition = new Vector3(x, targetChara.transform.position.y, z);
        targetChara.setStartPos(newPosition);
    }

    private void Start()
    {
        StartCoroutine(StartLevel());
    }

    IEnumerator StartLevel()
    {
        gameStatus = GameStatus.GetReady;
        uiManager.InitGameUI();
        // center camera to player
        NetworkCameraManager.S.ChangeTargetCharacter(localChar.CharacterId);
        NetworkCameraManager.S.RecenterCamera();
        // this call will mark every tile as unseen
        NetworkMapGenerator.Instance.updateFogOfWar_map(localChar.CharacterId);
        yield return new WaitForSeconds(2.0f); // wait for physic system to kick off
        // this call actually setup the correct FOW
        // the delay is needed because internal state of FOW needs physics trigger to work
        NetworkMapGenerator.Instance.updateFogOfWar_map(localChar.CharacterId);
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        remainingCharacterCount = 3;

        foreach (NetworkCharacter chara in inSceneCharacters)
        {
            chara.ResetActionPoints();
            if (chara.ActionPointsRemaining == 0) {
                remainingCharacterCount -= 1;
            }
        }
        PreparePlayerPinningPhase();
    }

    private void PreparePlayerPinningPhase()
    {
        gameStatus = GameStatus.Player_Pinning;
        uiManager.UpdateGamePhaseInfo();
        pinningSubmittedCount = 3 - remainingCharacterCount;
        
        localChar.StartPingPhase();
        StartPlayerPinningPhase();
    }

    private void StartPlayerPinningPhase()
    {
        if (remainingCharacterCount > 0)
        {
            localChar.FocusCharacter();
            player.UpdateCharacterUI();
            uiManager.ShowCharacterPinUI();
        }
        else
        {
            PreparePlayerPlanningPhase();
        }
    }

    public void NewPlayerPin()
    {
        player.PlacePinByFocusedCharacter();
        uiManager.UpdateActionPointsRemaining(localChar.ActionPointsRemaining);
        CheckPingPhaseEnd();
    }
    
    public void CheckPingPhaseEnd() {
        bool phaseEnd = true;
        foreach(NetworkCharacter character in inSceneCharacters) {
            if (!character.ReadyForNextPhase) {
                phaseEnd = false;
            }
        }
        if (phaseEnd) {
            EndPlayerPinningPhase();
        }
    }
    
    private void EndPlayerPinningPhase() {
        Debug.Log("Pinning phase ended.");

        foreach (NetworkCharacter chara in inSceneCharacters) {
            chara.EndPingPhase();
        }

        Debug.Log("Should start planning phase here.");
        PreparePlayerPlanningPhase();
    }

    // Prepare for player planning phase
    // Reset all the player planning parameters
    // If there are characters dead, update relevant params so they will skip planning
    private void PreparePlayerPlanningPhase()
    {
        gameStatus = GameStatus.Player_Planning;
        uiManager.UpdateGamePhaseInfo();
        
        foreach (NetworkCharacter chara in inSceneCharacters) {
            chara.StartPlanningPhase();
        }
        localChar.indicator.SetActive(true);


        if (remainingCharacterCount > 0) {
            CheckPlanPhaseEnd();
            player.UpdateCharacterUI();
        }
        else {
            StartCharacterMovingPhase();
        }
    }
    
    public void CheckPlanPhaseEnd() {
        bool phaseEnd = true;
        foreach (NetworkCharacter character in inSceneCharacters) {
            if (!character.ReadyForNextPhase) {
                phaseEnd = false;
            }
        }
        if (phaseEnd) {
            EndPlayerPlanningPhase();
        }
    }
    
    private void EndPlayerPlanningPhase() {
        Debug.Log("Planning phase ended.");
        uiManager.HideCharacterPlanUI();

        foreach (NetworkCharacter chara in inSceneCharacters) {
            chara.EndPlanning();
        }

        StartCharacterMovingPhase();
    }

    private void StartCharacterMovingPhase() {
        gameStatus = GameStatus.Player_Moving;
        uiManager.UpdateGamePhaseInfo();
        //moveFinishedCount = 0;       
        eventQueue = new Queue<NetworkTile>();

        currentCoroutine = StartCoroutine(CharacterMoveByStep());
    }
    
    // Characters move step by step
    // If events happen, deal with all the events and then back to moving
    private IEnumerator CharacterMoveByStep() {

        //// A flag, whether this round of step triggers a combat
        //bool hasCombat = false;

        bool allCharactersDone = false;
        
        while (!allCharactersDone) {
            foreach (NetworkCharacter chara in inSceneCharacters) {
                StartCoroutine(chara.TakeNextMove(excecutionStepTime));
            }
            bool doneMoving;
            do {
                doneMoving = true;
                foreach (NetworkCharacter chara in inSceneCharacters) {
                    if (chara.moving) {
                        doneMoving = false;
                    }
                }
                yield return null;
            } while(!doneMoving);

            if (eventQueue.Count != 0) {
                yield return ExecuteCombatOneByOne();
                //hasCombat = true;
            }

            allCharactersDone = true;
            foreach(NetworkCharacter character in inSceneCharacters) {
                if (character.ActionPlan.Count > 0) {
                    allCharactersDone = false;
                    break;
                }
            }
        }

        Debug.Log("Moving phase ended.");
        pinningSystem.ClearCurrentTurnPins();
        StartMonsterTurn();
        // StartPlayerTurn();
    }
    
    // Start monster moving phase
    private void StartMonsterTurn()
    {
        gameStatus = GameStatus.Monster_Moving;
        uiManager.UpdateGamePhaseInfo();
        //moveFinishedCount = 0;

        foreach (NetworkMonster m in inSceneMonsters) {
            m.MonsterTurnStart();
        }

        currentCoroutine = StartCoroutine(MonsterMoveByStep());
    }

    // Monsters moving step by step
    // Same with chara move by step, when events happened, deal with them and come back
    private IEnumerator MonsterMoveByStep() {
        bool allMonstersDone = false;


        while (!allMonstersDone) {
            foreach(NetworkMonster m in inSceneMonsters) {
                if (!m.turnFinished) {
                    StartCoroutine(m.TakeNextMove(excecutionStepTime));
                }
            }
            bool doneMoving;
            do {
                doneMoving = true;
                foreach(NetworkMonster m in inSceneMonsters) {
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
            foreach (NetworkMonster mon in inSceneMonsters)
            {
                NetworkTile currentTile = mon.currentTile;
                if (currentTile.enemyList.Count > 1)
                {
                    int startRow = currentTile.row;
                    int startCol = currentTile.col;
                    bool foundSpot = false;
                    //move monster to nearest avaiable tile where there is no character and no other monster on it
                    for (int distance = 1; distance < Math.Max(NetworkMapGenerator.Instance.Map.GetLength(0), NetworkMapGenerator.Instance.Map.GetLength(1)); distance++)
                    {
                        if(!foundSpot)
                        {
                            List<NetworkTile> availablePos = new List<NetworkTile>();
                            for (int rowOffset = distance * -1; rowOffset < distance + 1; rowOffset++)
                            {
                                for (int colOffset = distance * -1; colOffset < distance + 1; colOffset++)
                                {
                                    int targetRow = startRow + rowOffset;
                                    int targetCol = startCol + colOffset;
                                    if (NetworkMapGenerator.Instance.InMap(targetRow, targetCol))
                                    {
                                        NetworkTile targetTile = NetworkMapGenerator.Instance.GetTileAt(targetRow, targetCol);
                                        if (targetTile.tileType == NetworkTile.ObstacleType.None && targetTile.enemyList.Count == 0 && targetTile.charaList.Count == 0)
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
                                NetworkTile selectedTile = availablePos[randomIndex];
                                yield return StartCoroutine(mon.moveToTargetLocation(selectedTile.transform.position, excecutionStepTime));
                            }
                        }
                    }
                }
            }

            allMonstersDone = true;
            foreach(NetworkMonster m in inSceneMonsters) {
                if (!m.turnFinished) {
                    allMonstersDone = false;
                    break;
                }
            }
        }

        Debug.Log("Monster moving phase ended.");
        foreach (NetworkCharacter chara in inSceneCharacters) {
            if (chara.dead) {
                chara.RespawnCountdown();
            }
        }
        StartPlayerTurn();
    }
    
    private IEnumerator ExecuteCombatOneByOne()
    {
        Debug.LogFormat("Exectuing {0} events in queue.", eventQueue.Count);

        while (eventQueue.Count != 0)
        {
            bool win = false;
            NetworkTile t = eventQueue.Dequeue();

            Debug.LogFormat("Processing Event at {0}, {1}", t.row, t.col);

            NetworkCameraManager.S.ChangeTargetCharacter(t.charaList[0].CharacterId);
            switch (t.tileType) {
                case NetworkTile.ObstacleType.None:
                    win = Combat.ExecuteCombat(Combat.FightType.Monster, t, uiManager);
                    break;
                case NetworkTile.ObstacleType.Trap:
                    win = Combat.ExecuteCombat(Combat.FightType.Trap, t, uiManager);
                    break;
                case NetworkTile.ObstacleType.Rock:
                    win = Combat.ExecuteCombat(Combat.FightType.Rock, t, uiManager);
                    break;
            }
            //play attack animation for all characters and monster on the tile
            //make a copy of the characters and monsters that are originally in the tile. So that if a character or monster moves elsewhere, we can still find it
            List<NetworkCharacter> copiedCharacters = new List<NetworkCharacter>(t.charaList);
            List<NetworkMonster> copiedEnemies = new List<NetworkMonster>(t.enemyList);
            foreach (NetworkCharacter c in copiedCharacters)
            {
                c.State = NetworkCharacter.CharacterState.Attacking;
            }
            foreach (NetworkMonster mo in copiedEnemies) {
                mo.State = NetworkMonster.CharacterState.Attacking;
            }
                
            if (win) {
                // if the character(s) won the battle, destory the enemies
                Debug.Log("Character won.");
                switch (t.tileType) {
                    case NetworkTile.ObstacleType.None:
                        foreach (NetworkMonster m in t.enemyList) {
                            m.Kill(excecutionStepTime);
                            inSceneMonsters.Remove(m);
                        }
                        t.enemyList.Clear();
                        break;
                    case NetworkTile.ObstacleType.Trap:
                    case NetworkTile.ObstacleType.Rock:
                        GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                        NetworkTile newTile = opentile.GetComponent<NetworkTile>();
                        newTile.row = t.row;
                        newTile.col = t.col;
                        NetworkMapGenerator.Instance.SetTileAt(newTile.row, newTile.col, newTile);
                        Destroy(t.gameObject);
                        break;
                }
            }
            else {
                // If not, reduce health except rock
                // If character's turn, all remaining steps should be cleared.
                Debug.Log("Enemy won.");

                List<NetworkCharacter> deadChara = new List<NetworkCharacter>();
                List<NetworkCharacter> aliveChara = new List<NetworkCharacter>();
                switch (t.tileType) {
                    case NetworkTile.ObstacleType.None:
                        reduceCharacterHealth(t.charaList, deadChara, aliveChara);
                        if (gameStatus == GameStatus.Player_Moving)
                        {
                            clearCharacterMoves(t.charaList);
                        }
                        break;
                    case NetworkTile.ObstacleType.Trap:
                        reduceCharacterHealth(t.charaList, deadChara, aliveChara);
                        clearCharacterMoves(t.charaList);
                        Dictionary<int, NetworkTile.FogOfWarState> fogOfWarDictionary = new Dictionary<int, NetworkTile.FogOfWarState>();
                        foreach (KeyValuePair<int, NetworkTile.FogOfWarState> entry in t.fogOfWarDictionary)
                        {
                            fogOfWarDictionary.Add(entry.Key, entry.Value);
                        }
                        GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                        NetworkTile newTile = opentile.GetComponent<NetworkTile>();
                        newTile.fogOfWarDictionary = fogOfWarDictionary;
                        newTile.row = t.row;
                        newTile.col = t.col;
                        NetworkMapGenerator.Instance.SetTileAt(newTile.row, newTile.col, newTile);
                        Destroy(t.gameObject);
                        break;
                    case NetworkTile.ObstacleType.Rock:
                        foreach (NetworkCharacter c in t.charaList) {
                            aliveChara.Add(c);
                        }

                        clearCharacterMoves(t.charaList);
                        break;
                }

                foreach (NetworkCharacter c in deadChara) {
                    t.charaList.Remove(c);
                }
                if (gameStatus == GameStatus.Player_Moving)
                {
                    foreach (NetworkCharacter c in aliveChara)
                    {
                        c.Retreat();
                    }
                }
                else if (gameStatus == GameStatus.Monster_Moving && aliveChara.Count > 0)
                {
                    foreach (NetworkMonster m in t.enemyList)
                    {
                        m.Retreat();
                    }
                }
            }

            //TODO this should probably be waiting for a button click in the future.
            yield return new WaitForSeconds(2*excecutionStepTime);
            foreach (NetworkCharacter c in copiedCharacters)
            {
                if (c != null)
                {
                    c.State = NetworkCharacter.CharacterState.Idle;
                }
            }
            foreach (NetworkMonster mo in copiedEnemies)
            {
                if(mo != null)
                {
                    mo.State = NetworkMonster.CharacterState.Idle;
                }
            }
            uiManager.HideCombatUI();
        }
        yield break;
        NetworkMapGenerator.Instance.updateFogOfWar_map(localChar.CharacterId);
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
    
    private void reduceCharacterHealth(List<NetworkCharacter> charaList, List<NetworkCharacter> deadChara, List<NetworkCharacter> aliveChara)
    {
        foreach (NetworkCharacter c in charaList)
        {
            c.DecrementHealth();

            if (c.dead)
            {
                deadChara.Add(c);
            }
            else
            {
                aliveChara.Add(c);
            }
        }
    }
    
    // When chara fail a combat, clear all the remaining moves in queue this round
    private void clearCharacterMoves(List<NetworkCharacter> charaList ) {
        foreach (NetworkCharacter c in charaList) {
            //Debug.LogFormat("Clear plan: {0}", c.name);
            c.ActionPlan.Clear();
        }
    }
    
    public void GoalReached(int charaID)
    {
        uiManager.UpdateGoalStatus(charaID);
        goalCount += 1;
    }
    
    public void updateEventQueue(NetworkTile tile) {
        if(gameStatus != GameStatus.Player_Moving && gameStatus != GameStatus.Monster_Moving) {
            Debug.LogWarningFormat("Event Generated during the {0} Phase, ignoring it.", gameStatus);
            return;
        }

        Debug.LogFormat("Event generated at {0}, {1}, of type {2}", tile.row, tile.col, tile.tileType);
        if (!eventQueue.Contains(tile)) {
            eventQueue.Enqueue(tile);
        }
    }
    
    // Called by LocalCharacter.OnTriggerEnter(), when all three goals fetched and one character collide with the door after that
    // "Move" to next level by reset all relevant constants, delete monsters and tiles (tiles done by map generator) this level, and reset chara status
    public void NextLevel()
    {
        Debug.Log("Moving to next level.");
        currentLevel += 1;

        if (currentLevel <= gameData.levelTextFiles.Length) {
            goalCount = 0;
            remainingCharacterCount = 3;
            eventQueue.Clear();
            StopAllCoroutines();

            foreach (NetworkCharacter c in inSceneCharacters) {
                c.StopAllCoroutines();
                c.QuickRespawn();
            }

            while (inSceneMonsters.Count != 0)
            {
                NetworkMonster m = inSceneMonsters[0];
                inSceneMonsters.Remove(m);
                Destroy(m.gameObject);
            }

            uiManager.ResetGoalStatus();
            NetworkMapGenerator.Instance.LoadLevel(gameData.levelTextFiles[currentLevel - 1]);

            StartCoroutine(StartLevel());
        }
        else
        {
            Debug.Log("Game ends.");
            gameStatus = GameStatus.GameEnd;
        }
    }
}
