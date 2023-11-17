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

    public Image YouAreInfo;
    public GameObject HealthPanel;
    public GameObject ActionPanel;

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

    public void ShowCharacterPinUI(int charaID, int health, int movePoints)
    {
        switch (charaID)
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

        UpdateHealthPanel(health);

        UpdateActionPanel(movePoints);
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
    }

    public void HideCharacterPinUI()
    {
        PinFinishBtn.SetActive(false);
        SwitchCharaButton.SetActive(false);
        HealthPanel.SetActive(false);
        ActionPanel.SetActive(false);
    }

    public void ShowCharacterPlanUI(int charaID, int health, int movePoints)
    {
        switch (charaID)
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

        UpdateHealthPanel(health);

        UpdateActionPanel(movePoints);

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
    }

    public void HideCharacterPlanUI()
    {
        PlanUI.SetActive(false);
        SwitchCharaButton.SetActive(false);
        HealthPanel.SetActive(false);
        ActionPanel.SetActive(false);
    }

    public void UpdateActionPointsRemaining(int movePoints)
    {
        UpdateActionPanel(movePoints);
    }

    public void ShowCombatUI(Combat.FightType type, List<int> charaDice, List<int> enemyDice)
    {
        if (type == Combat.FightType.Monster)
        {
            text.text = "Combat with monster... Character: ";
            foreach (int i in charaDice)
            {
                text.text += string.Format("{0} ", i);
            }

            text.text += "Monster: ";

            foreach (int i in enemyDice)
            {
                text.text += string.Format("{0} ", i);
            }
        }
        else if (type == Combat.FightType.Trap)
        {
            text.text = "Combat with trap... Character: ";
            foreach (int i in charaDice)
            {
                text.text += string.Format("{0} ", i);
            }

            text.text += "Trap: ";

            foreach (int i in enemyDice)
            {
                text.text += string.Format("{0} ", i);
            }
        }
        else if (type == Combat.FightType.Rock)
        {
            text.text = "Combat with rock... Character: ";
            foreach (int i in charaDice)
            {
                text.text += string.Format("{0} ", i);
            }

            text.text += "Rock: ";

            foreach (int i in enemyDice)
            {
                text.text += string.Format("{0} ", i);
            }
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
}
