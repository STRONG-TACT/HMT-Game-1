using GameConstant;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalGameManager : GameManager
{

    // When awake, find all the managers and data.
    // Future update: Set isFirstLevel (currentLevel should by default be 1, may delete this step in future if we stick in the same scene)
    protected override void Awake()
    {
        isNetworkGame = false;

        base.Awake();
    }

    protected override void Start() {
        base.Start();
        UIManager.S.ShowCharacterSwitcher();
        HMT.ArgParser argParser = new HMT.ArgParser();

        argParser.AddArg("stepTime", HMT.ArgParser.ArgType.One);
        argParser.AddArg("instanceMode", HMT.ArgParser.ArgType.Flag);
        argParser.ParseArgs();

        excecutionStepTime = argParser.GetArgValue("stepTime", excecutionStepTime);
        if (argParser.CheckFlag("instantMode")) {
            excecutionStepTime = 0;
        }
    }

    protected override void TimeoutSubmit() {
        base.TimeoutSubmit();
        foreach(Character character in inSceneCharacters) {
            if (!character.ReadyForNextPhase) {
                CompetitionMiddleware.Instance.LogTimeOut(character.CharacterId);
                NetworkMiddleware.S.CallReadyForNextPhaseAuto(character.CharacterId, true);
            }
        }
    }


    //protected override void StartPlayerPinningPhase()
    //{
    //    // Local version of player planning stage
    //    if (!CheckPhaseEnd()) {
    //        UIManager.S.SwitchCharacterTo(localChar.CharacterId);
    //    }
    //    base.StartPlayerPinningPhase();
    //}

    //protected override void StartPlayerPlanningPhase() {
    //    base.StartPlayerPlanningPhase();
    //}



    // Called by LocalPlayer.SwitchCharacter(), when player press chara buttons.
    // Update ui text/icon, pass params about current chara planning status to LocalPlayer.
    // Update changes with camera control.
    public override void SwitchCharacter(int index)
    {
        if (gameStatus == GameStatus.Player_Pinning)
        {
            //uiManager.HideCharacterPlanUI();
            localChar.UnFocusCharacter();
            localChar = inSceneCharacters[index];
            localChar.FocusCharacter();
            UIManager.S.UpdateCommonHUD();
            UIManager.S.UpdateCharacterPinUI();
            //uiManager.ShowCharacterPinUI();
            //player.UpdateCharacterUI();
        }
        else if (gameStatus == GameStatus.Player_Planning)
        {
            localChar.UnFocusCharacter();
            localChar = inSceneCharacters[index];
            localChar.FocusCharacter();
            UIManager.S.ShowCharacterPlanUI();
            UIManager.S.UpdateCommonHUD();
            UIManager.S.UpdateCharacterPlanUI();
            //player.UpdateCharacterUI();
        }
        else {
            localChar.UnFocusCharacter();
            localChar = inSceneCharacters[index];
            localChar.FocusCharacter();
            UIManager.S.UpdateCommonHUD();
            UIManager.S.HideCharacterPinUI();
            UIManager.S.HideCharacterPlanUI();
        }
        CompetitionMiddleware.Instance.LogChangeCharacter(index);
       CameraManager.S.ChangeTargetCharacter(index);
       MapGenerator.Instance.UpdateFOWVisuals();
    }

}