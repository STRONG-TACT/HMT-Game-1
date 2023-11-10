using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance = null;
    public GameAssets gameAssets;
    public LocalGameData gameData;
    public Transform tileParent;
    public LocalTile[,] Map;

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
    };

    void Awake()
    {
        Instance = this;
        LoadLevel(gameData.levelTextFiles[0]);
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
        
        Map = new LocalTile[rowCount, colCount];

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

    private LocalTile SpawnTile(int row, int col, string code)
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
                SpawnEntity(name, x, z);
                break;
        }

        if (tileObj != null) {
            LocalTile tile = tileObj.GetComponent<LocalTile>();
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
        string tileName = AlternateSchema[code];

        if (tileName != null)
        {
            return tileName;
        }
        else
        {
            Debug.Log(string.Format("A unknown code {0} in level map file.", code));
            return "";
        }
    }

    private void SpawnEntity(string tileName, float x, float z)
    {
        switch (tileName)
        {
            case "1Spawn":
                LocalGameManager.Instance.setCharaPosition(0, x, z);
                break;

            case "2Spawn":
                LocalGameManager.Instance.setCharaPosition(1, x, z);
                break;

            case "3Spawn":
                LocalGameManager.Instance.setCharaPosition(2, x, z);
                break;

            case "1Goal":
                Instantiate(gameAssets.Goals[0], new Vector3(x, 0.2f, z), Quaternion.identity);
                break;

            case "2Goal":
                Instantiate(gameAssets.Goals[1], new Vector3(x, 0.2f, z), Quaternion.identity);
                break;

            case "3Goal":
                Instantiate(gameAssets.Goals[2], new Vector3(x, 0.2f, z), Quaternion.identity);
                break;

            case "Monster1":
                GameObject monster1 = Instantiate(gameAssets.Monsters[0], new Vector3(x, 0.5f, z), Quaternion.identity);
                LocalGameManager.Instance.inSceneMonsters.Add(monster1.GetComponent<LocalMonster>());
                monster1.GetComponent<LocalMonster>().SetUpConfig(gameData.monsterConfigs[0], LocalGameManager.Instance.inSceneMonsters.Count - 1, gameData);
                break;

            case "Monster2":
                GameObject monster2 = Instantiate(gameAssets.Monsters[1], new Vector3(x, 0.5f, z), Quaternion.identity);
                LocalGameManager.Instance.inSceneMonsters.Add(monster2.GetComponent<LocalMonster>());
                monster2.GetComponent<LocalMonster>().SetUpConfig(gameData.monsterConfigs[1], LocalGameManager.Instance.inSceneMonsters.Count - 1, gameData);
                break;

            case "Monster3":
                GameObject monster3 = Instantiate(gameAssets.Monsters[2], new Vector3(x, 0.5f, z), Quaternion.identity);
                LocalGameManager.Instance.inSceneMonsters.Add(monster3.GetComponent<LocalMonster>());
                monster3.GetComponent<LocalMonster>().SetUpConfig(gameData.monsterConfigs[2], LocalGameManager.Instance.inSceneMonsters.Count - 1, gameData);
                break;

            default:
                break;
        }
    }
}



