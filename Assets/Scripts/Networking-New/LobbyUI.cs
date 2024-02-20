using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI S;

    [Header("UI Block Parent")]
    [SerializeField] private GameObject gameModeUI;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject joinRoomModeUI;
    [SerializeField] private GameObject disconnectedUI;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    public void ShowGameModeUI()
    {
        DisableAllUI();
        gameModeUI.SetActive(true);
    }

    public void ShowLoadingUI(string msg = "Loading...")
    {
        DisableAllUI();
        loadingUI.GetComponentInChildren<TMP_Text>().text = msg;
        loadingUI.SetActive(true);
    }

    public void ShowJoinRoomModeUI()
    {
        DisableAllUI();
        joinRoomModeUI.SetActive(true);
    }

    public void ShowDisconnectedUI()
    {
        DisableAllUI();
        disconnectedUI.SetActive(true);
    }

    private void DisableAllUI()
    {
        gameModeUI.SetActive(false);
        loadingUI.SetActive(false);
        joinRoomModeUI.SetActive(false);
        disconnectedUI.SetActive(false);
    }
}
