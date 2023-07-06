#if DEPRECATED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DiceSideCheck : MonoBehaviour
{
    private GameObject Dice;
    private int diceNum;
    private bool diceRolled;
     
    private void Start()
    {
        Dice = this.transform.parent.GetChild(3).gameObject;
    }

    void OnEnable()
    {
        diceRolled = false;
    }

    void OnTriggerStay(Collider col)
    {
        //Debug.Log(col.gameObject.name);
        if (col.CompareTag("HumanDiceSide") || col.CompareTag("GiantDiceSide") || col.CompareTag("DwarfDiceSide")) 
        {
            if(Dice.GetComponent<Rigidbody>().IsSleeping() && !diceRolled) {
                if (col.CompareTag("HumanDiceSide"))
                {
                    HumanDiceCheck(col.gameObject);
                }
                else if (col.CompareTag("DwarfDiceSide"))
                {
                    DwarfDiceCheck(col.gameObject);
                }
                else if (col.CompareTag("GiantDiceSide"))
                {
                    GiantDiceCheck(col.gameObject);
                }
                diceRolled = true;
                CombatSystem.Instance.fightEnd = true;
                CombatSystem.Instance.playerDiceNum = diceNum;
            }
        }
    }

    void OnTriggerExit()
    {
        diceRolled = false;
    }

    private void HumanDiceCheck(GameObject diceSide)
    {
        switch (diceSide.name)
        {
            case "Side1":
                diceNum = 3;
                break;
            case "Side2":
                diceNum = 5; ;
                break;
            case "Side3":
                diceNum = 1; ;
                break;
            case "Side4":
                diceNum = 6; ;
                break;
            case "Side5":
                diceNum = 2; ;
                break;
            case "Side6":
                diceNum = 4; ;
                break;
        }
    }

    private void DwarfDiceCheck(GameObject diceSide)
    {
        if (diceSide.name == "Side1" || diceSide.name == "Side1_2")
        {
            diceNum = 3;
        }
        else if (diceSide.name == "Side3" || diceSide.name == "Side3_2")
        {
            diceNum = 1;
        }
        else
        {
            diceNum = 2;
        }
    }
    private void GiantDiceCheck(GameObject diceSide)
    {
        if (diceSide.name == "Side4" || diceSide.name == "Side4_2")
        {
            diceNum = 6;
        }
        else if (diceSide.name == "Side6" || diceSide.name == "Side6_2")
        {
            diceNum = 4;
        }
        else
        {
            diceNum = 5;
        }
    }
}
#endif