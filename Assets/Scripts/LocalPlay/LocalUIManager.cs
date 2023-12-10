using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocalUIManager : MonoBehaviour
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

    public GameObject GoalPanel; 
    public GameObject[] GoalStatus = new GameObject[3];

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InitGameUI()
    {
        text.text = "Level Starting";
        GoalPanel.SetActive(true);
    }

    public void UpdateGamePhaseInfo()
    {
        switch (LocalGameManager.Instance.gameStatus)
        {
            case LocalGameManager.GameStatus.Player_Pinning:
                text.text = "Player Pinning Phase";
                break;
            case LocalGameManager.GameStatus.Player_Planning:
                text.text = "Player Planning Phase";
                break;
            case LocalGameManager.GameStatus.Player_Moving:
                text.text = "Players Moving";
                break;
            case LocalGameManager.GameStatus.Monster_Moving:
                text.text = "Monsters Moving";
                break;
            case LocalGameManager.GameStatus.Animation_Pause:
                text.text = "An Event Happening";
                break;
            default:
                text.text = "Game Loading";
                break;
        }
    }

    public IEnumerator PlayTutorial()
    {
        text.text = "Tutorial Time";

        yield return new WaitForSeconds(TutorialTime);

        LocalGameManager.Instance.StartLevel();
    }

    public void ShowCharacterPinUI(LocalCharacter currentCharacter)
    {
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
        //if (!dead)
        //{
        //    PinFinishBtn.SetActive(true);
        //    SwitchCharaButton.SetActive(true);
        //}
        //else
        //{
        //    text.text = string.Format("{0}'s respawning...", charaName);
        //    PinFinishBtn.SetActive(true);
        //    SwitchCharaButton.SetActive(true);
        //}

        UpdateCharacterStats(currentCharacter);
        CharacterInfo.SetActive(true);
    }

    public void HideCharacterPinUI()
    {
        PinFinishBtn.SetActive(false);
        SwitchCharaButton.SetActive(false);
        CharacterInfo.SetActive(false);
        HealthPanel.SetActive(false);
        ActionPanel.SetActive(false);
    }

    public void ShowCharacterPlanUI(LocalCharacter currentCharacter)
    {
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

        PlanUI.SetActive(true);
        SwitchCharaButton.SetActive(true);

        // TODO check the case when health == 0

        UpdateHealthPanel(currentCharacter.Health);

        UpdateActionPanel(currentCharacter.ActionPointsRemaining);

        //if (!dead) {
        //    PlanUI.SetActive(true);
        //    SwitchCharaButton.SetActive(true);
        //}
        //else
        //{
        //    text.text = string.Format("{0}'s respawning...", charaName);
        //    PlanUI.SetActive(true);
        //    SwitchCharaButton.SetActive(true);
        //}

        UpdateCharacterStats(currentCharacter);
        CharacterInfo.SetActive(true);
    }

    public void HideCharacterPlanUI()
    {
        PlanUI.SetActive(false);
        SwitchCharaButton.SetActive(false);
        CharacterInfo.SetActive(false);
        HealthPanel.SetActive(false);
        ActionPanel.SetActive(false);
    }

    public void UpdateActionPointsRemaining(int movePoints)
    {
        UpdateActionPanel(movePoints);
    }

    public void ShowCombatUI(Combat.FightType type, List<int> charaIDs, List<int> charaDice, List<int> enemyDice,
                             int playerScore, int enemyScore, bool win)
    {
        for (int i = 0; i < charaIDs.Count; i++)
        {
            switch (charaIDs[i])
            {
                case 0:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.dwarfIcon;
                    PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 1:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.giantIcon;
                    PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 2:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.humanIcon;
                    PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                default:
                    Debug.Log("Character ID out of scope in ShowCombatUI");
                    break;
            }
        }

        if (type == Combat.FightType.Monster)
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

        PlayerFinalScore.text = playerScore.ToString();
        EnemyFinalScore.text = enemyScore.ToString();

        if (win)
        {
            ResultMessage.text = "You defeated the enemy!";
        }
        else
        {
            ResultMessage.text = "Oops...";
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

    private void UpdateCharacterStats(LocalCharacter currentCharacter)
    {
        DiceStats[0].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.monsterDice.type);
        DiceStats[1].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.stoneDice.type);
        DiceStats[2].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.trapDice.type);

        BonusStats[0].GetComponent<TMP_Text>().text = currentCharacter.config.monsterDice.bonus.ToString();
        BonusStats[1].GetComponent<TMP_Text>().text = currentCharacter.config.stoneDice.bonus.ToString();
        BonusStats[2].GetComponent<TMP_Text>().text = currentCharacter.config.trapDice.bonus.ToString();
    }

    public void UpdateGoalStatus(int CharaID)
    {
        GoalStatus[CharaID].GetComponent<Image>().sprite = gameAssets.GetGoalFilled(CharaID);
    }

    public void ResetGoalStatus()
    {
        GoalPanel.SetActive(false);
        GoalStatus[0].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(0);
        GoalStatus[1].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(1);
        GoalStatus[2].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(2);
    }
}
