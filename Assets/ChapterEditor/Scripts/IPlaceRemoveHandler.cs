﻿
using UnityEngine;

namespace ChapterEditor
{

public interface IPlaceRemoveHandler
{
    public abstract void ChangeAt(Vector2 worldPos, bool shouldPlaceNotRemove);
}

}