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
    [SerializeField] private GameObject ConsentFormUI;

    [Header("Text Input Fields")] 
    [SerializeField]
    // TODO: This is not TMP_Text, we want TMP_Text
    private Text roomSelectText;
    [SerializeField]
    private Text competitionIdText;
    [SerializeField] private TextMeshProUGUI competitionIDUIText;

    public bool RememberMe;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    private void Start()
    {
        //DisableAllUI();
        //startSceneUI.SetActive(true);
        //gameModeUI.SetActive(true);
        //PlayerPrefs.DeleteAll();
        
        string competitionID = PlayerPrefs.GetString("competitionID", string.Empty);
        competitionIDUIText.text = "Competition ID: " + competitionID;
        int consent_agreed = PlayerPrefs.GetInt("consent_agreed", 0);
        DisableAllUI();
        if (consent_agreed == 0)
        {
            ShowConsentFormUI();
        }
        if (competitionID == "")
        {
            ShowCompetitionIDUI();
        }
        else
        {
            CompetitionMiddleware.Instance.SetUserID(competitionID);
            ShowStartSceneUI();
        }

    }


    public void ShowGameModeUI()
    {
        DisableAllUI();
        gameModeUI.SetActive(true);
    }

    public void ShowStartSceneUI()
    {
        DisableAllUI();
        startSceneUI.SetActive(true);
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
        competitionIdText.text = PlayerPrefs.GetString("competitionID", String.Empty);
        competitionIDUI.SetActive(true);
    }

    public void ShowConsentFormUI()
    {
        ConsentFormUI.SetActive(true);
    }
    public void DisableConsentFormUI()
    {
        ConsentFormUI.SetActive(false);
    }

    private void DisableAllUI()
    {
        startSceneUI.SetActive(false);
        gameModeUI.SetActive(false);
        loadingUI.SetActive(false);
        joinRoomModeUI.SetActive(false);
        disconnectedUI.SetActive(false);
        createJoinUI.SetActive(false);
        ConsentFormUI.SetActive(false);
        competitionIDUI.SetActive(false);
    }

    public string GetRoomNameEntered()
    {
        return roomSelectText.text;
    }

    public void ToggleRememberMe(bool isOn)
    {
        RememberMe = isOn;
    }

    public void SetCompetitionID() {
        if(competitionIdText.text == "") {
            return;
        }
        //Debug.Log(RememberMe);
        if (RememberMe)
        {
            PlayerPrefs.SetString("competitionID", competitionIdText.text);
        }
        else
        {
            PlayerPrefs.DeleteKey("competitionID");
        }
        competitionIDUIText.text = "Competition ID: " + competitionIdText.text;
        CompetitionMiddleware.Instance.SetUserID(competitionIdText.text);
        competitionIDUI.SetActive(false);
        ShowStartSceneUI();
    }

    public void ConsentFormAnswer(bool Agree)
    {
        if (Agree)
        {
            PlayerPrefs.SetInt("consent_agreed", 1);
            DisableConsentFormUI();
        }
        else
        {
            PlayerPrefs.SetInt("consent_agreed", 0);
            //need further edit: what to do when user don't agree with consent form
        }

    }

    public void StartAnonymousGame() {

        PlayerPrefs.DeleteKey("competitionID");
        competitionIDUIText.text = "Competition ID: " + "Anonymous";
        CompetitionMiddleware.Instance.SetUserID( System.Guid.NewGuid().ToString());
        ShowStartSceneUI();
    }
}
