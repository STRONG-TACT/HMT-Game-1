using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayer : MonoBehaviour
{
    public LocalCharacter myCharacter { set; get; }

    public bool isPlanning = false;

    private int moveCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        isPlanning = false;
    }



    // Update is called once per frame
    void Update()
    {
        if (isPlanning)
        {
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
            {
                if (Input.GetAxisRaw("Horizontal") < 0 && myCharacter.CheckMove(LocalCharacter.Direction.Left))
                {
                    Debug.Log("Moving left");
                    moveCount += 1;
                    LocalGameManager.Instance.PlanUpdated(myCharacter.config.movement - moveCount);
                }
                else if (Input.GetAxisRaw("Horizontal") > 0 && myCharacter.CheckMove(LocalCharacter.Direction.Right))
                {
                    Debug.Log("Moving right");
                }
            }
            //vertical move
            else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
            {
                if (Input.GetAxisRaw("Vertical") > 0 && myCharacter.CheckMove(LocalCharacter.Direction.Up))
                {
                    Debug.Log("Moving up");
                }
                else if (Input.GetAxisRaw("Vertical") < 0 && myCharacter.CheckMove(LocalCharacter.Direction.Down))
                {
                    Debug.Log("Moving down");
                }
            }
        }
    }
}
