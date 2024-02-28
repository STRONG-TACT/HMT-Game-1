using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NetworkMapGenerator : MonoBehaviour
{
    public static NetworkMapGenerator Instance = null;
    public GameAssets gameAssets;
    public NetworkGameData gameData;
    public Transform tileParent;
    public NetworkTile[,] Map;

    public Dictionary<string, string> AlternateSchema = new Dictionary<string, string>()
    {
        {"**", "Door" },
        {"##", "Wall" },
        {"..", "Open" },
        {"1S", "1Spawn" },
        {"2S", "2Spawn" },
        {"3S", "3Spawn" },
        {"1G", "1Goal" },
        {"2G", "2Goal" },
        {"3G", "3Goal" },
        {"M1", "Monster1" },
        {"M2", "Monster2" },
        {"M3", "Monster3" },
        {"T1", "Trap1" },
        {"T2", "Trap2" },
        {"R1", "Rock1" },
        {"R2", "Rock2" },
        {"R3", "Rock3" },
        {"R4", "Rock4" },
        {"S1", "Rock1" }, //The S in the schema can also stand for stone, though in hindsight R is better
        {"S2", "Rock2" },
        {"S3", "Rock3" },
        {"S4", "Rock4" },
    };

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadLevel(gameData.levelTextFiles[0]);
    }


    public NetworkTile GetTileAt(int x, int y) {
        if (!InMap(x, y)) { 
            Debug.LogWarningFormat("Trying to get tile at invalid position ({0}, {1})", x, y);
            return null;
        }
        return Map[x, y];
    }

    public bool SetTileAt(int x, int y, NetworkTile tile) {
        if (!InMap(x,y)) {
            Debug.LogWarningFormat("Trying to set tile at invalid position ({0}, {1})", x, y);
            return false;
        }
        Map[x, y] = tile;
        return true;
    }

    public bool InMap(int x, int y) {
        return x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1);
    }

    public bool InMap(Vector2Int pos) {
        return InMap(pos.x, pos.y);
    }


    public void LoadLevel(TextAsset levelTextFile)
    {
        while (tileParent.transform.childCount != 0)
        {
            Transform child = tileParent.transform.GetChild(0);
            child.parent = null;
            Destroy(child.gameObject);
        }

        // Split the file content into lines
        string[] lines = levelTextFile.text.Split('\n');

        // Create a list to store tile data temporarily
        List<List<string>> tiles = new List<List<string>>();

        // Loop through each line and extract tiles
        for (int i = 0; i < lines.Length; i++)
        {
            List<string> tileRow = new List<string>();
            for (int j = 0; j < lines[i].Length; j += 2)
            {
                if (j + 1 < lines[i].Length) // make sure there is a pair of characters
                {
                    string tile = lines[i][j].ToString() + lines[i][j + 1].ToString();
                    tileRow.Add(tile);
                }
            }
            tiles.Add(tileRow);
        }

        int rowCount = tiles.Count;
        int colCount = tiles[0].Count;
        
        Map = new NetworkTile[rowCount, colCount];

        // Calculate map width and length
        float mapWidth = colCount * (gameData.tileSize + gameData.tileGapLength) - gameData.tileGapLength;
        float mapLength = rowCount * (gameData.tileSize + gameData.tileGapLength) - gameData.tileGapLength;
        // Calculate the middle point of map width and length
        float mapWidthMid = (colCount - 1) * (gameData.tileSize + gameData.tileGapLength) / 2;
        float mapLengthMid = (rowCount - 1) * (gameData.tileSize + gameData.tileGapLength) / 2;

        for (int i = 0; i < rowCount; i++)
        {
            Debug.Log("New Line");
            for (int j = 0; j < colCount; j++) {
                Debug.Log(tiles[i][j]);
                Map[i,j] = SpawnTile(i, j, tiles[i][j]);
            }
        }

        GameObject wall1 = Instantiate(gameAssets.MapBoundary, new Vector3(-mapWidthMid, 0, 1), Quaternion.identity, tileParent);
        wall1.GetComponent<BoxCollider>().size = new Vector3(mapWidth, 1, 1);
        GameObject wall2 = Instantiate(gameAssets.MapBoundary, new Vector3(-mapWidthMid, 0, -mapLength), Quaternion.identity, tileParent);
        wall2.GetComponent<BoxCollider>().size = new Vector3(mapWidth, 1, 1);
        GameObject wall3 = Instantiate(gameAssets.MapBoundary, new Vector3(1, 0, -mapLengthMid), Quaternion.identity, tileParent);
        wall3.GetComponent<BoxCollider>().size = new Vector3(1, 1, mapLength);
        GameObject wall4 = Instantiate(gameAssets.MapBoundary, new Vector3(-mapWidth, 0, -mapLengthMid), Quaternion.identity, tileParent);
        wall4.GetComponent<BoxCollider>().size = new Vector3(1, 1, mapLength);
    }

    private NetworkTile SpawnTile(int row, int col, string code)
    {
        float x = - col * (gameData.tileSize + gameData.tileGapLength);
        float z = - row * (gameData.tileSize + gameData.tileGapLength);
        string name = SearchSchema(code);

        GameObject tileObj = null;

        switch (name)
        {
            case "Open":
                tileObj = Instantiate(gameAssets.OpenTile, new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Wall":
                int randomIndex = Random.Range(0, gameAssets.WallTiles.Count);
                tileObj = Instantiate(gameAssets.WallTiles[randomIndex], new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Door":
                tileObj = Instantiate(gameAssets.DoorTile, new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Trap1":
                tileObj = Instantiate(gameAssets.TrapTiles[0], new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Trap2":
                tileObj = Instantiate(gameAssets.TrapTiles[1], new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Rock1":
                tileObj = Instantiate(gameAssets.RockTiles[0], new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Rock2":
                tileObj = Instantiate(gameAssets.RockTiles[1], new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Rock3":
                tileObj = Instantiate(gameAssets.RockTiles[2], new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "Rock4":
                tileObj = Instantiate(gameAssets.RockTiles[3], new Vector3(x, 0, z), Quaternion.identity, tileParent);
                break;

            case "":
                Debug.Log("SpawnTile() received empty code, but can not find responding tile");
                break;

            default:
                tileObj = Instantiate(gameAssets.OpenTile, new Vector3(x, 0, z), Quaternion.identity, tileParent);
                SpawnEntity(name, x, z, tileObj.GetComponent<NetworkTile>());
                break;
        }

        if (tileObj != null) {
            NetworkTile tile = tileObj.GetComponent<NetworkTile>();
            tile.row = row;
            tile.col = col;
            return tile;
        }
        else {
            return null;
        }

    }

    private string SearchSchema(string code)
    {
        if (AlternateSchema.ContainsKey(code)) {
            return AlternateSchema[code];
        }
        else {
            Debug.LogErrorFormat("A unknown code {0} in level map file.", code);
            return "";
        }
    }

    private void SpawnEntity(string tileName, float x, float z, NetworkTile tileObj)
    {
        switch (tileName)
        {
            case "1Spawn":
                NetworkGameManager.S.setCharaPosition(0, x, z);
                break;

            case "2Spawn":
                NetworkGameManager.S.setCharaPosition(1, x, z);
                break;

            case "3Spawn":
                NetworkGameManager.S.setCharaPosition(2, x, z);
                break;

            case "1Goal":
                GameObject goal1 = Instantiate(gameAssets.Goals[0], new Vector3(x, 0f, z), Quaternion.identity);
                tileObj.shrine = goal1.GetComponent<NetworkShrine>();
                goal1.transform.parent = tileObj.transform;
                break;

            case "2Goal":
                GameObject goal2 = Instantiate(gameAssets.Goals[1], new Vector3(x, 0f, z), Quaternion.identity);
                tileObj.shrine = goal2.GetComponent<NetworkShrine>();
                goal2.transform.parent = tileObj.transform;
                break;

            case "3Goal":
                GameObject goal3 = Instantiate(gameAssets.Goals[2], new Vector3(x, 0f, z), Quaternion.identity);
                tileObj.shrine = goal3.GetComponent<NetworkShrine>();
                goal3.transform.parent = tileObj.transform;
                break;

            case "Monster1":
                GameObject monster1 = Instantiate(gameAssets.Monsters[0], new Vector3(x, 0f, z), Quaternion.identity);
                NetworkGameManager.S.inSceneMonsters.Add(monster1.GetComponent<NetworkMonster>());
                monster1.GetComponent<NetworkMonster>().SetUpConfig(gameData.monsterConfigs[0], NetworkGameManager.S.inSceneMonsters.Count - 1, gameData);
                break;

            case "Monster2":
                GameObject monster2 = Instantiate(gameAssets.Monsters[1], new Vector3(x, 0f, z), Quaternion.identity);
                NetworkGameManager.S.inSceneMonsters.Add(monster2.GetComponent<NetworkMonster>());
                monster2.GetComponent<NetworkMonster>().SetUpConfig(gameData.monsterConfigs[1], NetworkGameManager.S.inSceneMonsters.Count - 1, gameData);
                break;

            case "Monster3":
                GameObject monster3 = Instantiate(gameAssets.Monsters[2], new Vector3(x, 0f, z), Quaternion.identity);
                NetworkGameManager.S.inSceneMonsters.Add(monster3.GetComponent<NetworkMonster>());
                monster3.GetComponent<NetworkMonster>().SetUpConfig(gameData.monsterConfigs[2], NetworkGameManager.S.inSceneMonsters.Count - 1, gameData);
                break;

            default:
                break;
        }
    }
}
