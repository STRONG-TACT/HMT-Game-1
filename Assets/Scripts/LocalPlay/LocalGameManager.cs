using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Photon.Pun.Demo.PunBasics;
using UnityEditor;

public class LocalGameManager : MonoBehaviour
{
    public enum GameStatus { GetReady, Player_Pinning, Player_Planning, Player_Moving, Monster_Moving, Animation_Pause, GameEnd }

    public static LocalGameManager Instance = null;
    public LocalGameData gameData { get; private set; }
    private LocalUIManager uiManager;
    public LocalPlayer player;
    public LocalPinningSystem pinningSystem;
    public bool debugRPCReceipts = false;

    public bool isFirstLevel = true;
    public int currentLevel = 1;

    public float excecutionStepTime = 1;

    [Tooltip("The in-scene pointers to the character prefabs")]
    public List<LocalCharacter> inSceneCharacters = new List<LocalCharacter>();

    [Tooltip("The in-scene pointers to the monster prefabs")]
    public List<LocalMonster> inSceneMonsters = new List<LocalMonster>();

    public Character mainCharacter;
    //[HideInInspector] public List<int> playerIDs = new List<int>(); // All the characters' Photon ViewID
    public int goalCount; // Goal collected, shown on the UI
    private int remainingCharacterCount = 3; // #Characters that are alive

    public GameStatus gameStatus = GameStatus.GetReady;
    private int pinningSubmittedCount = 0;
    private int planSubmittedCount = 0;
    private bool[] isSubmitted;
    private bool[] isEmpty;
    private bool[] isFull;
    public int moveFinishedCount = 0;

    //private List<LocalCharacter.Direction> DwarfPlan;
    //private List<LocalCharacter.Direction> GiantPlan;
    //private List<LocalCharacter.Direction> HumanPlan;

    //private Queue<LocalCharacter.Direction> DwarfMoves;
    //private Queue<LocalCharacter.Direction> GiantMoves;
    //private Queue<LocalCharacter.Direction> HumanMoves;
    private Queue<LocalTile> eventQueue;

    // When awake, find all the managers and data.
    // Future update: Set isFirstLevel (currentLevel should by default be 1, may delete this step in future if we stick in the same scene)
    private void Awake()
    {
        Instance = this;
        uiManager = FindObjectOfType<LocalUIManager>();
        gameData = FindObjectOfType<LocalGameData>();
        player = FindObjectOfType<LocalPlayer>();
        pinningSystem = FindObjectOfType<LocalPinningSystem>();
        goalCount = 0;

        if (currentLevel == 1)
        {
            isFirstLevel = true;
        }
        else
        {
            isFirstLevel = false;
        }
    }

    // Called by map generator to update characters' position at the beginning of the level.
    // Characters are pre created in the scene, since they should always be three of them.
    public void setCharaPosition(int ID, float x, float z)
    {
        LocalCharacter targetChara = inSceneCharacters[ID];
        Vector3 newPosition = new Vector3(x, inSceneCharacters[ID].transform.position.y, z);
        targetChara.setStartPos(newPosition);
    }

    // When start, play tutorial if the scene begins with level one
    // Future update: May not need the condition check if it always begin with the level one
    private void Start()
    {
        if (!isFirstLevel)
        {
            StartLevel();
        }
        else {
            StartCoroutine(uiManager.PlayTutorial());
        }
    }

    // Start level
    public void StartLevel()
    {
        gameStatus = GameStatus.GetReady;
        uiManager.InitGameUI();
        StartPlayerTurn();
    }

    // Start player turn
    private void StartPlayerTurn()
    {
        remainingCharacterCount = 3;

        foreach (LocalCharacter chara in inSceneCharacters)
        {
            int actionPoints = chara.resetActionPoints();
            if (actionPoints == 0)
            {
                remainingCharacterCount -= 1;
            }
        }
        PreparePlayerPinningPhase();
    }

