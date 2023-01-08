using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;
    public static Camera MainCamera;
    public GameData gameData;


    private Transform targetPlayer;
    public Vector3 cameraOffset;

    public bool cameraIsSet;

    float lerpDuration = 3f;
    float timer;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        MainCamera = Camera.main;
        cameraIsSet = true;
        gameData = FindObjectOfType<GameData>();

        if (gameData.differentCameraView)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1) //Dwarf view
            {
                MainCamera.transform.position = gameData.cameraViews[0];
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 2) //Giant view
            {
                MainCamera.transform.position = gameData.cameraViews[1];
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 3) //Human view
            {
                MainCamera.transform.position = gameData.cameraViews[2];
            }
        }

        targetPlayer = GameManager.instance.mainPlayer.transform;
        cameraOffset = MainCamera.transform.position - targetPlayer.transform.position;
    }

    private void Update()
    {
        if (GameManager.instance.turn != PhotonNetwork.LocalPlayer.ActorNumber) // when not the player's turn, camera can move around
        {
            cameraIsSet = false;
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
            {
                MainCamera.transform.position += new Vector3(Input.GetAxisRaw("Horizontal") *  0.06f, 0f, 0f);
            }
            else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
            {
                MainCamera.transform.position += new Vector3(0f, 0f, Input.GetAxisRaw("Vertical") * 0.06f);
            }
        }
        else if(!cameraIsSet) // when player's turn, camera move back before the player starts moving
        {
            timer += Time.deltaTime;
            float t = timer / lerpDuration;
            t = t * t * (3f - 2f * t);
            Vector3 endPosition = targetPlayer.transform.position + cameraOffset;
            MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, endPosition, t);
            if (MainCamera.transform.position == endPosition) // when camera moved back
            {
                cameraIsSet = true;
            }
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (GameManager.instance.turn == PhotonNetwork.LocalPlayer.ActorNumber && cameraIsSet)
        {
            MainCamera.transform.position = targetPlayer.transform.position + cameraOffset;
        }
    }
}
