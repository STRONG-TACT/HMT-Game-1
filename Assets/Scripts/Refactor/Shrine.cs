using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class Shrine : MonoBehaviour
{
    private static Dictionary<string, int> SHRINE_IDS_BY_OBJ_KEY = new Dictionary<string, int>();
    private static string GetObjID(string objKey) {
        if (!SHRINE_IDS_BY_OBJ_KEY.ContainsKey(objKey)) {
            SHRINE_IDS_BY_OBJ_KEY[objKey] = 0;
        }
        SHRINE_IDS_BY_OBJ_KEY[objKey]++;
        return objKey + SHRINE_IDS_BY_OBJ_KEY[objKey];
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

    public CharacterConfig.CharacterType CharacterType;
    public bool Reached { get; private set; } = true;

    Transform shrineStone;
    
    void Start()
    {
        shrineStone = transform.Find("ShrineStone");
    }

    public bool CheckShrineType(Character character) {
        if (character.config.type == CharacterType) {
            Reached = true;
            shrineStone.gameObject.SetActive(false);
            return true;
        }
        return false;
    }
    
    public void ReturnOrb()
    {
        shrineStone.gameObject.SetActive(true);
    }

    public JObject HMTStateRep() {
        return new JObject {
            {"entityType", "Shrine" },
            {"objKey",ObjKey },
            {"id", HMTObjID },
            { "character", CharacterType.ToString() },
            { "reached", Reached } };
    }
}