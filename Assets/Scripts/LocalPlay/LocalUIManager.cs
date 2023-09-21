using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LocalUIManager : MonoBehaviour
{
    public TMP_Text text;

    public float TutorialTime = 2.5f;

    private string stageText = "";

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

    public void ShowCharacterPlanUI(string charaName)
    {
        stageText = string.Format("{0}'s planning time - ", charaName);

        text.text = stageText;
    }

    public void ShowMoveLeft(int moveLeft)
    {
        text.text = stageText + string.Format("Moves left: {0}", moveLeft);
    }
}
