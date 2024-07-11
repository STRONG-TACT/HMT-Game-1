using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RememberMeToggleHandler : MonoBehaviour
{
    bool RememberMe = false;
    public Toggle rememberMeToggle;

    // Start is called before the first frame update
    void Start()
    {
        RememberMe = LobbyUI.S.RememberMe;
        rememberMeToggle.isOn = RememberMe;
        rememberMeToggle.onValueChanged.AddListener(OnToggleChanged);

    }
    void OnEnable()
    {
        if (LobbyUI.S != null) RememberMe = LobbyUI.S.RememberMe;
        rememberMeToggle.isOn = RememberMe;
    }

    void OnToggleChanged(bool isOn)
    {
        RememberMe = isOn;
        LobbyUI.S.RememberMe = isOn;
        Debug.Log("Remember Me status: " + RememberMe);
    }

}
