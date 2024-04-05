using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;

public class IntegratedNetworkGameManager : IntegratedGameManager
{

    // Pointer to the character that local client controls
    [HideInInspector] public NetworkCharacter localChar;

    public static NetworkGameManager S =null;
    
    public float excecutionStepTime = 1;

    protected override void Awake()
    {
        base.Awake();
        if (S) Destroy(this);
        else S = this;
        localChar.FocusCharacter();
    }
    


    public override IEnumerator StartLevel()
    {
        // center camera to player
        CameraManager.S.ChangeTargetCharacter(localChar.CharacterId);
        CameraManager.S.RecenterCamera();
        // this call will mark every tile as unseen
        IntegratedMapGenerator.Instance.updateFogOfWar_map(localChar.CharacterId);
        // this call actually setup the correct FOW
        // the delay is needed because internal state of FOW needs physics trigger to work
        IntegratedMapGenerator.Instance.updateFogOfWar_map(localChar.CharacterId);
        yield return StartCoroutine(base.StartLevel());
    }


    protected override void PreparePlayerPinningPhase()
    {
        base.PreparePlayerPinningPhase();

        localChar.StartPingPhase();
        StartPlayerPinningPhase();
    }

    protected override void StartPlayerPinningPhase()
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

    public override void NewPlayerPin()
    {
        uiManager.UpdateActionPointsRemaining(localChar.ActionPointsRemaining);
        base.NewPlayerPin();
    }
    
    

    // Prepare for player planning phase
    // Reset all the player planning parameters
    // If there are characters dead, update relevant params so they will skip planning
    protected override void PreparePlayerPlanningPhase()
    {
        base.PreparePlayerPlanningPhase();
        localChar.indicator.SetActive(true);

        if (remainingCharacterCount > 0) {
            CheckPlanPhaseEnd();
            player.UpdateCharacterUI();
        }
        else {
            StartCharacterMovingPhase();
        }
    }



    public void CheckLoseCondition()
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

    private void Lose()
    {
        uiManager.ShowDefeatedScreen();
    }
}
