using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSatus : MonoBehaviour
{
    public int monsterKilled;
    public int rockDestroyed;
    public int trapDestroyed;
    public int numOfDiceRolls;
    public int levelCompleted;
    public int pingingCount;
    public float gameTime;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

}
