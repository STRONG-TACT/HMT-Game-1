using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkMiddleware : MonoBehaviourPunCallbacks
{
    // ======== Used During Room ========
    
    // Actor 2 character map
    // public Dictionary<int, int> actor2character;

    // random seed, set to be the same across the network
    // so that (hopefully) we don't need to sync random dice rolls separately
    public int randomSeed = -1;
    
    // referenced by game manager
    public int myCharacterID = -1;
    
    // ======== Used During Gameplay ========
    
    
    

    public static NetworkMiddleware S;

    private void Awake()
    {
        if (S) Destroy(this.gameObject);
        else
        {
            S = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void SetupMiddleware(int randomSeed_, int characterID_)
    {
        randomSeed = randomSeed_;
        myCharacterID = characterID_ - 1;
        Random.InitState(randomSeed);
        Debug.Log($"Player {characterID_} middleware setup with random seed {randomSeed}");
    }
}
