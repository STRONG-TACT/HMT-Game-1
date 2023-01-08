using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class SpawnPlayer : MonoBehaviour
{
    public GameObject DwarfPlayer;
    public GameObject GiantPlayer;
    public GameObject HumanPlayer;

    [HideInInspector] public GameObject newPlayer;

    private Vector3 dwarfIniPosistion;
    private Vector3 giantIniPosistion;
    private Vector3 humanIniPosistion;

    private GameData gameData;

    private void Start()
    {
        //Debug.Log(PhotonNetwork.LocalPlayer.ActorNumber);
        gameData = FindObjectOfType<GameData>();

        // Initialize each player's position
        dwarfIniPosistion = GameObject.Find("DwarfIniPos").transform.position;
        giantIniPosistion = GameObject.Find("GiantIniPos").transform.position;
        humanIniPosistion = GameObject.Find("HumanIniPos").transform.position;

        // Spawn Player
        SpawnPlayerPrefab();

        // Call PunRPC for adding player's Photon viewID to the List
        GameManager.instance.CallAddPlayerID(PhotonNetwork.LocalPlayer.ActorNumber, newPlayer.GetPhotonView().ViewID);

        // Set mainplayer to the game
        GameManager.instance.mainPlayer = newPlayer;

        // Set Mask on/off
        SetMask();
    }

    private void SpawnPlayerPrefab()
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1) // First person that join the game is Dwarf Player
        {
            newPlayer = PhotonNetwork.Instantiate(DwarfPlayer.name, dwarfIniPosistion, Quaternion.identity);
            newPlayer.GetComponent<Player>().config = gameData.dwarfSettings;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 2) // Second person that join the game is Giant Player
        {
            newPlayer = PhotonNetwork.Instantiate(GiantPlayer.name, giantIniPosistion, Quaternion.identity);
            newPlayer.GetComponent<Player>().config = gameData.giantSettings;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 3) // Third person that join the game is Human Player
        {
            newPlayer = PhotonNetwork.Instantiate(HumanPlayer.name, humanIniPosistion, Quaternion.identity);
            newPlayer.GetComponent<Player>().config = gameData.humanSettings;
        }
    }

    private void SetMask()
    {
        if (gameData.maskOn)
        {
            GameManager.instance.mainPlayer.transform.GetChild(3).gameObject.SetActive(true); //open vision Mask
        }
        GameManager.instance.mainPlayer.transform.GetChild(6).gameObject.SetActive(false); //close shared Mask
    }
}
