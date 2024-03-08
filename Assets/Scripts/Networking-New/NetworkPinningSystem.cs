using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using GameConstant;
using Photon.Pun;

public class NetworkPinningSystem : MonoBehaviourPunCallbacks
{
    public static NetworkPinningSystem S { get; private set; }
    private Camera mainCamera;
    private Ray ray;
    private RaycastHit hit;
    private NetworkTile focusedTile;
    private GameObject tile;

    public GameObject pinWheel;
    private Vector3 pinPosition = new Vector3(0,0,0);
    //public Sprite[] pinWheelBtnImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    //public Sprite[] pinWheelBtnPressedImg = new Sprite[5]; // 0: danger, 1: Assist, 2: OMW, 3: Unknown, 4: cancel; 
    private bool isPinned;
    public Vector3 ui_offset = new Vector3(0,0.3f,0);
    public Vector3 pin_icon_offset = new Vector3(0.3f, 0.3f, 0.3f);
    // stores a list of 
    public List<NetworkPin> pinList = new List<NetworkPin>();

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
        if (NetworkGameManager.S.localChar.ReadyForNextPhase) return;
        
        if (isPinned)
        {
            pinPosition = mainCamera.WorldToScreenPoint(tile.transform.position + ui_offset);
            pinWheel.transform.position = pinPosition;
        }
        
        if (Input.GetMouseButtonDown(0) && NetworkGameManager.S.gameStatus == GameStatus.Player_Pinning)
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
                    focusedTile = tile.GetComponent<NetworkTile>();
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
    
    //TODO: GameManager should call this when chars start to move
    public void ClearCurrentTurnPins() {
        foreach (NetworkPin pin in pinList) {
            if (pin != null) Destroy(pin.gameObject);
        }       
        pinList = new List<NetworkPin>();
    }
    
    // ========== Pin Wheel Buttons ==========
    public void Cancel()
    {
        pinWheel.SetActive(false);
        isPinned = false;
    }
    

    /*
     * These DropPin and DropPinAt functions generalize all of the specific functions below.
     * It also allows for openning up the set of pins in the future.
     * We shouldn't need the specific functions anymore.
     */ 
    public void DropPin(int pinTypeIdx) {
        DropPinAt(pinTypeIdx, focusedTile.row, focusedTile.col, NetworkMiddleware.S.myCharacterID);
    }

    public void DropPinAt(int pinTypeIdx, int row, int col, int charId) {
        photonView.RPC(
            "AddPin", 
            RpcTarget.All,
            pinTypeIdx, 
            row, col, 
            charId);
        NetworkGameManager.S.NewPlayerPin();
    }


    //public void Danger() {
    //    DangerAt(focusedTile.row, focusedTile.col, NetworkMiddleware.S.myCharacterID);
    //}

    //public void DangerAt(int row, int col, int charId) {
    //    photonView.RPC(
    //        "AddPin",
    //        RpcTarget.All,
    //        pinPrefab2Idx[dangerPinPrefab],
    //        row, col, charId);
    //    NetworkGameManager.S.NewPlayerPin();
    //}
    
    //public void Assist()
    //{
    //    photonView.RPC(
    //        "AddPin", 
    //        RpcTarget.All, 
    //        pinPrefab2Idx[assistPinPrefab], 
    //        focusedTile.row, focusedTile.col, 
    //        NetworkMiddleware.S.myCharacterID);
    //    NetworkGameManager.S.NewPlayerPin();
    //}

    
    //public void Unknown()
    //{
    //    photonView.RPC(
    //        "AddPin", 
    //        RpcTarget.All, 
    //        pinPrefab2Idx[unknownPinPrefab], 
    //        focusedTile.row, focusedTile.col, 
    //        NetworkMiddleware.S.myCharacterID);
    //    NetworkGameManager.S.NewPlayerPin();
    //}
    
    //public void OMW()
    //{
    //    photonView.RPC(
    //        "AddPin", 
    //        RpcTarget.All, 
    //        pinPrefab2Idx[omwPinPrefab], 
    //        focusedTile.row, focusedTile.col, 
    //        NetworkMiddleware.S.myCharacterID);
    //    NetworkGameManager.S.NewPlayerPin();
    //}

    [PunRPC]
    private void AddPin(int pinIdx, int tileRow, int tileCol, int charID)
    {
        GameObject pinObj;
        NetworkTile targetTile = NetworkMapGenerator.Instance.GetTileAt(tileRow, tileCol);

        pinObj = Instantiate(idx2PinPrefab[pinIdx], targetTile.transform.position + pin_icon_offset, Quaternion.Euler(0, 180, 0));
        pinObj.transform.SetParent(targetTile.transform);
        NetworkPin pin = pinObj.GetComponent<NetworkPin>();
        pinList.Add(pin);
        targetTile.pinList.Add(pin);
        pin.locationTile = targetTile;
        pin.placingCharacter = NetworkGameManager.S.inSceneCharacters[charID];
        pinObj.transform.localScale = new Vector3(4f, 4f, 4f);
        
        Cancel();
    }
}
