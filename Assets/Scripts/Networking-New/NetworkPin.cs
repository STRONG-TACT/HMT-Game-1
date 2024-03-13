using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPin : MonoBehaviour
{
    private static Dictionary<string, int> PIN_IDS_BY_OBJ_KEY = new Dictionary<string, int>();
    private static string GetObjID(string objKey) {
        if (!PIN_IDS_BY_OBJ_KEY.ContainsKey(objKey)) {
            PIN_IDS_BY_OBJ_KEY[objKey] = 0;
        }
        PIN_IDS_BY_OBJ_KEY[objKey]++;
        return objKey + PIN_IDS_BY_OBJ_KEY[objKey];
    }

    public string ObjKey {
        get {
            return _objKey;
        }
        set {
            if (_objKey == null) {
                _objKey = value;
                HMTObjID = GetObjID(_objKey);
            }
        }
    }

    private string _objKey = null;
    private string HMTObjID = null;


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
        switch (pinType) {
            case PinType.Danger:
                ObjKey = "PA";
                break;
            case PinType.Assist:
                ObjKey = "PB";
                break;
            case PinType.Way:
                   ObjKey = "PC";
                break;
            case PinType.Unknown:
                ObjKey = "PD";
                break;
            default:
                ObjKey = "UNKNOWN PIN";
                break;
        }
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
            {"entityType", "Pin" },
            {"objKey", ObjKey },
            {"id", HMTObjID },
            {"placedBy", placingCharacter.CharacterId }
        };
    }
}
