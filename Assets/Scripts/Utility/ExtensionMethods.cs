using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class ExtensionMethods {
    public static T TryGetDefault<T>(this JObject job, string key, T defaultValue) {
        return job.TryGetValue(key, out JToken token) ? token.Value<T>() : defaultValue;
    }
}
