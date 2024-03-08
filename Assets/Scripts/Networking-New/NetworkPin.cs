using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPin : MonoBehaviour
{
    public enum PinType {
        Danger,
        Assist,
        Way,
        Unknown
    }

    public PinType pinType;

    public bool animate;
    [HideInInspector]
    public NetworkTile locationTile;

    [HideInInspector]
    public NetworkCharacter placingCharacter;

    float rotationSpeed = 10f;
    float floatSpeed = 2f;
    Vector3 spawnPosition = new Vector3(0, 0, 0);


    // Start is called before the first frame update
    void Start() {
        spawnPosition = transform.position;
    }

    // Update is called once per frame
    void Update() {
        this.transform.rotation *= Quaternion.Euler(0, rotationSpeed * Time.deltaTime, 0);
        transform.position = spawnPosition + new Vector3(0, Mathf.Sin(Time.time * floatSpeed) * 0.1f, 0);
    }

    private void OnDestroy() {
        if (locationTile != null) {
            locationTile.pinList.Remove(this);
        }
    }

    public JObject HMTStateRep() {
        return new JObject {
            { "type", pinType.ToString()},
            {"placedBy", placingCharacter.CharacterId }
        };
    }
}
