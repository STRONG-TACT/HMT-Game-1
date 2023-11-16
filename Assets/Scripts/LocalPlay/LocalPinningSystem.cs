using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalPinningSystem : MonoBehaviour
{
    
    private Camera mainCamera;
    private Ray ray;
    private RaycastHit hit;
    private LocalTile tileScript;
    private GameObject tile;

    public GameObject pinWheel;
    private Vector3 pinPosition = new Vector3(0,0,0);
    public Sprite[] pinWheelBtnImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    public Sprite[] pinWheelBtnPressedImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    private bool isPinned;
    public Vector3 ui_offset = new Vector3(0,0.3f,0);
    public Vector3 pin_icon_offset = new Vector3(0.3f, 0.3f, 0.3f);
    // stores a list of 
    public List<LocalPin> pinList = new List<LocalPin>();

    // 3D pin
    public GameObject unknownPinPrefab;
    public GameObject dangerPinPrefab;
    public GameObject assistPinPrefab;
    public GameObject omwPinPrefab;


    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        pinWheel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isPinned)
        {
            pinPosition = mainCamera.WorldToScreenPoint(tile.transform.position + ui_offset);
            pinWheel.transform.position = pinPosition;
        }
        if (Input.GetMouseButtonDown(0) && LocalGameManager.Instance.gameStatus == LocalGameManager.GameStatus.Player_Pinning)
        {
            Debug.Log("mouse clicked at " + Input.mousePosition);

            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!isPinned)
            {
                if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Ground")))  // if raycast on ground
                {
                    tile = hit.transform.gameObject;
                    tileScript = tile.GetComponent<LocalTile>();
                    Debug.LogFormat("tile location: {0}, {1}", tileScript.row, tileScript.col);
                    Debug.Log(tileScript.row);
                    Debug.Log(tileScript.col);
                    Debug.LogFormat("Mouse hit object {0}", hit.transform.gameObject.name);
                    pinWheel.transform.gameObject.SetActive(true);
                    isPinned = true;
                }
            }
        }
    }

    public void ClearCurrentTurnPins() {
        foreach (LocalPin pin in pinList) {
            Destroy(pin.gameObject);
        }       
        pinList = new List<LocalPin>();
    }

    /* PinWheel choice button clicked ---------------- */
    public void Cancel()
    {
        //pinWheel.transform.GetChild(4).GetComponent<Image>().sprite = pinWheelBtnPressedImg[4];
        pinWheel.SetActive(false);
        isPinned = false;
    }
    public void Danger()
    {
        //pinWheel.transform.GetChild(0).GetComponent<Image>().sprite = pinWheelBtnPressedImg[0];
        AddPin(dangerPinPrefab, 0);
        LocalGameManager.Instance.newPlayerPin();
    }
    public void Assist()
    {
        //pinWheel.transform.GetChild(1).GetComponent<Image>().sprite = pinWheelBtnPressedImg[1];
        AddPin(assistPinPrefab, 1);
        LocalGameManager.Instance.newPlayerPin();
    }
    public void OMW()
    {
        //pinWheel.transform.GetChild(2).GetComponent<Image>().sprite = pinWheelBtnPressedImg[2];
        AddPin(omwPinPrefab, 2);
        LocalGameManager.Instance.newPlayerPin();
    }
    public void Unknown()
    {
        //pinWheel.transform.GetChild(3).GetComponent<Image>().sprite = pinWheelBtnPressedImg[3];
        AddPin(unknownPinPrefab, 3);
        LocalGameManager.Instance.newPlayerPin();
    }

    public void AddPin(GameObject pinPrefab, int iconType) {
        GameObject pinObj;
        pinObj = Instantiate(pinPrefab, tile.transform.position + pin_icon_offset, Quaternion.Euler(0, 180, 0));
        pinObj.transform.SetParent(tile.transform);
        LocalPin pin = pinObj.GetComponent<LocalPin>();
        pinList.Add(pin);
        tileScript.pinList.Add(pinObj.GetComponent<LocalPin>());
        pin.locationTile = tileScript;
        pin.placingCharacter = LocalGameManager.Instance.player.myCharacter;
        pinObj.transform.localScale = new Vector3(4f, 4f, 4f);
        //PinWindow.Instance.AddPing(new Vector3(hit.transform.position.x, 0, hit.transform.position.z), 1);
        //pinUIObj = AddPinUI(pinPosition, iconType, PhotonNetwork.LocalPlayer.ActorNumber - 1); // Actor targetValues starts from 1, List index start from 0
        //AddPinToList(pinObj.GetPhotonView().ViewID, pinUIObj.GetPhotonView().ViewID);
        Cancel();
    }

}
