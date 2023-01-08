using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem instance;
    private GameAssets gameAssets;

    [HideInInspector] public GameObject mainPlayer;
    [HideInInspector] public GameObject enemy;
    [HideInInspector] public GameObject playerInFight;

    [HideInInspector] public bool isInFight;
    [HideInInspector] public bool fightEnd;
    [HideInInspector] public int fightType; // 0: Fight Rock, 1: fight Trap, 2: Fight Monster
    [HideInInspector] public int MonsterFightCount;
    private int MAXFIGHT = 3;

    [HideInInspector] public int playerDiceNum;
    [HideInInspector] public int[] monsterNum;

    PhotonView view;

    // UI
    [HideInInspector] public GameObject combatUIPanel;
    TextMeshProUGUI resultTxt;

    void Start()
    {
        combatUIPanel = GameObject.Find("UI").transform.GetChild(6).gameObject;
        resultTxt = combatUIPanel.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        instance = this;
        playerDiceNum = 0;
        isInFight = false;
        fightEnd = false;
        mainPlayer = GameManager.instance.mainPlayer;
        view = GetComponent<PhotonView>();
        MonsterFightCount = 0;
    }

    void Update()
    {
        if (isInFight)
        {
            if (playerInFight == mainPlayer)
            {
                ShowDiceImg();
                if (fightEnd) // show the result for 2 sec
                {
                    fightEnd = false;
                    if (fightType == 0)
                    {
                        StartCoroutine(RockFightEnd());
                    }
                    else if (fightType == 1)
                    {
                        StartCoroutine(TrapFightEnd());
                    }
                    else
                    {
                        StartCoroutine(MonsterFightEnd());
                    }
                }
            }
        }
    }

    public void StartFight(GameObject obj, int i, GameObject playerObj)
    {
        //Debug.Log("StartFight");
        playerInFight = playerObj;
        fightType = i;
        enemy = obj;
        if (playerInFight == mainPlayer)
        {
            playerInFight.transform.GetChild(3).gameObject.SetActive(true); // Dice
            playerInFight.transform.GetChild(4).gameObject.SetActive(true); // Dice Ground
        }
        if (fightType == 2)
        {
            monsterNum = instance.enemy.GetComponent<Monster>().num;
            enemy.GetComponent<Animator>().SetBool("Attack", true);
            enemy.GetComponent<Animator>().SetBool("Idle", false);
        }

        ShowPanel();
    }
    private void ShowPanel()
    {
        Image CombatUIImg = combatUIPanel.transform.GetChild(0).GetComponent<Image>();

        if (fightType == 0) // rock fight
        {
            CombatUIImg.sprite = gameAssets.rockCombatUI;
        }
        else if (fightType == 1) // trap fight
        {
            CombatUIImg.sprite = gameAssets.trapCombatUI;
        }
        else // monster fight
        {
            CombatUIImg.sprite = gameAssets.monsterCombatUI;
            TextMeshProUGUI monsterNumberTxt = combatUIPanel.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            string txt = "";
            for (int i = 0; i < monsterNum.Length; i++)
            {
                txt = txt + monsterNum[i].ToString() + " ";
            }
            monsterNumberTxt.text = txt;
        }
        combatUIPanel.SetActive(true);
    }
    private void ShowDiceImg()
    {
        Image diceImg = GameObject.Find("DiceImgs").transform.GetChild(fightType).gameObject.GetComponent<Image>();
        diceImg.sprite = gameAssets.diceImg[playerDiceNum - 1];
        diceImg.gameObject.SetActive(true);
    }

    private void CloseDiceImg()
    {
        Image diceImg = GameObject.Find("DiceImgs").transform.GetChild(fightType).gameObject.GetComponent<Image>();
        diceImg.gameObject.SetActive(false);
    }

    private IEnumerator RockFightEnd()
    {
        if (playerDiceNum >= 5) //win
        {
            resultTxt.text = "You win";
            yield return new WaitForSeconds(2f);
            EndGame();
            CallFightWin();
        }
        else //lose
        {
            resultTxt.text = "You lose";
            yield return new WaitForSeconds(2f);
            EndGame();
            CallFightLose();
        }
    }
    private IEnumerator TrapFightEnd()
    {
        if (playerDiceNum <= 3) //win
        {
            resultTxt.text = "You win";
            yield return new WaitForSeconds(2f);
            EndGame();
            CallFightWin();
        }
        else //lose
        {
            resultTxt.text = "You lose";
            yield return new WaitForSeconds(2f);
            EndGame();
            CallFightLose();
        }
    }
    private IEnumerator MonsterFightEnd()
    {
        if (monsterNum.Contains(playerDiceNum)) // win 
        {
            resultTxt.text = "You win";
            yield return new WaitForSeconds(2f);
            EndGame();
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
            playerInFight.GetComponent<PlayerHealth>().Damage();
            yield return new WaitForSeconds(2f);
            EndGame();
            CallFightLose();
            enemy.GetComponent<Animator>().SetBool("Attack", false);
            enemy.GetComponent<Animator>().SetBool("Idle", true);
        }
    }

    private void EndGame()
    {
        TextMeshProUGUI resultTxt = combatUIPanel.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
        CloseDiceImg();
        resultTxt.text = "";
        combatUIPanel.SetActive(false);
        playerDiceNum = 0;
        playerInFight.transform.GetChild(3).gameObject.SetActive(false); // Dice
        playerInFight.transform.GetChild(4).gameObject.SetActive(false); // Dice Ground
        isInFight = false;
    }

    public void CallFightWin()
    {
        view.RPC("FightWin", RpcTarget.All);
    }
    public void CallFightLose()
    {
        view.RPC("FightLose", RpcTarget.All);
    }
    [PunRPC]
    public void FightWin()
    {
        //win
        if (PhotonNetwork.IsMasterClient)
        {
            if (enemy!=null)
            {
                PhotonNetwork.Destroy(enemy);
            }
        }
        isInFight = false;
        //Debug.Log("FightEnd : win");
    }
    [PunRPC]
    public void FightLose()
    {
        playerInFight.GetComponent<Player>().movePoint.position = playerInFight.GetComponent<Player>().prevMovePointPos;
        isInFight = false;
        //Debug.Log("FightEnd : Lose");
    }
}
