using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HMT;
using Photon.Pun.Demo.PunBasics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Photon.Pun;
using Photon.Pun.Demo.Cockpit.Forms;

public class Game1Interface : HMTInterface {

    [Header("Game Specific Settings")]
    public KeyCode[] RecaptureHotKey;
    
    GameObject[] monsters;
    GameObject[] stones;
    GameObject[] traps;
    GameObject[] goals;
    GameObject[] walls;
    Player[] players;
    GameObject door;


    GameData gameData;
    GameManager gameManager;

    protected override void Start() {
        base.Start();
        SceneManager.activeSceneChanged += OnSceneChange;
        FindKeyObjects();
    }

    protected override void Update() {
        base.Update();
        if (CheckHotKey(RecaptureHotKey)) {
            Debug.Log("Recapturing KeyObjects");
            FindKeyObjects();
        }
    }

    public Vector2Int WorldPointToGridPosition(Vector3 point, float tileSize, float tileGap, Vector3 zeroPoint, bool ceil) {
        point -= zeroPoint;
        point /= (tileSize + tileGap);
        if(ceil) {
            return new Vector2Int(Mathf.CeilToInt(point.x), Mathf.CeilToInt(point.z));
        }
        else {
            return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
        }
        
    }

    public Vector2Int WorldPointToGridPosition(Vector3 point, float tileSize, float tileGap, Vector3 zeroPoint) {
        return WorldPointToGridPosition(point, tileSize, tileGap, zeroPoint, false);        
    }


    void FindKeyObjects() {
        monsters = GameObject.FindGameObjectsWithTag("Monster");
        stones = GameObject.FindGameObjectsWithTag("Rock");
        traps = GameObject.FindGameObjectsWithTag("Trap");
        goals = GameObject.FindGameObjectsWithTag("Goal");
        door = GameObject.FindGameObjectWithTag("Door");
        walls = GameObject.FindGameObjectsWithTag("Walls");
        players = FindObjectsOfType<Player>();
        var manager = GameObject.Find("GameManager");
        gameManager = manager != null ? manager.GetComponent<GameManager>() : null;
        gameData = manager != null ? manager.GetComponent<GameData>() : null;

        Debug.LogFormat("FindKeyObjects found {0} Doors, {1} Goals, {2} Players, {3} Walls, {4} Monsters, {5} Traps, {6} Stones",
            door == null ? 0 : 1,
            goals.Length,
            players.Length,
            walls.Length,
            monsters.Length,
            traps.Length,
            stones.Length) ;
        /*
         * Still need to find:
         *  The grid positions
         *      which can also be used to establish a discrete grid size instead of global space
         *  The walls
         *      not sure how best to represent those
         *  Players
         *  
         * 
         */ 
    }


    void OnSceneChange(Scene current, Scene next) {
        FindKeyObjects();
    }

    public void ConstructGrid() {
        Vector3 lowerLeft = door.transform.position;
        Vector3 upperRight = door.transform.position;
        foreach(GameObject wall in walls) {
            Bounds b = wall.GetComponent<BoxCollider>().bounds;
            lowerLeft = Vector3.Min(lowerLeft, b.max);
            upperRight = Vector3.Max(upperRight, b.min);
        }

        var boardWidth = upperRight.x - lowerLeft.x;
        var boardHeight = upperRight.z - lowerLeft.z;
    }