    // Prepare for player pinning phase
    // Reset all the player pinning parameters
    // If there are characters dead, update relevant params so they will skip pinning
    private void PreparePlayerPinningPhase()
    {
        //Player start to plan their moves
        gameStatus = GameStatus.Player_Pinning;
        pinningSubmittedCount = 3 - remainingCharacterCount;

        isSubmitted = new bool[3] { false, false, false };

        foreach (LocalCharacter chara in inSceneCharacters)
        {
            if (chara.dead)
            {
                isSubmitted[chara.CharacterId] = true;
            }
        }

        StartPlayerPinningPhase();
    }

    private void StartPlayerPinningPhase()
    {
        // Local version of player planning stage
        if (remainingCharacterCount > 0)
        {
            player.myCharacter = inSceneCharacters[0];
            player.myCharacter.startPinning();
            player.charaSwitched(0, isSubmitted[0], false, false);

            uiManager.ShowCharacterPinUI(inSceneCharacters[0].name, getActionPoints(0), inSceneCharacters[0].dead);
        }
        else
        {
            PreparePlayerPlanningPhase();
        }
    }

    public void newPlayerPin()
    {
        player.myCharacter.pinNew();
        int actionPoints = getActionPoints(player.myCharacter.CharacterId);
        if (actionPoints == 0)
        {
            player.pinFinished();
            pinningSubmittedCount += 1;
        }
        uiManager.ShowMoveLeft(actionPoints);

        if (pinningSubmittedCount == 3)
        {
            endPlayerPinningPhase();
        }
    }

    // 
    // Update params, if all end their pinning, move to planning phase
    public void newPinFinished(int index)
    {
        // When a player finish pinning
        if (!isSubmitted[index])
        {
            pinningSubmittedCount += 1;
            isSubmitted[index] = true;

            player.pinFinished();
        }

        if (pinningSubmittedCount == 3)
        {
            endPlayerPinningPhase();
        }
    }

    private void endPlayerPinningPhase()
    {
        Debug.Log("Pinning phase ended.");
        uiManager.HideCharacterPinUI();

        foreach (LocalCharacter chara in inSceneCharacters)
        {
            chara.pausePinning();
        }

        PreparePlayerPlanningPhase();
    }

    // Prepare for player planning phase
    // Reset all the player planning parameters
    // If there are characters dead, update relevant params so they will skip planning
    private void PreparePlayerPlanningPhase()
    {
        //Player start to plan their moves
        gameStatus = GameStatus.Player_Planning;

        //DwarfPlan = new List<LocalCharacter.Direction>();
        //GiantPlan = new List<LocalCharacter.Direction>();
        //HumanPlan = new List<LocalCharacter.Direction>();

        isSubmitted = new bool[3] { false, false, false };
        isEmpty = new bool[3] { true, true, true };
        isFull = new bool[3] { false, false, false };

        foreach (LocalCharacter chara in inSceneCharacters)
        {
            chara.ResetPlan();
            int moveLeft = chara.ActionPointsRemaining;
            if (moveLeft == 0)
            {
                isSubmitted[chara.CharacterId] = true;
                planSubmittedCount += 1;
            }
        }

        StartPlayerPlanningPhase();
    }

    // Start player planning phase
    // If there are characters alive, start with the first alive character. All skip planning.
    private void StartPlayerPlanningPhase()
    {
        // Local version of player planning stage
        if (remainingCharacterCount > 0)
        {
            player.myCharacter = inSceneCharacters[0];
            player.myCharacter.startPlanning();
            player.charaSwitched(0, isSubmitted[0], isEmpty[0], isFull[0]);

            uiManager.ShowCharacterPlanUI(inSceneCharacters[0].name, getActionPoints(0), inSceneCharacters[0].dead);
        }
        else
        {
            StartCharacterMovingPhase();
        }
    }

