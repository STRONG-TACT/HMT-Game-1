using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DetectWalls : MonoBehaviour
{
    [HideInInspector] public GameObject player;
    private int dir;
    PhotonView view;

    // Start is called before the first frame update
    void Awake()
    {
        view = this.transform.parent.parent.GetComponent<PhotonView>();
    }
    void Start()
    {
        if (this.name == "left")
        {
            dir = 0;
        }
        else if (this.name == "right")
        {
            dir = 1;
        }
        else if (this.name == "front")
        {
            dir = 2;
        }
        else if (this.name == "back")
        {
            dir = 3;
        }
        player = GameManager.instance.mainPlayer;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider col)
    {
        if (view.IsMine && col.CompareTag("Walls"))
        {
            //Debug.Log("Triggered walls " + dir);
            player.GetComponent<Player>().movable[dir] = false;
        }
    }
    void OnTriggerExit(Collider col)
    {
        if (view.IsMine && col.CompareTag("Walls"))
        {
            Debug.Log("Triggered walls Exit " + dir);
            player.GetComponent<Player>().movable[dir] = true;
        }
    }

}
