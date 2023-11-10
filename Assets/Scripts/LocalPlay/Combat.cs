using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat : MonoBehaviour
{
    public enum FightType
    {
        Rock = 0,
        Trap = 1,
        Monster = 2
    }

    public enum DiceType
    {
        D0 = 0,
        D4 = 4,
        D6 = 6,
        D8 = 8,
        D10 = 10
    }

    public static bool ExecuteCombat(FightType type, LocalTile tile, LocalUIManager uiManager)
    {
        bool result = false;
        List<int> enemyScores = new List<int>();
        List<int> charaScores = new List<int>();
        int enemyScore = 0;
        int charaScore = 0;

        foreach (LocalCharacter c in tile.charaList)
        {
            c.State = LocalCharacter.CharacterState.Attacking;
            int outcome = RollDice((int)c.config.monsterDice);
            charaScores.Add(outcome);
            charaScore += outcome;
        }

        if (type == FightType.Monster)
        {
            foreach (LocalMonster m in tile.enemyList)
            {
                int outcome = RollDice((int)m.config.combatDice);
                enemyScores.Add(outcome);
                enemyScore += outcome;
            }
        }
        else if (type == FightType.Trap || type == FightType.Rock)
        {
            int outcome = RollDice((int)tile.localDice);
            enemyScore += outcome;
            enemyScore += tile.diceBonus;
            enemyScores.Add(enemyScore);
        }

        if (enemyScore <= charaScore)
        {
            result = true;
        }

        uiManager.ShowCombatUI(type, charaScores, enemyScores);
        foreach (LocalCharacter c in tile.charaList) {
            c.State = LocalCharacter.CharacterState.Idle;
        }
            return result;
    }

    public static int RollDice(int dice)
    {
        int result = 0;

        if (dice != 0)
        {
            result = Random.Range(1, dice + 1);
        }

        return result;
    }
}
