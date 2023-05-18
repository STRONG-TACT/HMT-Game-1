using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PinWindow : MonoBehaviour
{
    public static PinWindow instance;
    public GameObject PinIconPrefab;

    private PhotonView photonView;
    public GameObject PingUIObj;
    public Transform PingUITransform;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    public void AddPing(Vector3 position, int iconType)
    {
        //Transform pingUITransform = Instantiate(PinIconPrefab.transform, Instance.transform);
        PingUIObj = PhotonNetwork.Instantiate(PinIconPrefab.name, instance.transform.position, Quaternion.identity);
        CallPingUISetUp(position, PingUIObj.GetPhotonView().ViewID, iconType);
    }

    public void CallPingUISetUp(Vector3 position, int pingUIID, int iconType)
    {
        photonView.RPC("pingUISetUp", RpcTarget.All, position, pingUIID, iconType);

    }

/*    [PunRPC]
    public void pingUISetUp(Vector3 position, int pingUIID, int iconType)
    {
        Debug.Log("PingUISetUpCalled " + position + " " + "ID: "+ "pingUIID");
        PingUITransform = PhotonView.Find(pingUIID).transform;
        PingUITransform.parent = GameObject.Find("UI").transform;
        PingUITransform.GetComponent<PinUIHandler>().SetUp(position, iconType);
    }*/
}
