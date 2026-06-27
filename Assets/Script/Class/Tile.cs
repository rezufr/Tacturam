using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class Tile
{
    public TilemapRenderer tilemapRenderer;
    public TileType tileType;
    public int indexLayer;
    public bool isActive;
}

public enum TileType
{
    Move,
    Block,
    Telegraph,
    Stop,
    Slip,
}

