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

    public void ShowCharacterPlanUI(string charaName, int movePoints)
    {
        stageText = string.Format("{0}'s planning - ", charaName);

        text.text = stageText + string.Format("Moves left: {0}", movePoints);

        PlanUI.SetActive(true);
        SwitchCharaButton.SetActive(true);
    }

    public void HideCharacterPlanUI()
    {
        text.text = "Characters moving...";

        PlanUI.SetActive(false);
        SwitchCharaButton.SetActive(false);
    }

    public void ShowMoveLeft(int movePoints)
    {
        text.text = stageText + string.Format("Moves left: {0}", movePoints);
    }

    public void ShowMonsterTurnUI()
    {
        text.text = "Monsters moving...";
    }
}
