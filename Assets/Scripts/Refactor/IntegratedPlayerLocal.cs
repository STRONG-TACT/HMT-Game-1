using System;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;
public class IntegratedPlayerLocal : IntegratedPlayer
{
    public Button dwarfBtn;
    public Button gaintBtn;
    public Button humanBtn;

    protected override void Start()
    {
        base.Start();
        dwarfBtn.onClick.AddListener(delegate { SwitchCharacter(0); });
        gaintBtn.onClick.AddListener(delegate { SwitchCharacter(1); });
        humanBtn.onClick.AddListener(delegate { SwitchCharacter(2); });
    }
    
    public void SwitchCharacter(int index)
    {
        myCharacter.UnFocusCharacter();
        myCharacter = IntegratedGameManager.S.inSceneCharacters[index];
        myCharacter.FocusCharacter();
        UpdateCharacterUI();
        IntegratedGameManager.S.localChar = myCharacter;
        if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Pinning)
        {
            IntegratedGameManager.S.uiManager.ShowCharacterPinUI();
        } else if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning)
        {
            IntegratedGameManager.S.uiManager.ShowCharacterPlanUI();
        }

        CameraManager.S.ChangeTargetCharacter(index);
    }
}
