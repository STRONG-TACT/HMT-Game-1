using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI S;

    [Header("UI Block Parent")]
    [SerializeField] private GameObject startSceneUI;
    [SerializeField] private GameObject gameModeUI;
    [SerializeField] private GameObject loadingUI;
    [SerializeField] private GameObject joinRoomModeUI;
    [SerializeField] private GameObject disconnectedUI;
    [SerializeField] private GameObject createJoinUI;

    [Header("Text Input Fields")] [SerializeField]
    // TODO: This is not TMP_Text, we want TMP_Text
    private Text roomSelectText;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    private void Start()
    {
        DisableAllUI();
        startSceneUI.SetActive(true);
        //gameModeUI.SetActive(true);
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

    public void ShowCreateJoinRoomUI()
    {
        DisableAllUI();
        createJoinUI.SetActive(true);
    }

    public void ShowDisconnectedUI()
    {
        DisableAllUI();
        disconnectedUI.SetActive(true);
    }

    private void DisableAllUI()
    {
        startSceneUI.SetActive(false);
        gameModeUI.SetActive(false);
        loadingUI.SetActive(false);
        joinRoomModeUI.SetActive(false);
        disconnectedUI.SetActive(false);
        createJoinUI.SetActive(false);
    }

    public string GetRoomNameEntered()
    {
        return roomSelectText.text;
    }
}