    // Called by LocalPlayer.AddMoveToFocusedCharacter(), when player press direction buttons.
    // Add the move to corresponding queue, and confirm with current LocalCharacter.
    public void newPlayerMovePlan(int index, LocalCharacter.Direction move)
    {
     
        player.myCharacter.AddActionToPlan(move);
        if (inSceneCharacters[index].ActionPointsRemaining == 0) {
            isFull[index] = true;
        }
        else if (inSceneCharacters[index].ActionPlan.Count == 1) {
            isEmpty[index] = false;
        }
        player.UpdatePlanUI(false, false, isFull[index]);


        uiManager.ShowMoveLeft(getActionPoints(index));
    }

    // Called by LocalPlayer.switchCharacter(), when player press chara buttons.
    // Update ui text/icon, pass params about current chara planning status to LocalPlayer.
    // Update changes with camera control.
    public void switchCharacter(int index)
    {
        if (gameStatus == GameStatus.Player_Pinning)
        {
            player.myCharacter.pausePinning();

            player.myCharacter = inSceneCharacters[index];
            player.myCharacter.startPinning();
            uiManager.ShowCharacterPinUI(inSceneCharacters[index].name, getActionPoints(index), inSceneCharacters[index].dead);
            player.charaSwitched(index, isSubmitted[index], false, false);
        }
        else if (gameStatus == GameStatus.Player_Planning)
        {
            player.myCharacter.pausePlanning();

            player.myCharacter = inSceneCharacters[index];
            player.myCharacter.startPlanning();
            uiManager.ShowCharacterPlanUI(inSceneCharacters[index].name, getActionPoints(index), inSceneCharacters[index].dead);
            player.charaSwitched(index, isSubmitted[index], isEmpty[index], isFull[index]);
        }

        LocalCameraManager.Instance.ChangeTargetCharacter(index);
    }

    // Called by LocalPlayer.submitPlan(), when player press submit button.
    // Update params, if all submitted their plan, move to moving phase
    public void newPlanSubmitted(int index)
    {
        // When a player submit move plan
        if (!isSubmitted[index])
        {
            planSubmittedCount += 1;
            isSubmitted[index] = true;

            player.UpdatePlanUI(true, false, true);
        }

        if (planSubmittedCount == 3)
        {
            // Local version of character moving
            Debug.Log("Planning phase ended.");
            uiManager.HideCharacterPlanUI();

            foreach (LocalCharacter chara in inSceneCharacters)
            {
                chara.endPlanning();
            }

            StartCharacterMovingPhase();
        }
    }

    // Start charater moving phase
    public void StartCharacterMovingPhase()
    {
        gameStatus = GameStatus.Player_Moving;
        moveFinishedCount = 0;       
        eventQueue = new Queue<LocalTile>();

        StartCoroutine(CharacterMoveByStep());
    }

    // Characters move step by step
    // If events happen, deal with all the events and then back to moving
    private IEnumerator CharacterMoveByStep()
    {
        uiManager.ShowCharacterMovingUI();

        // A flag, whether this round of step triggers a combat
        bool hasCombat = false;

        if (moveFinishedCount <= 0)
        {
            isEmpty = new bool[3] { false, false, false };
        }

        while (moveFinishedCount < 3) {
            foreach (LocalCharacter chara in inSceneCharacters) {
                StartCoroutine(chara.TakeNextMove(excecutionStepTime));
            }
            bool doneMoving;
            do {
                
                doneMoving = true;
                foreach (LocalCharacter chara in inSceneCharacters) {
                    if (chara.moving) {
                        doneMoving = false;
                    }
                }
                yield return null;
            } while(!doneMoving);

            if (eventQueue.Count != 0) {
                yield return ExecuteCombatOneByOne();
                hasCombat = true;
                break;
            }

            moveFinishedCount = 0;
            foreach(LocalCharacter character in inSceneCharacters) {
                if (character.ActionPlan.Count == 0) {
                    moveFinishedCount += 1;
                }
            }
        }

        // If everyone finishes and no combat happens
            // if there are combats, deal with them and then come back
        if (moveFinishedCount == 3 && !hasCombat)
        {
            Debug.Log("Moving phase ended.");
            pinningSystem.ClearCurrentTurnPins();
            StartMonsterTurn();
        }
    }

