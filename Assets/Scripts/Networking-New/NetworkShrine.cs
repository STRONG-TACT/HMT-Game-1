using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class NetworkShrine : MonoBehaviour
{
    public CharacterConfig.CharacterType CharacterType;
    public bool Reached { get; private set; } = true;

    Transform shrineStone;

    void Start()
    {
        shrineStone = transform.Find("ShrineStone");
    }

    public bool CheckShrineType(NetworkCharacter character) {
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
