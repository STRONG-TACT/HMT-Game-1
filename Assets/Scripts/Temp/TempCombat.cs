using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TempCombat : MonoBehaviour
{
    public static TempCombat instance;
    public GameObject player;

    [HideInInspector] public GameObject enemy;
    [HideInInspector] public bool isInFight;
    [HideInInspector] public bool fightEnd;
    [HideInInspector] public int fightType; // 0: Fight Rock, 1: fight Trap, 2: Fight Monster
    [HideInInspector] public int playerDiceNum;
    [HideInInspector] public int[] monsterNum;

    // UI
    public GameObject combatUIPanel;
    public Text playerTxt;
    public Text enemyTxt;
    public Text resultTxt;

    void Start()
    {
        instance = this;
        playerDiceNum = 0;
        isInFight = false;
        fightEnd = false;
    }

    void Update()
    {
        if (isInFight)
        {
            showPanel();
            if (fightEnd)
            {
                fightEnd = false;
                Debug.Log("FightEnd" + fightEnd);
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

    public void StartFight(GameObject obj, int i)
    {
        fightType = i;
        enemy = obj;
        player.transform.GetChild(2).gameObject.SetActive(true); // Dice
        player.transform.GetChild(3).gameObject.SetActive(true); // Dice Ground
        if (fightType == 2)
        {
            monsterNum = instance.enemy.GetComponent<Monster>().targetValues;
        }
        combatUIPanel.SetActive(true);
    }
    private void showPanel()
    {
        playerTxt.text = "Character Dice: " + instance.playerDiceNum.ToString();
        if (fightType == 0)
        {
            enemyTxt.text = "Roll > 5 to move rock";
        }
        else if (fightType == 1)
        {
            enemyTxt.text = "Roll < 3 to dstroy the trap";
        }
        else
        {
            string txt = "Monster:";
            for (int i = 0; i < monsterNum.Length; i++)
            {
                txt = txt + monsterNum[i].ToString() + " ";
            }
            enemyTxt.text = txt;
        }

    }
    private IEnumerator RockFightEnd()
    {
        if (playerDiceNum >= 5) //win
        {
            Destroy(enemy);
            resultTxt.text = "You win";
        }
        else //lose
        {
            player.GetComponent<PlayerTemp>().movePoint.position = player.GetComponent<PlayerTemp>().prevMovePointPos; // move back
            resultTxt.text = "You lose";
        }
        yield return new WaitForSeconds(2f);
        EndGame();
    }
    private IEnumerator TrapFightEnd()
    {
        if (playerDiceNum <= 3) //win
        {
            Destroy(enemy);
            resultTxt.text = "You win";
        }
        else //lose
        {
            player.GetComponent<PlayerTemp>().movePoint.position = player.GetComponent<PlayerTemp>().prevMovePointPos; // move back
            resultTxt.text = "You lose";
        }
        yield return new WaitForSeconds(2f);
        EndGame();
    }
    private IEnumerator MonsterFightEnd()
    {
        if (monsterNum.Contains(playerDiceNum))
        {
            //win
            Destroy(enemy);
            resultTxt.text = "You win";
        }
        else
        {
            //lose
            player.GetComponent<PlayerTemp>().movePoint.position = player.GetComponent<PlayerTemp>().prevMovePointPos; // move back
            resultTxt.text = "You lose";
        }
        yield return new WaitForSeconds(2f);
        EndGame();
    }

    private void EndGame()
    {
        resultTxt.text = "";
        combatUIPanel.SetActive(false);
        playerDiceNum = 0;
        player.transform.GetChild(2).gameObject.SetActive(false); // Dice
        player.transform.GetChild(3).gameObject.SetActive(false); // Dice Ground
        isInFight = false;
    }
}
