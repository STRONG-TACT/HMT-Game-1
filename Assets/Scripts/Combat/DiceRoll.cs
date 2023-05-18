using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceRoll : MonoBehaviour {
    public enum DieState {
        Idle,
        Spinning,
        Rolling,
        Stopped
    }

    private Vector3 orignialPosition;
    new private Rigidbody rigidbody;
    public bool acceptInput = false;

    public Material[] faceMaterials;

    public DieState dieState;
    public float spinTime = 1.5f;

    private Transform[] faces;
    private int[] faceValues;

    void Awake() {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.useGravity = false;
        faces = new Transform[6];
        foreach (Transform child in transform) {
            if (child.name.StartsWith("Face")) {
                faces[int.Parse(child.name[4] + "")-1] = child;
            }
        }
        dieState = DieState.Idle;
        ConfigureDie(new int[] { 1, 2, 3, 4, 5, 6 });
    }

    public void ConfigureDie(int[] faceValues) {
        for (int i = 0; i < faceValues.Length; i++) {
            faces[i].GetComponent<MeshRenderer>().material = faceMaterials[faceValues[i] - 1];
        }
        this.faceValues = faceValues;
    }

    void OnEnable() {
        orignialPosition = this.transform.localPosition;
        rigidbody.useGravity = false;
        dieState = DieState.Idle;
    }
    void OnDisable() {
        ResetDie();
        dieState = DieState.Idle;
    }

    // Update is called once per frame
    void Update() {
        if (acceptInput && Input.GetKeyDown(KeyCode.Space) && dieState == DieState.Idle) {
            StartCoroutine(RollCoroutine());
        }
    }

    IEnumerator RollCoroutine() {
        float startTime = Time.time;
        this.transform.Rotate(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        dieState = DieState.Spinning;
        while (Time.time - startTime <= spinTime) {
            this.transform.Rotate(300f * Time.deltaTime, 300f * Time.deltaTime, 300f * Time.deltaTime, Space.Self);
            yield return new WaitForEndOfFrame();
        }
        dieState = DieState.Rolling;
        rigidbody.useGravity = true;
        while (!rigidbody.IsSleeping()) {
            yield return new WaitForEndOfFrame();
        }
        dieState = DieState.Stopped;
        yield break;
    }

    public void ResetDie() {
        this.transform.localPosition = orignialPosition;
        this.transform.rotation = Quaternion.Euler(0, 0, 0);
        rigidbody.useGravity = false;
        dieState = DieState.Idle;
    }

    public void Roll() {
        StartCoroutine(RollCoroutine());
    }

    public void CallReroll() {
        ResetDie();
        Roll();
    }

    //since the individual faces are their own subobjects the one with the greatest y value is on top
    public int GetFaceValue() {
        int maxDex = 0;
        for(int i = 1; i < faces.Length; i++) {
            if (faces[i].position.y > faces[maxDex].position.y) {
                maxDex = i;
            }
        }
        return faceValues[maxDex];
    }

}
