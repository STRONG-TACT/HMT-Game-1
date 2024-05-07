using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;

public class IntegratedNetworkGameManager : IntegratedGameManager
{
    

    public static IntegratedGameManager S;
    
    protected override void Awake()
    {
        base.Awake();
        isNetworkGame = true;
        if (S) Destroy(this);
        else S = this;
    }

    protected override void Start()
    {
        base.Start();
        localChar.FocusCharacter();
    }
    
    public override IEnumerator StartLevel()
    {
        // center camera to player
        CameraManager.S.ChangeTargetCharacter(localChar.CharacterId);
        CameraManager.S.RecenterCamera();
        
        yield return base.StartLevel();
        // this call will mark every tile as unseen
        // IntegratedMapGenerator.Instance.updateFogOfWar_map(localChar.CharacterId);
        // this call actually setup the correct FOW
        // the delay is needed because internal state of FOW needs physics trigger to work
        yield return new WaitForFixedUpdate();
        IntegratedMapGenerator.Instance.updateFogOfWar_map(localChar.CharacterId);
    }


    protected override void PreparePlayerPinningPhase()
    {
        base.PreparePlayerPinningPhase();

        localChar.StartPingPhase();
        StartPlayerPinningPhase();
    }

    protected override void StartPlayerPinningPhase()
    {
        uiManager.UpdateTimer(95);
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
