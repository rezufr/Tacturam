using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapController : MonoBehaviour
{
    [Header("References")]
    public Tilemap gridTilemap;
    
    [Header("Tiles")]
    public Tile[] Tile;

    [Header("Gizmos Settings")]
    public Transform player; // Tetap ada cuma buat visualisasi Gizmos saja

    void Start()
    {
        
    }

    public void CalculateTilemapMove()
    {
        for (int i = 0; i < Tile.Length; i++)
        {
            if (Tile[i].tileType == TileType.Block)
            {
                Tile[i].isActive = true;
                BlockTile(Tile[i]);
            }
        }
    }

    public bool IsTileWalkable(Vector3Int gridPos)
    {
        foreach (var t in Tile)
        {
            if (t.tileType != TileType.Move || t.tilemapRenderer == null) continue;
            
            Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
            if (tm != null && tm.HasTile(gridPos)) return true;
        }
        return false;
    }

    public bool IsTileBlocked(Vector3Int gridPos)
    {
        foreach (var t in Tile)
        {
            if (t.tileType != TileType.Block || t.tilemapRenderer == null) continue;

            Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
            if (tm != null && tm.HasTile(gridPos)) return true;
        }
        return false;
    }

    /// <summary>
    /// Cek apakah player bisa pindah ke koordinat ini.
    /// Syarat: Punya tile di layer 'Move' DAN tidak ada tile di layer 'Block'.
    /// </summary>
    public bool CanMoveTo(Vector3Int gridPos)
    {
        return IsTileWalkable(gridPos) && !IsTileBlocked(gridPos);
    }


    public bool IsTileType(Vector3Int gridPos, TileType type)
    {
        foreach (var t in Tile)
        {
            if (t.tileType != type || t.tilemapRenderer == null) continue;
            Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
            if (tm != null && tm.HasTile(gridPos)) return true;
        }
        return false;
    }

    public Tile GetTileDataAt(Vector3Int gridPos)
    {
        if (Tile == null) return null;
        // Cek layer fungsional dulu (selain Move)
        foreach (var t in Tile)
        {
            if (t.tilemapRenderer == null || t.tileType == TileType.Move) continue;
            Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
            if (tm != null && tm.HasTile(gridPos)) return t;
        }
        // Terakhir cek layer Move
        foreach (var t in Tile)
        {
            if (t.tileType == TileType.Move && t.tilemapRenderer != null)
            {
                Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
                if (tm != null && tm.HasTile(gridPos)) return t;
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        // 1. DRAW ALL REGISTERED LAYERS
        if (Tile != null)
        {
            foreach (var t in Tile)
            {
                if (t.tilemapRenderer == null) continue;
                Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
                if (tm == null) continue;

                BoundsInt bounds = tm.cellBounds;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (tm.HasTile(pos))
                    {
                        Vector3 worldCenter = tm.GetCellCenterWorld(pos);
                        switch (t.tileType)
                        {
                            case TileType.Move: Gizmos.color = new Color(0, 1, 1, 0.3f); break; 
                            case TileType.Block: Gizmos.color = new Color(1, 0, 0, 0.5f); break;
                            case TileType.Telegraph: Gizmos.color = new Color(1, 1, 0, 0.5f); break;
                            case TileType.Stop: Gizmos.color = new Color(0.8f, 0, 1, 0.6f); break; // Ungu
                            case TileType.Slip: Gizmos.color = new Color(1, 0.5f, 0, 0.6f); break; // Oranye
                            default: Gizmos.color = new Color(1, 1, 1, 0.1f); break;
                        }
                        Gizmos.DrawCube(worldCenter, tm.cellSize * 0.9f);
                    }
                }
            }
        }

        // 2. PLAYER MOVE RANGE (Checking)
        if (player != null && gridTilemap != null)
        {
            Vector3Int pGridPos = gridTilemap.WorldToCell(player.position);
            Vector3Int[] neighbors = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            
            foreach (var dir in neighbors)
            {
                Vector3Int targetPos = pGridPos + dir;
                if (CanMoveTo(targetPos))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(gridTilemap.GetCellCenterWorld(targetPos), gridTilemap.cellSize * 1.05f);
                }
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(gridTilemap.GetCellCenterWorld(pGridPos), 0.3f);
        }
    }


    public void CheckTileType() { }
    public void BlockTile(Tile tile) { Debug.Log("Blocking tile: " + tile.tilemapRenderer.name); }
    public void TelegraphTile(Tile tile) { Debug.Log("Telegraphing tile: " + tile.tilemapRenderer.name); }
    public void MoveTile(Tile tile) { Debug.Log("Moving tile: " + tile.tilemapRenderer.name); }
}


