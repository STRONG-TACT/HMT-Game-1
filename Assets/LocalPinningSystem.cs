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

    private GameObject pinWheel;
    private Vector3 pinPosition = new Vector3(0,0,0);
    public Sprite[] pinWheelBtnImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    public Sprite[] pinWheelBtnPressedImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    private bool isPinned;
    public Vector3 ui_offset = new Vector3(0,0.3f,0);
    public Vector3 pin_icon_offset = new Vector3(0.3f, 0.3f, 0.3f);
    // stores a list of 
    public List<GameObject> pinList = new List<GameObject>();

    // 3D pin
    public GameObject unknownPinPrefab;
    public GameObject dangerPinPrefab;
    public GameObject assistPinPrefab;
    public GameObject omwPinPrefab;


    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        pinWheel = GameObject.Find("PinWheelUI");
        pinWheel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.C))
        {
            clearPins();
        }
        if (isPinned)
        {
            pinPosition = mainCamera.WorldToScreenPoint(tile.transform.position + ui_offset);
            pinWheel.transform.position = pinPosition;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("mouse clicked at " + Input.mousePosition);

            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!isPinned)
            {
                if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Ground")))  // if raycast on ground
                {
                    Vector3 pinPosition = new Vector3(hit.transform.position.x, 1f, hit.transform.position.z);
                    tile = hit.transform.gameObject;
                    tileScript = tile.GetComponent<LocalTile>();
                    Debug.LogFormat("tile location: ", tileScript.row, tileScript.col);
                    Debug.Log(tileScript.row);
                    Debug.Log(tileScript.col);
                    Debug.LogFormat("Mouse hit object {0}", hit.transform.gameObject.name);
                    pinWheel.transform.gameObject.SetActive(true);
                    isPinned = true;
                }
            }
        }
    }

    public void clearPins()
    {
        for (int i=0; i<pinList.Count; i++)
        {
            Destroy(pinList[i]);
        }
        pinList = new List<GameObject>();
    }

    /* PinWheel choice button clicked ---------------- */
    public void Cancel()
    {
        pinWheel.transform.GetChild(4).GetComponent<Image>().sprite = pinWheelBtnPressedImg[4];
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
        pinObj = Instantiate(pinPrefab, tile.transform.position + pin_icon_offset, Quaternion.Euler(0, 180, 0));
        pinObj.transform.SetParent(tile.transform);
        pinList.Add(pinObj);
        pinObj.transform.localScale = new Vector3(4f, 4f, 4f);
        //PinWindow.Instance.AddPing(new Vector3(hit.transform.position.x, 0, hit.transform.position.z), 1);
        //pinUIObj = AddPinUI(pinPosition, iconType, PhotonNetwork.LocalPlayer.ActorNumber - 1); // Actor targetValues starts from 1, List index start from 0
        //AddPinToList(pinObj.GetPhotonView().ViewID, pinUIObj.GetPhotonView().ViewID);
        Cancel();
    }

}