    public override string GetState(bool formated) {

        if (door == null) {
            return "No Door Found, probably not in a scene or currently transitioning";
        }
        else {
            if(players.Length ==0) {
                FindKeyObjects();
            }
            if(players.Length == 0) {
                return "Found No Players, probably not in a scene or currently transitioning";
            }
        }

        Vector3 lowerLeft = door.transform.position;
        Vector3 upperRight = door.transform.position;
        foreach (GameObject wall in walls) {
            Bounds b = wall.GetComponent<BoxCollider>().bounds;
            lowerLeft = Vector3.Min(lowerLeft, b.max);
            upperRight = Vector3.Max(upperRight, b.min);
        }

        var boardWidth = upperRight.x - lowerLeft.x;
        var boardHeight = upperRight.z - lowerLeft.z;
        

        JObject ret = new JObject();
        ret["gameData"] = new JObject {
            {"tileSize", gameData.tileSize },
            {"tileGap", gameData.tileGapLength },
            {"boardWidth", boardWidth},
            {"boardHeight", boardHeight},
            {"gridWidth",  Mathf.CeilToInt((boardWidth - gameData.tileGapLength) / (gameData.tileSize + gameData.tileGapLength))},
            {"gridHeight", Mathf.CeilToInt((boardHeight- gameData.tileGapLength) / (gameData.tileSize + gameData.tileGapLength))},
            {"level", gameData.gameLevel },
            {"currentPlayer", GameManager.instance.turn },
            {"localPlayerId", PhotonNetwork.LocalPlayer.ActorNumber },
        };

        JArray scene = new JArray();

        //DOOR
        var pos = WorldPointToGridPosition(door.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
        scene.Add(new JObject {
            {"name", "door" },
            {"type","door" },
            {"x", pos.x },
            {"y", pos.y }
        });

        //GOALS 
        foreach(GameObject goal in goals) {
            if(goal == null) { continue; }
            pos = WorldPointToGridPosition(goal.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            scene.Add(new JObject {
                {"name", goal.name },
                {"type", "goal" },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //CHARACTERS
        foreach(Player player in players) {
            if(player == null) { continue;  }
            pos = WorldPointToGridPosition(player.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            var role = player.name[0] switch {
                'D' => "drawf",
                'G' => "giant",
                'H' => "human",
                _ => "UNKOWN"
            };
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            scene.Add(new JObject {
                {"name", player.name },
                {"controllingPlayerId", player.playerId },
                {"type", role},
                {"moveCount", player.moveCount },
                {"movementRange", player.config.movement },
                {"sightRange", player.config.sightRange },
                {"dieFaces", new JArray(player.config.dieFaces) },
                {"health", health.heart.Length}
            });
        }


        //MONSTERS
        foreach (GameObject monster in monsters) {
            if(monster == null) { continue; }
            pos = WorldPointToGridPosition(monster.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            Monster mon = monster.GetComponent<Monster>();

            scene.Add(new JObject {
                {"name", mon.name },
                {"type", "monster" },
                {"monsterSize", mon.monsterType },
                {"targets", new JArray(mon.num) },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //TRAPS
        foreach(GameObject trap in traps) {
            if(trap == null) { continue; }
            pos = WorldPointToGridPosition(trap.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            scene.Add(new JObject {
                {"name", trap.name },
                {"type","trap" },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //ROCKS
        foreach (GameObject stone in stones) {
            if (stone == null) { continue; }
            pos = WorldPointToGridPosition(stone.transform.position, gameData.tileSize, gameData.tileGapLength, lowerLeft);
            scene.Add(new JObject {
                {"name", stone.name },
                {"type","stone" },
                {"x", pos.x },
                {"y", pos.y }
            });
        }

        //WALLS??
        foreach (GameObject wall in walls) {
            Bounds bound = wall.GetComponent<Collider>().bounds;
            pos = WorldPointToGridPosition(bound.min, gameData.tileSize, gameData.tileGapLength, lowerLeft, true);
            scene.Add(new JObject {
                {"name", wall.name },
                {"type", "wall" },
                {"x", pos.x },
                {"y", pos.y },
                {"w", Mathf.Clamp(Mathf.Ceil((bound.size.x - gameData.tileGapLength) / (gameData.tileGapLength + gameData.tileSize)),1,float.PositiveInfinity) },
                {"h", Mathf.Clamp(Mathf.Ceil((bound.size.z - gameData.tileGapLength) / (gameData.tileGapLength + gameData.tileSize)),1,float.PositiveInfinity) }
            }); ;
        }


        ret["scene"] = scene;
        if (formated) {
            return JsonConvert.SerializeObject(ret, Formatting.Indented);
        }
        else {
            return JsonConvert.SerializeObject(ret, Formatting.None);
        }
    }

    public override string ExecuteAction(string action) {
        return "No Action to Execute for now.";
    }

}
