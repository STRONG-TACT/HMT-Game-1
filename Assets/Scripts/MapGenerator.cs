using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public TextAsset levelTextFile;

    // Start is called before the first frame update
    void Start()
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
            }
        }
    }
}



