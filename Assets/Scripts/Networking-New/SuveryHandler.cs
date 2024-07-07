using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;
using GameConstant;

public class SuveryHandler : MonoBehaviour
{
    [SerializeField] private GameObject DialogBox;
    [SerializeField] private GameObject SurveyUI;
    [SerializeField] private ToggleGroup[] questions;
    [SerializeField] private Button submitBtn;
    private List<string> questionTexts;
    private List<string> responses;


    public static SuveryHandler S;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    private void Update()
    {
    }


    // Start is called before the first frame update
    void Start()
    {
        foreach (ToggleGroup question in questions)
        {
            Toggle[] toggles = question.GetComponentsInChildren<Toggle>();
            foreach (Toggle toggle in toggles)
            {
                toggle.onValueChanged.AddListener(delegate { CheckIfAllQuestionAnswered(); });
            }
        }
        submitBtn.interactable = false;

        questionTexts = new List<string>();
        responses = new List<string>();
        questionTexts.Add("I would play with this team again.");
        questionTexts.Add("Our team was efficient.");
        questionTexts.Add("Our team communicated well.");
        questionTexts.Add("Our team performed well.");
        questionTexts.Add("Our team got better over time.");
        ShowSurveyUI();
    }

    public void DisableAllUI()
    {
        DialogBox.SetActive(false);
        SurveyUI.SetActive(false);
    }

    public void ShowDialogBox()
    {
        DisableAllUI();
        DialogBox.SetActive(true);
    }

    public void ShowSurveyUI()
    {
        DisableAllUI();
        SurveyUI.SetActive(true);
    }


    private void CheckIfAllQuestionAnswered()
    {
        int questionAnsweredCount = 0;
        foreach (ToggleGroup question in questions)
        {
            Toggle[] toggles = question.GetComponentsInChildren<Toggle>();
            foreach (Toggle toggle in toggles)
            {
                if (toggle.isOn)
                {
                    questionAnsweredCount++;
                }
            }
        }
        if(questionAnsweredCount >= 5)
        {
            submitBtn.interactable = true;
        }
        else
        {
            submitBtn.interactable = false;
        }
    }


    public void Submit()
    {
        responses.Clear();
        foreach (ToggleGroup question in questions)
        {
            Toggle toggle = question.ActiveToggles().FirstOrDefault();
            //Debug.Log(toggle.GetComponentInChildren<Text>().text);
            //response = response + toggle.GetComponentInChildren<Text>().text;
            string response = toggle.GetComponentInChildren<Text>().text;
            Debug.Log(response);
            responses.Add(response);
        }
        Debug.Log(responses);
        CompetitionMiddleware.Instance.LogSurveyResponse(questionTexts, responses);
        ShowDialogBox();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(GlobalConstant.LOBBY_SCENE);
    }

}
