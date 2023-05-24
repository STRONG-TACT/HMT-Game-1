using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

#if DEPRECATED
public class SpawnPlayer : MonoBehaviour
{
    public GameObject DwarfPlayer;
    public GameObject GiantPlayer;
    public GameObject HumanPlayer;

    //[HideInInspector] public Character newPlayer;

    private Vector3 dwarfIniPosistion;
    private Vector3 giantIniPosistion;
    private Vector3 humanIniPosistion;

    private GameData gameData;

    private void Start()
    {
        //Debug.Log(PhotonNetwork.LocalPlayer.ActorNumber);
        gameData = FindObjectOfType<GameData>();

        // Initialize each character's position
        dwarfIniPosistion = GameObject.Find("DwarfIniPos").transform.position;
        giantIniPosistion = GameObject.Find("GiantIniPos").transform.position;
        humanIniPosistion = GameObject.Find("HumanIniPos").transform.position;

        // Spawn Character
        Character newPlayer =  SpawnPlayerPrefab();

        // Call PunRPC for adding character's Photon viewID to the List
        GameManager.Instance.CallAddPlayerID(PhotonNetwork.LocalPlayer.ActorNumber, newPlayer.gameObject.GetPhotonView().ViewID);

        // Set mainplayer to the game
        GameManager.Instance.mainPlayer = newPlayer;

        // Set Mask on/off
        SetMask();
    }

    private Character SpawnPlayerPrefab()
    {

        GameData.CharacterConfig config = gameData.characterConfigs[PhotonNetwork.LocalPlayer.ActorNumber-1];
        Character newPlayer;
        GameObject instantiatedPrefab = null;
        switch (config.type) {
            case GameData.CharacterType.Dwarf:
                instantiatedPrefab = PhotonNetwork.Instantiate(DwarfPlayer.name, dwarfIniPosistion, Quaternion.identity);
                break;
            case GameData.CharacterType.Giant:
                instantiatedPrefab = PhotonNetwork.Instantiate(GiantPlayer.name, giantIniPosistion, Quaternion.identity);
                break;
            case GameData.CharacterType.Human:
                instantiatedPrefab = PhotonNetwork.Instantiate(HumanPlayer.name, humanIniPosistion, Quaternion.identity);
                break;
        }
        newPlayer = instantiatedPrefab.GetComponent<Character>();
        newPlayer.config = config;
        return newPlayer;
/*

        if (PhotonNetwork.LocalPlayer.ActorNumber == 1) // First person that join the game is Dwarf Character
        {
            newPlayer = PhotonNetwork.Instantiate(DwarfPlayer.name, dwarfIniPosistion, Quaternion.identity);
            newPlayer.GetComponent<Character>().config = gameData.dwarfSettings;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 2) // Second person that join the game is Giant Character
        {
            newPlayer = PhotonNetwork.Instantiate(GiantPlayer.name, giantIniPosistion, Quaternion.identity);
            newPlayer.GetComponent<Character>().config = gameData.giantSettings;
        }
        else if (PhotonNetwork.LocalPlayer.ActorNumber == 3) // Third person that join the game is Human Character
        {
            newPlayer = PhotonNetwork.Instantiate(HumanPlayer.name, humanIniPosistion, Quaternion.identity);
            newPlayer.GetComponent<Character>().config = gameData.humanSettings;
        }*/
    }

    private void SetMask()
    {
        if (gameData.maskOn)
        {
            GameManager.Instance.mainPlayer.transform.GetChild(3).gameObject.SetActive(true); //open vision Mask
        }
        GameManager.Instance.mainPlayer.transform.GetChild(6).gameObject.SetActive(false); //close shared Mask
    }
}
#endif