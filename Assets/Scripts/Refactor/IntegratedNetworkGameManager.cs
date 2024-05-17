using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;

public class IntegratedNetworkGameManager : IntegratedGameManager
{
    
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
        UIManager.S.HideCharacterSwitcher();
    }
    
    public override IEnumerator StartLevel()
    {
        // center camera to player
        CameraManager.S.ChangeTargetCharacter(localChar.CharacterId);
        CameraManager.S.RecenterCamera();
        
        yield return base.StartLevel();
        // this call will mark every tile as unseen
        // IntegratedMapGenerator.Instance.UpdateFOWVisuals(localChar.CharacterId);
        // this call actually setup the correct FOW
        // the delay is needed because internal state of FOW needs physics trigger to work
        yield return new WaitForFixedUpdate();
        IntegratedMapGenerator.Instance.UpdateFOWVisuals();
    }

    protected override void TimeoutSubmit() {
        base.TimeoutSubmit();
        if (CompetitionMiddleware.Instance.IsAI) {
            //iterate through the registered agent characters and call submit for them

            foreach(CompetitionMiddleware.AgentRecord agent in CompetitionMiddleware.Instance.RegisteredAgents.Values) {
                if (!inSceneCharacters[agent.characterID].ReadyForNextPhase) {
                    NetworkMiddleware.S.CallReadyForNextPhase(agent.characterID, true);
                }
            }
        }
        else {
            NetworkMiddleware.S.CallReadyForNextPhase(localChar.CharacterId,true);
        }
    }

}
