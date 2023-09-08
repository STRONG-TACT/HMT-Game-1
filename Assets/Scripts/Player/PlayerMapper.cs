using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Assigns Characters to the seats in Photon
/// is set to Don't Destroy on Load to make sure the information persists.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerMapper : MonoBehaviour {
    public static PlayerMapper Instance { get; private set; }
    
    public enum AssignmentMode {
        Static,
        Random,
        Choice
    }

    public AssignmentMode mode = AssignmentMode.Static;
    public int[] StaticCharacterMapping = new int[3];
    PhotonView photonView;

    public GameObject playerPrefab;

    public bool Inititialized {
        get { return characterMapping.Count == 3; }
    }

    /// <summary>
    /// The Index of the Local Player's Character in the GameData.inSceneCharacters array
    /// </summary>
    public int LocalCharacterNumber { get ; private set; }
    /// <summary>
    /// The PhotonID of the Local Player's Player Object
    /// </summary>
    public int LocalPlayerNumber { get { return PhotonNetwork.LocalPlayer.ActorNumber; } }

    private Dictionary<int, int> playerIDMapping = new Dictionary<int, int>();
    private Dictionary<int, int> characterMapping = new Dictionary<int, int>();

    public Dictionary<int, int> CharacterMapping {
        get {
            return characterMapping;
        }
    }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Debug.LogWarning("Duplicate Singleton Instance of PlayerMapper. Destroying this one.");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start() {
        if(StaticCharacterMapping.Length != 3) {
            Debug.LogError("StaticCharacterMapping must only be 3 elements!");
        }

        photonView = GetComponent<PhotonView>();
        playerIDMapping = new Dictionary<int, int>();
        characterMapping = new Dictionary<int, int>();

        GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);
        CallAddPlayerID(PhotonNetwork.LocalPlayer.ActorNumber, newPlayer.GetPhotonView().ViewID);
    }

    
    public void CallAddPlayerID(int playerNum, int photonId) {
        photonView.RPC("AddPlayerID", RpcTarget.All, playerNum, photonId);
    }

    [PunRPC]
    public void AddPlayerID(int playerNum, int photonId) {
        //Debug.LogFormat("[RPC Recieve] AddPlayerId, playerNum:{0}, photonId:{1}", playerNum, photonId);
        playerIDMapping[playerNum] = photonId;
        if(playerIDMapping.Count == 3 && PhotonNetwork.IsMasterClient) {
            CreateCharacterAssignment();
        }
    }

  
    public void CallAssignCharacter(int playerNum, int characterID) {
        photonView.RPC("AssignCharacter", RpcTarget.All, playerNum, characterID);
    }

    [PunRPC]
    public void AssignCharacter(int playerNum, int characterID) {
        //Debug.LogFormat("[RPC Recieve] AssignCharacter, playerNum:{0}, characterID:{1}", playerNum, characterID);
        characterMapping[playerNum] = characterID;
        if (playerNum == PhotonNetwork.LocalPlayer.ActorNumber) {
            Debug.LogFormat("Actor Number {0} adopting character {1}", PhotonNetwork.LocalPlayer.ActorNumber, characterID);
            LocalCharacterNumber = characterID;
        }
    }

    private void CreateCharacterAssignment() {
        switch(mode) {
            case AssignmentMode.Static:
                List<int> playerIds = playerIDMapping.Keys.ToList();
                for(int i = 0; i < StaticCharacterMapping.Length; i++) {
                    CallAssignCharacter(playerIds[i], StaticCharacterMapping[i]);
                }
                break;
            case AssignmentMode.Random:
                //shuffle StaticCharacterMapping
                StaticCharacterMapping = StaticCharacterMapping.OrderBy(x => Random.value).ToArray();
                goto case AssignmentMode.Static;
            case AssignmentMode.Choice:
                goto case AssignmentMode.Static;
            default:
                goto case AssignmentMode.Static;
        }
    }

    public int GetCharacterFromPlayer(int playerNum) {
        if(!characterMapping.ContainsKey(playerNum)) {
            Debug.LogErrorFormat("PlayerMapper: Player {0} could not find character assignment defaulting to 0!", playerNum);
            return -1;
        }
        return characterMapping[playerNum];
    }

    public int GetPlayerIdFromCharacterId(int characterNum) {
        return characterMapping.FirstOrDefault(x => x.Value == characterNum).Key;
    }

    public int GetPlayerIdFromCharacter(Character character) {
        for(int i = 0; i < GameManager.Instance.gameData.inSceneCharacters.Length; i++) {
            if (GameManager.Instance.gameData.inSceneCharacters[i] == character) {
                return GetPlayerIdFromCharacterId(i);
            }
        }
        return -1;
    }
}
