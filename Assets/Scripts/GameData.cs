using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class ONLY CONTAINS DATA. It is the main lever for configuring characters in a scene.
/// The only logic it should have is helpers for looking things up.
/// </summary>
public class GameData : MonoBehaviour
{
    public enum CharacterType {
        Human,
        Dwarf,
        Giant
    }

    [System.Serializable]
    public class CharacterConfig {
        [Tooltip("Name for this character type. Usually just Dwarf, Giant, or Human.")]
        public string name;
        [Tooltip("The Type of character to use, used for graphical model and icons.")]
        public CharacterType type;
        [Tooltip("Movement Limit.")]
        [Min(1)]
        public int movement;

        //Below here currently only used for reference.
        [Tooltip("How many adjacent tiles they can see. Currently for reference only.")]
        [Min(1)]
        public int sightRange; 
        [Tooltip("Where to initial place the camera realtive to the character. Currently for reference only.")]
        public Vector3 cameraPosition; 
        [Tooltip("The faces on the character die. Currently for reference only.")]
        public int[] dieFaces = { 1, 2, 3, 4, 5, 6 }; 
    }

    public int gameLevel;
    [Tooltip("The Configurations of Characters. Original order was: Dwarf, Giant, Human")]
    public CharacterConfig[] characters;

    public int[] characterMapping = null;

    public int[] CreateCharacterMapping() {
        int[] a = new int[] { 0, 1, 2 };
        int[] b = new int[] { 0, 2, 1 };
        int[] c = new int[] { 1, 0, 2 };
        int[] d = new int[] { 1, 2, 0 };
        int[] e = new int[] { 2, 1, 0 };
        int[] f = new int[] { 2, 0, 1 };
        return new int[][] { a, b, c, d, e, f }[Random.Range(0, 6)];
    } 

    public CharacterConfig GetCharacter(int photonActorNumber) {
        Debug.LogFormat("Geting Actor Number:{0}", photonActorNumber);
        return characters[photonActorNumber-1];
    }

    public bool maskOn;
    public float tileSize;
    public float tileGapLength; // the length between tiles, mainlt used in PlayerMovement.cs
    public bool differentCameraView; // Whether the photonView size of each character is different

    public Vector3[] cameraViews; // <-- next on the chopping block

    void Start() {
        if (characterMapping == null) {
            characterMapping = new int[] { 0, 1, 2 };
        }
    }
}
