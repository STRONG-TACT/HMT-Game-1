using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UIElements;
using Photon.Pun.Demo.PunBasics;
using UnityEditor;

public class IntegratedLocalGameManager : IntegratedGameManager
{
    public enum GameStatus { GetReady, Player_Pinning, Player_Planning, Player_Moving, Monster_Moving, Animation_Pause, GameEnd }

    public static IntegratedGameManager S = null;

    public bool debugRPCReceipts = false;

    public bool isFirstLevel = true;

    public Character mainCharacter;


    // When awake, find all the managers and data.
    // Future update: Set isFirstLevel (currentLevel should by default be 1, may delete this step in future if we stick in the same scene)
    protected override void Awake()
    {
        isNetworkGame = false;

        base.Awake();

        S = this;

        if (currentLevel == 1)
        {
            isFirstLevel = true;
        }
        else
        {
            isFirstLevel = false;
        }
    }

    // Prepare for player pinning phase
    // Reset all the player pinning parameters
    // If there are characters dead, update relevant params so they will skip pinning
    protected override void PreparePlayerPinningPhase()
    {
        base.PreparePlayerPinningPhase();

        foreach (Character chara in inSceneCharacters) {
            chara.StartPingPhase();
        }
        StartPlayerPinningPhase();
    }


    protected override void StartPlayerPinningPhase()
    {
        // Local version of player planning stage
        if (remainingCharacterCount > 0) {
            player.myCharacter = inSceneCharacters[0];
            player.myCharacter.FocusCharacter();
            // player.UpdateCharacterUI(0, player.myCharacter);
            // uiManager.ShowCharacterPinUI(player.myCharacter);
            //MapGenerator.Instance.updateFogOfWar_map(player.myCharacter.CharacterId);
        }
        else {
            PreparePlayerPlanningPhase();
        }
    }

    public override void NewPlayerPin()
    {
        uiManager.UpdateActionPointsRemaining(player.myCharacter.ActionPointsRemaining, player.myCharacter.config.movement);
        base.NewPlayerPin();
    }


    // Prepare for player planning phase
    // Reset all the player planning parameters
    // If there are characters dead, update relevant params so they will skip planning
    protected override void PreparePlayerPlanningPhase()
    {
        base.PreparePlayerPlanningPhase();
        if (remainingCharacterCount > 0) {
            SwitchCharacter(0);
            CheckPlanPhaseEnd();
        }
        else {
            StartCharacterMovingPhase();
        }
    }

    // Called by LocalPlayer.AddMoveToFocusedCharacter(), when player press direction buttons.
    // Add the move to corresponding queue, and confirm with current LocalCharacter.
    public void UpdateFocusPlayPlan(int index, Character.Direction move) {
        // player.myCharacter.AddActionToPlan(move);
        // player.UpdateCharacterUI(index, player.myCharacter);
        uiManager.UpdateActionPointsRemaining(player.myCharacter.ActionPointsRemaining, player.myCharacter.config.movement);
    }

    // Called by LocalPlayer.SwitchCharacter(), when player press chara buttons.
    // Update ui text/icon, pass params about current chara planning status to LocalPlayer.
    // Update changes with camera control.
    public void SwitchCharacter(int index)
    {
       //  if (gameStatus == GameStatus.Player_Pinning)
       //  {
       //      player.myCharacter.UnFocusCharacter();
       //      player.myCharacter = inSceneCharacters[index];
       //      player.myCharacter.FocusCharacter();
       //      uiManager.ShowCharacterPinUI(player.myCharacter);
       //      player.UpdateCharacterUI(index, player.myCharacter);
       //  }
       //  else if (gameStatus == GameStatus.Player_Planning)
       //  {
       //      player.myCharacter.UnFocusCharacter();
       //      player.myCharacter = inSceneCharacters[index];
       //      player.myCharacter.FocusCharacter();
       //      uiManager.ShowCharacterPlanUI(player.myCharacter);
       //      player.UpdateCharacterUI(index, player.myCharacter);
       //  }
       //
       // CameraManager.S.ChangeTargetCharacter(index);
    }

}