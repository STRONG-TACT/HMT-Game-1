using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

#if DEPRECATED
public class PinUIHandler : MonoBehaviour
{
    public int playerPinnedIndex; // Character that made this pin
    public int IconType; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: location
    public Sprite[] pinIcons = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: location

    private RectTransform rectTransform;
    private Vector3 pingPosition; // 3D pin position
    private Transform Player;

    private float VisionDistance;
    private float pinDistance;

    private GameData gameData;
    private Vector3 cameraOffset;

    private void Start()
    {
        cameraOffset = GameObject.FindObjectOfType<CameraManager>().cameraOffset;
    }

    public void SetUp(Vector3 pingPosition, int iconType, int player)
    {
        gameData = GameObject.FindObjectOfType<GameData>();
        playerPinnedIndex = player;
        IconType = iconType;
        this.pingPosition = pingPosition;
        rectTransform = transform.GetComponent<RectTransform>();
        Player = GameManager.Instance.mainCharacter.transform;

        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            VisionDistance = (gameData.tileSize + gameData.tileGapLength) * Mathf.Sqrt(2) + 0.5f;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            VisionDistance = (gameData.tileSize + gameData.tileGapLength) * 2 * Mathf.Sqrt(2) + 0.5f; ;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 3)
        {
            VisionDistance = (gameData.tileSize + gameData.tileGapLength) * 3 * Mathf.Sqrt(2) + 0.5f;
        }
        //Debug.Log("Setup: " + pingPosition + " Character: " + Character);
    }

    private void Update()
    {
        pinDistance = Vector3.Distance(Player.position, this.pingPosition);
        //Debug.Log(pinDistance);
        if (pinDistance > VisionDistance) // if pin position is out of visable area, show 2D pinIcon
        {
            if (PinningSystem.pinViewEnable[playerPinnedIndex]) // if overlap with other, pinViewEnabled = true
            {
                this.GetComponent<Image>().sprite = pinIcons[IconType]; // show specific pin icon
            }
            else  // if not overlap with other
            {
                this.GetComponent<Image>().sprite = pinIcons[4]; //show location pin icon
            }
            this.GetComponent<Image>().enabled = true;

            Vector3 cameraViewPos = Camera.main.transform.position - cameraOffset;
            Vector3 fromPosition = new Vector3(cameraViewPos.x, cameraViewPos.z, 0);
            Vector3 dir = (new Vector3(pingPosition.x, pingPosition.z, 0) - fromPosition).normalized;
            //Vector3 fromPosition = new Vector3(Character.position.x, Character.position.z, 0);
            //Vector3 dir = (new Vector3(pingPosition.x, pingPosition.z, 0) - fromPosition).normalized;
            float uiRadius = 250f;
            rectTransform.anchoredPosition = dir * uiRadius;
        }
        else
        {
            this.GetComponent<Image>().enabled = false;
        }
    }
}
#endif