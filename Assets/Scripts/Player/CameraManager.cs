using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    public static Camera MainCamera;
    public GameData gameData;


    private Transform targetPlayer;
    public Vector3 cameraOffset;

    public bool cameraCentered;

    float lerpDuration = 3f;
    float timer;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        MainCamera = Camera.main;
        cameraCentered = true;
        gameData = FindObjectOfType<GameData>();

        if (gameData.differentCameraView)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == 1) //Dwarf photonView
            {
                MainCamera.transform.position = gameData.cameraViews[0];
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 2) //Giant photonView
            {
                MainCamera.transform.position = gameData.cameraViews[1];
            }
            else if (PhotonNetwork.LocalPlayer.ActorNumber == 3) //Human photonView
            {
                MainCamera.transform.position = gameData.cameraViews[2];
            }
        }

        targetPlayer = GameManager.Instance.mainPlayer.transform;
        cameraOffset = MainCamera.transform.position - targetPlayer.transform.position;
    }

    private void Update()
    {
        if (GameManager.Instance.turn != PhotonNetwork.LocalPlayer.ActorNumber) // when not the character's turn, camera can move around
        {
            cameraCentered = false;
            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) == 1f)
            {
                MainCamera.transform.position += new Vector3(Input.GetAxisRaw("Horizontal") *  0.06f, 0f, 0f);
            }
            else if (Mathf.Abs(Input.GetAxisRaw("Vertical")) == 1f)
            {
                MainCamera.transform.position += new Vector3(0f, 0f, Input.GetAxisRaw("Vertical") * 0.06f);
            }
        }
        else if(!cameraCentered) // when character's turn, camera move back before the character starts moving
        {
            timer += Time.deltaTime;
            float t = timer / lerpDuration;
            t = t * t * (3f - 2f * t);
            Vector3 endPosition = targetPlayer.transform.position + cameraOffset;
            MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, endPosition, t);
            if (MainCamera.transform.position == endPosition) // when camera moved back
            {
                cameraCentered = true;
            }
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (GameManager.Instance.turn == PhotonNetwork.LocalPlayer.ActorNumber && cameraCentered)
        {
            MainCamera.transform.position = targetPlayer.transform.position + cameraOffset;
        }
    }

    public void RecenterCamera() {
        if(!cameraCentered) {
            StartCoroutine(RecenterCameraCoroutine());
        }
    }

    IEnumerator RecenterCameraCoroutine() {
        Vector3 target = targetPlayer.transform.position + cameraOffset;
        float startTime = Time.time;
        while(Time.time - startTime < lerpDuration) {
            MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, target, (Time.time - startTime) / lerpDuration);
            yield return new WaitForEndOfFrame();
        }
        MainCamera.transform.position = target;
        cameraCentered = true;
        yield break;
    }

}
