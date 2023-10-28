using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPinningSystem : MonoBehaviour
{
    private Camera mainCamera;
    private Ray ray;
    private RaycastHit hit;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("mouse clicked at " + Input.mousePosition);

            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask("Ground")))  // if raycast on ground
            {
                Vector3 pinPosition = new Vector3(hit.transform.position.x, 1f, hit.transform.position.z);

                Debug.LogFormat("Mouse hit object {0}", hit.transform.gameObject.name);
            }
        }
    }
}
