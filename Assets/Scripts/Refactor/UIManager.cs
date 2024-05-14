using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;
using System;
using System.Reflection;


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

    [Header("Chracter Stats UI (Deprecated)")]
    public GameObject[] DiceStats = new GameObject[3];
    public GameObject[] BonusStats = new GameObject[3];

    [Header("Combat UI")]
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
    public GameObject CombatSkillDisplay;
    public GameObject opponent_icon;
    public GameObject opponent_dice;
    public GameObject opponent_bonus;
    public GameObject[] self_icons = new GameObject[4];
    public GameObject[] partner1_icons = new GameObject[2];
    public GameObject[] partner2_icons = new GameObject[2];
    public GameObject[] self_dices = new GameObject[4];
    public GameObject[] partner1_dices = new GameObject[2];
    public GameObject[] partner2_dices = new GameObject[2];
    bool combatSkillDisplayActive;

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


    public void InitGameUI()
    {
        CurrentPhaseLabel.text = "Level Starting";
        CombatUI.SetActive(false);
        
        //GoalPanel.SetActive(true);
        TeamInfoPanel.SetActive(true);
        VictoryScreen.SetActive(false);
        LossScreen.SetActive(false);

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
        UpdateActionPanel(currentCharacter.ActionPointsRemaining, currentCharacter.config.movement);

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
            PlanUndoBtn.interactable = false;
            PlanSubmitBtn.interactable = false;
        }
        // we have no actions remaining so lock the directions
        else if(currentCharacter.ActionPointsRemaining == 0) {
            PlanUpBtn.interactable = false;
            PlanDownBtn.interactable = false;
            PlanLeftBtn.interactable = false;
            PlanRightBtn.interactable = false;
            PlanUndoBtn.interactable = true;
            PlanSubmitBtn.interactable = true;
        }
        // we haven't taken any actions so lock undo
        else if(currentCharacter.ActionPlan.Count == 0) {
            PlanUpBtn.interactable = true;
            PlanDownBtn.interactable = true;
            PlanLeftBtn.interactable = true;
            PlanRightBtn.interactable = true;
            PlanUndoBtn.interactable = false;
            PlanSubmitBtn.interactable = true;
        }
        // fall through we unlock everything
        else {
            PlanUpBtn.interactable = true;
            PlanDownBtn.interactable = true;
            PlanLeftBtn.interactable = true;
            PlanRightBtn.interactable = true;
            PlanUndoBtn.interactable = true;
            PlanSubmitBtn.interactable = true;
        }
    }

    public void HideCharacterPlanUI() {
        PlanUIPanel.SetActive(false);
    }
    public void AddMoveToCharacter(Character.Direction direction) {
        NetworkMiddleware.S.CallAddMoveToCharacter(IntegratedGameManager.S.localChar.CharacterId, direction);
    }

    public void UndoPlanStep() {
        NetworkMiddleware.S.CallUndoPlanStep(IntegratedGameManager.S.localChar.CharacterId);
    }

    public void SubmitPlan() {
        NetworkMiddleware.S.CallReadyForNextPhase(IntegratedGameManager.S.localChar.CharacterId, true);
    }

    #endregion

    #region Combat UI

    public void ShowCombatUI(Combat.FightType type, List<int> charaIDs, List<int> charaDice, List<int> enemyDice,
                             int playerScore, int enemyScore, bool win, bool visible=true)
    {
        GameObject scoreBoard = CombatUI.transform.Find("Scoreboard").gameObject;
        GameObject vsText = CombatUI.transform.Find("VSText").gameObject;
        GameObject inProgressText = CombatUI.transform.Find("InProgressText").gameObject;
        GameObject UnknownOpponentSlot = CombatUI.transform.Find("UnknownOpponent").gameObject;

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
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 1:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.giantIcon;
                    if (visible == true)
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
                    else
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = "";
                    PlayerCombatSlots[i].SetActive(true);
                    break;
                case 2:
                    PlayerCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.humanIcon;
                    if (visible == true)
                        PlayerCombatSlots[i].GetComponentInChildren<TMP_Text>().text = charaDice[i].ToString();
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
                EnemyCombatSlots[i].GetComponentInChildren<Image>().sprite = gameAssets.monsterIcon;
                EnemyCombatSlots[i].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
                EnemyCombatSlots[i].SetActive(true);
            }
        }
        else if (type == Combat.FightType.Trap)
        {
            EnemyCombatSlots[0].GetComponentInChildren<Image>().sprite = gameAssets.trapIcon;
            EnemyCombatSlots[0].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
            EnemyCombatSlots[0].SetActive(true);
        }
        else if (type == Combat.FightType.Rock)
        {
            EnemyCombatSlots[0].GetComponentInChildren<Image>().sprite = gameAssets.rockIcon;
            EnemyCombatSlots[0].GetComponentInChildren<TMP_Text>().text = enemyDice[0].ToString();
            EnemyCombatSlots[0].SetActive(true);
        }

        if(visible == true) 
        {
            PlayerFinalScore.text = playerScore.ToString();
            EnemyFinalScore.text = enemyScore.ToString();
        }
        else
        {
            PlayerFinalScore.text = "";
            EnemyFinalScore.text = "";
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

        CombatUI.SetActive(true);
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

    public void ResetTurnTimer() {
        lastTimeReset = Time.time;
    }

    private void UpdateTimer(int secondsLeft) {
        TimeSpan timeSpan = TimeSpan.FromSeconds(secondsLeft);
        TimerText.text = string.Format("{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
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

    public void DisplayCombatSkills(GameObject opponent, String opponent_type) {
        Character currentCharacter = IntegratedGameManager.S.localChar;
        Character partner1 = null;
        Character partner2 = null;

        
        Camera mainCamera = Camera.main;
        Vector3 corrected_offset = combat_skill_display_offset * mainCamera.orthographicSize / 3; //correct display offset according to current zoom in value of the camera
        Vector3 displayPosition = mainCamera.WorldToScreenPoint(opponent.transform.position + corrected_offset);
        CombatSkillDisplay.transform.position = displayPosition;
        Sprite self_icon_sprite = null;
        Sprite partner1_icon_sprite = null;
        Sprite partner2_icon_sprite = null;
        TextMeshProUGUI opponent_bonus_text = opponent_bonus.GetComponent<TextMeshProUGUI>();
        CombatSkillDisplay.SetActive(true);

        switch (currentCharacter.config.type){
            case CharacterConfig.CharacterType.Dwarf:
                partner1 = GameObject.Find("n_Giant").GetComponent<Character>(); // Giant
                partner2 = GameObject.Find("n_Human").GetComponent<Character>(); // Human
                self_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Dwarf);
                partner1_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Giant);
                partner2_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Human);
                break;
            case CharacterConfig.CharacterType.Human:
                partner1 = GameObject.Find("n_Dwarf").GetComponent<Character>(); // Dwarf
                partner2 = GameObject.Find("n_Giant").GetComponent<Character>(); // Giant
                self_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Human);
                partner1_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Dwarf);
                partner2_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Giant);
                break;
            case CharacterConfig.CharacterType.Giant:
                partner1 = GameObject.Find("n_Dwarf").GetComponent<Character>(); // Dwarf
                partner2 = GameObject.Find("n_Human").GetComponent<Character>(); // human
                self_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Giant);
                partner1_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Dwarf);
                partner2_icon_sprite = gameAssets.GetCharacterIcon(CharacterConfig.CharacterType.Human);
                break;
        }
        //Display character icons
        foreach (GameObject self_icon in self_icons)
            self_icon.GetComponent<Image>().sprite = self_icon_sprite;
        foreach (GameObject partner1_icon in partner1_icons)
            partner1_icon.GetComponent<Image>().sprite = partner1_icon_sprite;
        foreach (GameObject partner2_icon in partner2_icons)
            partner2_icon.GetComponent<Image>().sprite = partner2_icon_sprite;

        //Display Character dice, opponent dice, and opponent icon
        switch (opponent_type) {
            case "Monster":
                //Character dice
                foreach (GameObject self_dice in self_dices)
                    self_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.monsterDice.type);
                foreach (GameObject partner1_dice in partner1_dices)
                    partner1_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(partner1.config.monsterDice.type);
                foreach (GameObject partner1_dice in partner2_dices)
                    partner1_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(partner2.config.monsterDice.type);
                //opponent dice
                opponent_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(opponent.GetComponent<Monster>().config.combatDice.type);
                opponent_bonus_text.text = opponent.GetComponent<Monster>().config.combatDice.bonus.ToString();
                //opponent icon
                opponent_icon.GetComponent<Image>().sprite = opponent.GetComponent<Monster>().icon;
                break;
            case "Rock":
                //Character dice
                foreach (GameObject self_dice in self_dices)
                    self_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.stoneDice.type);
                foreach (GameObject partner1_dice in partner1_dices)
                    partner1_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(partner1.config.stoneDice.type);
                foreach (GameObject partner1_dice in partner2_dices)
                    partner1_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(partner2.config.stoneDice.type);
                //opponent dice
                opponent_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(opponent.GetComponent<Tile>().dice.type);
                opponent_bonus_text.text = opponent.GetComponent<Tile>().dice.bonus.ToString();
                //opponent icon
                opponent_icon.GetComponent<Image>().sprite = gameAssets.rockIcon;
                break;
            case "Trap":
                //Character dice
                foreach (GameObject self_dice in self_dices)
                    self_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.trapDice.type);
                foreach (GameObject partner1_dice in partner1_dices)
                    partner1_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(partner1.config.trapDice.type);
                foreach (GameObject partner1_dice in partner2_dices)
                    partner1_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(partner2.config.trapDice.type);
                //opponent dice
                opponent_dice.GetComponent<Image>().sprite = gameAssets.GetDiceIcon(opponent.GetComponent<Tile>().dice.type);
                opponent_bonus_text.text = opponent.GetComponent<Tile>().dice.bonus.ToString();
                //opponent icon
                opponent_icon.GetComponent<Image>().sprite = gameAssets.trapIcon;
                break;
        }
    }

    #endregion

    private void Update() {

        switch (IntegratedGameManager.S.gameStatus) {
            case GameStatus.Player_Planning:
                if (Time.time - lastTimeReset < GameData.S.TurntimeLimit) {
                    UpdateTimer(GameData.S.TurntimeLimit - Mathf.RoundToInt(Time.time - lastTimeReset));
                }
                else {
                    SubmitPlan();
                }
                break;
            case GameStatus.Player_Pinning:
                if (Time.time - lastTimeReset < GameData.S.TurntimeLimit) {
                    UpdateTimer(GameData.S.TurntimeLimit - Mathf.RoundToInt(Time.time - lastTimeReset));
                }
                else {
                    SubmitPins();
                }

                break;
        }

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
            if (hitObject.tag == "Monster" || hitObject.tag == "Rock" || hitObject.tag == "Trap")
            {
                Tile.FogOfWarState visibility;
                if (hitObject.tag == "Monster")
                {
                    visibility = hitObject.GetComponent<Monster>().currentTile.fogOfWarDictionary[IntegratedGameManager.S.localChar.CharacterId];
                }
                else
                {
                    visibility = hitObject.GetComponent<Tile>().fogOfWarDictionary[IntegratedGameManager.S.localChar.CharacterId];
                }
                if (!combatSkillDisplayActive && visibility == Tile.FogOfWarState.Visible)
                {
                    combatSkillDisplayActive = true;
                    DisplayCombatSkills(hitObject, hitObject.tag);
                }
            }
        }
        if(!combatSkillDisplayActive) CombatSkillDisplay.SetActive(false);
    }



}
