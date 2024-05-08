using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameConstant;
using System;


public class UIManager : MonoBehaviour
{
    public GameAssets gameAssets;

    public TMP_Text text;

    public float TutorialTime = 1f;

    public GameObject PlanUI;
    public GameObject PinFinishBtn;

    public GameObject CharacterInfo;
    public Image YouAreInfo;
    //public Image YouAreInfo;
    public GameObject[] DiceStats = new GameObject[3];
    public GameObject[] BonusStats = new GameObject[3];
    public GameObject HealthPanel;
    public GameObject ActionPanel;

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
    public GameObject Timer;
    public TextMeshProUGUI TimerText;

    //public GameObject GoalPanel; 
    public GameObject[] GoalStatus = new GameObject[3];
    public GameObject[] LifeStatus = new GameObject[3];
    public GameObject[] ActionStatus = new GameObject[3];

    private Coroutine[] dotsCoroutine = new Coroutine[3];

    //Combat Skill Display
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


    public void InitGameUI()
    {
        text.text = "Level Starting";
        CombatUI.SetActive(false);
        
        //GoalPanel.SetActive(true);
        CharacterInfo.SetActive(true);
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
            text.text = "Waiting Respawn";
            return;
        }
        
        switch (IntegratedGameManager.S.gameStatus)
        {
            case GameStatus.Player_Pinning:
                text.text = "Player Pinning Phase";
                Timer.SetActive(true);
                break;
            case GameStatus.Player_Planning:
                text.text = "Player Planning Phase";
                PinFinishBtn.SetActive(false);
                Timer.SetActive(true);
                break;
            case GameStatus.Player_Moving:
                text.text = "Players Moving";
                Timer.SetActive(false);
                break;
            case GameStatus.Monster_Moving:
                text.text = "Monsters Moving";
                Timer.SetActive(false);
                break;
            case GameStatus.Animation_Pause:
                text.text = "An Event Happening";
                Timer.SetActive(false);
                break;
            default:
                text.text = "Game Loading";
                Timer.SetActive(false);
                break;
        }
    }
    
    public void LoadLevelEndUI() {
        text.text = "Level Conquered!";
    }
    
    private void UpdateHealthPanel(int health)
    {
        foreach (Transform child in HealthPanel.transform.GetComponentsInChildren<Transform>())
        {
            child.gameObject.SetActive(false);
        }

        for (int i = 0; i < health; i++)
        {
            HealthPanel.transform.GetChild(i).gameObject.SetActive(true);
        }

        for (int j = 0; j < GlobalConstant.START_HEALTH - health; j++)
        {
            HealthPanel.transform.GetChild(j + GlobalConstant.START_HEALTH).gameObject.SetActive(true);
        }

        HealthPanel.gameObject.SetActive(true);
    }
    
    private void UpdateActionPanel(int current_actionPointCount, int total_actionPointCount)
    {
        foreach (Transform child in ActionPanel.transform.GetComponentsInChildren<Transform>())
        {
            child.gameObject.SetActive(false);
        }

        for (int i = 0; i < total_actionPointCount; i++)
        {
            ActionPanel.transform.GetChild(i).gameObject.SetActive(true);
            ActionPanel.transform.GetChild(i).gameObject.GetComponent<Image>().sprite = gameAssets.actionpoint_grey;
        }

        for (int i = 0; i < current_actionPointCount; i++)
        {
            ActionPanel.transform.GetChild(i).gameObject.SetActive(true);
            ActionPanel.transform.GetChild(i).gameObject.GetComponent<Image>().sprite = gameAssets.actionpoint;
        }

        ActionPanel.gameObject.SetActive(true);
    }
    
    private void UpdateCharacterStats()
    {
        Character currentCharacter = IntegratedGameManager.S.localChar;
        
        DiceStats[0].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.monsterDice.type);
        DiceStats[1].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.stoneDice.type);
        DiceStats[2].GetComponent<Image>().sprite = gameAssets.GetDiceIcon(currentCharacter.config.trapDice.type);

        BonusStats[0].GetComponent<TMP_Text>().text = currentCharacter.config.monsterDice.bonus.ToString();
        BonusStats[1].GetComponent<TMP_Text>().text = currentCharacter.config.stoneDice.bonus.ToString();
        BonusStats[2].GetComponent<TMP_Text>().text = currentCharacter.config.trapDice.bonus.ToString();
    }
    
    public void ShowCharacterPinUI()
    {
        Character currentCharacter = IntegratedGameManager.S.localChar;

        PinFinishBtn.SetActive(true);

        // TODO check the case when health == 0
        UpdateHealthPanel(currentCharacter.Health);
        Debug.Log($"Health: {currentCharacter.Health}");
        UpdateActionPanel(currentCharacter.ActionPointsRemaining, currentCharacter.config.movement);
        UpdateCharacterStats();
        CharacterInfo.SetActive(true);
    }
    
    public void ShowCharacterPlanUI()
    {
        Character currentCharacter = IntegratedGameManager.S.localChar;
        //Debug.Log("Enable Plan UI in UI manager");
        PlanUI.SetActive(true);

        // TODO check the case when health == 0

        UpdateHealthPanel(currentCharacter.Health);
        Debug.Log($"Health: {currentCharacter.Health}");
        UpdateActionPanel(currentCharacter.ActionPointsRemaining, currentCharacter.config.movement);

        UpdateActionPanel(currentCharacter.ActionPointsRemaining, currentCharacter.config.movement);
        
        UpdateCharacterStats();
        CharacterInfo.SetActive(true);
    }
    
    public void UpdateActionPointsRemaining(int current_movePoints, int total_movepoints)
    {
        UpdateActionPanel(current_movePoints, total_movepoints);
    }
    
    public void HideCharacterPinUI()
    {
        PinFinishBtn.SetActive(false);
        CharacterInfo.SetActive(false);
        HealthPanel.SetActive(false);
        ActionPanel.SetActive(false);
    }


    public void HideCharacterPlanUI()
    {
        //Debug.Log("Hide Planning UI in manager");
        PlanUI.SetActive(false);
        CharacterInfo.SetActive(false);
        HealthPanel.SetActive(false);
        ActionPanel.SetActive(false);
    }
    
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
    
    public void UpdateGoalStatus(int CharaID, bool reached = true)
    {
        if (reached)
            GoalStatus[CharaID].GetComponent<Image>().sprite = gameAssets.GetGoalFilled(CharaID);
        else 
            GoalStatus[CharaID].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(CharaID);
    }
    
    public void ResetGoalStatus()
    {
        //GoalPanel.SetActive(false);
        GoalStatus[0].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(0);
        GoalStatus[1].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(1);
        GoalStatus[2].GetComponent<Image>().sprite = gameAssets.GetGoalUnfilled(2);
    }

    public void UpdateCharacterLifeStatus(int CharaID, bool alive = true)
    {
        if (alive)
            LifeStatus[CharaID].GetComponent<Image>().sprite = gameAssets.aliveIcon;
        else
            LifeStatus[CharaID].GetComponent<Image>().sprite = gameAssets.deadIcon;

    }

    public void ResetActionStatus()
    {
        UpdateCharacterActionStatus(0, false);
        UpdateCharacterActionStatus(1, false);
        UpdateCharacterActionStatus(2, false);
    }

    public void UpdateCharacterActionStatus(int CharaID, bool ready = true) {
        if (ready)
        //action status ui set to ready
        {
            ActionStatus[CharaID].transform.Find("Complete").gameObject.SetActive(true);
            if (dotsCoroutine[CharaID] != null) 
            {
                StopCoroutine(dotsCoroutine[CharaID]);
                dotsCoroutine[CharaID] = null;
            }
            ActionStatus[CharaID].transform.Find("Planning").gameObject.SetActive(false);
        }
        //action status ui set to plannning
        else
        {
            ActionStatus[CharaID].transform.Find("Complete").gameObject.SetActive(false);
            GameObject planning = ActionStatus[CharaID].transform.Find("Planning").gameObject;
            planning.SetActive(true);
            if(dotsCoroutine[CharaID] == null)
            {
                TextMeshProUGUI dotsText = planning.GetComponent<TextMeshProUGUI>();
                dotsCoroutine[CharaID] = StartCoroutine(DotsAnimation(dotsText));
            }

        }

    }

    
    public void ShowDefeatedScreen()
    {
        LossScreen.SetActive(true);
    }

    public IEnumerator DotsAnimation(TextMeshProUGUI dotsText)
    {
        string[] dotsStates = new string[] { ".", "..", "..." };
        int currentState = 0;
        while (true) 
        {
            dotsText.text = dotsStates[currentState]; // Update the text to the current state
            currentState = (currentState + 1) % dotsStates.Length; 
            yield return new WaitForSeconds(0.5f); 
        }
    }

    public void UpdateTimer(int secondsLeft)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(secondsLeft);
        string text_to_display = string.Format("{0}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);

        TimerText.text = text_to_display;

        //TextMeshProUGUI timer_text = Timer.transform.Find("Timer_text").gameObject.GetComponent<TextMeshProUGUI>();
        //timer_text.text = text_to_display;

    }


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

    private void Update()
    {
        //display combat skills when mouse hover on objects
        Ray ray;
        RaycastHit[] hits;
        Camera mainCamera = Camera.main;
        ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        //Debug.Log(mainCamera.orthographicSize);
        hits = Physics.RaycastAll(ray, 1000f);
        combatSkillDisplayActive = false;
        foreach (RaycastHit hit in hits)
        {
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
