using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace HMT {
    public class ArgParser {

        public string FlagMarker = "-";

        public ArgParser() { }

        public ArgParser(string flagMarker) {
            FlagMarker = flagMarker;
        }

        public enum ArgType {
            Flag,
            One,
            List
        }

        public class Arg {
            public string key;
            public ArgType type;
            public int max = 1;
            public string value = string.Empty;
            public bool IsSet { get; private set; } = false;

            public Arg(string key, ArgType type, int max) {
                this.key = key;
                this.type = type;
                this.max = max;
            }

            public void SetValue(string value) {
                if (!IsSet) {
                    this.value = value;
                    IsSet = true;
                }
            }

            public void SetValue(bool value) {
                if (!IsSet) {
                    this.value = value.ToString();
                    IsSet = true;
                }
            }

            public void SetValue(IEnumerable<string> values) {
                if (IsSet) {
                    this.value = string.Join(" ", values);
                    IsSet = true;
                }
            }
        }

        public Dictionary<string, Arg> args = new Dictionary<string, Arg>();

        public void AddArg(string key, ArgType type, int max) {
            args.Add(key, new Arg(key, type, max));
        }

        public void AddArg(string key, ArgType type) {
            args.Add(key, new Arg(key, type, 1));
        }

        public bool CheckFlag(string key) {
            return args.ContainsKey(key) && args[key].IsSet;
        }

        public string GetArgValue(string key) {
            if (args.ContainsKey(key) && args[key].IsSet) {
                return args[key].value;
            }
            else {
                return string.Empty;
            }
        }

        public string GetArgValue(string key, string defaultValue) {
            if (args.ContainsKey(key) && args[key].IsSet) {
                return args[key].value;
            }
            else {
                return defaultValue;
            }
        }

        public bool GetArgValue(string key, bool defaultValue) {
            if (args.ContainsKey(key) && args[key].IsSet && bool.TryParse(args[key].value, out bool ret)) {
                return ret;
            }
            else {
                return defaultValue;
            }
        }

        public int GetArgValue(string key, int defaultValue) {
            if (args.ContainsKey(key) && args[key].IsSet && int.TryParse(args[key].value, out int ret)) {
                return ret;
            }
            else {
                return defaultValue;
            }
        }

        public float GetArgValue(string key, float defaultValue) {
            if (args.ContainsKey(key) && args[key].IsSet && float.TryParse(args[key].value, out float ret)) {
                return ret;
            }
            else {
                return defaultValue;
            }
        }

        public string[] GetArgValues(string key) {
            if (args.ContainsKey(key) && args[key].IsSet) {
                return args[key].value.Split(' ');
            }
            else {
                return new string[0];
            }
        }

        public void ParseArgs() {
            string[] arguments = System.Environment.GetCommandLineArgs();
            string currentFlag = string.Empty;
            List<string> paramCollection = new List<string>();
            for (int i = 0; i < arguments.Length; i++) {
                bool argIsFlag = arguments[i].StartsWith(FlagMarker);
                bool isTargetArg = args.ContainsKey(arguments[i].Substring(1));
                if (currentFlag == string.Empty) {
                    if (isTargetArg) {
                        Arg arg = args[arguments[i].Substring(1)];
                        switch (arg.type) {
                            case ArgType.Flag:
                                arg.SetValue(true);
                                break;
                            case ArgType.One:
                                currentFlag = arg.key;
                                break;
                            case ArgType.List:
                                currentFlag = arg.key;
                                paramCollection = new List<string>();
                                break;
                        }
                    }
                    else {
                        continue;
                    }
                }
                else {
                    Arg arg = args[currentFlag];
                    if (argIsFlag) {
                        switch (arg.type) {
                            case ArgType.One:
                                Debug.LogErrorFormat("Argument {0} did not recieve a value setting it to true.", currentFlag);
                                arg.SetValue(true);
                                break;
                            case ArgType.List:
                                arg.SetValue(paramCollection);
                                paramCollection.Clear();
                                break;
                        }
                        currentFlag = arguments[i].Substring(1);
                    }
                    else {
                        switch (arg.type) {
                            case ArgType.One:
                                arg.SetValue(arguments[i]);
                                currentFlag = string.Empty;
                                break;
                            case ArgType.List:
                                paramCollection.Add(arguments[i]);
                                if (paramCollection.Count >= arg.max) {
                                    currentFlag = string.Empty;
                                    arg.SetValue(paramCollection);
                                    paramCollection.Clear();
                                }
                                break;
                        }
                    }
                }
            }
            if(paramCollection.Count > 0) {
                args[currentFlag].SetValue(paramCollection);
            }
        }
    }
}