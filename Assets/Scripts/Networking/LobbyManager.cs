using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public InputField roomInputField;
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    public Text roomName;

    public RoomItem roomItemPrefabs;
    List<RoomItem> roomItemsList = new List<RoomItem>();
    public Transform contentObject;

    public float timeBetweenUpdates = 1.5f;
    float nextUpdateTime;

    private int currentPageNum;
    private bool isInRoom;
    public GameObject lobbyUI;
    public GameObject roomUI;
    public GameObject tutorialUI;
    public GameObject tutorialPagesUI;
    public Sprite tutorialBgImg;
    public Sprite lobbyBgImg;
    public GameObject BgImg;
    public int TotalPages = 2;

    private void Start()
    {
        PhotonNetwork.JoinLobby();
        isInRoom = false;
    }

    public void OnClickCreate()
    {
        if (roomInputField.text.Length >= 1)
        {
            PhotonNetwork.CreateRoom(roomInputField.text, new RoomOptions() { MaxPlayers = 3});
        }
    }

    public override void OnJoinedRoom()
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        roomName.text = "Room: " + PhotonNetwork.CurrentRoom.Name;
        isInRoom = true;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (Time.time >= nextUpdateTime) 
        {
            UpdateRoomList(roomList);
            nextUpdateTime = Time.time + timeBetweenUpdates;
        }
    }

    void UpdateRoomList(List<RoomInfo> list)
    {
        foreach (RoomItem item in roomItemsList)
        {
            Destroy(item.gameObject);
        }
        roomItemsList.Clear();

        foreach (RoomInfo room in list)
        {
            RoomItem newRoom = Instantiate(roomItemPrefabs, contentObject);
            newRoom.SetRoomName(room.Name, room.PlayerCount);
            roomItemsList.Add(newRoom);
        }
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    // Tutorial Pages
    public void OpenTutorial()
    {
        BgImg.GetComponent<Image>().sprite = tutorialBgImg;
        currentPageNum = 0;
        lobbyUI.SetActive(false);
        roomUI.SetActive(false);
        tutorialUI.SetActive(true);
    }
    public void NextPage()
    {
        if (currentPageNum == TotalPages - 1) // End of tutorial
        {
            BgImg.GetComponent<Image>().sprite = lobbyBgImg;
            if (isInRoom) // back to room
            {
                roomUI.SetActive(true);
                tutorialUI.SetActive(false);
                tutorialPagesUI.transform.GetChild(currentPageNum).gameObject.SetActive(false);
                tutorialPagesUI.transform.GetChild(0).gameObject.SetActive(true);
            }
            else  // back to lobby
            {
                lobbyUI.SetActive(true);
                tutorialUI.SetActive(false);
                tutorialPagesUI.transform.GetChild(currentPageNum).gameObject.SetActive(false);
                tutorialPagesUI.transform.GetChild(0).gameObject.SetActive(true);
            }
        }
        else
        {
            tutorialPagesUI.transform.GetChild(currentPageNum + 1).gameObject.SetActive(true);
            tutorialPagesUI.transform.GetChild(currentPageNum).gameObject.SetActive(false);
            currentPageNum++;
        }
    }

    public void BackPage()
    {
        if (currentPageNum == 0) // First page of tutorial
        {
            BgImg.GetComponent<Image>().sprite = lobbyBgImg;
            if (isInRoom) 
            {
                roomUI.SetActive(true);
                tutorialUI.SetActive(false);
            }
            else
            {
                lobbyUI.SetActive(true);
                tutorialUI.SetActive(false);
            }
        }
        else
        {
            tutorialPagesUI.transform.GetChild(currentPageNum - 1).gameObject.SetActive(true);
            tutorialPagesUI.transform.GetChild(currentPageNum).gameObject.SetActive(false);
            currentPageNum--;
        }
    }
}
