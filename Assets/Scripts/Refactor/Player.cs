using UnityEngine;
using UnityEngine.UI;
using GameConstant;

public class Player : MonoBehaviour
{
    public Character myCharacter
    {
        get { return IntegratedGameManager.S.localChar; }
    }

    public int charaID { get { return myCharacter.CharacterId; } }

    public int turnTimeLimit = 120;
    private float turnStartTime;

    public Button pinFinishBtn;

    public GameObject planParent;
    public Button upBtn;
    public Button downBtn;
    public Button leftBtn;
    public Button rightBtn;
    public Button waitBtn;
    public Button backBtn;
    public Button submitBtn;
    
    void Start()
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
        turnStartTime = Time.time;
    }

    private void Update() {
        switch (IntegratedGameManager.S.gameStatus) {
            case GameStatus.Player_Planning:
                if (Time.time - turnStartTime < turnTimeLimit) {
                    UIManager.S.UpdateTimer(turnTimeLimit - Mathf.RoundToInt(Time.time - turnStartTime));
                }
                else {
                    SubmitPlan();
                }
                break;
            case GameStatus.Player_Pinning:
                if (Time.time - turnStartTime < turnTimeLimit) {
                    UIManager.S.UpdateTimer(turnTimeLimit - Mathf.RoundToInt(Time.time - turnStartTime));
                }
                else {
                    SubmitPings();
                }

                break;
        }
    }

    public void UpdateCharacterUI() {
        if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Pinning) {
            planParent.SetActive(false);
            UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
        }
        else if (IntegratedGameManager.S.gameStatus == GameStatus.Player_Planning) {
            UpdatePlanUI(myCharacter.ReadyForNextPhase, 
                myCharacter.ActionPlan.Count == 0, 
                myCharacter.ActionPointsRemaining == 0);
        }
    }

    public void ResetTurnTimer() {
        turnStartTime = Time.time;
    }
    
    private void SubmitPings() {
        PinningSystem.S.Cancel();
        NetworkMiddleware.S.ReadyForNextPhaseLocal(myCharacter.CharacterId, true);
        UpdatePinBtnStatus(myCharacter.ReadyForNextPhase);
        IntegratedGameManager.S.CheckPingPhaseEnd();
    }

    public void UpdatePinBtnStatus(bool submitted) {
        pinFinishBtn.interactable = !submitted;
    }

    public void AddMoveToCharacter(Character character, Character.Direction direction)
    {
        if (character.ActionPointsRemaining > 0 && character.CheckMove(direction))
        {
            if (IntegratedGameManager.S.isNetworkGame)
            {
                NetworkMiddleware.S.AddMoveToCharacterLocal(direction, character.CharacterId);
            }
            else
            {
                character.AddActionToPlan(direction);
                IntegratedGameManager.S.player.UpdateCharacterUI();
                IntegratedGameManager.S.uiManager.UpdateActionPointsRemaining(IntegratedGameManager.S.localChar.ActionPointsRemaining, IntegratedGameManager.S.localChar.config.movement);
            }
        }
    }
    
    public void AddMoveToCharacter(Character.Direction direction)
    {
        AddMoveToCharacter(IntegratedGameManager.S.localChar, direction);
    }
    
    public void UndoPlanStep() 
    {
        NetworkMiddleware.S.UndoPlanStepLocal(myCharacter.CharacterId);
        UpdatePlanUI(false, myCharacter.ActionPlan.Count == 0, false);
        IntegratedGameManager.S.uiManager.UpdateActionPointsRemaining(IntegratedGameManager.S.localChar.ActionPointsRemaining, IntegratedGameManager.S.localChar.config.movement);
    }

    public void SubmitPlan() {
        NetworkMiddleware.S.ReadyForNextPhaseLocal(myCharacter.CharacterId, true);
        UpdatePlanUI(true, false, false);
    }

    public void UpdatePlanUI(bool submitted, bool isEmpty, bool isFull)
    {
        Debug.Log("Show Plan UI in player script");
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