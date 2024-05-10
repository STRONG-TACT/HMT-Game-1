using System;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;

public class IntegratedPlayer : MonoBehaviour
{
    public Character myCharacter { set; get; }
    public int charaID { get { return myCharacter.CharacterId; } }
    
    public Button pinFinishBtn;

    public Button upBtn;
    public Button downBtn;
    public Button leftBtn;
    public Button rightBtn;
    public Button waitBtn;
    public Button backBtn;
    public Button submitBtn;

    protected virtual void Start()
    {
        DontDestroyOnLoad(gameObject);

        pinFinishBtn.onClick.AddListener(delegate { SubmitPings(); });

        upBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Up); });
        downBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Down); });
        leftBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Left); });
        rightBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Right); });
        waitBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Wait); });
        
        backBtn.onClick.AddListener(delegate { UndoPlanStep(); });
        submitBtn.onClick.AddListener(delegate { SubmitPlan(); });
    }

    public virtual void SubmitPings()
    {
        PinningSystem.S.Cancel();
        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
        IntegratedGameManager.S.CheckPingPhaseEnd();
        // network: call middleware
        // local:   myCharacter.ReadyForNextPhase = true;
    }

    //public void UpdateCharacterUI()
    //{
    //    if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Pinning) {
    //        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
    //    }
    //    else if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning) {
    //        UpdatePlanUI(myCharacter.ReadyForNextPhase, 
    //            myCharacter.ActionPlan.Count == 0, 
    //            myCharacter.ActionPointsRemaining == 0);
    //    }
        
    //    // local: switch character button on/off
    //}
    
    private void UpdatePinBtnStatus(bool submitted) {
        pinFinishBtn.interactable = !submitted;
    }
    
    public void PlacePinByFocusedCharacter() {
        myCharacter.PlacePin();
        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
    }

    public virtual void AddMoveToCharacter(Character.Direction direction)
    {
        //if (myCharacter.ActionPointsRemaining <= 0 || !myCharacter.CheckMove(direction))
        //{
        //    return;
        //}
        // network: middleware - AddMoveToCharacterLocal
        // local:   LocalGameManager.Instance.UpdateFocusPlayPlan(charaID, move);
    }

    public virtual void UndoPlanStep()
    {
        //UpdatePlanUI(false, myCharacter.ActionPlan.Count == 0, false);
        // network: NetworkMiddleware.S.UndoPlanStepLocal(myCharacter.CharacterId);
        // local:   myCharacter.UndoPlanStep();
    }

    public virtual void SubmitPlan()
    {
        //UpdatePlanUI(true, false, false);
        // network: middleware
        // local:   CheckPlanPhaseEnd
    }
    
    //public void UpdatePlanUI(bool submitted, bool isEmpty, bool isFull)
    //{
    //    if (submitted)
    //    {
    //        shutDownPlanButtons();
    //    }
    //    else if (isFull)
    //    {
    //        lastMovePlaned();
    //    }
    //    else if (isEmpty)
    //    {
    //        noMovePlaned();
    //    }
    //    else
    //    {
    //        someMovePlaned();
    //    }
    //}

    //public void lastMovePlaned()
    //{
    //    upBtn.interactable = false;
    //    downBtn.interactable = false;
    //    leftBtn.interactable = false;
    //    rightBtn.interactable = false;
    //    waitBtn.interactable = false;

    //    backBtn.interactable = true;
    //    submitBtn.interactable = true;
    //}

    //public void noMovePlaned()
    //{
    //    upBtn.interactable = true;
    //    downBtn.interactable = true;
    //    leftBtn.interactable = true;
    //    rightBtn.interactable = true;
    //    waitBtn.interactable = true;

    //    backBtn.interactable = false;
    //    submitBtn.interactable = true;
    //}

    //public void someMovePlaned()
    //{
    //    upBtn.interactable = true;
    //    downBtn.interactable = true;
    //    leftBtn.interactable = true;
    //    rightBtn.interactable = true;
    //    waitBtn.interactable = true;

    //    backBtn.interactable = true;
    //    submitBtn.interactable = true;
    //}

    //public void shutDownPlanButtons()
    //{
    //    upBtn.interactable = false;
    //    downBtn.interactable = false;
    //    leftBtn.interactable = false;
    //    rightBtn.interactable = false;
    //    waitBtn.interactable = false;

    //    backBtn.interactable = false;
    //    submitBtn.interactable = false;
    //}
}
