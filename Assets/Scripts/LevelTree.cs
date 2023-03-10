using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct LevelTree
{
    [System.Serializable]
    public struct LevelData
    {
        public int id;
        public string name;
        public int[] necessaryLevelsIDs;
        public string path;
        public bool completed;
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
        //GameObject.FindGameObjectWithTag("Test").SetActive(false);
        //StreamReader reader = new StreamReader("Assets/Resources/" + path);
        Object file = Resources.Load(path);
        GameObject.FindGameObjectWithTag("Test").SetActive(false);
        
        string json = ((TextAsset)file).text;
        //reader.Close();

        LevelTree tree = JsonUtility.FromJson<LevelTree>(json);
        return tree;
    }
}
