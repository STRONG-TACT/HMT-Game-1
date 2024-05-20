using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public bool Initialized { get; private set; } = false;

    public TextAsset[] levelTextFiles;
    [Tooltip("The Configurations of Characters. Original order was: Dwarf, Giant, Human")]
    public CharacterConfig[] characterConfigs;
    //[Tooltip("The in-scene pointers to the character prefabs")]
    //public LocalCharacter[] inSceneCharacters;
    [Tooltip("The Configurations of Characters.")]
    public MonsterConfig[] monsterConfigs;
    //[Tooltip("The in-scene pointers to the monster prefabs")]
    //public LocalMonster[] inSceneMonsters;
    public float tileSize;
    public float tileGapLength; // the length between tiles, mainlt used in PlayerMovement.cs

    [Tooltip("The number of seconds a player has to take their turn in each Phase.")]
    public int TurntimeLimit = 120;

    [Tooltip("The number of lives a character starts the game with.")]
    [Min(1)]
    public int LivesPerCharacter = 3;

    [Tooltip("The number of Start Turns a character has to sit out before they can respawn.")]
    [Min(1)]
    public int RespawnDelay = 1;

    public static GameData S;

    private void Awake()
    {
        S = this;
    }

    void Start()
    {
        if (characterConfigs.Length != 3)
        {
            Debug.LogError("GameData: characterConfigs must have exactly 3 elements.");
        }
        if (IntegratedGameManager.S.inSceneCharacters.Count != 3)
        {
            Debug.LogError("GameData: inSceneCharacters must have exactly 3 elements.");
        }

        for (int i = 0; i < characterConfigs.Length; i++)
        {
            IntegratedGameManager.S.inSceneCharacters[i].SetUpConfig(characterConfigs[i], i, this);
        }

        Initialized = true;
    }
}
