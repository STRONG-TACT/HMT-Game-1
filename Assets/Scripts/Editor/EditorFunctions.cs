using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorFunctions : MonoBehaviour {

    [MenuItem("CONTEXT/Transform/Center Parent")]
    public static void CenterParent(MenuCommand command) {
        
        Transform target = (Transform)command.context;
        Vector3 offset = target.localPosition;
        Debug.LogFormat("Centering {0} with offset {1}", target.name, offset);
        target.localPosition = Vector3.zero;
        foreach (Transform t in target) {
            t.localPosition += offset;
        }
    }


}
