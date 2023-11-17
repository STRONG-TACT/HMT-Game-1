using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalShrine : MonoBehaviour {

    public CharacterConfig.CharacterType CharacterType;
    public bool Reached { get; private set; } = true;

    Transform shrineStone;

    // Start is called before the first frame update
    void Start()
    {
        shrineStone = transform.Find("ShrineStone");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool CheckShrineType(LocalCharacter character) {
        if (character.config.type == CharacterType) {
            Reached = true;
            shrineStone.gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    public JObject HMTStateRep() {
        return new JObject {
            { "type", "shrine" },
            { "character", CharacterType.ToString() },
            { "reached", Reached } };
    }
}
