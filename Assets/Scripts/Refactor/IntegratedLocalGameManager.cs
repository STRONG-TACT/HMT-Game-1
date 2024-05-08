using GameConstant;

public class IntegratedLocalGameManager : IntegratedGameManager
{
    // When awake, find all the managers and data.
    // Future update: Set isFirstLevel (currentLevel should by default be 1, may delete this step in future if we stick in the same scene)
    protected override void Awake()
    {
        isNetworkGame = false;

        base.Awake();
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
            SwitchCharacter(0);
        }
        else {
            PreparePlayerPlanningPhase();
        }
        base.StartPlayerPinningPhase();
    }
    
    

    // Called by LocalPlayer.SwitchCharacter(), when player press chara buttons.
    // Update ui text/icon, pass params about current chara planning status to LocalPlayer.
    // Update changes with camera control.
    public override void SwitchCharacter(int index)
    {
        if (gameStatus == GameStatus.Player_Pinning)
        {
            uiManager.HideCharacterPlanUI();
            localChar.UnFocusCharacter();
            localChar = inSceneCharacters[index];
            localChar.FocusCharacter();
            uiManager.ShowCharacterPinUI();
            player.UpdateCharacterUI();
        }
        else if (gameStatus == GameStatus.Player_Planning)
        {
            localChar.UnFocusCharacter();
            localChar = inSceneCharacters[index];
            localChar.FocusCharacter();
            uiManager.ShowCharacterPlanUI();
            player.UpdateCharacterUI();
        }
       
       CameraManager.S.ChangeTargetCharacter(index);
       IntegratedMapGenerator.Instance.updateFogOfWar_map(player.myCharacter.CharacterId);
    }

}