    // Start monster moving phase
    private void StartMonsterTurn()
    {
        gameStatus = GameStatus.Monster_Moving;
        moveFinishedCount = 0;

        foreach (LocalMonster m in inSceneMonsters)
        {
            m.MonsterTurnStart();
        }

        StartCoroutine(MonsterMoveByStep());
    }

    // Monsters moving step by step
    // Same with chara move by step, when events happened, deal with them and come back
    private IEnumerator MonsterMoveByStep()
    {
        uiManager.ShowMonsterTurnUI();
        bool hasCombat = false;

        while (moveFinishedCount < inSceneMonsters.Count)
        {
            foreach(LocalMonster m in inSceneMonsters) {
                if (!m.turnFinished) {
                    StartCoroutine(m.TakeNextMove(excecutionStepTime));
                }
            }
            bool doneMoving;
            do {
                doneMoving = true;
                foreach(LocalMonster m in inSceneMonsters) {
                    if (m.moving) {
                        doneMoving = false;
                    }
                }
                yield return null;
            } while (!doneMoving);

            if (eventQueue.Count != 0) {
                yield return ExecuteCombatOneByOne();
                hasCombat = true;
                break;
            }
        }

        if (moveFinishedCount >= inSceneMonsters.Count && !hasCombat)
        {
            Debug.Log("Monster moving phase ended.");

            foreach (LocalCharacter chara in inSceneCharacters)
            {
                if (chara.dead)
                {
                    chara.RespawnCountdown();
                }
            }

            StartPlayerTurn();
        }
    }

    // Called by LocalMonster.moveOneStep()
    public void monsterMoveFinished()
    {
        moveFinishedCount += 1;
    }

