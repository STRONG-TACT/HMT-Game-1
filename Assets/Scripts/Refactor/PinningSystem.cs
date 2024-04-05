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
    private GameObject tile;

    public GameObject pinWheel;
    private Vector3 pinPosition = new Vector3(0,0,0);
    //public Sprite[] pinWheelBtnImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    //public Sprite[] pinWheelBtnPressedImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    private bool isPinned;
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
    }
    
    private void Start()
    {
        if (S) Destroy(this);
        else S = this;
        
        mainCamera = Camera.main;
        pinWheel.SetActive(false);
    }

    private void Update()
    {
        if (IntegratedGameManager.S.localChar.ReadyForNextPhase) return;
        
        if (isPinned)
        {
            pinPosition = mainCamera.WorldToScreenPoint(tile.transform.position + ui_offset);
            pinWheel.transform.position = pinPosition;
        }
        
        if (Input.GetMouseButtonDown(0) && IntegratedGameManager.S.gameStatus == GameStatus.Player_Pinning)
        {
            if (EventSystem.current.IsPointerOverGameObject()) {
                Debug.Log("mouse clicked on UI");
                return;
            }
            Debug.Log("mouse clicked at " + Input.mousePosition);

            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (!isPinned)
            {
                if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Ground")))  // if raycast on ground
                {
                    tile = hit.transform.gameObject;
                    focusedTile = tile.GetComponent<Tile>();
                    Debug.LogFormat("tile location: {0}, {1}", focusedTile.row, focusedTile.col);
                    Debug.Log(focusedTile.row);
                    Debug.Log(focusedTile.col);
                    Debug.LogFormat("Mouse hit object {0}", hit.transform.gameObject.name);
                    pinWheel.transform.gameObject.SetActive(true);
                    isPinned = true;
                }
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
    public void Cancel()
    {
        pinWheel.SetActive(false);
        isPinned = false;
    }
    
    public void DropPin(int pinTypeIdx) {
        DropPinAt(pinTypeIdx, focusedTile.row, focusedTile.col, IntegratedGameManager.S.localChar.playerId);
    }

    public void DropPinAt(int pinTypeIdx, int row, int col, int charId) {
        if (IntegratedGameManager.S.isNetworkGame)
        {
            NetworkMiddleware.S.DropPinAtLocal(pinTypeIdx, row, col, charId);
        }

        else
        {
            AddPin(pinTypeIdx, row, col, charId);
        }
    }
    
    public void AddPin(int pinIdx, int tileRow, int tileCol, int charID)
    {
        GameObject pinObj;
        Tile targetTile = IntegratedMapGenerator.Instance.GetTileAt(tileRow, tileCol);

        pinObj = Instantiate(idx2PinPrefab[pinIdx], targetTile.GetNextPingLocation() + pin_icon_offset, Quaternion.Euler(0, 180, 0));
        pinObj.transform.SetParent(targetTile.transform);
        Pin pin = pinObj.GetComponent<Pin>();
        pinList.Add(pin);
        targetTile.pinList.Add(pin);
        pin.locationTile = targetTile;
        pin.SetPlacingCharacter(IntegratedGameManager.S.inSceneCharacters[charID]);
        pinObj.transform.localScale = new Vector3(4f, 4f, 4f);
        
        Cancel();
    }
}
