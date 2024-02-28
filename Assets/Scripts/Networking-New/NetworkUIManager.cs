using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;

public class NetworkUIManager : MonoBehaviour
{
    public GameAssets gameAssets;

    public TMP_Text text;

    public float TutorialTime = 1f;

    public GameObject PlanUI;
    public GameObject PinFinishBtn;
    public GameObject SwitchCharaButton;

    public GameObject CharacterInfo;
    public Image YouAreInfo;
    public GameObject[] DiceStats = new GameObject[3];
    public GameObject[] BonusStats = new GameObject[3];
    public GameObject HealthPanel;
    public GameObject ActionPanel;

    public GameObject CombatUI;
    public GameObject[] PlayerCombatSlots = new GameObject[3];
    public GameObject[] EnemyCombatSlots = new GameObject[4];
    public TMP_Text PlayerFinalScore;
    public TMP_Text EnemyFinalScore;
    public TMP_Text ResultMessage;
    public GameObject WinBG;
    public GameObject LoseBG;

    public GameObject GoalPanel; 
    public GameObject[] GoalStatus = new GameObject[3];

    public void InitGameUI()
    {
        text.text = "Level Starting";
        GoalPanel.SetActive(true);
    }
    
    public void UpdateGamePhaseInfo()
    {
        switch (NetworkGameManager.S.gameStatus)
        {
            case GameStatus.Player_Pinning:
                text.text = "Player Pinning Phase";
                break;
            case GameStatus.Player_Planning:
                text.text = "Player Planning Phase";
                break;
            case GameStatus.Player_Moving:
                text.text = "Players Moving";
                break;
            case GameStatus.Monster_Moving:
                text.text = "Monsters Moving";
                break;
            case GameStatus.Animation_Pause:
                text.text = "An Event Happening";
                break;
            default:
                text.text = "Game Loading";
                break;
        }
    }
    
    private void UpdateHealthPanel(int health)
    {
        foreach (Transform child in HealthPanel.transform.GetComponentsInChildren<Transform>())
        {
            child.gameObject.SetActive(false);
        }

        for (int i = 0; i < health; i++)
        {
            HealthPanel.transform.GetChild(i).gameObject.SetActive(true);
        }

        // TODO: maybe not hard code max health value
        for (int j = 0; j < 3 - health; j++)
        {
            HealthPanel.transform.GetChild(j+3).gameObject.SetActive(true);
        }

        HealthPanel.gameObject.SetActive(true);
    }
    
    private void UpdateActionPanel(int actionPointCount)
    {
        foreach (Transform child in ActionPanel.transform.GetComponentsInChildren<Transform>())
        {
            child.gameObject.SetActive(false);
        }

        for (int i = 0; i < actionPointCount; i++)
        {
            ActionPanel.transform.GetChild(i).gameObject.SetActive(true);
        }

        ActionPanel.gameObject.SetActive(true);
    }
    
    private void UpdateCharacterStats()
    {
        NetworkCharacter currentCharacter = NetworkGameManager.S.localChar;
        
        DiceStats[0].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.monsterDice.type);
        DiceStats[1].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.stoneDice.type);
        DiceStats[2].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.trapDice.type);

        BonusStats[0].GetComponent<TMP_Text>().text = currentCharacter.config.monsterDice.bonus.ToString();
        BonusStats[1].GetComponent<TMP_Text>().text = currentCharacter.config.stoneDice.bonus.ToString();
        BonusStats[2].GetComponent<TMP_Text>().text = currentCharacter.config.trapDice.bonus.ToString();
    }
    
    public void ShowCharacterPinUI()
    {
        NetworkCharacter currentCharacter = NetworkGameManager.S.localChar;
        switch (currentCharacter.CharacterId)
        {
            case 0:
                YouAreInfo.sprite = gameAssets.youAreDwarf;
                break;
            case 1:
                YouAreInfo.sprite = gameAssets.youAreGiant;
                break;
            case 2:
                YouAreInfo.sprite = gameAssets.youAreHuman;
                break;
        }

        PinFinishBtn.SetActive(true);
        SwitchCharaButton.SetActive(true);

        // TODO check the case when health == 0
        UpdateHealthPanel(currentCharacter.Health);
        UpdateActionPanel(currentCharacter.ActionPointsRemaining);
        UpdateCharacterStats();
        CharacterInfo.SetActive(true);
    }
    
    
    
}
