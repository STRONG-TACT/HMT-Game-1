using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using static CombatSystem;

/// <summary>
/// This class is a central hub for any edits to the UI.
/// UI manipulation should not happen in any other class!
/// </summary>
public class UIManager : MonoBehaviour {

    public UIManager Instance { get; private set; }

    private GameAssets gameAssets;
    
    public GameObject PinErrorPanel;
    public GameObject PinWindow;
    

    public GameObject WaitingForPlayersPanel;

    //Turn Indicator
    public GameObject TurnIndicatorPanel;
    Image characterIcon;

    //Action Panel
    public GameObject ActionPanel;
    List<GameObject> actionPoints = new List<GameObject>();

    //Health Panel
    public GameObject HealthPanel;
    List<GameObject> hearts = new List<GameObject>(); 
    List<GameObject> brokenHearts = new List<GameObject>();

    //Goal Panel
    public GameObject GoalPanel;
    
    //Combat Panel
    public GameObject CombatPanel;
    Image CombatUIImg;
    TextMeshProUGUI CombatMonsterNumbers;
    TextMeshProUGUI CombatResultText;


    // Start is called before the first frame update
    IEnumerator Start() {
        Instance = this;
        gameAssets = FindObjectOfType<GameAssets>();

        //TurnIndicatorPanel = GameObject.Find("UI/TurnIndicator");
        //ActionPanel = GameObject.Find("UI/Action");
        //HealthPanel = GameObject.Find("UI/Health");
        //PinErrorPanel = GameObject.Find("UI/PinErrorMessage");
        //PinWindow = GameObject.Find("UI/PinWindow");
        //GoalPanel = GameObject.Find("UI/GoalPanel");
        
        //WaitingForPlayersPanel = GameObject.Find("UI/WaitForPlayerTxt");

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

        //CombatUI Pointers
        //CombatPanel = GameObject.Find("UI/CombatPanel");
        if (CombatPanel != null) {
            CombatPanel.SetActive(false);

            CombatUIImg = CombatPanel.transform.Find("CombatUIImg").GetComponent<Image>();
            CombatMonsterNumbers = CombatPanel.transform.Find("MonsterNumberTxt").GetComponent<TextMeshProUGUI>();
            CombatResultText = CombatPanel.transform.Find("ResultTxt").GetComponent<TextMeshProUGUI>();

            //Debug.LogFormat("<color=cyan>IS NULL</color>CombatUIImg: {0}, CombatMonsterNumbers: {1}, CombatResultText: {2}", 
            //    CombatUIImg == null,
            //    CombatMonsterNumbers == null,
            //    CombatResultText == null);
            //foreach(Transform child in CombatPanel.transform) {
            //    Debug.LogFormat("<color=green>CombatPanel Child</color>: {0}", child.name);
            //}

            //foreach(MonoBehaviour mb in CombatPanel.transform.Find("MonsterNumberTxt").GetComponents<MonoBehaviour>()) {
            //    Debug.LogFormat("<color=red>CombatPanel MonsterNumberTxt</color>: {0}", mb.GetType().Name);
            //}

        }

        while (!PlayerMapper.Instance.Inititialized) {
            yield return null;
        }
        GameData gameData = FindObjectOfType<GameData>();
        Image playerIndicator = GameObject.Find("UI/YouArePanel").GetComponent<Image>();
        playerIndicator.sprite = gameData.characterConfigs[PlayerMapper.Instance.LocalCharacterNumber].youAreIcon;
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
    public void UpdateTurnIndicator(CharacterConfig.CharacterType character) {
        characterIcon.sprite = gameAssets.GetCharacterIcon(character);
    }

    public void UpdateGoalUI(string character, bool achieved=true) {
        switch(character.ToLower()) {
            case "dwarf":
                UpdateGoalUI(CharacterConfig.CharacterType.Dwarf,achieved);
                break;
            case "giant":
                UpdateGoalUI(CharacterConfig.CharacterType.Giant,achieved);
                break;
            case "human":
                UpdateGoalUI(CharacterConfig.CharacterType.Human,achieved);
                break;
        }
    }

    public void UpdateGoalUI(CharacterConfig.CharacterType character, bool achieved = true) {
        Image goalIcon;
        switch (character) {
            case CharacterConfig.CharacterType.Dwarf:
                goalIcon = GoalPanel.transform.Find("GoalDwarf").GetComponent<Image>();
                break;
            case CharacterConfig.CharacterType.Human:
                goalIcon = GoalPanel.transform.Find("GoalHuman").GetComponent<Image>();
                break;
            case CharacterConfig.CharacterType.Giant:
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

    public void ShowCombatUI(CombatSystem.FightType fightType, int[] enemyNumbers ) {
        switch (fightType) {
            case FightType.Rock:
                CombatUIImg.sprite = gameAssets.rockCombatUI;
                CombatMonsterNumbers.text = string.Empty;
                break;

            case FightType.Trap:
                CombatUIImg.sprite = gameAssets.trapCombatUI;
                CombatMonsterNumbers.text = string.Empty;
                break;

            case FightType.Monster:
                CombatUIImg.sprite = gameAssets.monsterCombatUI;
                string txt = "";
                for (int i = 0; i < enemyNumbers.Length; i++) {
                    txt = txt + enemyNumbers[i].ToString() + " ";
                }
                CombatMonsterNumbers.text = txt;
                break;
        }
        CombatResultText.text = string.Empty;
        CombatPanel.SetActive(true);
    }

    public void DisplayCombatResult(string message) {
        CombatResultText.text = message;
    }

    public void DismissCombatUI() {
        CombatResultText.text = string.Empty;
        CloseDiceImg();
        CombatPanel.SetActive(false);
    }

    public void ShowDiceImg(FightType fightType, int playerDie) {
        Image diceImg = GameObject.Find("DiceImgs").transform.GetChild((int)fightType).gameObject.GetComponent<Image>();
        diceImg.sprite = gameAssets.diceImg[playerDie - 1];
        diceImg.gameObject.SetActive(true);
    }

    public void CloseDiceImg() {
        GameObject DiceImgs = GameObject.Find("DiceImgs");
        foreach(Transform t in DiceImgs.transform) {
            t.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update() {

    }
}
