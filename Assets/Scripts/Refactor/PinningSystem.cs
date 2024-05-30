using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using GameConstant;

public class PinningSystem : MonoBehaviour
{
    public static PinningSystem S { get; private set; }
    private Camera mainCamera;
    private Ray ray;
    private RaycastHit hit;
    private Tile focusedTile;
    //private GameObject tile;

    public GameObject pinWheel;
    private Vector3 pinPosition = new Vector3(0,0,0);
    //public Sprite[] pinWheelBtnImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    //public Sprite[] pinWheelBtnPressedImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    public bool PinUIUp { get; private set; }
    public Vector3 ui_offset = new Vector3(0,0.3f,0);
    public Vector3 pin_icon_offset = new Vector3(0.3f, 0.3f, 0.3f);
    // stores a list of 
    public List<Pin> pinList = new List<Pin>();

    public float pinOffsetSide = .3f;

    // 3D pin
    public GameObject unknownPinPrefab;
    public GameObject dangerPinPrefab;
    public GameObject assistPinPrefab;
    public GameObject omwPinPrefab;
    private Dictionary<int, GameObject> idx2PinPrefab;
    private Dictionary<GameObject, int> pinPrefab2Idx;
    
    private void Awake()
    {
        idx2PinPrefab = new Dictionary<int, GameObject>
        {
            {0, unknownPinPrefab},
            {1, dangerPinPrefab},
            {2, assistPinPrefab},
            {3, omwPinPrefab}
        };

        pinPrefab2Idx = new Dictionary<GameObject, int>
        {
            {unknownPinPrefab, 0},
            {dangerPinPrefab, 1},
            {assistPinPrefab, 2},
            {omwPinPrefab, 3}
        };
        
        if (S) Destroy(this);
        else S = this;
    }
    
    private void Start()
    {
        mainCamera = Camera.main;
        pinWheel.SetActive(false);
    }

    private void Update()
    {
        //Debug.LogFormat("GameManager Is null? {0}", IntegratedGameManager.S == null);
        //Debug.LogFormat("localChar Is null? {0}", IntegratedGameManager.S.localChar == null);
        if (IntegratedGameManager.S.localChar.ReadyForNextPhase || IntegratedGameManager.S.gameStatus != GameStatus.Player_Pinning) return;
        
        //if (PinUIUp)
        //{
        //    pinPosition = mainCamera.WorldToScreenPoint(tile.transform.position + ui_offset);
        //    pinWheel.transform.position = pinPosition;
        //}
        
        if (Input.GetMouseButtonDown(0)) {
            if (EventSystem.current.IsPointerOverGameObject()) {
                //Debug.Log("mouse clicked on UI");
                return;
            }
            //Debug.Log("mouse clicked at " + Input.mousePosition);

            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!PinUIUp) {
                // if raycast on ground
                if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Ground"))) { 
                    focusedTile = hit.transform.gameObject.GetComponent<Tile>();
                    //Debug.LogFormat("tile location: {0}, {1}", focusedTile.row, focusedTile.col);
                    Debug.Log(focusedTile.row);
                    Debug.Log(focusedTile.col);
                    //Debug.LogFormat("Mouse hit object {0}", hit.transform.gameObject.name);
                    ShowPinWheel();
                }
            }
            else {
                ClosePinWheel();
            }
        }
    }
    
    public void ClearCurrentTurnPins() {
        foreach (Pin pin in pinList) {
            if (pin != null) Destroy(pin.gameObject);
        }       
        pinList = new List<Pin>();
    }
    
    // ========== Pin Wheel Buttons ==========
    public void ShowPinWheel() {
        pinWheel.SetActive(true);
        pinPosition = mainCamera.WorldToScreenPoint(focusedTile.transform.position + ui_offset);
        pinWheel.transform.position = pinPosition;
        PinUIUp = true;
    }
    
    public void ClosePinWheel()
    {
        pinWheel.SetActive(false);
        PinUIUp = false;
    }
    
    public void DropPin(int pinTypeIdx) {
        NetworkMiddleware.S.CallDropPinAt(IntegratedGameManager.S.localChar.playerId, pinTypeIdx, focusedTile.row, focusedTile.col);
    }

    public void InstantiatePin(int charID, int pinIdx, int tileRow, int tileCol) {
        GameObject pinObj;
        Tile targetTile = IntegratedMapGenerator.Instance.GetTileAt(tileCol, tileRow);

        pinObj = Instantiate(idx2PinPrefab[pinIdx], targetTile.GetNextPingLocation() + pin_icon_offset, Quaternion.Euler(0, 180, 0));
        pinObj.transform.SetParent(targetTile.transform);
        Pin pin = pinObj.GetComponent<Pin>();
        pinList.Add(pin);
        targetTile.pinList.Add(pin);
        pin.locationTile = targetTile;
        pin.SetPlacingCharacter(IntegratedGameManager.S.inSceneCharacters[charID]);
        pinObj.transform.localScale = new Vector3(4f, 4f, 4f);
        
        ClosePinWheel();
    }

    public static int PinTypeToPinIdx(string pinType) {
        switch (pinType.ToLower()) {
            case "danger":
            case "a":
                return 1;
            case "assist":
            case "b":
                return 2;
            case "way":
            case "omw":
            case "c":
                return 3;
            case "unknown":
            case "question":
            case "d":
                return 0;
            default:
                return -1;
        }
    }

    public static string PinIndxToPinType(int idx) {
        switch (idx) {
            case 0:
                return "d";
            case 1:
                return "a";
            case 2:
                return "b";
            case 3:
                return "c";
            default:
                return "Unknown";
        }
    }
}
