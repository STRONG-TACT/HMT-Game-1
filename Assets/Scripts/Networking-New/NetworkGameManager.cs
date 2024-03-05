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
    
    // ======== Pointer to In Game Prefabs ========
    [Header("In Game Prefabs")]
    [Tooltip("Set Manually")]
    public List<NetworkCharacter> inSceneCharacters = new List<NetworkCharacter>();
    [Tooltip("Set automatically by MapGenerator")]
    public List<NetworkMonster> inSceneMonsters = new List<NetworkMonster>();
    // Pointer to the character that local client controls
    [HideInInspector] public NetworkCharacter localChar;

    public static NetworkGameManager S;

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
        // TODO: should use this time to do some setup
        gameStatus = GameStatus.GetReady;
        uiManager.InitGameUI();
        yield return new WaitForSeconds(1.0f);
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        localChar.ResetActionPoints();
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

        PreparePlayerPlanningPhase();
        Debug.Log("Should start planning phase here.");
    }

    private void PreparePlayerPlanningPhase()
    {
        
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
}
