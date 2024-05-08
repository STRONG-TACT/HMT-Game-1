using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSwitcher : MonoBehaviour
{
    public Button dwarfBtn;
    public Button gaintBtn;
    public Button humanBtn;

    public static CharacterSwitcher S;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    private void Start()
    {
        dwarfBtn.onClick.AddListener(delegate { CharacterSwitch(0); });
        gaintBtn.onClick.AddListener(delegate { CharacterSwitch(1); });
        humanBtn.onClick.AddListener(delegate { CharacterSwitch(2); });
    }
    
    public void CharacterSwitch(int index)
    {
        IntegratedGameManager.S.SwitchCharacter(index);

        switch (index)
        {
            case 0:
                dwarfBtn.interactable = false;
                gaintBtn.interactable = true;
                humanBtn.interactable = true;
                break;
            case 1:
                dwarfBtn.interactable = true;
                gaintBtn.interactable = false;
                humanBtn.interactable = true;
                break;
            case 2:
                dwarfBtn.interactable = true;
                gaintBtn.interactable = true;
                humanBtn.interactable = false;
                break;
        }
    }
}
