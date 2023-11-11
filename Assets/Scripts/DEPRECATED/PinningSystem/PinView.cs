using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

#if DEPRECATED
public class PinView : MonoBehaviour
{
    PhotonView view;
    //public bool isTriggered;
    public int playerId;

    private void Awake()
    {
        view = this.GetComponentInParent<PhotonView>();
    }
    void Start()
    {
        //isTriggered = false;
        //Need to spend some time figuring this one out
        //playerId = GameManager.Instance.playerIDs.IndexOf(this.transform.parent.gameObject.GetPhotonView().ViewID);
        playerId = PlayerMapper.Instance.LocalPlayerNumber;

    }

    private void OnTriggerStay(Collider col)
    {
        if (view.IsMine && col.CompareTag("VisableArea") )
        {
            //Debug.Log("Trigger Vislable area" + col.GetComponent<PinView>().playerId);
            PinningSystem.pinViewEnable[col.GetComponent<PinView>().playerId] = true;
        }
    }
    private void OnTriggerExit(Collider col)
    {
        if (view.IsMine && col.CompareTag("VisableArea"))
        {
            PinningSystem.pinViewEnable[col.GetComponent<PinView>().playerId] = false;
        }
    }
}
#endif