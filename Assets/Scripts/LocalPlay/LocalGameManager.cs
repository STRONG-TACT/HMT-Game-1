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

    private List<int> DwarfPlan;
    private List<int> GiantPlan;
    private List<int> HumanPlan;

    private Queue<int> DwarfMoves;
    private Queue<int> GiantMoves;
    private Queue<int> HumanMoves;
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

        DwarfPlan = new List<int>();
        GiantPlan = new List<int>();
        HumanPlan = new List<int>();

        isSubmitted = new bool[3] { false, false, false };
        isEmpty = new bool[3] { true, true, true };
        isFull = new bool[3] { false, false, false };

        foreach (LocalCharacter chara in inSceneCharacters)
        {
            int moveLeft = chara.getActionPoints();
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

    // Called by LocalPlayer.addNewMove(), when player press direction buttons.
    // Add the move to corresponding queue, and confirm with current LocalCharacter.
    public void newPlayerMovePlan(int index, int move)
    {
        if (move >= 0 && move < 5)
        {
            updateActionQueue(index, move);
        }

        player.myCharacter.planNewStep((LocalCharacter.Direction)move);

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

    // Called by LocalPlayer.backOneMove(), when player press back button.
    // Remove lastest from corresponding queue, and confirm with current LocalCharacter.
    public void backOneMove(int index)
    {
        if (!isEmpty[index])
        {
            int moveRemoved = moveLastFromActionQueue(index);

            player.myCharacter.backOnePlannedStep((LocalCharacter.Direction)moveRemoved);

            uiManager.ShowMoveLeft(getActionPoints(index));
        }
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

            player.planUpdated(true, false, true);
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
        DwarfMoves = new Queue<int>();
        GiantMoves = new Queue<int>();
        HumanMoves = new Queue<int>();
        eventQueue = new Queue<LocalTile>();

        for (int i = 0; i < DwarfPlan.Count; i ++)
        {
            DwarfMoves.Enqueue(DwarfPlan[i]);
        }
        for (int i = 0; i < GiantPlan.Count; i++)
        {
            GiantMoves.Enqueue(GiantPlan[i]);
        }
        for (int i = 0; i < HumanPlan.Count; i++)
        {
            HumanMoves.Enqueue(HumanPlan[i]);
        }

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

        while (moveFinishedCount < 3)
        {
            if (!isEmpty[0])
            {
                //Debug.Log("In Loop.");
                if (DwarfMoves.Count == 0)
                {
                    isEmpty[0] = true;
                    moveFinishedCount += 1;
                }
                else
                {
                    inSceneCharacters[0].moveOneStep((LocalCharacter.Direction)DwarfMoves.Dequeue());
                }
            }

            if (!isEmpty[1])
            {
                if (GiantMoves.Count == 0)
                {
                    isEmpty[1] = true;
                    moveFinishedCount += 1;
                }
                else
                {
                    inSceneCharacters[1].moveOneStep((LocalCharacter.Direction)GiantMoves.Dequeue());
                }
            }

            if (!isEmpty[2])
            {
                if (HumanMoves.Count == 0)
                {
                    isEmpty[2] = true;
                    moveFinishedCount += 1;
                }
                else
                {
                    inSceneCharacters[2].moveOneStep((LocalCharacter.Direction)HumanMoves.Dequeue());
                }
            }

            yield return new WaitForSeconds(2f);

            if (eventQueue.Count != 0)
            {
                StartCoroutine(ExecuteCombatOneByOne());
                hasCombat = true;
                break;
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
            foreach(LocalMonster m in inSceneMonsters)
            {
                if (!m.turnFinished)
                {
                    // if monster still have moves
                    m.moveOneStep();
                }
            }

            yield return new WaitForSeconds(1.2f);

            if (eventQueue.Count != 0)
            {
                StartCoroutine(ExecuteCombatOneByOne());
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
            if (t.tileType == LocalTile.TileType.Other)
            {
                win = Combat.ExecuteCombat(Combat.FightType.Monster, t, uiManager);
            }
            else if (t.tileType == LocalTile.TileType.Trap)
            {
                win = Combat.ExecuteCombat(Combat.FightType.Trap, t, uiManager);
            }
            else if (t.tileType == LocalTile.TileType.Rock)
            {
                win = Combat.ExecuteCombat(Combat.FightType.Rock, t, uiManager);
            }

            if (win)
            {
                // if the character(s) won the battle, destory the enemies
                Debug.Log("Character won.");

                if (t.tileType == LocalTile.TileType.Other)
                {
                    foreach (LocalMonster m in t.enemyList)
                    {
                        inSceneMonsters.Remove(m);
                        Destroy(m.gameObject);
                    }

                    t.enemyList.Clear();
                }
                else if (t.tileType == LocalTile.TileType.Trap || t.tileType == LocalTile.TileType.Rock)
                {
                    GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                    opentile.GetComponent<LocalTile>().row = t.row;
                    opentile.GetComponent<LocalTile>().col = t.col;

                    Destroy(t.gameObject);
                }
            }
            else
            {
                // If not, reduce health except rock
                // If character's turn, all remaining steps should be cleared.
                Debug.Log("Enemy won.");

                List<LocalCharacter> deadChara = new List<LocalCharacter>();
                List<LocalCharacter> aliveChara = new List<LocalCharacter>();

                if (t.tileType == LocalTile.TileType.Other)
                {
                    reduceCharacterHealth(t.charaList, deadChara, aliveChara);

                    if (gameStatus == GameStatus.Player_Moving)
                    {
                        clearCharacterMoves(t.charaList);
                    }
                }
                else if (t.tileType == LocalTile.TileType.Trap)
                {
                    reduceCharacterHealth(t.charaList, deadChara, aliveChara);

                    clearCharacterMoves(t.charaList);

                    GameObject opentile = Instantiate(FindObjectOfType<GameAssets>().OpenTile, new Vector3(t.transform.position.x, 0, t.transform.position.z), Quaternion.identity, t.transform.parent);
                    opentile.GetComponent<LocalTile>().row = t.row;
                    opentile.GetComponent<LocalTile>().col = t.col;

                    Destroy(t.gameObject);
                }
                else if (t.tileType == LocalTile.TileType.Rock)
                {
                    foreach (LocalCharacter c in t.charaList)
                    {
                        aliveChara.Add(c);
                    }

                    clearCharacterMoves(t.charaList);
                }

                foreach (LocalCharacter c in deadChara)
                {
                    t.charaList.Remove(c);
                }
                foreach (LocalCharacter c in aliveChara)
                {
                    c.withdrawn();
                }
            }

            yield return new WaitForSeconds(2f);
        }

        if (gameStatus == GameStatus.Player_Moving)
        {
            StartCoroutine(CharacterMoveByStep());
        }else if (gameStatus == GameStatus.Monster_Moving)
        {
            StartCoroutine(MonsterMoveByStep());
        }
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
    private void clearCharacterMoves(List<LocalCharacter> charaList)
    {
        foreach (LocalCharacter c in charaList)
        {
            switch (c.CharacterId)
            {
                case 0:
                    DwarfMoves.Clear();
                    break;
                case 1:
                    GiantMoves.Clear();
                    break;
                case 2:
                    HumanMoves.Clear();
                    break;
                default:
                    break;
            }
        }
    }

    // Called by LocalTile.OnTriggerEnter(), when an event happens at the tile
    // The same tile (where an event happens) will only appear in queue once
    public void updateEventQueue(LocalTile tile)
    {
        if (!eventQueue.Contains(tile))
        {
            eventQueue.Enqueue(tile);
        }
    }

    // Helper function to get character's remaining action points when planning
    private int getActionPoints(int index)
    {
        switch (index)
        {
            case 0:
                return inSceneCharacters[index].getActionPoints();

            case 1:
                return inSceneCharacters[index].getActionPoints();

            case 2:
                return inSceneCharacters[index].getActionPoints();

            default:
                return -1;
        }
    }

    // Helper function to add new planned move to the current chara's queue
    private void updateActionQueue(int index, int move)
    {
        switch (index)
        {
            case 0:
                DwarfPlan.Add(move);

                if(inSceneCharacters[index].config.movement == DwarfPlan.Count)
                {
                    isFull[index] = true;
                }
                else if (DwarfPlan.Count == 1)
                {
                    isEmpty[index] = false;
                }

                break;
            case 1:
                GiantPlan.Add(move);

                if (inSceneCharacters[index].config.movement == GiantPlan.Count)
                {
                    isFull[index] = true;
                }
                else if (GiantPlan.Count == 1)
                {
                    isEmpty[index] = false;
                }

                break;
            case 2:
                HumanPlan.Add(move);

                if (inSceneCharacters[index].config.movement == HumanPlan.Count)
                {
                    isFull[index] = true;
                }
                else if (HumanPlan.Count == 1)
                {
                    isEmpty[index] = false;
                }


                break;
            default:
                break;
        }

        player.planUpdated(false, false, isFull[index]);
    }

    // Helper function to remove one last move from current chara's queue
    private int moveLastFromActionQueue(int index)
    {
        int move = -1;
        switch (index)
        {
            case 0:
                move = DwarfPlan[DwarfPlan.Count - 1];
                DwarfPlan.RemoveAt(DwarfPlan.Count - 1);

                if (DwarfPlan.Count == 0)
                {
                    isEmpty[index] = true;
                }
                if (isFull[index])
                {
                    isFull[index] = false;
                }
                
                break;
            case 1:
                move = GiantPlan[GiantPlan.Count - 1];
                GiantPlan.RemoveAt(GiantPlan.Count - 1);

                if (GiantPlan.Count == 0)
                {
                    isEmpty[index] = true;
                }
                if (isFull[index])
                {
                    isFull[index] = false;
                }

                break;
            case 2:
                move = HumanPlan[HumanPlan.Count - 1];
                HumanPlan.RemoveAt(HumanPlan.Count - 1);

                if (HumanPlan.Count == 0)
                {
                    isEmpty[index] = true;
                }
                if (isFull[index])
                {
                    isFull[index] = false;
                }

                break;
            default:
                break;
        }

        player.planUpdated(false, isEmpty[index], false);
        return move;
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