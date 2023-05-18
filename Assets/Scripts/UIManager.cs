using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

/// <summary>
/// This class is a central hub for any edits to the UI.
/// UI manipulation should not happen in any other class!
/// </summary>
public class UIManager : MonoBehaviour {

    public UIManager Instance { get; private set; }

    private GameAssets gameAssets;
    
    GameObject PinErrorPanel;
    GameObject PinWindow;
    
    GameObject CombatUI;
    GameObject WaitingForPlayersPanel;

    //Turn Indicator
    GameObject TurnIndicatorPanel;
    Image characterIcon;

    //Action Panel
    GameObject ActionPanel;
    List<GameObject> actionPoints = new List<GameObject>();

    //Health Panel
    GameObject HealthPanel;
    List<GameObject> hearts = new List<GameObject>(); 
    List<GameObject> brokenHearts = new List<GameObject>();

    //Goal Panel
    GameObject GoalPanel;


    // Start is called before the first frame update
    void Start() {
        Instance = this;
        gameAssets = FindObjectOfType<GameAssets>();

        TurnIndicatorPanel = GameObject.Find("UI/TurnIndicator");
        ActionPanel = GameObject.Find("UI/Action");
        HealthPanel = GameObject.Find("UI/Health");
        PinErrorPanel = GameObject.Find("UI/PinErrorMessage");
        PinWindow = GameObject.Find("UI/PinWindow");
        GoalPanel = GameObject.Find("UI/GoalPanel");
        CombatUI = GameObject.Find("UI/CombatUI");
        WaitingForPlayersPanel = GameObject.Find("UI/WaitForPlayerTxt");

        //TurnIndicator Links
        characterIcon = TurnIndicatorPanel.transform.Find("CharacterIcon").GetComponent<Image>();

        //Action UI Links
        actionPoints = new List<GameObject>();
        foreach(Transform child in ActionPanel.transform.Find("ActionIcons").transform) {
            actionPoints.Add(child.gameObject);
        }
        actionPoints = actionPoints.OrderBy(gob => gob.transform.position.x).ToList();

        //Health UI Links
        hearts = new List<GameObject>();
        brokenHearts = new List<GameObject>();
        foreach(Transform child in HealthPanel.transform) {
            if (child.name.StartsWith("Broke")) {
                brokenHearts.Add(child.gameObject);
            }
            else {
                hearts.Add(child.gameObject);
            }
        }
        hearts = hearts.OrderBy(gob => gob.transform.position.x).ToList();
        brokenHearts = brokenHearts.OrderBy(gob => gob.transform.position.x).ToList();
    }

    public void HideWaitingForPlayers() {
        WaitingForPlayersPanel.SetActive(false);
    }

    public void InitGameUI() {
        TurnIndicatorPanel.SetActive(true);
        ActionPanel.SetActive(true);
        GoalPanel.SetActive(true);
    }

    public void UpdateActionPointsUI(int actionPoints) {
        if(actionPoints > 0) {
            for(int i = 0; i <  this.actionPoints.Count; i++) {
                if(i <  actionPoints) {
                    this.actionPoints[i].SetActive(true); 
                }
                else {
                    this.actionPoints[i].SetActive(false);
                }
            }
        }
    }

    public void UpdateHealthUI(int health) {
        if(health > 0) {
            for(int i = 0; i < hearts.Count; i++) { 
                if(i < health) {
                    hearts[i].SetActive(true);
                    brokenHearts[i].SetActive(false);
                }
                else {
                    hearts[i].SetActive(false);
                    brokenHearts[i].SetActive(true);
                }
            }
        }
    }

    //It would be great to have a combined image that was just each character's turn or "YOUR turn"
    //Also this could implicitly kick off the turn transition indicator
    public void UpdateTurnIndicator(GameData.CharacterType character) {
        characterIcon.sprite = gameAssets.GetCharacterIcon(character);
    }

    public void UpdateGoalUI(string character, bool achieved=true) {
        switch(character.ToLower()) {
            case "dwarf":
                UpdateGoalUI(GameData.CharacterType.Dwarf,achieved);
                break;
            case "giant":
                UpdateGoalUI(GameData.CharacterType.Giant,achieved);
                break;
            case "human":
                UpdateGoalUI(GameData.CharacterType.Human,achieved);
                break;
        }
    }

    public void UpdateGoalUI(GameData.CharacterType character, bool achieved = true) {
        Image goalIcon;
        switch (character) {
            case GameData.CharacterType.Dwarf:
                goalIcon = GoalPanel.transform.Find("GoalDwarf").GetComponent<Image>();
                break;
            case GameData.CharacterType.Human:
                goalIcon = GoalPanel.transform.Find("GoalHuman").GetComponent<Image>();
                break;
            case GameData.CharacterType.Giant:
                goalIcon = GoalPanel.transform.Find("GoalGiant").GetComponent<Image>();
                break;
            default:
                return;
        }
        Color tint = goalIcon.color;
        if (achieved) {
            tint.a = 1f;
        }
        else {
            tint.a = .5f;
        }
        goalIcon.color = tint;
    }

    // Update is called once per frame
    void Update() {

    }
}
