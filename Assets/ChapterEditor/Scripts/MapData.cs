﻿using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChapterEditor
{
[Serializable]
public struct MapData : IPackable
{
    public Vector2Int SpaceSize;
    public string[] LayerNames;
    public string[] LayerData;

    public string Pack() => JsonUtility.ToJson(this);

    public void Unpack(string data) => this = JsonUtility.FromJson<MapData>(data);
}

}