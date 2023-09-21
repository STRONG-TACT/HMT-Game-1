using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalMonster : MonoBehaviour
{
    public MonsterConfig config;
    public int monsterId;
    public LocalGameData gameData;

    private float cellScale;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetUpConfig(MonsterConfig config, int MonsterId, LocalGameData data)
    {
        this.config = config;
        monsterId = MonsterId;

        gameData = data;

        cellScale = data.tileSize + 2 * data.tileGapLength;
    }
}
