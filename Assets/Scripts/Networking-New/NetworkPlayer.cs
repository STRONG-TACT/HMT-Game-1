using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;

public class NetworkPlayer : MonoBehaviour
{
    public NetworkCharacter myCharacter { set; get; }

    public int charaID { get { return myCharacter.CharacterId; } }

    public Button pinFinishBtn;

    public GameObject planParent;
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

        pinFinishBtn.onClick.AddListener(delegate { SubmitPings(); });

        upBtn.onClick.AddListener(delegate { AddMoveToCharacter(NetworkCharacter.Direction.Up); });
        downBtn.onClick.AddListener(delegate { AddMoveToCharacter(NetworkCharacter.Direction.Down); });
        leftBtn.onClick.AddListener(delegate { AddMoveToCharacter(NetworkCharacter.Direction.Left); });
        rightBtn.onClick.AddListener(delegate { AddMoveToCharacter(NetworkCharacter.Direction.Right); });
        waitBtn.onClick.AddListener(delegate { AddMoveToCharacter(NetworkCharacter.Direction.Wait); });
        
        backBtn.onClick.AddListener(delegate { UndoPlanStep(); });
        submitBtn.onClick.AddListener(delegate { SubmitPlan(); });
    }

    public void UpdateCharacterUI() {
        if (NetworkGameManager.S.gameStatus == GameStatus.Player_Pinning) {
            UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
        }
        else if (NetworkGameManager.S.gameStatus == GameStatus.Player_Planning) {
            UpdatePlanUI(myCharacter.ReadyForNextPhase, 
                         myCharacter.ActionPlan.Count == 0, 
                         myCharacter.ActionPointsRemaining == 0);
        }
    }

    private void SubmitPings() {
        NetworkMiddleware.S.ReadyForNextPhaseLocal(myCharacter.CharacterId, true);
        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
        NetworkGameManager.S.CheckPingPhaseEnd();
    }

    private void UpdatePinBtnStatus(bool submitted) {
        pinFinishBtn.interactable = !submitted;
    }

    public void PlacePinByFocusedCharacter() {
        myCharacter.PlacePin();
        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
    }

    public void AddMoveToCharacter(NetworkCharacter.Direction direction)
    {
        if (myCharacter.ActionPointsRemaining > 0 && myCharacter.CheckMove(direction))
        {
            NetworkMiddleware.S.AddMoveToCharacterLocal(direction, myCharacter.CharacterId);
        }
    }
    
    public void UndoPlanStep() 
    {
        NetworkMiddleware.S.UndoPlanStepLocal(myCharacter.CharacterId);
        UpdatePlanUI(false, myCharacter.ActionPlan.Count == 0, false);
    }

    public void SubmitPlan() {
        UpdatePlanUI(true, false, false);
        NetworkMiddleware.S.ReadyForNextPhaseLocal(myCharacter.CharacterId, true);
    }

    public void UpdatePlanUI(bool submitted, bool isEmpty, bool isFull)
    {
        planParent.SetActive(true);
        
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
        submitBtn.interactable = true;
    }

    public void someMovePlaned()
    {
        upBtn.interactable = true;
        downBtn.interactable = true;
        leftBtn.interactable = true;
        rightBtn.interactable = true;
        waitBtn.interactable = true;

        backBtn.interactable = true;
        submitBtn.interactable = true;
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
