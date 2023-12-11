using HMT;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HMT {
    public class HMTDebugGUI : MonoBehaviour {

        [Header("Hot Keys")]
        public KeyCode[] OpenHMTInterfaceWindowHotKey;
        public KeyCode[] PrintCurrentStateHotKey;

#if HMT_BUILD
        HMTInterface apiInterface;
        private string lastState = string.Empty;

        private bool isOpen = false;
        private Vector2 scrollPos = Vector2.zero;

        private void Awake() {
            apiInterface = GetComponent<HMTInterface>();
        }


        // Update is called once per frame
        void Update() {
            if (CheckHotKey(OpenHMTInterfaceWindowHotKey)) {
                isOpen = !isOpen;
            }
            if (CheckHotKey(PrintCurrentStateHotKey)) {
                Debug.LogFormat("[HMTInterface] State Hotkey: {0}", apiInterface.GetState(false));
            }
        }

        private void OnGUI() {
            if (isOpen) {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Close")) {
                    isOpen = !isOpen;
                }

                if (apiInterface.server == null) {
                    if (GUILayout.Button("Start Socket Server")) {
                        apiInterface.StartSocketServer();
                    }
                }
                else {
                    if (apiInterface.server.IsListening) {
                        if (GUILayout.Button("Stop Socket Server")) {
                            apiInterface.server.Stop();
                        }
                    }
                    else {
                        if (GUILayout.Button("Start Socket Server")) {
                            apiInterface.server.Start();
                        }
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.Label("Socket Server Status");
                if (apiInterface.server == null) {
                    GUILayout.Label("Server NOT connected.");
                    GUILayout.Label(string.Format("URL: {0}", apiInterface.Args.GetArgValue("hmtsocketurl", apiInterface.url)));
                    GUILayout.Label(string.Format("Port: {0}", apiInterface.Args.GetArgValue("hmtsocketport", apiInterface.socketPort)));
                }
                else {
                    GUILayout.Label(string.Format("Address: {0}", apiInterface.server.Address.ToString()));
                    GUILayout.Label(string.Format("Listening: {0}", apiInterface.server.IsListening));
                    GUILayout.Label(string.Format("Secure: {0}", apiInterface.server.IsSecure));
                    GUILayout.Label(string.Format("WaitTime: {0}", apiInterface.server.WaitTime));
                }

                GUILayout.Space(50);

                GUILayout.Label("STATE:");
                if (GUILayout.Button("Snap State")) {
                    lastState = apiInterface.GetState(true);
                }
                if (lastState != string.Empty) {
                    if (GUILayout.Button("Copy State")) {
                        GUIUtility.systemCopyBuffer = lastState;
                    }
                }
                GUILayout.Label(lastState);
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }
        protected bool CheckHotKey(KeyCode[] code) {
            foreach (KeyCode key in code) {
                if (!Input.GetKey(key)) {
                    return false;
                }
            }
            return code.Length > 0;
        }
#else
        private void Start() {
            Destroy(this);
        }
#endif

    }
}