using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;
using GameConstant;

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
    [SerializeField] private GameObject invalid_id_prompt;
    [SerializeField] private GameObject ConsentFormUI;
    [SerializeField] private GameObject reset_id_UI;
    [SerializeField] private GameObject prompt;

    [Header("Text Input Fields")] 
    [SerializeField]
    // TODO: This is not TMP_Text, we want TMP_Text
    private Text roomSelectText;
    [SerializeField]
    private Text competitionIdText;
    [SerializeField] private TextMeshProUGUI competitionIDUIText;
    [SerializeField] private InputField inputFieldText;

    public bool RememberMe;
    private bool CheckCompetitionIDCoroutineRunning;
    private bool verificationCompleted = false;
    private bool isCompetitionIDValid = false;

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
        else if (competitionID == "")
        {
            ShowCompetitionIDUI();
        }
        else
        {
            CompetitionMiddleware.Instance.SetUserID(competitionID);
            ShowStartSceneUI();
        }
        reset_id_UI.SetActive(true);

    }


    public void ShowGameModeUI()
    {
        DisableAllUI();
        reset_id_UI.SetActive(false);
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
    public void DisagreeConsent()
    {
        DisableAllUI();
        prompt.SetActive(true);
    }

    public void ShowConsentFormUI()
    {
        DisableAllUI();
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
        prompt.SetActive(false);
    }

    public string GetRoomNameEntered()
    {
        return roomSelectText.text;
    }

    public void ToggleRememberMe(bool isOn)
    {
        RememberMe = isOn;
    }

    public void ResetCompetitionID()
    {
        PlayerPrefs.DeleteAll();
        competitionIDUIText.text = "Competition ID: ";
        invalid_id_prompt.SetActive(false);
        DisableAllUI();
        ShowConsentFormUI();
    }


    public void PasteCompetitionID()
    {
        string clipboardText = GUIUtility.systemCopyBuffer;
        Debug.Log("Clipboard text: " + clipboardText);
        inputFieldText.text = clipboardText;
        Debug.Log(competitionIdText.text);
    }


    public void SetCompetitionID() {
        if (!CheckCompetitionIDCoroutineRunning)
        {
            CheckCompetitionIDCoroutineRunning = true;
            StartCoroutine(CheckCompetitionIDCoroutine());
        }
    }

    // Callback method to handle the verification response
    private void OnVerifyCompetitionIDResponse(JObject response)
    {
        if (response != null && response.ContainsKey("valid") && response["valid"].ToObject<bool>())
        {
            Debug.Log("Competition ID is valid.");
            isCompetitionIDValid = true;
        }
        else
        {
            //Debug.LogError("Invalid competition ID.");
            invalid_id_prompt.SetActive(true);
            isCompetitionIDValid = false;
        }

        verificationCompleted = true;
    }

    private IEnumerator CheckCompetitionIDCoroutine()
    {
        if (competitionIdText.text == "")
        {
            CheckCompetitionIDCoroutineRunning = false;
            yield break;
        }
        if (RememberMe)
        {
            PlayerPrefs.SetString("competitionID", competitionIdText.text);
        }
        else
        {
            PlayerPrefs.DeleteKey("competitionID");
        }

        verificationCompleted = false;
        isCompetitionIDValid = false;
        CompetitionMiddleware.Instance.CallVerifyCompetitionId(competitionIdText.text, OnVerifyCompetitionIDResponse);

        // Wait for the verification to complete
        while (!verificationCompleted)
        {
            yield return null;
        }
        if (isCompetitionIDValid)
        {
            competitionIDUIText.text = "Competition ID: " + competitionIdText.text;
            CompetitionMiddleware.Instance.SetUserID(competitionIdText.text);
            competitionIDUI.SetActive(false);
            ShowStartSceneUI();
        }
        CheckCompetitionIDCoroutineRunning = false;
    }

    public void AgreeConsent()
    {

        if (RememberMe)
        {
            PlayerPrefs.SetInt("consent_agreed", 1);
        }
        //DisableConsentFormUI();
        ShowCompetitionIDUI();


    }

    public void StartAnonymousGame() {

        PlayerPrefs.DeleteKey("competitionID");
        competitionIDUIText.text = "Competition ID: " + "Anonymous";
        CompetitionMiddleware.Instance.SetUserID( System.Guid.NewGuid().ToString());

        //for testing survey scene only
        //SceneManager.LoadScene(GlobalConstant.SURVEY_SCENE);
        ShowStartSceneUI();
    }

    public void OpenHelpPageLink()
    {
        CompetitionMiddleware.Instance.LogOpenHelp();
        Application.OpenURL("https://strong-tact.github.io/game/");
    }


}
