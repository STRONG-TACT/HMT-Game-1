using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public TextAsset levelTextFile;
    public GameAssets gameAssets;
    public LocalGameData gameData;

    // Start is called before the first frame update
    void Awake()
    {
        LoadLevel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void LoadLevel()
    {
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

        for (int i = 0; i < tiles.Count; i++)
        {
            Debug.Log("New Line");
            for (int j = 0; j < tiles[i].Count; j++) {
                Debug.Log(tiles[i][j]);
                SpawnTile(i, j, tiles[i][j]);
            }
        }
    }

    private void SpawnTile(int row, int col, string code)
    {
        float x = col * (gameData.tileSize + gameData.tileGapLength);
        float z = row * (gameData.tileSize + gameData.tileGapLength);
        switch (code)
        {
            case "..":
                GameObject opentile = Instantiate(gameAssets.OpenTile, new Vector3(x, 0, z), Quaternion.identity);
                opentile.GetComponent<LocalTile>().row = row;
                opentile.GetComponent<LocalTile>().col = col;
                break;

            case "##":
                GameObject walltile = Instantiate(gameAssets.WallTile, new Vector3(x, 0, z), Quaternion.identity);
                walltile.GetComponent<LocalTile>().row = row;
                walltile.GetComponent<LocalTile>().col = col;
                break;

            case "**":
                GameObject doortile = Instantiate(gameAssets.DoorTile, new Vector3(x, 0, z), Quaternion.identity);
                doortile.GetComponent<LocalTile>().row = row;
                doortile.GetComponent<LocalTile>().col = col;
                break;

            default:
                Debug.Log(string.Format("SpawnTile() received code {0}, but can not find responding tile", code));
                break;
        }
    }
}



