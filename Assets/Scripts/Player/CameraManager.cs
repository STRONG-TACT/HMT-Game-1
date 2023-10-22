using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    public static Camera MainCamera;
    public GameObject cameraPivot;
    public GameData gameData;
    public float cameraMoveSpeed = 0.06f;
    public float zoomSensitivity = 0.8f;


    private Transform targetCharacter;
    public Vector3 cameraOffset;

    private GameObject mask;
    //private bool maskSet = false;
    private Transform visibleMask;

    public bool cameraCentered;


    float lerpDuration = 3f;
    float timer;

    private void Awake()
    {
        Instance = this;
    }
    IEnumerator Start()
    {
        MainCamera = Camera.main;
        cameraCentered = true;
        gameData = FindObjectOfType<GameData>();

        //targetCharacter = GameManager.Instance.mainCharacter.transform;
        while (!PlayerMapper.Instance.Inititialized)
        {
            yield return null;
        }

        targetCharacter = gameData.inSceneCharacters[PlayerMapper.Instance.LocalCharacterNumber].transform;

        if (gameData.maskOn == true)
        {
            mask = GameObject.Find("Mask");
            visibleMask = targetCharacter.Find("VisibleMask");
            visibleMask.gameObject.SetActive(true);
        }
        cameraOffset = MainCamera.transform.position - targetCharacter.transform.position;
        yield break;
    }

    private void Update()
    {
        if (gameData.maskOn == true)
        {
            /*
            if (GameManager.Instance.CurrentTurnPlayerNum == PhotonNetwork.LocalPlayer.ActorNumber && maskSet == false)
            {
                visibleMask = targetCharacter.Find("VisibleMask");
                visibleMask.gameObject.SetActive(true);
                maskSet = true;
            }

            else
            {
                visibleMask = targetCharacter.Find("VisibleMask");
                visibleMask.gameObject.SetActive(false);
            }
            */
        }

        if (Input.GetKey(KeyCode.I))
        {
            cameraPivot.transform.position += new Vector3(0f, 1 * cameraMoveSpeed, 0f);
        }
        if (Input.GetKey(KeyCode.K))
        {
            cameraPivot.transform.position += new Vector3(0f, -1 * cameraMoveSpeed, 0f);
        }
        if (Input.GetKey(KeyCode.J))
        {
            cameraPivot.transform.position += new Vector3(-1 * cameraMoveSpeed / 2, 0f, -1 * cameraMoveSpeed / 2);
        }
        if (Input.GetKey(KeyCode.L))
        {
            cameraPivot.transform.position += new Vector3(1 * cameraMoveSpeed / 2, 0f, 1 * cameraMoveSpeed / 2);
        }

    }

    // Update is called once per frame
    void LateUpdate()
    {

        //if (GameManager.Instance.CurrentTurnPlayerNum == PhotonNetwork.LocalPlayer.ActorNumber && cameraCentered)
        if (Input.GetKey(KeyCode.T))
        {
            cameraPivot.transform.position = targetCharacter.transform.position; //+ cameraOffset;
        }

        MainCamera.orthographicSize -= Input.GetAxis("Mouse ScrollWheel") * zoomSensitivity;
        if (MainCamera.orthographicSize <= 1)
            MainCamera.orthographicSize = 1;
        if (MainCamera.orthographicSize >= 10)
            MainCamera.orthographicSize = 10;


    }

    public void RecenterCamera()
    {
        if (!cameraCentered)
        {
            StartCoroutine(RecenterCameraCoroutine());
        }
    }

    IEnumerator RecenterCameraCoroutine()
    {
        Vector3 target = targetCharacter.transform.position + cameraOffset;
        float startTime = Time.time;
        while (Time.time - startTime < lerpDuration)
        {
            MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, target, (Time.time - startTime) / lerpDuration);
            yield return new WaitForEndOfFrame();
        }
        MainCamera.transform.position = target;
        cameraCentered = true;
        yield break;
    }

}
