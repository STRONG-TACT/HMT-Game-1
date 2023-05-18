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

    public int LocalCharacterNumber { get ; private set; }
    public int LocalPlayerNumber { get; private set; }
    public Character LocalCharacter { get; private set; }

    private Dictionary<int, int> playerIDMapping = new Dictionary<int, int>();
    private Dictionary<int, int> characterMapping = new Dictionary<int, int>();

    // Start is called before the first frame update
    void Start() {
        if(StaticCharacterMapping.Length != 3) {
            Debug.LogError("StaticCharacterMapping must only be 3 elements!");
        }

        if (Instance == null) {
            Instance = this;
        }
        else {
            Debug.LogWarning("Duplicate Singleton Instance of PlayerMapper. Destroying this one.");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
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
        characterMapping[playerNum] = characterID;
        if (playerNum == PhotonNetwork.LocalPlayer.ActorNumber) {
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
}
