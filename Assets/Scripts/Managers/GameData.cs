using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class ONLY CONTAINS DATA. It is the main lever for configuring characters in a scene.
/// The only logic it should have is helpers for looking things up.
/// </summary>
public partial class GameData : MonoBehaviour
{

    public int gameLevel;
    [Tooltip("The Configurations of Characters. Original order was: Dwarf, Giant, Human")]
    public CharacterConfig[] characterConfigs;
    public Character[] inSceneCharacters;

    public bool maskOn;
    public float tileSize;
    public float tileGapLength; // the length between tiles, mainlt used in PlayerMovement.cs
    public bool differentCameraView; // Whether the photonView size of each character is different

    public Vector3[] cameraViews; // <-- next on the chopping block

    void Start() {
        if(characterConfigs.Length != 3) {
            Debug.LogError("GameData: characterConfigs must have exactly 3 elements.");
        }
        if(inSceneCharacters.Length != 3) {
            Debug.LogError("GameData: inSceneCharacters must have exactly 3 elements.");
        }

        for(int i =0; i < characterConfigs.Length; i++) {
            inSceneCharacters[i].config = characterConfigs[i];
        }
    }
}
