using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class ONLY CONTAINS DATA. It is the main lever for configuring characters in a scene.
/// The only logic it should have is helpers for looking things up.
/// </summary>
public partial class GameData : MonoBehaviour
{

    public bool Initialized { get; private set; } = false;

    public int gameLevel;
    [Tooltip("The Configurations of Characters. Original order was: Dwarf, Giant, Human")]
    public CharacterConfig[] characterConfigs;
    [Tooltip("The in-scene pointers to the character prefabs")]
    public Character[] inSceneCharacters;
    [Tooltip("The Configurations of Characters.")]
    public MonsterConfig[] monsterConfigs;
    [Tooltip("The in-scene pointers to the monster prefabs")]
    public Monster[] inSceneMonsters;
    public float tileSize;
    public float tileGapLength; // the length between tiles, mainlt used in PlayerMovement.cs

    [Tooltip("Whether this level should use fog of war or not")]
    public bool maskOn;
    public bool differentCameraView; // Whether the photonView size of each character is different
    [Tooltip("Whether a moster turn shold be included in the turn rotation")]
    public bool inlcudeMonsterTurn = false; //Whether the monsters should be inlcuded in the turn rotation
    [Tooltip("The maximum number of combat attempts a player can make. Default=3")]
    public int maxCombatAttempts = 3; //The maximum number of combat attempts a player can make

    public Vector3[] cameraViews; // <-- next on the chopping block

    void Start() {
        if(characterConfigs.Length != 3) {
            Debug.LogError("GameData: characterConfigs must have exactly 3 elements.");
        }
        if(inSceneCharacters.Length != 3) {
            Debug.LogError("GameData: inSceneCharacters must have exactly 3 elements.");
        }

        for(int i =0; i < characterConfigs.Length; i++) {
            inSceneCharacters[i].SetUpConfig(characterConfigs[i], i, this);
        }

        for (int j = 0; j < monsterConfigs.Length; j++)
        {
            inSceneMonsters[j].SetUpConfig(monsterConfigs[j], j, this);
        }

        Initialized = true;
    }
}