    // Execute all the events happened within one step time
    // Combat.ExecuteCombat() is the actual combat function
    private IEnumerator ExecuteCombatOneByOne()
    {
        Debug.Log("An event happened.");

        while (eventQueue.Count != 0)
        {
            bool win = false;
            LocalTile t = eventQueue.Dequeue();

            LocalCameraManager.Instance.ChangeTargetCharacter(t.charaList[0].CharacterId);
            switch (t.tileType) {
                case LocalTile.ObstacleType.None:
                    win = Combat.ExecuteCombat(Combat.FightType.Monster, t, uiManager);
                    break;
                case LocalTile.ObstacleType.Trap:
                    win = Combat.ExecuteCombat(Combat.FightType.Trap, t, uiManager);
                    break;
                case LocalTile.ObstacleType.Rock:
                    win = Combat.ExecuteCombat(Combat.FightType.Rock, t, uiManager);
                    break;
            }

            if (win) {
                // if the character(s) won the battle, destory the enemies
                Debug.Log("Character won.");
                switch (t.tileType) {
                    case LocalTile.ObstacleType.None:
                        foreach (LocalMonster m in t.enemyList) {
                            inSceneMonsters.Remove(m);
                            Destroy(m.gameObject);
                        }
                        t.enemyList.Clear();
                        break;
                    case LocalTile.ObstacleType.Trap:
                    case LocalTile.ObstacleType.Rock:
                        GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                        LocalTile newTile = opentile.GetComponent<LocalTile>();
                        newTile.row = t.row;
                        newTile.col = t.col;
                        MapGenerator.Instance.Map[newTile.row, newTile.col] = newTile;
                        Destroy(t.gameObject);
                        break;
                }
            }
            else {
                // If not, reduce health except rock
                // If character's turn, all remaining steps should be cleared.
                Debug.Log("Enemy won.");

                List<LocalCharacter> deadChara = new List<LocalCharacter>();
                List<LocalCharacter> aliveChara = new List<LocalCharacter>();
                switch (t.tileType) {
                    case LocalTile.ObstacleType.None:
                        reduceCharacterHealth(t.charaList, deadChara, aliveChara);
                        if (gameStatus == GameStatus.Player_Moving) {
                            clearCharacterMoves(t.charaList);
                        }
                        break;
                    case LocalTile.ObstacleType.Trap:
                        reduceCharacterHealth(t.charaList, deadChara, aliveChara);
                        clearCharacterMoves(t.charaList);
                        GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                        LocalTile newTile = opentile.GetComponent<LocalTile>();
                        newTile.row = t.row;
                        newTile.col = t.col;
                        MapGenerator.Instance.Map[newTile.row, newTile.col] = newTile;
                        Destroy(t.gameObject);
                        break;
                    case LocalTile.ObstacleType.Rock:
                        foreach (LocalCharacter c in t.charaList) {
                            aliveChara.Add(c);
                        }

                        clearCharacterMoves(t.charaList);
                        break;
                }

                foreach (LocalCharacter c in deadChara) {
                    t.charaList.Remove(c);
                }
                foreach (LocalCharacter c in aliveChara) {
                    c.Retreat();
                }
            }

            //TODO this should probably be waiting for a button click in the future.
            yield return new WaitForSeconds(2*excecutionStepTime);
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

    // Chara failed a combat with monster/trap
    private void reduceCharacterHealth(List<LocalCharacter> charaList, List<LocalCharacter> deadChara, List<LocalCharacter> aliveChara)
    {
        foreach (LocalCharacter c in charaList)
        {
            c.HealthReduced();

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
    private void clearCharacterMoves(List<LocalCharacter> charaList ) {
        foreach (LocalCharacter c in charaList) {
            c.ActionPlan.Clear();

            //switch (c.CharacterId)
            //{
            //    case 0:
            //        DwarfMoves.Clear();
            //        break;
            //    case 1:
            //        GiantMoves.Clear();
            //        break;
            //    case 2:
            //        HumanMoves.Clear();
            //        break;
            //    default:
            //        break;
            //}
        }
    }

    // Called by LocalTile.OnTriggerEnter(), when an event happens at the newTile
    // The same newTile (where an event happens) will only appear in queue once
    public void updateEventQueue(LocalTile tile)
    {
        if (!eventQueue.Contains(tile))
        {
            eventQueue.Enqueue(tile);
        }
    }

    // Helper function to get character's remaining action points when planning
    private int getActionPoints(int index) {
        return inSceneCharacters[index].ActionPointsRemaining;
    }

    // Called by LocalCharacter.OnTriggerEnter(), when a character collide with its goal
    public void GoalReached(int charaID)
    {
        goalCount += 1;
    }

    // Called by LocalCharacter.OnTriggerEnter(), when all three goals fetched and one character collide with the door after that
    // "Move" to next level by reset all relevant constants, delete monsters and tiles (tiles done by map generator) this level, and reset chara status
    public void NextLevel()
    {
        Debug.Log("Moving to next level.");
        currentLevel += 1;

        if (currentLevel <= gameData.levelTextFiles.Length)
        {
            goalCount = 0;
            remainingCharacterCount = 3;

            foreach (LocalCharacter c in inSceneCharacters)
            {
                c.QuickRespawn();
            }

            Debug.Log("Before monster.");
            while (inSceneMonsters.Count != 0)
            {
                LocalMonster m = inSceneMonsters[0];
                inSceneMonsters.Remove(m);
                Destroy(m.gameObject);
            }

            MapGenerator.Instance.LoadLevel(gameData.levelTextFiles[currentLevel - 1]);

            StartLevel();
        }
        else
        {
            Debug.Log("Game ends.");
            gameStatus = GameStatus.GameEnd;
        }
    }
}