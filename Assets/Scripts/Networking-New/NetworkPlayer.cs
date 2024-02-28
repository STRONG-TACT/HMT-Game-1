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

        pinFinishBtn.onClick.AddListener(delegate { SubmitPings(); });

        // dwarfBtn.onClick.AddListener(delegate { SwitchCharacter(0); });
        // gaintBtn.onClick.AddListener(delegate { SwitchCharacter(1); });
        // humanBtn.onClick.AddListener(delegate { SwitchCharacter(2); });

        // upBtn.onClick.AddListener(delegate { AddMoveToFocusedCharacter(LocalCharacter.Direction.Up); });
        // downBtn.onClick.AddListener(delegate { AddMoveToFocusedCharacter(LocalCharacter.Direction.Down); });
        // leftBtn.onClick.AddListener(delegate { AddMoveToFocusedCharacter(LocalCharacter.Direction.Left); });
        // rightBtn.onClick.AddListener(delegate { AddMoveToFocusedCharacter(LocalCharacter.Direction.Right); });
        // waitBtn.onClick.AddListener(delegate { AddMoveToFocusedCharacter(LocalCharacter.Direction.Wait); });
        //
        // backBtn.onClick.AddListener(delegate { UndoPlanStep(); });
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
        myCharacter.ReadyForNextPhase = true;
        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
        LocalGameManager.Instance.CheckPingPhaseEnd();
    }

    private void UpdatePinBtnStatus(bool submitted) {
        pinFinishBtn.interactable = !submitted;
    }

    public void PlacePinByFocusedCharacter() {
        myCharacter.PlacePin();
        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
    }

    // public void AddMoveToFocusedCharacter(LocalCharacter.Direction move) {
    //     if (myCharacter.CheckMove(move)) {
    //         LocalGameManager.Instance.UpdateFocusPlayPlan(charaID, move);
    //     }
    // }
    //
    // public void UndoPlanStep() {
    //     //LocalGameManager.Instance.undoMove(charaID);
    //     myCharacter.UndoPlanStep();
    //     UpdatePlanUI(false, myCharacter.ActionPlan.Count == 0, false);
    // }

    public void SubmitPlan() {
        myCharacter.ReadyForNextPhase = true;
        LocalGameManager.Instance.CheckPlanPhaseEnd();
        UpdatePlanUI(true, false, true);
    }

    public void UpdatePlanUI(bool submitted, bool isEmpty, bool isFull)
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
