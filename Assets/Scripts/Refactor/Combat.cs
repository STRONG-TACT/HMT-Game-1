using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameConstant;
using System.Linq;

public class Combat : MonoBehaviour {
    public enum FightType {
        Rock = 0,
        Trap = 1,
        Monster = 2
    }

    public enum DiceSize {
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

        public int NetworkRoll() {
            if (type == DiceSize.D0) {
                return bonus;
            }
            else {
                return NetworkMiddleware.S.NextRandomInt(1, (int)type + 1) + bonus;
            }
        }

        public IList<int> Values() {
            List<int> values = new List<int>();
            for (int i = 1; i <= (int)type; i++) {
                values.Add(i + bonus);
            }
            return values;
        }
    }

    public static bool ExecuteCombat(FightType type, Tile tile, bool visibility) {
        bool result = false;
        List<int> charaIDs = new List<int>();
        List<Dice> charaDice = new List<Dice>();
        List<int> enemyScores = new List<int>();
        List<Dice> enemyDice = new List<Dice>();
        List<int> charaScores = new List<int>();
        List<string> challenges = new List<string>();
        int enemyScore = 0;
        int charaScore = 0;

        

        int roll;

        switch (type) {
            case FightType.Monster:
                foreach (Character c in tile.LivingCharacterList) {
                    charaIDs.Add(c.CharacterId);
                    roll = c.config.monsterDice.Roll();
                    charaDice.Add(c.config.monsterDice);
                    charaScores.Add(roll);
                    charaScore += roll;
                }
                foreach (Monster m in tile.MonsterList) {
                    roll = m.config.combatDice.Roll();
                    enemyDice.Add(m.config.combatDice);
                    challenges.Add(m.ObjKey);
                    enemyScores.Add(roll);
                    enemyScore += roll;
                }
                break;
            case FightType.Trap:
                foreach (Character c in tile.LivingCharacterList) {
                    charaIDs.Add(c.CharacterId);
                    roll = c.config.trapDice.Roll();
                    charaDice.Add(c.config.trapDice);
                    charaScores.Add(roll);
                    charaScore += roll;
                }
                roll = tile.dice.Roll();
                enemyScore += roll;
                enemyScores.Add(roll);
                enemyDice.Add(tile.dice);
                enemyScores.Add(enemyScore);
                challenges.Add(tile.ObjKey);
                break;
            case FightType.Rock:
                foreach (Character c in tile.LivingCharacterList) {
                    charaIDs.Add(c.CharacterId);
                    roll = c.config.stoneDice.Roll();
                    charaDice.Add(c.config.stoneDice);
                    charaScores.Add(roll);
                    charaScore += roll;
                }
                roll = tile.dice.Roll();
                enemyScore += roll;
                enemyScores.Add(roll);
                enemyDice.Add(tile.dice);
                enemyScores.Add(enemyScore);
                challenges.Add(tile.ObjKey);
                break;
        }


        
        if (charaScore >= enemyScore) {
            result = true;
        }

        if (CompetitionMiddleware.Instance.LogSystemEvents) {
            CompetitionMiddleware.Instance.LogChallengeEncounter(
                tile.col, tile.row,
                charaIDs.Select(id => IntegratedGameManager.S.inSceneCharacters[id].config.characterName).ToList(),
                challenges,
                charaScores, enemyScores,
                CalculateOdds(charaDice, enemyDice), result);
        }


        UIManager.S.ShowCombatUI(type, charaIDs, charaDice, enemyDice, charaScores, enemyScores, charaScore, enemyScore, result, visibility);
        return result;
    }

    private static (int wins, int total) CalculateOddsOpponentInnerLoop(int p1, int p2, int p3, IList<Dice> opponents) {
        var wins = 0;
        var total = 0;

        switch (opponents.Count) {
            case 1:
                foreach (int o1 in opponents[0].Values()) {
                    if (p1 + p2 + p3 >= o1) {
                        wins++;
                    }
                    total++;
                }
                break;

            case 2:
                foreach (int o1 in opponents[0].Values()) {
                    foreach (int o2 in opponents[1].Values()) {
                        if (p1 + p2 + p3 >= o1 + o2) {
                            wins++;
                        }
                        total++;
                    }
                }
                break;
            case 3:
                foreach (int o1 in opponents[0].Values()) {
                    foreach (int o2 in opponents[1].Values()) {
                        foreach (int o3 in opponents[2].Values()) {
                            if (p1 + p2 + p3 >= o1 + o2 + o3) {
                                wins++;
                            }
                            total++;
                        }
                    }
                }
                break;
            default:
                Debug.LogWarningFormat("Calculate Odds called with {0} opponents", opponents.Count);
                break;
        }

        return (wins, total);
    }


    public static float CalculateOdds(Dice player, IList<Dice> opponents) {
        return CalculateOdds(new List<Dice> { player }, opponents);
    }

    public static float CalculateOdds(IList<Dice> players, Dice opponent) {
        return CalculateOdds(players, new List<Dice> { opponent });
    }

    public static float CalculateOdds(Dice player, Dice opponent) {
        return CalculateOdds(new List<Dice> { player }, new List<Dice> { opponent });
    }

    public static float CalculateOdds(IList<Dice> players, IList<Dice> opponents) {
        float wins = 0;
        float total = 0;

        switch(players.Count) {
            case 1:
                foreach (int p1 in players[0].Values()) {
                    var (w, t) = CalculateOddsOpponentInnerLoop(p1, 0, 0, opponents);
                    wins += w;
                    total += t;
                }
                break;
            case 2:
                foreach (int p1 in players[0].Values()) {
                    foreach (int p2 in players[1].Values()) {
                        var (w, t) = CalculateOddsOpponentInnerLoop(p1, p2, 0, opponents);
                        wins += w;
                        total += t;
                    }
                }
                break;
            case 3:
                foreach (int p1 in players[0].Values()) {
                    foreach (int p2 in players[1].Values()) {
                        foreach (int p3 in players[2].Values()) {
                            var (w, t) = CalculateOddsOpponentInnerLoop(p1, p2, p3, opponents);
                            wins += w;
                            total += t;
                        }
                    }
                }
                break;
        }
        return wins / total;
    }
}
