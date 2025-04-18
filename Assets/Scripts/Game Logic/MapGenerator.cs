using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; } = null;

    public List<LevelSpec> levelSpecs = null;
    public GameAssets gameAssets;
    public GameData gameData;
    public Transform tileParent;
    public Tile[,] Map;
    public bool FOW_enabled = true;
    public string CurrentLevelName { get; private set; }

    public List<KeyCode> SecretFOWToggleCode = new List<KeyCode> { KeyCode.F, KeyCode.O, KeyCode.W };

    public Dictionary<string, string> AlternateSchema = new Dictionary<string, string>()
    {
        {"**", "Door" },
        {"##", "Wall" },
        {"..", "Open" },
        {"C1", "1Spawn" },
        {"C2", "2Spawn" },
        {"C3", "3Spawn" },
        {"K1", "1Goal" },
        {"K2", "2Goal" },
        {"K3", "3Goal" },
        {"M1", "Monster1" },
        {"M2", "Monster2" },
        {"M3", "Monster3" },
        {"T1", "Trap1" },
        {"T2", "Trap2" },
        {"R1", "Rock1" },
        {"R2", "Rock2" },
        {"R3", "Rock3" },
        {"R4", "Rock4" },

//Everything below here is around for backwards compatibility of old level specs
        {"1S", "1Spawn" },
        {"2S", "2Spawn" },
        {"3S", "3Spawn" },
        {"1G", "1Goal" },
        {"2G", "2Goal" },
        {"3G", "3Goal" },
        {"S1", "Rock1" },
        {"S2", "Rock2" },
        {"S3", "Rock3" },
        {"S4", "Rock4" },
    };

    /// <summary>
    /// A simple structure just to hold the split up string of a Level.
    /// 
    /// The generator should take this new struct and use it as a means for actually generating the level.
    /// </summary>
    public struct LevelSpec {
        public string name;
        public string[,] grid;
        public int width { get { return grid.GetLength(0); } }
        public int height { get { return grid.GetLength(1); } }
    }

    void Awake()
    {
        Instance = this;
    }

    void Update() {
        for(int i = 0; i < SecretFOWToggleCode.Count; i++) {
            if (Input.GetKeyDown(SecretFOWToggleCode[i])) {
                bool toggle = true;
                for(int j = 0; j < SecretFOWToggleCode.Count; j++) {
                    if (j == i) continue;
                    if (!Input.GetKey(SecretFOWToggleCode[j])) {
                        toggle = false;
                        break;
                    }
                }
                if (toggle) {
                    ToggleFOW_OnOff();
                }
            }
        }
    }

    public void UpdateFOWVisuals()
    {
        foreach (Tile tile in Map)
        {
            if (FOW_enabled) {
                tile.SetFOWVisualsToCharacter(GameManager.Instance.localChar.CharacterId);
            }
            else {
                tile.SetFOWVisualsToVisible();
            }
        }
    }

    public void ToggleFOW_OnOff()
    {
        FOW_enabled = !FOW_enabled;
        UpdateFOWVisuals();
    }

    private void Start()
    {
        //LoadLevel(gameData.levelTextFiles[0]);
    }

    public Tile GetTileAt(Vector2Int vec) {
        return GetTileAt(vec.x, vec.y);
    }


    public Tile GetTileAt(int x, int y) {
        if (!InMap(x, y)) { 
            Debug.LogWarningFormat("Trying to get tile at invalid position ({0}, {1})", x, y);
            return null;
        }
        return Map[x, y];
    }

    public bool SetTileAt(int x, int y, Tile tile) {
        if (!InMap(x,y)) {
            Debug.LogWarningFormat("Trying to set tile at invalid position ({0}, {1})", x, y);
            return false;
        }
        Map[x, y] = tile;
        return true;
    }

    public void ClearTile(int x, int y) {
        Tile target = GetTileAt(x, y);
/*      Shouldn't need to copy the map you should be able to just move it over
        var fowMap = new Dictionary<int, Tile.FogOfWarState>();
        foreach (KeyValuePair<int, Tile.FogOfWarState> entry in target.fogOfWarDictionary) {
            fowMap.Add(entry.Key, entry.Value);
        }
*/
        GameObject newOpenTileGO = Instantiate(gameAssets.OpenTile, target.transform.position, Quaternion.identity, tileParent);
        Tile newTile = newOpenTileGO.GetComponent<Tile>();
        newTile.fogOfWarDictionary = target.fogOfWarDictionary;
        newTile.row = target.row;
        newTile.col = target.col;
        SetTileAt(x,y,newTile);
        Destroy(target.gameObject);
    }

    public bool InMap(int x, int y) {
        return x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1);
    }

    public bool InMap(Vector2Int pos) {
        return InMap(pos.x, pos.y);
    }

    public Tile DoorTile { get; private set; }

    public void ParseLevelSpec(TextAsset levelSpecAsset) {
        ParseLevelSpec(levelSpecAsset.text);
    }

    public void ParseLevelSpec(IList<TextAsset> levelSpecAssets) {
        ParseLevelSpec(string.Join("`", levelSpecAssets.Select(l => l.text)));
    }

    public void ParseLevelSpec(string levelSpecString) {
        string[] levelBlocks = levelSpecString.Split('`');
        levelSpecs = new List<LevelSpec>();
        foreach(string levelBlock in levelBlocks) {
            string[] lines = levelBlock.Trim().Split('\n');

            LevelSpec spec = new LevelSpec();
            spec.name = lines[0].Trim();
            string[] dimensions = lines[1].Split('x');
            int colCount = int.Parse(dimensions[0]);
            int rowCount = int.Parse(dimensions[1]);

            spec.grid = new string[colCount, rowCount];

            for (int i = 2; i < lines.Length; i++) {
                for (int j = 0; j < lines[i].Length; j += 2) {
                    if (j + 1 < lines[i].Length) // make sure there is a pair of characters
                    {
                        spec.grid[j / 2, rowCount - i + 1] = lines[i][j].ToString() + lines[i][j + 1].ToString();
                    }
                }
            }

            levelSpecs.Add(spec);
        }
    }

    public void LoadLevel(int indx)
    {
        LevelSpec levelSpec = levelSpecs[indx];
        while (tileParent.transform.childCount != 0)
        {
            Transform child = tileParent.transform.GetChild(0);
            child.parent = null;
            Destroy(child.gameObject);
        }

        // Split the file content into lines
        //string[] lines = levelTextFile.text.Split('\n');

        //CurrentLevelName = lines[0].Trim();
        CurrentLevelName = levelSpec.name;

        CompetitionMiddleware.Instance.LogStartLevel(CurrentLevelName);
        //string[] dimensions = lines[1].Split('x');
        //int colCount = int.Parse(dimensions[0]);
        //int rowCount = int.Parse(dimensions[1]);

        //string[,] tileStrings = new string[colCount, rowCount];

        Map = new Tile[levelSpec.width, levelSpec.height];


        // Loop through each line and extract tiles
        //for (int i = 2; i < lines.Length; i++) {
        //    for (int j = 0; j < lines[i].Length; j += 2) {
        //        if (j + 1 < lines[i].Length) // make sure there is a pair of characters
        //        {
                    
        //            string tile = lines[i][j].ToString() + lines[i][j + 1].ToString();
        //            //Debug.LogFormat("Adding Tile {0} at {1}, {2}", tile , j / 2, rowCount - i + 1);
        //            Map[  j / 2, rowCount - i + 1] = SpawnTile(rowCount - i + 1, j / 2,  tile);
        //        }
        //    }
        //}

        for (int i = 0; i < levelSpec.width; i++) {
            for (int j = 0; j < levelSpec.height; j++) {

                Map[i, j] = SpawnTile(j, i, levelSpec.grid[i,j]);

                //if (j + 1 < lines[i].Length) // make sure there is a pair of characters
                //{

                //    string tile = lines[i][j].ToString() + lines[i][j + 1].ToString();
                //    //Debug.LogFormat("Adding Tile {0} at {1}, {2}", tile , j / 2, rowCount - i + 1);
                //    Map[j / 2, rowCount - i + 1] = SpawnTile(rowCount - i + 1, j / 2, tile);
                //}
            }
        }


        // Calculate map width and length
        float mapWidth = levelSpec.width * (gameData.tileSize + gameData.tileGapLength) - gameData.tileGapLength;
        float mapHeight = levelSpec.height * (gameData.tileSize + gameData.tileGapLength) - gameData.tileGapLength;
        // Calculate the middle point of map width and length
        float mapWidthMid = (levelSpec.width - 1) * (gameData.tileSize + gameData.tileGapLength) / 2;
        float mapHeightMid = (levelSpec.height - 1) * (gameData.tileSize + gameData.tileGapLength) / 2;

        /*        for (int i = 0; i < rowCount; i++)
                {
                    Debug.Log("New Line");
                    for (int j = 0; j < colCount; j++) {
                        Debug.Log(tiles[i][j]);
                        Map[i,j] = SpawnTile(i, j, tiles[i][j]);
                    }
                }
        */

        StartCoroutine(LogCharacterSpawns());
        

        GameObject wallSW = Instantiate(gameAssets.MapBoundary, new Vector3(mapWidthMid, 0, -1), Quaternion.identity, tileParent);
        wallSW.GetComponent<BoxCollider>().size = new Vector3(mapWidth, 1, 1);
        wallSW.name = "WallSW";
        GameObject wallNW = Instantiate(gameAssets.MapBoundary, new Vector3(-1, 0, mapHeightMid), Quaternion.identity, tileParent);
        wallNW.GetComponent<BoxCollider>().size = new Vector3(1, 1, mapHeight);
        wallNW.name = "WallNW";
        GameObject wallNE = Instantiate(gameAssets.MapBoundary, new Vector3(mapWidthMid, 0, mapHeight), Quaternion.identity, tileParent);
        wallNE.GetComponent<BoxCollider>().size = new Vector3(mapWidth, 1, 1);
        wallNE.name = "WallNE";
        GameObject wallSE = Instantiate(gameAssets.MapBoundary, new Vector3(mapWidth, 0, mapHeightMid), Quaternion.identity, tileParent);
        wallSE.GetComponent<BoxCollider>().size = new Vector3(1, 1, mapHeight);
        wallSE.name = "WallSE";
    }

    IEnumerator LogCharacterSpawns() {
        yield return new WaitForFixedUpdate();
        for (int i = 0; i < GameManager.Instance.inSceneCharacters.Count; i++) {
            Tile tile = GameManager.Instance.inSceneCharacters[i].currentTile;
            CompetitionMiddleware.Instance.LogPlayerSpawn(i, tile.col, tile.row);
        }
    }

    private Tile SpawnTile(int row, int col, string code)
    {
        float x = col * (gameData.tileSize + gameData.tileGapLength);
        float z = row * (gameData.tileSize + gameData.tileGapLength);
        string name = SearchSchema(code);

        GameObject tileObj = null;
        bool spawnEntity = false;

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
                DoorTile = tileObj.GetComponent<Tile>();
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
                spawnEntity = true;
                tileObj = Instantiate(gameAssets.OpenTile, new Vector3(x, 0, z), Quaternion.identity, tileParent);
                SpawnEntity(name, x, z, tileObj.GetComponent<Tile>(), code);
                break;
        }

        if (tileObj != null) {
            Tile tile = tileObj.GetComponent<Tile>();
            tile.row = row;
            tile.col = col;
            if (spawnEntity) {
                tile.ObjKey = "..";
            }
            else { 
                tile.ObjKey = code;
            }
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
            Debug.LogErrorFormat("A unknown code {0} in currLevel map file.", code);
            return "";
        }
    }

    private void SpawnEntity(string tileName, float x, float z, Tile tileObj, string code)
    {
        Renderer[] renderers = null;
        switch (tileName)
        {
            case "1Spawn":
                GameManager.Instance.SetupCharacter(0, x, z);
                break;

            case "2Spawn":
                GameManager.Instance.SetupCharacter(1, x, z);
                break;

            case "3Spawn":
                GameManager.Instance.SetupCharacter(2, x, z);
                break;

            case "1Goal":
                GameObject goal1 = Instantiate(gameAssets.Goals[0], new Vector3(x, 0f, z), Quaternion.identity);
                tileObj.shrine = goal1.GetComponent<Shrine>();
                tileObj.shrine.tile = tileObj;
                tileObj.shrine.ObjKey = code;
                GameManager.Instance.InSceneShrines[0] = tileObj.shrine;
                tileObj.tag = "Goal";
                goal1.transform.parent = tileObj.transform;
                renderers = goal1.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    // Store the original materials
                    tileObj.GetComponent<Tile>().originalMaterials[renderer] = renderer.materials;
                }
                break;

            case "2Goal":
                GameObject goal2 = Instantiate(gameAssets.Goals[1], new Vector3(x, 0f, z), Quaternion.identity);
                tileObj.shrine = goal2.GetComponent<Shrine>();
                tileObj.shrine.tile = tileObj;
                tileObj.shrine.ObjKey = code;
                GameManager.Instance.InSceneShrines[1] = tileObj.shrine;
                tileObj.tag = "Goal";
                goal2.transform.parent = tileObj.transform;
                renderers = goal2.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    // Store the original materials
                    tileObj.GetComponent<Tile>().originalMaterials[renderer] = renderer.materials;
                }
                break;

            case "3Goal":
                GameObject goal3 = Instantiate(gameAssets.Goals[2], new Vector3(x, 0f, z), Quaternion.identity);
                tileObj.shrine = goal3.GetComponent<Shrine>();
                tileObj.shrine.tile = tileObj;
                tileObj.shrine.ObjKey = code;
                GameManager.Instance.InSceneShrines[2] = tileObj.shrine;
                tileObj.tag = "Goal";
                goal3.transform.parent = tileObj.transform;
                renderers = goal3.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    // Store the original materials
                    tileObj.GetComponent<Tile>().originalMaterials[renderer] = renderer.materials;
                }
                break;

            case "Monster1":
                GameObject monster1 = Instantiate(gameAssets.Monsters[0], new Vector3(x, 0f, z), Quaternion.identity);
                GameManager.Instance.inSceneMonsters.Add(monster1.GetComponent<Monster>());
                monster1.GetComponent<Monster>().SetUpConfig(gameData.monsterConfigs[0], GameManager.Instance.inSceneMonsters.Count - 1, gameData, code);
                break;

            case "Monster2":
                GameObject monster2 = Instantiate(gameAssets.Monsters[1], new Vector3(x, 0f, z), Quaternion.identity);
                GameManager.Instance.inSceneMonsters.Add(monster2.GetComponent<Monster>());
                monster2.GetComponent<Monster>().SetUpConfig(gameData.monsterConfigs[1], GameManager.Instance.inSceneMonsters.Count - 1, gameData, code);
                break;

            case "Monster3":
                GameObject monster3 = Instantiate(gameAssets.Monsters[2], new Vector3(x, 0f, z), Quaternion.identity);
                GameManager.Instance.inSceneMonsters.Add(monster3.GetComponent<Monster>());
                monster3.GetComponent<Monster>().SetUpConfig(gameData.monsterConfigs[2], GameManager.Instance.inSceneMonsters.Count - 1, gameData, code);
                break;

            default:
                break;
        }
    }
}
