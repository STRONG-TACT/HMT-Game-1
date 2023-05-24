using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DetectWalls : MonoBehaviour {
    Character character;
    public Character.Direction direction;
    //private int dir;


    void Start() {
        character = transform.parent.parent.GetComponent<Character>();
    }

    void OnTriggerEnter(Collider col) {
        if (col.CompareTag("Walls")) {
            //Debug.Log("Triggered walls " + dir);
         //   character.movable[direction] = false;
        }
    }
    void OnTriggerExit(Collider col) {
        if (col.CompareTag("Walls")) {
            //Debug.Log("Triggered walls Exit " + direction);
        //    character.movable[direction] = true;
        }
    }

}
