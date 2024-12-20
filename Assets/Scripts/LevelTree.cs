using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct LevelTree
{
    [System.Serializable]
    public class LevelData
    {
        public int id;
        public string name;
        public string path;
        public bool published;
    }

    public LevelData[] levels;

    public int GetLevelIndex (int id)
    {
        for (int i = 0; i < levels.Length; i++)
            if (levels[i].id == id)
                return i;

        return -1;
    }

    public static LevelTree Extract (string path)
    {
        Object file = Resources.Load(path);
        
        string json = ((TextAsset)file).text;

        LevelTree tree = JsonUtility.FromJson<LevelTree>(json);
        return tree;
    }

    public static LevelTree ExtractFromText (string text)
    {
        LevelTree tree = JsonUtility.FromJson<LevelTree>(text);
        return tree;
    }
}
