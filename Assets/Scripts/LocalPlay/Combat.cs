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

    public enum DiceSize
    {
        D0 = 0,
        D4 = 4,
        D6 = 6,
        D8 = 8,
        D10 = 10
    }

    [System.Serializable]
    public class Dice {
        public DiceSize type;
        public int bonus;
        public Dice(DiceSize type, int bonus = 0) {
            this.type = type;
            this.bonus = bonus;
        }

        public override string ToString() {
            string result = type.ToString();
            if (bonus >= 0) {
                result += "+" + bonus.ToString();
            }
            else {
                result += bonus.ToString();
            }
            return result;
        }

        public int Roll() {
            if (type == DiceSize.D0) {
                return bonus;
            }
            else {
                return Random.Range(1, (int)type + 1) + bonus;
            }
        }
    }

    public static bool ExecuteCombat(FightType type, LocalTile tile, LocalUIManager uiManager)
    {
        bool result = false;
        List<int> charaIDs = new List<int>();
        List<int> enemyScores = new List<int>();
        List<int> charaScores = new List<int>();
        int enemyScore = 0;
        int charaScore = 0;

        foreach (LocalCharacter c in tile.charaList)
        {
            charaIDs.Add(c.CharacterId);
            int outcome = c.config.monsterDice.Roll();
            charaScores.Add(outcome);
            charaScore += outcome;
        }

        if (type == FightType.Monster)
        {
            foreach (LocalMonster m in tile.enemyList)
            {
                int outcome = m.config.combatDice.Roll();
                enemyScores.Add(outcome);
                enemyScore += outcome;
            }
        }
        else if (type == FightType.Trap || type == FightType.Rock)
        {
            int outcome = tile.dice.Roll();
            enemyScore += outcome;
            //enemyScore += tile.diceBonus;
            enemyScores.Add(enemyScore);
        }

        if (enemyScore <= charaScore)
        {
            result = true;
        }

        uiManager.ShowCombatUI(type, charaIDs, charaScores, enemyScores, charaScore, enemyScore, result);
            return result;
    }
}
