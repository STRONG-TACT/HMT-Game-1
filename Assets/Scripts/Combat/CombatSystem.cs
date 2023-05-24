using System.Collections;
using UnityEngine;
using System.Linq;
using Photon.Pun;

public class CombatSystem : MonoBehaviour {
    public static CombatSystem Instance;

    public enum FightType {
        Rock=0,
        Trap=1,
        Monster=2
    }

    private GameAssets gameAssets;

    [HideInInspector] public Character mainCharacter;
    [HideInInspector] public GameObject enemy;
    [HideInInspector] public Character characterInFight;

    [HideInInspector] public bool isInFight;
    [HideInInspector] public bool fightEnd;
    [HideInInspector] public FightType fightType; // 0: Fight Rock, 1: fight Trap, 2: Fight Monster
    [HideInInspector] public int MonsterFightCount;
    private int MAXFIGHT = 3;

    [HideInInspector] public int playerDiceNum;
    [HideInInspector] public int[] monsterNum;

    PhotonView view;

    // UI
    //[HideInInspector] public GameObject combatUIPanel;
    //TextMeshProUGUI resultTxt;
    UIManager uiManager;


    DiceRoll die;
    GameObject roller;

    void Start() {
        //combatUIPanel = GameObject.Find("UI/CombatUI");
        //resultTxt = GameObject.Find("UI/CombatUI/ResultTxt").GetComponent<TextMeshProUGUI>();
        uiManager = FindObjectOfType<UIManager>();
        Instance = this;
        playerDiceNum = 0;
        isInFight = false;
        fightEnd = false;
        mainCharacter = GameManager.Instance.mainCharacter;
        view = GetComponent<PhotonView>();
        MonsterFightCount = 0;
        roller = transform.Find("Roller").gameObject;
        roller.SetActive(false);
        die = transform.Find("Roller/Die").GetComponent<DiceRoll>();
    }

    void Update() {
        if (isInFight) {
            if (characterInFight == mainCharacter) {
                uiManager.ShowDiceImg(fightType, playerDiceNum);
                if (fightEnd) // show the result for 2 sec
                {
                    fightEnd = false;

                    switch (fightType) {
                        case FightType.Rock:
                            StartCoroutine(RockFightEnd());
                            break;
                        case FightType.Trap:
                            StartCoroutine(TrapFightEnd());
                            break;
                        case FightType.Monster:
                            StartCoroutine(MonsterFightEnd());
                            break;
                    }


                }
            }
        }
    }

    public void StartFight(GameObject enemy, FightType fightType, Character playerObj) {
        //Debug.Log("StartFight");
        isInFight = true;
        characterInFight = playerObj;
        //instantiate the die and initialize it with the character's dice number


        roller.SetActive(true);
        roller.transform.position = playerObj.transform.position;

        //activate the die platform as well
        die.ConfigureDie(characterInFight.config.dieFaces);
        die.gameObject.SetActive(true);

        this.fightType = fightType;
        this.enemy = enemy;
        if (characterInFight == mainCharacter) {
            characterInFight.transform.GetChild(3).gameObject.SetActive(true); // Dice
            characterInFight.transform.GetChild(4).gameObject.SetActive(true); // Dice Ground
        }
        if (this.fightType == FightType.Monster) {
            monsterNum = this.enemy.GetComponent<Monster>().targetValues;
            this.enemy.GetComponent<Animator>().SetBool("Attack", true);
            this.enemy.GetComponent<Animator>().SetBool("Idle", false);
        }
        else {
            monsterNum = new int[0];
        }

        uiManager.ShowCombatUI(fightType, monsterNum);
    }



    private IEnumerator RockFightEnd() {
        if (playerDiceNum >= 5) //win
        {
            uiManager.DisplayCombatResult("You broke the rock!");
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightWin();
        }
        else //lose
        {
            uiManager.DisplayCombatResult("The rock didn't budge...");
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightLose();
        }
    }
    private IEnumerator TrapFightEnd() {
        if (playerDiceNum <= 3) //win
        {
            uiManager.DisplayCombatResult("You disabled the trap!");
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightWin();
        }
        else //lose
        {
            uiManager.DisplayCombatResult("The trap went off!");
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightLose();
        }
    }
    private IEnumerator MonsterFightEnd() {
        if (monsterNum.Contains(playerDiceNum)) // win 
        {
            uiManager.DisplayCombatResult("You deafeted the monster!");
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightWin();
        }
        else if (MonsterFightCount < MAXFIGHT - 1) // lose a chances
        {
            MonsterFightCount++;
            characterInFight.transform.GetChild(3).gameObject.GetComponent<DiceRoll>().CallReroll();
            yield return new WaitForSeconds(1f);
            int chanceLeft = MAXFIGHT - MonsterFightCount;
            uiManager.DisplayCombatResult(chanceLeft.ToString() + " chances left.");
        }
        else  // lose
        {
            MonsterFightCount = 0;
            uiManager.DisplayCombatResult("Health -1");
            characterInFight.Damage(1);
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightLose();
            enemy.GetComponent<Animator>().SetBool("Attack", false);
            enemy.GetComponent<Animator>().SetBool("Idle", true);
        }
    }

    private void EndFight() {
        uiManager.DismissCombatUI();
        playerDiceNum = 0;
        characterInFight.transform.GetChild(3).gameObject.SetActive(false); // Dice
        characterInFight.transform.GetChild(4).gameObject.SetActive(false); // Dice Ground
        isInFight = false;
    }

    public void CallFightWin() {
        view.RPC("FightWin", RpcTarget.All);
    }
    public void CallFightLose() {
        view.RPC("FightLose", RpcTarget.All);
    }
    [PunRPC]
    public void FightWin() {
        //win
        if (PhotonNetwork.IsMasterClient) {
            if (enemy != null) {
                PhotonNetwork.Destroy(enemy);
            }
        }
        isInFight = false;
        //Debug.Log("FightEnd : win");
    }
    [PunRPC]
    public void FightLose() {
        characterInFight.ResetPosition();
        isInFight = false;
        //Debug.Log("FightEnd : Lose");
    }
}
