using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSwapGUI : MonoBehaviour
{

    LocalGameManager gameManager;

    bool displayed = false;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GetComponent<LocalGameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            displayed = !displayed;
        }

        if(Input.GetKey(KeyCode.LeftControl) && 
            Input.GetKey(KeyCode.LeftShift) && 
            Input.GetKeyDown(KeyCode.Period)) {
            gameManager.NextLevel();
        }

    }

    private void OnGUI() {
        if (displayed) {
            if(GUI.Button(new Rect(0,0,250,100), "Next Level")) {
                gameManager.NextLevel();
            }
        }
    }
}
