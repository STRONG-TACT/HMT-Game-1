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
    public static LocalGameManager Instance = null;
    public LocalGameData gameData { get; private set; }
    private LocalUIManager uiManager;
    public LocalPlayer player;
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

    public bool isPlayerTurn = true;
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

    private void Awake()
    {
        Instance = this;
        uiManager = FindObjectOfType<LocalUIManager>();
        gameData = FindObjectOfType<LocalGameData>();
        player = FindObjectOfType<LocalPlayer>();
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

    public void setCharaPosition(int ID, float x, float z)
    {
        LocalCharacter targetChara = inSceneCharacters[ID];
        Vector3 newPosition = new Vector3(x, inSceneCharacters[ID].transform.position.y, z);
        targetChara.setStartPos(newPosition);
    }

    // Start is called before the first frame update
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

    public void StartLevel()
    {
        uiManager.InitGameUI();
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        //Player start to plan their moves
        isPlayerTurn = true;
        planSubmittedCount = 0;
        remainingCharacterCount = 3;

        DwarfPlan = new List<int>();
        GiantPlan = new List<int>();
        HumanPlan = new List<int>();

        isSubmitted = new bool[3] { false, false, false };
        isEmpty = new bool[3] { true, true, true };
        isFull = new bool[3] { false, false, false };

        foreach (LocalCharacter chara in inSceneCharacters)
        {
            if (chara.dead)
            {
                isSubmitted[chara.CharacterId] = true;
                planSubmittedCount += 1;
                remainingCharacterCount -= 1;
            }
        }

        StartPlayerPlanningPhase();
    }

    private void StartPlayerPlanningPhase()
    {
        // Local version of player planning stage
        if (remainingCharacterCount > 0)
        {
            player.myCharacter = inSceneCharacters[0];
            player.myCharacter.startPlanning();
            player.charaSwitched(0, isSubmitted[0], isEmpty[0], isFull[0]);

            uiManager.ShowCharacterPlanUI(inSceneCharacters[0].name, getMovePoints(0), inSceneCharacters[0].dead);
        }
        else
        {
            StartCharacterMovingPhase();
        }
    }

    public void newPlayerMovePlan(int index, int move)
    {
        if (move >= 0 && move < 5)
        {
            updateActionQueue(index, move);
        }

        player.myCharacter.planNewStep((LocalCharacter.Direction)move);

        uiManager.ShowMoveLeft(getMovePoints(index));
    }

    public void switchCharacter(int index)
    {
        player.myCharacter.pausePlanning();

        player.myCharacter = inSceneCharacters[index];
        player.myCharacter.startPlanning();
        uiManager.ShowCharacterPlanUI(inSceneCharacters[index].name, getMovePoints(index), inSceneCharacters[index].dead);
        player.charaSwitched(index, isSubmitted[index], isEmpty[index], isFull[index]);
        LocalCameraManager.Instance.ChangeTargetCharacter(index);
    }

    public void backOneMove(int index)
    {
        if (!isEmpty[index])
        {
            int moveRemoved = moveLastFromActionQueue(index);

            player.myCharacter.backOnePlannedStep((LocalCharacter.Direction)moveRemoved);

            uiManager.ShowMoveLeft(getMovePoints(index));
        }
    }

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
            StartCharacterMovingPhase();
        }
    }

    public void StartCharacterMovingPhase()
    {
        // Local version of character moving
        Debug.Log("Planning phase ended.");
        uiManager.HideCharacterPlanUI();

        foreach(LocalCharacter chara in inSceneCharacters)
        {
            chara.endPlanning();
        }

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
            StartMonsterTurn();
        }
    }

    private void StartMonsterTurn()
    {
        isPlayerTurn = false;
        moveFinishedCount = 0;

        foreach (LocalMonster m in inSceneMonsters)
        {
            m.MonsterTurnStart();
        }

        StartCoroutine(MonsterMoveByStep());
    }

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

    public void monsterMoveFinished()
    {
        moveFinishedCount += 1;
    }

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

                    if (isPlayerTurn)
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

        if (isPlayerTurn)
        {
            StartCoroutine(CharacterMoveByStep());
        }else if (!isPlayerTurn)
        {
            StartCoroutine(MonsterMoveByStep());
        }
    }

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

    public void updateEventQueue(LocalTile tile)
    {
        if (!eventQueue.Contains(tile))
        {
            eventQueue.Enqueue(tile);
        }
    }

    private int getMovePoints(int index)
    {
        switch (index)
        {
            case 0:
                return inSceneCharacters[index].config.movement - DwarfPlan.Count;

            case 1:
                return inSceneCharacters[index].config.movement - GiantPlan.Count;

            case 2:
                return inSceneCharacters[index].config.movement - HumanPlan.Count;

            default:
                return -1;
        }
    }

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

    public void GoalReached(int charaID)
    {
        goalCount += 1;
    }

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
        }
    }
}