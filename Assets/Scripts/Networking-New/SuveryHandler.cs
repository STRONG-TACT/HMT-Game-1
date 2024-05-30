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
    string response;
    // Start is called before the first frame update
    void Start()
    {
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

    public void Submit()
    {
        foreach (ToggleGroup question in questions)
        {
            Toggle toggle = question.ActiveToggles().FirstOrDefault();
            //Debug.Log(toggle.GetComponentInChildren<Text>().text);
            response = response + toggle.GetComponentInChildren<Text>().text;
        }
        Debug.Log(response);
        ShowDialogBox();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(GlobalConstant.LOBBY_SCENE);
    }

}
