using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LocalUIManager : MonoBehaviour
{
    public TMP_Text text;

    public float TutorialTime = 2.5f;

    private string stageText = "";

    public GameObject PlanUI;
    public GameObject PinFinishBtn;
    public GameObject SwitchCharaButton;

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

    public IEnumerator PlayTutorial()
    {
        text.text = "Tutorial";

        yield return new WaitForSeconds(TutorialTime);

        LocalGameManager.Instance.StartLevel();
    }

    public void ShowCharacterPinUI(string charaName, int movePoints, bool dead)
    {
        if (!dead)
        {
            stageText = string.Format("{0}'s pinning - ", charaName);

            text.text = stageText + string.Format("Moves left: {0}", movePoints);

            PinFinishBtn.SetActive(true);
            SwitchCharaButton.SetActive(true);
        }
        else
        {
            text.text = string.Format("{0}'s respawning...", charaName);
            PinFinishBtn.SetActive(true);
            SwitchCharaButton.SetActive(true);
        }
    }

    public void HideCharacterPinUI()
    {
        PinFinishBtn.SetActive(false);
        SwitchCharaButton.SetActive(false);
    }

    public void ShowCharacterPlanUI(string charaName, int movePoints, bool dead)
    {
        if (!dead) {
            stageText = string.Format("{0}'s planning - ", charaName);

            text.text = stageText + string.Format("Moves left: {0}", movePoints);

            PlanUI.SetActive(true);
            SwitchCharaButton.SetActive(true);
        }
        else
        {
            text.text = string.Format("{0}'s respawning...", charaName);
            PlanUI.SetActive(true);
            SwitchCharaButton.SetActive(true);
        }
    }

    public void HideCharacterPlanUI()
    {
        PlanUI.SetActive(false);
        SwitchCharaButton.SetActive(false);
    }

    public void ShowCharacterMovingUI()
    {
        text.text = "Characters moving...";
    }

    public void UpdateActionPointsRemaining(int movePoints)
    {
        text.text = stageText + string.Format("Moves left: {0}", movePoints);
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

    public void ShowMonsterTurnUI()
    {
        text.text = "Monsters moving...";
    }
}
