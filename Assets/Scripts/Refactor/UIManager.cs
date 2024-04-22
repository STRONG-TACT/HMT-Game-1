using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;
using System;


public class UIManager : MonoBehaviour
{
    public GameAssets gameAssets;

    public TMP_Text text;

    public float TutorialTime = 1f;

    public GameObject PlanUI;
    public GameObject PinFinishBtn;

    public GameObject CharacterInfo;
    //public Image YouAreInfo;
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
    public GameObject VictoryScreen;
    public GameObject LossScreen;
    public GameObject Timer;

    //public GameObject GoalPanel; 
    public GameObject[] GoalStatus = new GameObject[3];
    public GameObject[] LifeStatus = new GameObject[3];
    public GameObject[] ActionStatus = new GameObject[3];

    private Coroutine dotsCoroutine;

    public void InitGameUI()
    {
        text.text = "Level Starting";
        CombatUI.SetActive(false);
        
        //GoalPanel.SetActive(true);
        CharacterInfo.SetActive(true);
        VictoryScreen.SetActive(false);
        LossScreen.SetActive(false);

        VictoryScreen.SetActive(false);
        LossScreen.SetActive(false);
    }
    
    public void DisplayVictoryScreen() {
        VictoryScreen.SetActive(true);
    }

    public void DisplayLossScreen()
    {
        LossScreen.SetActive(true);
    }
    
    public void UpdateGamePhaseInfo()
    {
        if (IntegratedGameManager.S.localChar.dead)
        {
            text.text = "Waiting Respawn";
            return;
        }
        
        switch (IntegratedGameManager.S.gameStatus)
        {
            case GameStatus.Player_Pinning:
                text.text = "Player Pinning Phase";
                break;
            case GameStatus.Player_Planning:
                text.text = "Player Planning Phase";
                PinFinishBtn.SetActive(false);
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
    
    public void LoadLevelEndUI() {
        text.text = "Level Conquered!";
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

        for (int j = 0; j < GlobalConstant.START_HEALTH - health; j++)
        {
            HealthPanel.transform.GetChild(j + GlobalConstant.START_HEALTH).gameObject.SetActive(true);
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
        Character currentCharacter = IntegratedGameManager.S.localChar;
        
        DiceStats[0].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.monsterDice.type);
        DiceStats[1].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.stoneDice.type);
        DiceStats[2].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.trapDice.type);

        BonusStats[0].GetComponent<TMP_Text>().text = currentCharacter.config.monsterDice.bonus.ToString();
        BonusStats[1].GetComponent<TMP_Text>().text = currentCharacter.config.stoneDice.bonus.ToString();
        BonusStats[2].GetComponent<TMP_Text>().text = currentCharacter.config.trapDice.bonus.ToString();
    }
    
    public void ShowCharacterPinUI()
    {
        Character currentCharacter = IntegratedGameManager.S.localChar;
        /*
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
        */

        PinFinishBtn.SetActive(true);

        // TODO check the case when health == 0
        UpdateHealthPanel(currentCharacter.Health);
        Debug.Log($"Health: {currentCharacter.Health}");
        UpdateActionPanel(currentCharacter.ActionPointsRemaining);
        UpdateCharacterStats();
        CharacterInfo.SetActive(true);
    }
    
    public void UpdateActionPointsRemaining(int movePoints)
    {
        UpdateActionPanel(movePoints);
    }
    
    public void HideCharacterPlanUI()
    {
        PlanUI.SetActive(false);
        CharacterInfo.SetActive(false);
        HealthPanel.SetActive(false);
        ActionPanel.SetActive(false);
    }
    
