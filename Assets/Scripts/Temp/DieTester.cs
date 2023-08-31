using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieTester : MonoBehaviour
{

    DiceRoll roller;
    bool siming;
    int simCount = 100;
    Dictionary<int ,int > rollCounts = new Dictionary<int, int>();
    public int[] faces = new int[6];


    // Start is called before the first frame update
    void Start()
    {
        roller = FindObjectOfType<DiceRoll>();
        rollCounts = new Dictionary<int, int>();
        faces = new int[] { 1, 2, 3, 4, 5, 6 };
        foreach (int face in faces) {
            rollCounts[face] = 0;
        }
    }


    private void OnGUI() {
        GUILayout.BeginArea(new Rect(0, 0, Screen.width / 4, Screen.height));

        GUILayout.BeginVertical();


        GUILayout.BeginHorizontal();
        GUILayout.Label("Die State:");
        GUI.enabled = false;
        GUILayout.TextField(roller.dieState.ToString());
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Die Face:");
        GUI.enabled = false;
        GUILayout.TextField(roller.GetFaceValue().ToString());
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUI.enabled = !siming;
        GUILayout.Label("Faces");
        bool changes = false;
        for (int i = 0; i < faces.Length; i++) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Face " + i);
            string temp = GUILayout.TextField(faces[i].ToString());
            if (temp != faces[i].ToString() && int.TryParse(temp, out int result) && result > 0 && result <= 6) {
                faces[i] = result;
                changes = true;
            }
            GUILayout.EndHorizontal();
        }
        if (changes) {
            roller.ConfigureDie(faces);
            rollCounts = new Dictionary<int, int>();
            foreach (int face in faces) {
                rollCounts[face] = 0;
            }
        }



        GUILayout.BeginHorizontal();
        GUI.enabled = roller.dieState == DiceRoll.DieState.Idle;
        if (GUILayout.Button("Roll")) {
            roller.Roll();
        }
        GUI.enabled = roller.dieState == DiceRoll.DieState.Stopped;
        if (GUILayout.Button("Reset")) {
            roller.ResetDie();
        }
        GUI.enabled = !siming;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Sim Count");
        string temp2 = GUILayout.TextField(simCount.ToString());
        if (temp2 != simCount.ToString() && int.TryParse(temp2, out int result2) && result2 > 0) {
            simCount = result2;
        }
        if (GUILayout.Button("Simulate")) {
            StartCoroutine(RunSim(simCount));
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("Sim Counts");
        foreach (int face in faces) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Face " + face);
            GUI.enabled = false;
            GUILayout.TextField(rollCounts[face].ToString());
            GUI.enabled = !siming;
            GUILayout.EndHorizontal();
        }



        GUILayout.EndVertical();

        GUI.enabled = true;
        GUILayout.EndArea();
    }

    IEnumerator RunSim(int count) {
        rollCounts = new Dictionary<int, int>();
        siming = true;
        Time.timeScale = 3;
        foreach (int face in faces) {
            rollCounts[face] = 0;
        }
        roller.ResetDie();
        for (int i = 0; i < count; i++) {
            roller.Roll();
            while (roller.dieState != DiceRoll.DieState.Stopped) {
                yield return null;
            }
            rollCounts[roller.GetFaceValue()]++;
            roller.ResetDie();
        }
        Time.timeScale = 1;
        siming = false;
        yield break;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
