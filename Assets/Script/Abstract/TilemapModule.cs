using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class TilemapModule : MonoBehaviour
{
    [SerializeField] protected Tilemap tilemap;
    public int priority = 0;

    public virtual bool IsBlocking(Vector3Int pos) => false;

    public virtual void OnEnterTile(GameObject obj, Vector3Int pos) { }

    public virtual Color GetGizmoColor(Vector3Int pos) => Color.clear;

    public bool HasTile(Vector3Int pos)
    {
        return tilemap != null && tilemap.HasTile(pos);
    }

    public Tilemap GetTilemap()
    {
        return tilemap;
    }
}
