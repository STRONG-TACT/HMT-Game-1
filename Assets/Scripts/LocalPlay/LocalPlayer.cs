using System;
using UnityEngine;
using UnityEngine.UI;

public class LocalPlayer : MonoBehaviour
{
    public LocalCharacter myCharacter { set; get; }

    public int charaID { get { return myCharacter.CharacterId; } }

    public Button pinFinishBtn;

    public Button dwarfBtn;
    public Button gaintBtn;
    public Button humanBtn;

    public Button upBtn;
    public Button downBtn;
    public Button leftBtn;
    public Button rightBtn;
    public Button waitBtn;
    public Button backBtn;
    public Button submitBtn;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        pinFinishBtn.onClick.AddListener(delegate { finishPinning(); });

        dwarfBtn.onClick.AddListener(delegate { switchCharacter(0); });
        gaintBtn.onClick.AddListener(delegate { switchCharacter(1); });
        humanBtn.onClick.AddListener(delegate { switchCharacter(2); });

        upBtn.onClick.AddListener(delegate { addNewMove(1); });
        downBtn.onClick.AddListener(delegate { addNewMove(2); });
        leftBtn.onClick.AddListener(delegate { addNewMove(3); });
        rightBtn.onClick.AddListener(delegate { addNewMove(4); });
        waitBtn.onClick.AddListener(delegate { addNewMove(0); });

        backBtn.onClick.AddListener(delegate { backOneMove(); });
        submitBtn.onClick.AddListener(delegate { submitPlan(); });
    }

    private void switchCharacter(int index)
    {
        LocalGameManager.Instance.switchCharacter(index);
    }

    public void charaSwitched(int index, bool submitted, bool isEmpty, bool isFull)
    {
        switch (index)
        {
            case 0:
                dwarfBtn.interactable = false;
                gaintBtn.interactable = true;
                humanBtn.interactable = true;
                break;
            case 1:
                dwarfBtn.interactable = true;
                gaintBtn.interactable = false;
                humanBtn.interactable = true;
                break;
            case 2:
                dwarfBtn.interactable = true;
                gaintBtn.interactable = true;
                humanBtn.interactable = false;
                break;
            default:
                break;
        }

        if (LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Pinning)
        {
            checkPinBtnStatus(submitted);
        }
        else if (LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Planning)
        {
            checkPlanBtnStatus(submitted, isEmpty, isFull);
        }
    }

    private void finishPinning()
    {
        LocalGameManager.Instance.newPinFinished(charaID);
    }

    public void pinFinished()
    {
        checkPinBtnStatus(true);
    }

    private void checkPinBtnStatus(bool submitted)
    {
        if (submitted)
        {
            disablePinBtn();
        }
        else
        {
            enablePinBtn();
        }
    }

    public void addNewMove(int move)
    {
        if(move > 0 && move < 5 && myCharacter.CheckMove((LocalCharacter.Direction)move))
        {
            LocalGameManager.Instance.newPlayerMovePlan(charaID, move);
        }else if (move == 0)
        {
            LocalGameManager.Instance.newPlayerMovePlan(charaID, move);
        }
    }

    public void planUpdated(bool submitted, bool isEmpty, bool isFull)
    {
        checkPlanBtnStatus(submitted, isEmpty, isFull);
    }

    public void backOneMove()
    {
        LocalGameManager.Instance.backOneMove(charaID);
    }

    public void submitPlan()
    {
        LocalGameManager.Instance.newPlanSubmitted(charaID);
    }

    private void checkPlanBtnStatus(bool submitted, bool isEmpty, bool isFull)
    {
        if (submitted)
        {
            shutDownPlanButtons();
        }
        else if (isFull)
        {
            lastMovePlaned();
        }
        else if (isEmpty)
        {
            noMovePlaned();
        }
        else
        {
            someMovePlaned();
        }
    }

    public void disablePinBtn()
    {
        pinFinishBtn.interactable = false;
    }

    public void enablePinBtn()
    {
        pinFinishBtn.interactable = true;
    }

    public void lastMovePlaned()
    {
        upBtn.interactable = false;
        downBtn.interactable = false;
        leftBtn.interactable = false;
        rightBtn.interactable = false;
        waitBtn.interactable = false;

        backBtn.interactable = true;
        submitBtn.interactable = true;
    }

    public void noMovePlaned()
    {
        upBtn.interactable = true;
        downBtn.interactable = true;
        leftBtn.interactable = true;
        rightBtn.interactable = true;
        waitBtn.interactable = true;

        backBtn.interactable = false;
        submitBtn.interactable = false;
    }

    public void someMovePlaned()
    {
        upBtn.interactable = true;
        downBtn.interactable = true;
        leftBtn.interactable = true;
        rightBtn.interactable = true;
        waitBtn.interactable = true;

        backBtn.interactable = true;
        submitBtn.interactable = false;
    }

    public void shutDownPlanButtons()
    {
        upBtn.interactable = false;
        downBtn.interactable = false;
        leftBtn.interactable = false;
        rightBtn.interactable = false;
        waitBtn.interactable = false;

        backBtn.interactable = false;
        submitBtn.interactable = false;
    }
}
