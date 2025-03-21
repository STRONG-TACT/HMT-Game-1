using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;
using Photon.Pun;

public class NetworkGameManager : GameManager {
    
    protected override void Awake() {
        base.Awake();
        isNetworkGame = true;
    }

    protected override void Start()
    {
        base.Start();
        localChar.FocusCharacter();
        UIManager.Instance.HideCharacterSwitcher();
    }
    
    public override IEnumerator StartLevel()
    {
        // center camera to player
        CameraManager.Instance.ChangeTargetCharacter(localChar.CharacterId);
        CameraManager.Instance.RecenterCamera();
        
        yield return base.StartLevel();
        // this call will mark every tile as unseen
        // IntegratedMapGenerator.Instance.UpdateFOWVisuals(localChar.CharacterId);
        // this call actually setup the correct FOW
        // the delay is needed because internal state of FOW needs physics trigger to work
        yield return new WaitForFixedUpdate();
        MapGenerator.Instance.UpdateFOWVisuals();
    }

    protected override void TimeoutSubmit() {
        base.TimeoutSubmit();
        if (CompetitionMiddleware.Instance.IsAI) {
            //iterate through the registered agent characters and call submit for them

            foreach(CompetitionMiddleware.AgentRecord agent in CompetitionMiddleware.Instance.RegisteredAgents.Values) {
                if (!inSceneCharacters[agent.characterID].ReadyForNextPhase) {
                    CompetitionMiddleware.Instance.LogTimeOut(agent.characterID);
                    NetworkMiddleware.Instance.CallReadyForNextPhase(agent.characterID, true);
                }
            }
        }
        else {
            CompetitionMiddleware.Instance.LogTimeOut(localChar.CharacterId);
            NetworkMiddleware.Instance.CallReadyForNextPhase(localChar.CharacterId,true);
        }
    }
}
