using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; } = null;
    public static Camera MainCamera;
    public GameObject cameraPivot;
    public GameData gameData;
    public float cameraMoveSpeed = 0.06f;
    public float zoomSensitivity = 0.8f;


    private Transform targetCharacter;
    public Vector3 cameraOffset;


    public bool cameraCentered;


    float lerpDuration = 1.0f;
    float timer;

    private void Awake()
    {
        Instance = this;
    }

    IEnumerator Start()
    {
        MainCamera = Camera.main;
        cameraCentered = true;

        yield return new WaitForEndOfFrame();
        targetCharacter = GameManager.Instance.localChar.transform;
        cameraOffset = MainCamera.transform.position - targetCharacter.transform.position;
    }

    private void Update()
    {

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            cameraPivot.transform.position += new Vector3(0f, 1 * cameraMoveSpeed, 0f);
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            cameraPivot.transform.position += new Vector3(0f, -1 * cameraMoveSpeed, 0f);
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            cameraPivot.transform.position += new Vector3(-1 * cameraMoveSpeed / 2, 0f, -1 * cameraMoveSpeed / 2);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
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
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Home) || Input.GetKey(KeyCode.F))
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
        while (Time.time - startTime < GameManager.Instance.excecutionStepTime)
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
        targetCharacter = GameManager.Instance.inSceneCharacters[id].transform;
        cameraCentered = false;
        RecenterCamera();
    }
}
