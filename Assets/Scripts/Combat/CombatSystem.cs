using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class CombatSystem : MonoBehaviour {
    public static CombatSystem Instance;

    public enum FightType {
        Rock=0,
        Trap=1,
        Monster=2
    }

    private GameAssets gameAssets;

    [HideInInspector] public Character mainPlayer;
    [HideInInspector] public GameObject enemy;
    [HideInInspector] public Character playerInFight;

    [HideInInspector] public bool isInFight;
    [HideInInspector] public bool fightEnd;
    [HideInInspector] public FightType fightType; // 0: Fight Rock, 1: fight Trap, 2: Fight Monster
    [HideInInspector] public int MonsterFightCount;
    private int MAXFIGHT = 3;

    [HideInInspector] public int playerDiceNum;
    [HideInInspector] public int[] monsterNum;

    PhotonView view;

    // UI
    [HideInInspector] public GameObject combatUIPanel;
    TextMeshProUGUI resultTxt;

    DiceRoll die;
    GameObject roller;

    void Start() {
        combatUIPanel = GameObject.Find("UI/CombatUI").transform.GetChild(6).gameObject;
        resultTxt = combatUIPanel.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        Instance = this;
        playerDiceNum = 0;
        isInFight = false;
        fightEnd = false;
        mainPlayer = GameManager.Instance.mainPlayer;
        view = GetComponent<PhotonView>();
        MonsterFightCount = 0;
        roller = transform.Find("Roller").gameObject;
        roller.SetActive(false);
        die = transform.Find("Roller/Die").GetComponent<DiceRoll>();
    }

    void Update() {
        if (isInFight) {
            if (playerInFight == mainPlayer) {
                ShowDiceImg();
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
        playerInFight = playerObj;
        //instantiate the die and initialize it with the character's dice number


        roller.SetActive(true);
        roller.transform.position = playerObj.transform.position;

        //activate the die platform as well
        die.ConfigureDie(playerInFight.config.dieFaces);
        die.gameObject.SetActive(true);

        this.fightType = fightType;
        this.enemy = enemy;
        if (playerInFight == mainPlayer) {
            playerInFight.transform.GetChild(3).gameObject.SetActive(true); // Dice
            playerInFight.transform.GetChild(4).gameObject.SetActive(true); // Dice Ground
        }
        if (this.fightType == FightType.Monster) {
            monsterNum = this.enemy.GetComponent<Monster>().targetValues;
            this.enemy.GetComponent<Animator>().SetBool("Attack", true);
            this.enemy.GetComponent<Animator>().SetBool("Idle", false);
        }

        ShowPanel();
    }
    private void ShowPanel() {
        Image CombatUIImg = combatUIPanel.transform.GetChild(0).GetComponent<Image>();

        switch (fightType) {
            case FightType.Rock:
                CombatUIImg.sprite = gameAssets.rockCombatUI;
                break;

            case FightType.Trap:
                CombatUIImg.sprite = gameAssets.trapCombatUI;
                break;

            case FightType.Monster:
                CombatUIImg.sprite = gameAssets.monsterCombatUI;
                TextMeshProUGUI monsterNumberTxt = combatUIPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
                string txt = "";
                for (int i = 0; i < monsterNum.Length; i++) {
                    txt = txt + monsterNum[i].ToString() + " ";
                }
                monsterNumberTxt.text = txt;
                break;
        }
        combatUIPanel.SetActive(true);
    }
    private void ShowDiceImg() {
        Image diceImg = GameObject.Find("DiceImgs").transform.GetChild((int)fightType).gameObject.GetComponent<Image>();
        diceImg.sprite = gameAssets.diceImg[playerDiceNum - 1];
        diceImg.gameObject.SetActive(true);
    }

    private void CloseDiceImg() {
        Image diceImg = GameObject.Find("DiceImgs").transform.GetChild((int)fightType).gameObject.GetComponent<Image>();
        diceImg.gameObject.SetActive(false);
    }

    private IEnumerator RockFightEnd() {
        if (playerDiceNum >= 5) //win
        {
            resultTxt.text = "You win";
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightWin();
        }
        else //lose
        {
            resultTxt.text = "You lose";
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightLose();
        }
    }
    private IEnumerator TrapFightEnd() {
        if (playerDiceNum <= 3) //win
        {
            resultTxt.text = "You win";
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightWin();
        }
        else //lose
        {
            resultTxt.text = "You lose";
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightLose();
        }
    }
    private IEnumerator MonsterFightEnd() {
        if (monsterNum.Contains(playerDiceNum)) // win 
        {
            resultTxt.text = "You win";
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightWin();
        }
        else if (MonsterFightCount < MAXFIGHT - 1) // lose a chances
        {
            MonsterFightCount++;
            playerInFight.transform.GetChild(3).gameObject.GetComponent<DiceRoll>().CallReroll();
            yield return new WaitForSeconds(1f);
            int chanceLeft = MAXFIGHT - MonsterFightCount;
            resultTxt.text = chanceLeft.ToString() + " chances left.";
        }
        else  // lose
        {
            MonsterFightCount = 0;
            resultTxt.text = "Health -1";
            playerInFight.Damage(1);
            yield return new WaitForSeconds(2f);
            EndFight();
            CallFightLose();
            enemy.GetComponent<Animator>().SetBool("Attack", false);
            enemy.GetComponent<Animator>().SetBool("Idle", true);
        }
    }

    private void EndFight() {
        TextMeshProUGUI resultTxt = combatUIPanel.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        CloseDiceImg();
        resultTxt.text = "";
        combatUIPanel.SetActive(false);
        playerDiceNum = 0;
        playerInFight.transform.GetChild(3).gameObject.SetActive(false); // Dice
        playerInFight.transform.GetChild(4).gameObject.SetActive(false); // Dice Ground
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
        playerInFight.movePoint.position = playerInFight.prevMovePointPos;
        isInFight = false;
        //Debug.Log("FightEnd : Lose");
    }
}
