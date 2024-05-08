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
    [SerializeField] private GameObject competitionIDUI;

    [Header("Text Input Fields")] 
    [SerializeField]
    // TODO: This is not TMP_Text, we want TMP_Text
    private Text roomSelectText;
    [SerializeField]
    private Text competitionIdText;

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

    public void ShowCompetitionIDUI() {
        DisableAllUI();
        competitionIDUI.SetActive(true);
    }


    private void DisableAllUI()
    {
        startSceneUI.SetActive(false);
        gameModeUI.SetActive(false);
        loadingUI.SetActive(false);
        joinRoomModeUI.SetActive(false);
        disconnectedUI.SetActive(false);
        createJoinUI.SetActive(false);
        //competitionIDUI.SetActive(false);
    }

    public string GetRoomNameEntered()
    {
        return roomSelectText.text;
    }

    public void SetCompetitionID() {
        if(competitionIdText.text == "") {
            return;
        }

        CompetitionMiddleware.Instance.SetUserID(competitionIdText.text);
        competitionIDUI.SetActive(false);
    }

    public void StartAnoymousGame() {
        
        CompetitionMiddleware.Instance.SetUserID( System.Guid.NewGuid().ToString());
        competitionIDUI.SetActive(false);
    }
}
