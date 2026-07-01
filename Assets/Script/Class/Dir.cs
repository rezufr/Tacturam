using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dir
{
    public Direction dirEnum; // Enum arah (Up, Down, Left, Right)
    public Vector2Int direction => dirEnum switch
    {
        Direction.Up => Vector2Int.up,
        Direction.Down => Vector2Int.down,
        Direction.Left => Vector2Int.left,
        Direction.Right => Vector2Int.right,
        _ => Vector2Int.zero
    }; // Arah (misal: Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right)
    public bool isActive;
}

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}