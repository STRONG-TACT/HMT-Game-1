using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCameraManager : MonoBehaviour
{
    public static NetworkCameraManager S { get; private set; }
    public static Camera MainCamera;
    public GameObject cameraPivot;
    public NetworkGameData gameData;
    public float cameraMoveSpeed = 0.06f;
    public float zoomSensitivity = 0.8f;


    private Transform targetCharacter;
    public Vector3 cameraOffset;


    public bool cameraCentered;


    float lerpDuration = 1.0f;
    float timer;

    private void Awake()
    {
        S = this;
    }

    void Start()
    {
        MainCamera = Camera.main;
        cameraCentered = true;

        targetCharacter = NetworkGameManager.S.localChar.transform;
        cameraOffset = MainCamera.transform.position - targetCharacter.transform.position;

    }

    private void Update()
    {

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
    public void centerCamera()
    {
        cameraPivot.transform.position = targetCharacter.transform.position;
    }

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
        Vector3 target = targetCharacter.transform.position;// + cameraOffset;
        float startTime = Time.time;
        while (Time.time - startTime < lerpDuration)
        {
            cameraPivot.transform.position = Vector3.Lerp(cameraPivot.transform.position, target, (Time.time - startTime) / lerpDuration);
            yield return new WaitForEndOfFrame();
        }
        cameraPivot.transform.position = target;
        cameraCentered = true;
        yield break;
    }

    public void ChangeTargetCharacter(int id)
    {
        targetCharacter = NetworkGameManager.S.inSceneCharacters[id].transform;
        cameraCentered = false;
        RecenterCamera();
    }
}
