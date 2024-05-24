using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;
using System;
using System.Reflection;
using System.Threading;
using System.ComponentModel;
using UnityEngine.SceneManagement;


public class UIManager : MonoBehaviour
{
    [Header("Scene References")]
    public GameAssets gameAssets;

    [Header("Common HUD Elements")]
    public TMP_Text CurrentPhaseLabel;
    public GameObject HealthPanel;
    public GameObject ActionPanel;
    public Image YouAreIcon;

    [Header("Timer Panel")]
    public GameObject TimerPanel;
    public TextMeshProUGUI TimerText;
    private float lastTimeReset;

    [Header("Team Status Panel")]
    public GameObject TeamInfoPanel;
    public GameObject[] GoalStatusIcons = new GameObject[3];
    public GameObject[] LifeStatusIcons = new GameObject[3];
    public GameObject[] ActionStatusIcons = new GameObject[3];

    [Header("Planning UI")]
    public GameObject PlanUIPanel;
    public Button PlanUpBtn;
    public Button PlanDownBtn;
    public Button PlanLeftBtn;
    public Button PlanRightBtn;
    public Button PlanWaitBtn;
    public Button PlanUndoBtn;
    public Button PlanSubmitBtn;

    [Header("Pinning UI")]
    public Button PinFinishBtn;

    [Header("Character Switching UI")]
    public GameObject CharacterSwitchPanel;
    public Button DwarfBtn;
    public Button GiantBtn;
    public Button HumanBtn;

    [Header("Combat UI")]
    public float animationDuration = 1.5f;
    public float spinSpeed = 360.0f;
    public GameObject CombatUI;
    public GameObject[] PlayerCombatSlots = new GameObject[3];
    public GameObject[] EnemyCombatSlots = new GameObject[4];
    public TMP_Text PlayerFinalScore;
    public TMP_Text EnemyFinalScore;
    public TMP_Text ResultMessage;
    public GameObject WinBG;
    public GameObject LoseBG;
    public GameObject VictoryScreen;
    public GameObject LossScreen;

    private Coroutine[] dotsCoroutine = new Coroutine[3];

    //Combat Skill Display
    [Header("Combat Tooltip")]
    public Vector3 combat_skill_display_offset = new Vector3(0, 2f, 0);
    public GameObject CombatSkillTooltip;
    public Image TooltipChallengeTypeIcon;
    public Image TooltipChallengeDieIcon;
    public TextMeshProUGUI TooltipChallengeBonusText;
    public Image[] TooltipSelfPortraitIcons = new Image[4];
    public Image[] TooltipPartner1PortraitIcons = new Image[2];
    public Image[] TooltipPartner2PortraitIcons = new Image[2];
    public Image[] TooltipSelfDiceIcons = new Image[4];
    public Image[] TooltipPartner1DiceIcons = new Image[2];
    public Image[] TooltipPartner2DiceIcons = new Image[2];
    bool combatSkillDisplayActive;

    [Header("Network Status UI")] 
    public GameObject networkStatusHandle;
    public TMP_Text networkStatusMsg;

    public static UIManager S;

    private void Awake()
    {
        if (S) Destroy(this);
        else S = this;
    }