    public void ShowCombatUI(Combat.FightType type, List<int> charaIDs, List<int> charaDice, List<int> enemyDice,
                             int playerScore, int enemyScore, bool win, bool visible=true)
    {
        GameObject scoreBoard = CombatUI.transform.Find("Scoreboard").gameObject;
        GameObject vsText = CombatUI.transform.Find("VSText").gameObject;
        GameObject inProgressText = CombatUI.transform.Find("InProgressText").gameObject;
        GameObject UnknownOpponentSlot = CombatUI.transform.Find("UnknownOpponent").gameObject;

        if (visible == true)
        {
            scoreBoard.GetComponentInChildren<Image>().sprite = gameAssets.Scoreboard_visible;
            vsText.SetActive(true);
            inProgressText.SetActive(false);
            UnknownOpponentSlot.SetActive(false);
        }
        //if the combat happens outside of vision range
        else
        {
            scoreBoard.GetComponentInChildren<Image>().sprite = gameAssets.Scoreboard_hidden;
            vsText.SetActive(false);
            inProgressText.SetActive(true);
            UnknownOpponentSlot.SetActive(true);
        }

        for (int i = 0; i < charaIDs.Count; i++)
        {
            switch (charaIDs[i])
            {
                case 0:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.dwarfIcon;
                    if(visible == true)
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 1:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.giantIcon;
                    if (visible == true)
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 2:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.humanIcon;
                    if (visible == true)
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                default:
                    Debug.Log("Character ID out of scope in ShowCombatUI");
                    break;
            }
        }

        if (visible == false) 
        {
            
        }
        else if (type == Combat.FightType.Monster)
        {
            // TODO: differenciate monster types
            for (int i = 0; i < enemyDice.Count; i++)
            {
                EnemyCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.monsterIcon;
                EnemyCombatSlots[i].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
                EnemyCombatSlots[i].SetActive(true);
            }
        }
        else if (type == Combat.FightType.Trap)
        {
            EnemyCombatSlots[0].GetComponentInChildren<Image>().sprite = gameAssets.trapIcon;
            EnemyCombatSlots[0].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
            EnemyCombatSlots[0].SetActive(true);
        }
        else if (type == Combat.FightType.Rock)
        {
            EnemyCombatSlots[0].GetComponentInChildren<Image>().sprite = gameAssets.rockIcon;
            EnemyCombatSlots[0].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
            EnemyCombatSlots[0].SetActive(true);
        }

        if(visible == true) 
        {
            PlayerFinalScore.text = playerScore.ToString();
            EnemyFinalScore.text = enemyScore.ToString();
        }
        else
        {
            PlayerFinalScore.text = "";
            EnemyFinalScore.text = "";
        }


        if (win)
        {
            WinBG.SetActive(true);
            LoseBG.SetActive(false);
            ResultMessage.text = "The opponent is defeated!";
        }
        else
        {
            WinBG.SetActive(false);
            LoseBG.SetActive(true);
            ResultMessage.text = "Failed to defeat the opponent!";
        }

        CombatUI.SetActive(true);
    }
    
    public void HideCombatUI()
    {
        foreach (GameObject slot in PlayerCombatSlots)
        {
            slot.SetActive(false);
        }
        foreach (GameObject slot in EnemyCombatSlots)
        {
            slot.SetActive(false);
        }

        CombatUI.SetActive(false);
    }
    
    public void UpdateGoalStatus(int CharaID, bool reached = true)
    {
        if (reached)
            GoalStatus[CharaID].GetComponent<Image>().sprite = gameAssets.GetGoalFilled(CharaID);
        else 
            GoalStatus[CharaID].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(CharaID);
    }
    
    public void ResetGoalStatus()
    {
        //GoalPanel.SetActive(false);
        GoalStatus[0].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(0);
        GoalStatus[1].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(1);
        GoalStatus[2].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(2);
    }

    public void UpdateCharacterLifeStatus(int CharaID, bool alive = true)
    {
        if (alive)
            LifeStatus[CharaID].GetComponent<Image>().sprite = gameAssets.aliveIcon;
        else
            LifeStatus[CharaID].GetComponent<Image>().sprite = gameAssets.deadIcon;

    }

    public void ResetActionStatus()
    {
        UpdateCharacterActionStatus(0, false);
        UpdateCharacterActionStatus(1, false);
        UpdateCharacterActionStatus(2, false);
    }

    public void UpdateCharacterActionStatus(int CharaID, bool ready = true) {
        if (ready)
        //action status ui set to ready
        {
            ActionStatus[CharaID].transform.Find("Complete").gameObject.SetActive(true);
            if (dotsCoroutine != null) 
            {
                StopCoroutine(dotsCoroutine);
                dotsCoroutine = null;
            }
            ActionStatus[CharaID].transform.Find("Planning").gameObject.SetActive(false);
        }
        //action status ui set to plannning
        else
        {
            ActionStatus[CharaID].transform.Find("Complete").gameObject.SetActive(false);
            GameObject planning = ActionStatus[CharaID].transform.Find("Planning").gameObject;
            planning.SetActive(true);
            TextMeshProUGUI dotsText = planning.GetComponent<TextMeshProUGUI>();
            dotsCoroutine =  StartCoroutine(DotsAnimation(dotsText));
        }


    }

    
    public void ShowDefeatedScreen()
    {
        LossScreen.SetActive(true);
    }

    public IEnumerator DotsAnimation(TextMeshProUGUI dotsText)
    {
        string[] dotsStates = new string[] { ".", "..", "..." };
        int currentState = 0;
        while (true) 
        {
            dotsText.text = dotsStates[currentState]; // Update the text to the current state
            currentState = (currentState + 1) % dotsStates.Length; 
            yield return new WaitForSeconds(0.5f); 
        }
    }

    public void UpdateTimer(int secondsLeft)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(secondsLeft);
        string text_to_display = string.Format("{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);

        TextMeshProUGUI timer_text = Timer.transform.Find("Timer_text").gameObject.GetComponent<TextMeshProUGUI>();
        timer_text.text = text_to_display;

    }



}
