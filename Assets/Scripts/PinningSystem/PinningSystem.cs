using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class PinningSystem : MonoBehaviour
{
    private PhotonView photonView;
    private Camera mainCamera;
    private Transform Player;
    private GameData gameData;

    private Ray ray;
    private RaycastHit hit;

    private float maxVisionDistance;
    private float pinDistance; // pin and character distance

    private Vector3 pinPosition;
    private bool isPinned;
    public static bool[] pinViewEnable = new bool[3];

    private Text alertMessage;


    // 3D pin
    public GameObject unknownPinPrefab;
    public GameObject dangerPinPrefab;
    public GameObject assistPinPrefab;
    public GameObject omwPinPrefab;

    // 2D Pin
    private GameObject pinWheel;
    private GameObject pinWindow;
    public GameObject pinIconPrefab;
    public Sprite[] pinWheelBtnImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    public Sprite[] pinWheelBtnPressedImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 

    List<Pin> pinList = new List<Pin>();
    private void Awake()
    {
        pinViewEnable[0] = false;
        pinViewEnable[1] = false;
        pinViewEnable[2] = false;
    }

    /*private LayerMask _myLayerMask = 6; -> not working */

    void Start()
    {
        gameData = GameObject.FindObjectOfType<GameData>();
        mainCamera = Camera.main;
        isPinned = false;
        pinWheel = GameObject.Find("PinWheelUI");
        pinWindow = GameObject.Find("PinWindow");
        alertMessage = GameObject.Find("UI").GetComponentsInChildren<Text>(true)[3];
        //isPinnable = false;
        photonView = GetComponent<PhotonView>();
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            maxVisionDistance = (gameData.tileSize + gameData.tileGapLength) * Mathf.Sqrt(2) + 0.5f;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 2)
        {
            maxVisionDistance = (gameData.tileSize + gameData.tileGapLength) * 2 * Mathf.Sqrt(2) + 0.5f;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 3)
        {
            maxVisionDistance = (gameData.tileSize + gameData.tileGapLength) * 3 * Mathf.Sqrt(2) + 0.5f;
        }
    }


    void Update()
    {
        Player = GameManager.Instance.mainPlayer.transform;
        if (Input.GetMouseButtonDown(0)){
            //Debug.Log("mouse clicked + " + Input.mousePosition);

            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!isPinned) // it pinwheel is unopened
            {
                if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Ground")))  // if raycast on ground
                {
                    pinPosition = new Vector3(hit.transform.position.x, 1f, hit.transform.position.z);
                    pinDistance = Vector3.Distance(Player.position, pinPosition);
                    if (pinDistance < maxVisionDistance)  // if Pin in vision area
                    {
                        pinWheel.transform.position = Input.mousePosition;
                        resetPinWheelImg();
                        pinWheel.transform.gameObject.SetActive(true);
                        isPinned = true;
                    }
                    else  // if Pin out of vision area
                    {
                        StartCoroutine("AlertMessage");
                    }
                }
                else // if raycast not on ground
                {
                    StartCoroutine("AlertMessage");
                }

            }
        }
    }

    /* PinWheel choice button clicked ---------------- */
    public void Cancel()
    {
        pinWheel.transform.GetChild(4).GetComponent<Image>().sprite= pinWheelBtnPressedImg[4];
        pinWheel.SetActive(false);
        isPinned = false;
    }
    public void Danger()
    {
        pinWheel.transform.GetChild(0).GetComponent<Image>().sprite = pinWheelBtnPressedImg[0];
        AddPin(dangerPinPrefab, 0);
    }
    public void Assist()
    {
        pinWheel.transform.GetChild(1).GetComponent<Image>().sprite = pinWheelBtnPressedImg[1];
        AddPin(assistPinPrefab, 1);
    }
    public void OMW()
    {
        pinWheel.transform.GetChild(2).GetComponent<Image>().sprite = pinWheelBtnPressedImg[2];
        AddPin(omwPinPrefab, 2);
    }
    public void Unknown()
    {
        pinWheel.transform.GetChild(4).GetComponent<Image>().sprite = pinWheelBtnPressedImg[3];
        AddPin(unknownPinPrefab, 3);
    }

    public void AddPin(GameObject pinPrefab, int iconType)
    {
        GameObject pinObj;
        GameObject pinUIObj;
        pinObj = PhotonNetwork.Instantiate(pinPrefab.name, pinPosition, Quaternion.Euler(0, 180, 0));
        //PinWindow.Instance.AddPing(new Vector3(hit.transform.position.x, 0, hit.transform.position.z), 1);
        pinUIObj = AddPinUI(pinPosition, iconType, PhotonNetwork.LocalPlayer.ActorNumber-1); // Actor targetValues starts from 1, List index start from 0
        AddPinToList(pinObj.GetPhotonView().ViewID, pinUIObj.GetPhotonView().ViewID);
        Cancel();
    }

    private void resetPinWheelImg()
    {
        for (int i = 0 ; i < 5; i++)
        {
            pinWheel.transform.GetChild(i).GetComponent<Image>().sprite = pinWheelBtnImg[i];
        }
    }
    /* --------------End PinWheel choice button clicked */

    // Alert messsage if not pinnable 
    private IEnumerator AlertMessage()
    {
        alertMessage.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        alertMessage.gameObject.SetActive(false);
    }

    // Pin amount control: Each character can only has 3 pins
    private void AddPinToList(int _pinObjId, int _pinUIId)
    {
        Pin newPin = new Pin();
        newPin.pinIdSetup(_pinObjId, _pinUIId);

        if (pinList.Count == 3) // Detroy the first Pin if there is 3 pins
        {
            CallDestroy(pinList[0].pinObjId);
            CallDestroy(pinList[0].pinUIId);
            pinList.RemoveAt(0); 
        }
        pinList.Add(newPin);
    }

    public void CallDestroy(int ObjId)
    {
        photonView.RPC("Destroy", RpcTarget.MasterClient, ObjId);
    }
 
    [PunRPC]
    public void Destroy(int ObjId)
    {
        PhotonNetwork.Destroy(PhotonView.Find(ObjId));
    }


    /* PinUI setUp ---------------- */
    public GameObject AddPinUI(Vector3 position, int iconType, int playerNum)
    {
        GameObject pinUIObj;
        pinUIObj = PhotonNetwork.Instantiate(pinIconPrefab.name, pinWindow.transform.position, Quaternion.identity);
        CallPingUISetUp(position, pinUIObj.GetPhotonView().ViewID, iconType, playerNum);
        return pinUIObj;
    }

    public void CallPingUISetUp(Vector3 position, int pingUIID, int iconType, int playerNum)
    {
        photonView.RPC("PingUISetUp", RpcTarget.All, position, pingUIID, iconType, playerNum);
    }

    [PunRPC]
    public void PingUISetUp(Vector3 position, int pingUIID, int iconType, int playerNum)  // use pin viewID to set up pin positions in other character's game
    {
        //Debug.Log("PingUISetUpCalled " + position + " " + "ID: " + "pingUIID");
        Transform pinUITransform;
        pinUITransform = PhotonView.Find(pingUIID).transform;
        pinUITransform.parent = GameObject.Find("UI").transform;
        pinUITransform.GetComponent<PinUIHandler>().SetUp(position, iconType, playerNum);
    }

    /* -------------- PinUI setUp */
}