    private void Start() {
        PinFinishBtn.onClick.AddListener(delegate { SubmitPins(); });

        PlanUpBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Up); });
        PlanDownBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Down); });
        PlanLeftBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Left); });
        PlanRightBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Right); });
        PlanWaitBtn.onClick.AddListener(delegate { AddMoveToCharacter(Character.Direction.Wait); });

        PlanUndoBtn.onClick.AddListener(delegate { UndoPlanStep(); });
        PlanSubmitBtn.onClick.AddListener(delegate { SubmitPlan(); });

        DwarfBtn.onClick.AddListener(delegate { SwitchCharacter(0); });
        GiantBtn.onClick.AddListener(delegate { SwitchCharacter(1); });
        HumanBtn.onClick.AddListener(delegate { SwitchCharacter(2); });

        lastTimeReset = Time.time;
    }

    private void Update() {
        TooltipHoverUpdate();
        //StartCoroutine(DiceAnimation(PlayerCombatSlots[0].transform.GetChild(2).gameObject));
    }


    public void InitGameUI() {
        CurrentPhaseLabel.text = "Level Starting";
        HideCombatUI();
        ShowCommonHUD();
        VictoryScreen.SetActive(false);
        LossScreen.SetActive(false);
    }
    
    public void DisplayVictoryScreen() {
        VictoryScreen.SetActive(true);
    }

    public void DisplayLossScreen()
    {
        LossScreen.SetActive(true);
    }

    public void UpdateGamePhaseInfo()
    {
        if (IntegratedGameManager.S.localChar.dead)
        {
            CurrentPhaseLabel.text = "Waiting Respawn";
            HideCharacterPlanUI();
            HideCharacterPinUI();
            return;
        }
        
        switch (IntegratedGameManager.S.gameStatus)
        {
            case GameStatus.Player_Pinning:
                CurrentPhaseLabel.text = "Player Pinning Phase";
                TimerPanel.SetActive(true);
                ShowCommonHUD();
                ShowCharacterPinUI();
                HideCharacterPlanUI();
                break;
            case GameStatus.Player_Planning:
                CurrentPhaseLabel.text = "Player Planning Phase";
                TimerPanel.SetActive(true);
                ShowCommonHUD();
                ShowCharacterPlanUI();
                HideCharacterPinUI();
                break;
            case GameStatus.Player_Moving:
                CurrentPhaseLabel.text = "Players Moving";
                TimerPanel.SetActive(false);
                HideCommonHUD();
                HideCharacterPlanUI();
                HideCharacterPinUI();
                break;
            case GameStatus.Monster_Moving:
                CurrentPhaseLabel.text = "Monsters Moving";
                TimerPanel.SetActive(false);
                HideCommonHUD();
                HideCharacterPlanUI();
                HideCharacterPinUI();
                break;
            case GameStatus.Animation_Pause:
                CurrentPhaseLabel.text = "An Event Happening";
                TimerPanel.SetActive(false);
                break;
            default:
                CurrentPhaseLabel.text = "Game Loading";
                TimerPanel.SetActive(false);
                break;
        }
    }
    
    public void LoadLevelEndUI() {
        CurrentPhaseLabel.text = "Level Conquered!";
    }

    #region Common HUD

    private void UpdateHealthPanel(int health) {
        foreach (Transform child in HealthPanel.transform.GetComponentsInChildren<Transform>()) {
            child.gameObject.SetActive(false);
        }

        for (int i = 0; i < health; i++) {
            HealthPanel.transform.GetChild(i).gameObject.SetActive(true);
        }

        for (int j = 0; j < GlobalConstant.START_HEALTH - health; j++) {
            HealthPanel.transform.GetChild(j + GlobalConstant.START_HEALTH).gameObject.SetActive(true);
        }

        HealthPanel.gameObject.SetActive(true);
    }

    private void UpdateActionPanel(int current_actionPointCount, int total_actionPointCount) {
        foreach (Transform child in ActionPanel.transform.GetComponentsInChildren<Transform>()) {
            child.gameObject.SetActive(false);
        }

        for (int i = 0; i < total_actionPointCount; i++) {
            ActionPanel.transform.GetChild(i).gameObject.SetActive(true);
            ActionPanel.transform.GetChild(i).gameObject.GetComponent<Image>().sprite = gameAssets.actionpoint_grey;
        }

        for (int i = 0; i < current_actionPointCount; i++) {
            ActionPanel.transform.GetChild(i).gameObject.SetActive(true);
            ActionPanel.transform.GetChild(i).gameObject.GetComponent<Image>().sprite = gameAssets.actionpoint;
        }

        ActionPanel.gameObject.SetActive(true);
    }

    /// <summary>
    /// The Common HUD consists of the:
    /// 1. Action Points
    /// 2. Health Points
    /// 4. "You Are" Icon
    /// </summary>
    public void ShowCommonHUD() {
        ActionPanel.SetActive(true);
        HealthPanel.SetActive(true);
        TeamInfoPanel.SetActive(true);
        UpdateCommonHUD();
    }

    public void UpdateCommonHUD() {
        Character currentCharacter = IntegratedGameManager.S.localChar;
        UpdateHealthPanel(currentCharacter.Health);
        UpdateActionPanel(currentCharacter.ActionPointsRemaining, currentCharacter.config.StartingActionPoints);

    }

    public void HideCommonHUD() {
        ActionPanel.SetActive(false);
        HealthPanel.SetActive(false);
        TeamInfoPanel.SetActive(false);
    }


    #endregion

    #region Pin UI

    public void ShowCharacterPinUI() {
        PinFinishBtn.gameObject.SetActive(true);
        UpdateCharacterPinUI();
    }

    public void UpdateCharacterPinUI() {
        Character currentCharacter = IntegratedGameManager.S.localChar;
        PinFinishBtn.interactable = !currentCharacter.ReadyForNextPhase;
    }

    public void HideCharacterPinUI() {
        PinFinishBtn.gameObject.SetActive(false);
    }

    private void SubmitPins() {
        //TODO we may want to send different names for these log functions
        PinningSystem.S.ClosePinWheel();
        NetworkMiddleware.S.CallReadyForNextPhase(IntegratedGameManager.S.localChar.CharacterId, true);
        
        UpdateCharacterPinUI();
    }

    #endregion

    #region Plan UI

    public void ShowCharacterPlanUI() {
        PlanUIPanel.SetActive(true);
        UpdateCharacterPlanUI();
    }

    /// <summary>
    /// Updates accessiblity of the Plan UI buttons, always in reference to the current character.
    /// </summary>
    public void UpdateCharacterPlanUI() {
        Character currentCharacter = IntegratedGameManager.S.localChar;

        // we've submitted so lock everything
        if (currentCharacter.ReadyForNextPhase) {
            PlanUpBtn.interactable = false;
            PlanDownBtn.interactable = false;
            PlanLeftBtn.interactable = false;
            PlanRightBtn.interactable = false;
            PlanWaitBtn.interactable = false;
            PlanUndoBtn.interactable = false;
            PlanSubmitBtn.interactable = false;
        }
        // we have no actions remaining so lock the directions
        else if(currentCharacter.ActionPointsRemaining == 0) {
            PlanUpBtn.interactable = false;
            PlanDownBtn.interactable = false;
            PlanLeftBtn.interactable = false;
            PlanRightBtn.interactable = false;
            PlanWaitBtn.interactable = false;
            PlanUndoBtn.interactable = true;
            PlanSubmitBtn.interactable = true;
        }
        // we haven't taken any actions so lock undo
        else if(currentCharacter.ActionPlan.Count == 0) {
            PlanUpBtn.interactable = currentCharacter.CheckMove(Character.Direction.Up);//    true;
            PlanDownBtn.interactable = currentCharacter.CheckMove(Character.Direction.Down);// true;
            PlanLeftBtn.interactable = currentCharacter.CheckMove(Character.Direction.Left); //true;
            PlanRightBtn.interactable = currentCharacter.CheckMove(Character.Direction.Right);// true;
            PlanWaitBtn.interactable = true;
            PlanUndoBtn.interactable = false;
            PlanSubmitBtn.interactable = true;
        }
        // fall through we unlock everything
        else {
            PlanUpBtn.interactable = currentCharacter.CheckMove(Character.Direction.Up);//    true;
            PlanDownBtn.interactable = currentCharacter.CheckMove(Character.Direction.Down);// true;
            PlanLeftBtn.interactable = currentCharacter.CheckMove(Character.Direction.Left); //true;
            PlanRightBtn.interactable = currentCharacter.CheckMove(Character.Direction.Right);// true;
            PlanWaitBtn.interactable = true;
            PlanUndoBtn.interactable = true;
            PlanSubmitBtn.interactable = true;
        }
    }

    public void HideCharacterPlanUI() {
        PlanUIPanel.SetActive(false);
    }
    public void AddMoveToCharacter(Character.Direction direction) {
        if (IntegratedGameManager.S.localChar.ActionPointsRemaining > 0 && IntegratedGameManager.S.localChar.CheckMove(direction)) {
            NetworkMiddleware.S.CallAddMoveToCharacter(IntegratedGameManager.S.localChar.CharacterId, direction);
        }
    }

    public void UndoPlanStep() {
        if (IntegratedGameManager.S.localChar.ActionPlan.Count > 0) {
            NetworkMiddleware.S.CallUndoPlanStep(IntegratedGameManager.S.localChar.CharacterId);
        }
    }

    public void SubmitPlan() {
        NetworkMiddleware.S.CallReadyForNextPhase(IntegratedGameManager.S.localChar.CharacterId, true);
    }

    #endregion

    #region Combat UI

    public void ShowCombatUI(Combat.FightType type, List<int> charaIDs, List<int> charaDice, List<int> enemyDice,
                         int playerScore, int enemyScore, bool win, bool visible = true)
    {
        StartCoroutine(CombatUICoroutine(type, charaIDs, charaDice, enemyDice, playerScore, enemyScore, win, visible));
    }

    public IEnumerator CombatUICoroutine(Combat.FightType type, List<int> charaIDs, List<int> charaDice, List<int> enemyDice,
                             int playerScore, int enemyScore, bool win, bool visible=true)
    {
        GameObject scoreBoard = CombatUI.transform.Find("Scoreboard").gameObject;
        GameObject vsText = CombatUI.transform.Find("VSText").gameObject;
        GameObject inProgressText = CombatUI.transform.Find("InProgressText").gameObject;
        GameObject UnknownOpponentSlot = CombatUI.transform.Find("UnknownOpponent").gameObject;
        CombatUI.SetActive(true);


        PlayerFinalScore.text = "";
        EnemyFinalScore.text = "";
        ResultMessage.text = "";
        WinBG.SetActive(false);
        LoseBG.SetActive(false);


        if (visible == true)
        {
            scoreBoard.GetComponentInChildren<Image>().sprite = gameAssets.Scoreboard_visible;
            vsText.SetActive(true);
            inProgressText.SetActive(false);
            UnknownOpponentSlot.SetActive(false);
        }
        //if the combat happens outside of vision range
        else
        {
            scoreBoard.GetComponentInChildren<Image>().sprite = gameAssets.Scoreboard_hidden;
            vsText.SetActive(false);
            inProgressText.SetActive(true);
            UnknownOpponentSlot.SetActive(true);
        }

        for (int i = 0; i < charaIDs.Count; i++)
        {
            switch (charaIDs[i])
            {
                case 0:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.dwarfIcon;
                    if(visible == true)
                    {
                        StartCoroutine(DiceAnimation(PlayerCombatSlots[i].transform.GetChild(2).gameObject));
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    }
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 1:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.giantIcon;   
                    if (visible == true)
                    {
                        StartCoroutine(DiceAnimation(PlayerCombatSlots[i].transform.GetChild(2).gameObject));
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    }
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 2:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.humanIcon;
                    if (visible == true)
                    {
                        StartCoroutine(DiceAnimation(PlayerCombatSlots[i].transform.GetChild(2).gameObject));
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    }
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                default:
                    Debug.Log("Character ID out of scope in ShowCombatUI");
                    break;
            }
        }

        if (visible == false) 
        {
            
        }
        else if (type == Combat.FightType.Monster)
        {
            // TODO: differenciate monster types
            for (int i = 0; i < enemyDice.Count; i++)
            {
                StartCoroutine(DiceAnimation(EnemyCombatSlots[i].transform.GetChild(2).gameObject));
                EnemyCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.monsterIcon;
                EnemyCombatSlots[i].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
                EnemyCombatSlots[i].SetActive(true);
            }
        }
        else if (type == Combat.FightType.Trap)
        {
            StartCoroutine(DiceAnimation(EnemyCombatSlots[0].transform.GetChild(2).gameObject));
            EnemyCombatSlots[0].GetComponentInChildren<Image>().sprite = gameAssets.trapIcon;
            EnemyCombatSlots[0].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
            EnemyCombatSlots[0].SetActive(true);
        }
        else if (type == Combat.FightType.Rock)
        {                
            StartCoroutine(DiceAnimation(EnemyCombatSlots[0].transform.GetChild(2).gameObject));
            EnemyCombatSlots[0].GetComponentInChildren<Image>().sprite = gameAssets.rockIcon;
            EnemyCombatSlots[0].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
            EnemyCombatSlots[0].SetActive(true);
        }

        yield return new WaitForSeconds(animationDuration);

        if (visible == true) 
        {
            PlayerFinalScore.text = playerScore.ToString();
            EnemyFinalScore.text = enemyScore.ToString();
        }
        else
        {
            PlayerFinalScore.text = "";
            EnemyFinalScore.text = "";
        }

        //expand and shrink the score text 
        float shrink_rate = 0.04f;
        for(int i=0; i<animationDuration*50; i++)
        {
            PlayerFinalScore.gameObject.transform.localScale += new Vector3(shrink_rate, shrink_rate, shrink_rate);
            EnemyFinalScore.gameObject.transform.localScale += new Vector3(shrink_rate, shrink_rate, shrink_rate);
            yield return new WaitForSeconds(0.01f);
        }
        for (int i = 0; i < animationDuration*50; i++)
        {
            PlayerFinalScore.gameObject.transform.localScale -= new Vector3(shrink_rate, shrink_rate, shrink_rate);
            EnemyFinalScore.gameObject.transform.localScale -= new Vector3(shrink_rate, shrink_rate, shrink_rate);
            yield return new WaitForSeconds(0.01f);
        }


        

        if (win)
        {
            WinBG.SetActive(true);
            LoseBG.SetActive(false);
            ResultMessage.text = "The opponent is defeated!";
        }
        else
        {
            WinBG.SetActive(false);
            LoseBG.SetActive(true);
            ResultMessage.text = "Failed to defeat the opponent!";
        }
        yield return new WaitForSeconds(animationDuration);

        yield return null;
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

    private IEnumerator DiceAnimation(GameObject dice) 
    {
        dice.SetActive(true);
        float elapsedTime = 0.0f;
        while (elapsedTime < animationDuration)
        {
            float deltaRotation = spinSpeed * Time.deltaTime;
            dice.transform.Rotate(Vector3.up, deltaRotation); 
            dice.transform.Rotate(Vector3.right, deltaRotation); 
            dice.transform.Rotate(Vector3.forward, deltaRotation); 

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        dice.SetActive(false);
    }




    #endregion

    #region Team Status UI

    public void UpdateCharacterGoalStatus(int charID, bool reached = true)
    {
        if (reached)
            GoalStatusIcons[charID].GetComponent<Image>().sprite = gameAssets.GetGoalFilled(charID);
        else 
            GoalStatusIcons[charID].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(charID);
    }
    
    public void ResetTeamGoalStatus()
    {
        //GoalPanel.SetActive(false);
        GoalStatusIcons[0].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(0);
        GoalStatusIcons[1].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(1);
        GoalStatusIcons[2].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(2);
    }

    public void UpdateCharacterLifeStatus(int charID, bool alive = true)
    {
        if (alive)
            LifeStatusIcons[charID].GetComponent<Image>().sprite = gameAssets.aliveIcon;
        else
            LifeStatusIcons[charID].GetComponent<Image>().sprite = gameAssets.deadIcon;

    }

    public void ResetTeamActionStatus()
    {
        UpdateCharacterActionStatus(0, false);
        UpdateCharacterActionStatus(1, false);
        UpdateCharacterActionStatus(2, false);
    }

    public void UpdateCharacterActionStatus(int charID, bool ready = true) {
        if (ready)
        //action status ui set to ready
        {
            ActionStatusIcons[charID].transform.Find("Complete").gameObject.SetActive(true);
            if (dotsCoroutine[charID] != null) 
            {
                StopCoroutine(dotsCoroutine[charID]);
                dotsCoroutine[charID] = null;
            }
            ActionStatusIcons[charID].transform.Find("Planning").gameObject.SetActive(false);
        }
        //action status ui set to plannning
        else
        {
            ActionStatusIcons[charID].transform.Find("Complete").gameObject.SetActive(false);
            GameObject planning = ActionStatusIcons[charID].transform.Find("Planning").gameObject;
            planning.SetActive(true);
            if(dotsCoroutine[charID] == null)
            {
                TextMeshProUGUI dotsText = planning.GetComponent<TextMeshProUGUI>();
                dotsCoroutine[charID] = StartCoroutine(DotsAnimation(dotsText));
            }

        }

    }

    public IEnumerator DotsAnimation(TextMeshProUGUI dotsText) {
        string[] dotsStates = new string[] { ".", "..", "..." };
        int currentState = 0;
        while (true) {
            dotsText.text = dotsStates[currentState]; // Update the CurrentPhaseLabel to the current state
            currentState = (currentState + 1) % dotsStates.Length;
            yield return new WaitForSeconds(0.5f);
        }
    }

    #endregion

    #region Timer

    public float TimeRemaining {
        get {
            switch (IntegratedGameManager.S.gameStatus) {
                case GameStatus.Player_Pinning:
                case GameStatus.Player_Planning:
                    return GameData.S.TurntimeLimit - Mathf.RoundToInt(Time.time - lastTimeReset);
                default:
                    return float.PositiveInfinity;
            }
        }
    }

    public void UpdateTurnTimer() {
        if (float.IsInfinity(IntegratedGameManager.S.TimeRemaining)) {
            TimerText.text = "\u221E";
        }
        else {
            TimeSpan timeSpan = TimeSpan.FromSeconds(IntegratedGameManager.S.TimeRemaining);
            TimerText.text = string.Format("{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
            if (TimeRemaining < 10) {
                TimerText.color = Color.red;
            }
            else {
                TimerText.color = Color.white;
            }
        }
    }

    #endregion

    #region CharacterSwitcher

    public void ShowCharacterSwitcher() {
        CharacterSwitchPanel.SetActive(true);
    }

    public void SwitchCharacterTo(int charID) {
        SwitchCharacter(charID);
    }

    private void SwitchCharacter(int charID) {
        IntegratedGameManager.S.SwitchCharacter(charID);

        switch (charID) {
            case 0:
                DwarfBtn.interactable = false;
                GiantBtn.interactable = true;
                HumanBtn.interactable = true;
                break;
            case 1:
                DwarfBtn.interactable = true;
                GiantBtn.interactable = false;
                HumanBtn.interactable = true;
                break;
            case 2:
                DwarfBtn.interactable = true;
                GiantBtn.interactable = true;
                HumanBtn.interactable = false;
                break;
        }
    }

    public void HideCharacterSwitcher() {
        CharacterSwitchPanel.SetActive(false);
    }

    #endregion

    #region Combat Skill Tooltip

    private GameObject tooltipTarget = null;

    private void TooltipHoverUpdate() {
        //display combat skills when mouse hover on objects
        Ray ray;
        RaycastHit[] hits;
        Camera mainCamera = Camera.main;
        ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        //Debug.Log(mainCamera.orthographicSize);
        hits = Physics.RaycastAll(ray, 1000f);
        combatSkillDisplayActive = false;
        foreach (RaycastHit hit in hits) {
            GameObject hitObject = hit.collider.gameObject;

            Tile tile;
            Combat.FightType challengeType;
            switch (hitObject.tag) {
                case "Monster":
                    tile = hitObject.GetComponent<Monster>().currentTile;
                    challengeType = Combat.FightType.Monster;
                    break;
                case "Rock":
                    tile = hitObject.GetComponent<Tile>();
                    challengeType = Combat.FightType.Rock;
                    break;
                case "Trap":
                    tile = hitObject.GetComponent<Tile>();
                    challengeType = Combat.FightType.Trap;
                    break;
                default:
                    continue;
            }
            if (!combatSkillDisplayActive && tile != null && tile.fogOfWarDictionary[IntegratedGameManager.S.localChar.CharacterId] == Tile.FogOfWarState.Visible) {
                if (hitObject != tooltipTarget) {
                    tooltipTarget = hitObject;
                    /*
                     * UpdateChallengetooltipIcons send a log message so we want to make sure we don't send a bunch of messages in a row.
                     */ 
                    UpdateChallengeTooltipIcons(hitObject, challengeType, tile);
                }
                Vector3 corrected_offset = combat_skill_display_offset * mainCamera.orthographicSize / 3; //correct display offset according to current zoom in value of the camera
                Vector3 displayPosition = mainCamera.WorldToScreenPoint(hitObject.transform.position + corrected_offset);
                CombatSkillTooltip.transform.position = displayPosition;
                CombatSkillTooltip.SetActive(true);
                combatSkillDisplayActive = true;
            }
        }
        if (!combatSkillDisplayActive) {
            CombatSkillTooltip.SetActive(false);
        }
    }



    public void UpdateChallengeTooltipIcons(GameObject opponent, Combat.FightType challengeType, Tile tile) {
        Character currentCharacter = IntegratedGameManager.S.localChar;
        Character partner1 = null;
        Character partner2 = null;


        Sprite self_icon_sprite, partner1_icon_sprite, partner2_icon_sprite, challengeSprite;
        Combat.Dice self_die, partner1_die, partner2_die, challengeDie;
        string challengeBonus;
        
        switch (currentCharacter.config.type){
            case CharacterConfig.CharacterType.Dwarf:
                partner1 = IntegratedGameManager.S.inSceneCharacters[1];    // Giant
                partner2 = IntegratedGameManager.S.inSceneCharacters[2];    // Human
                break;
            case CharacterConfig.CharacterType.Human:
                partner1 = IntegratedGameManager.S.inSceneCharacters[0];    // Dwarf
                partner2 = IntegratedGameManager.S.inSceneCharacters[1];    // Giant
                break;
            case CharacterConfig.CharacterType.Giant:
                partner1 = IntegratedGameManager.S.inSceneCharacters[0];    // Dwarf
                partner2 = IntegratedGameManager.S.inSceneCharacters[2];    // human
                break;
        }

        self_icon_sprite = gameAssets.GetCharacterIcon(currentCharacter);
        partner1_icon_sprite = gameAssets.GetCharacterIcon(partner1);
        partner2_icon_sprite = gameAssets.GetCharacterIcon(partner2);

        //Display character icons
        foreach (Image self_icon in TooltipSelfPortraitIcons)
            self_icon.sprite = self_icon_sprite;
        foreach (Image partner1_icon in TooltipPartner1PortraitIcons)
            partner1_icon.sprite = partner1_icon_sprite;
        foreach (Image partner2_icon in TooltipPartner2PortraitIcons)
            partner2_icon.sprite = partner2_icon_sprite;

        //Display Character dice, opponent dice, and opponent icon
        switch (challengeType) {
            case Combat.FightType.Monster:
                self_die = currentCharacter.config.monsterDice;
                partner1_die = partner1.config.monsterDice;
                partner2_die = partner2.config.monsterDice;

                Monster monster = opponent.GetComponent<Monster>();
                challengeSprite = monster.icon;
                challengeDie = monster.config.combatDice;
                challengeBonus = monster.config.combatDice.bonus.ToString();
                break;

            case Combat.FightType.Rock:
                self_die = currentCharacter.config.stoneDice;
                partner1_die = partner1.config.stoneDice;
                partner2_die = partner2.config.stoneDice;

                challengeSprite = gameAssets.rockIcon;
                challengeDie = tile.dice;
                challengeBonus = tile.dice.bonus.ToString();
                break;
            
            case Combat.FightType.Trap:
                self_die = currentCharacter.config.trapDice;
                partner1_die = partner1.config.trapDice;
                partner2_die = partner2.config.trapDice;

                challengeSprite = gameAssets.trapIcon;
                challengeDie = tile.dice;
                challengeBonus = tile.dice.bonus.ToString();
                break;

            default:
                Debug.LogErrorFormat("Unknown Combat Type {0} in Tooltip", challengeType);
                return;
        }

        foreach (Image self_dice in TooltipSelfDiceIcons)
            self_dice.sprite = gameAssets.GetDiceSprite(self_die);
        foreach (Image partner1_dice in TooltipPartner1DiceIcons)
            partner1_dice.sprite = gameAssets.GetDiceSprite(partner1_die);
        foreach (Image partner1_dice in TooltipPartner2DiceIcons)
            partner1_dice.sprite = gameAssets.GetDiceSprite(partner2_die);
        //opponent dice
        TooltipChallengeDieIcon.sprite = gameAssets.GetDiceSprite(challengeDie);
        TooltipChallengeBonusText.text = challengeBonus;
        TooltipChallengeTypeIcon.sprite = challengeSprite;
        CompetitionMiddleware.Instance.LogInspectChallenge(currentCharacter.CharacterId,
            tile.col, tile.row, challengeType.ToString(),
            self_die, partner1_die, partner2_die, challengeDie);
    }

    #endregion

    #region Network StatusnUI

    public void ShowOtherPlayerDisconnectUI(string playerName)
    {
        networkStatusMsg.text = $"Player {playerName} disconnected from the game." +
                                $"This game will not be recorded";
        networkStatusHandle.SetActive(true);
    }

    public void ReturnToLobby()
    {
        SceneManager.LoadScene(GlobalConstant.LOBBY_SCENE);
    }

    #endregion





}
