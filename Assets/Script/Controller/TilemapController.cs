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

    [Header("Telegraph Tiles")]
    public TileBase moveTile;
    public TileBase RotateUpTile;
    public TileBase RotateDownTile;
    public TileBase RotateLeftTile;
    public TileBase RotateRightTile;
    public TileBase AttackTile;

    [Header("Gizmos Settings")]
    public Transform player; // Tetap ada cuma buat visualisasi Gizmos saja
    public PlayerMovement playerMovement; // Tetap ada cuma buat visualisasi Gizmos saja
    public EnemyMovement[] enemyMovement; // Tetap ada cuma buat visualisasi Gizmos saja
    public Transform[] enemy;

    public int neighborRange = 1; // Jarak neighbor yang diperiksa (default 1)
    public Dir[] neighborDirections; // Arah neighbor yang diperiksa

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

    public void CalculateTilemapTelegraph()
    {
        for (int i = 0; i < Tile.Length; i++)
        {
            if (Tile[i].tileType == TileType.Telegraph)
            {
                Tile[i].isActive = true;
                TelegraphTile(Tile[i]);
            }
        }
    }

    public void CalculateTileTelegraphPlayer(Vector3Int gridPos, Direction dir, int range)
    {
        for (int i = 0; i < Tile.Length; i++)
        {
            if (Tile[i].tileType != TileType.Telegraph) continue;

            Vector2Int directionVector = dir switch
            {
                Direction.Up => Vector2Int.up,
                Direction.Down => Vector2Int.down,
                Direction.Left => Vector2Int.left,
                Direction.Right => Vector2Int.right,
                _ => Vector2Int.zero
            };

            for (int j = 1; j <= range; j++)
            {
                Vector3Int targetPos = gridPos + new Vector3Int(directionVector.x, directionVector.y, 0) * j;
                if (CanMoveTo(targetPos))
                {
                    if (CheckEnemyIsThere(targetPos))
                    {
                        Tile[i].tilemap.SetTile(targetPos, AttackTile);
                    }
                    else if (CheckMoveToEnemy(targetPos))
                    {
                        Tile[i].tilemap.SetTile(targetPos, AttackTile);
                    }
                    else if (directionVector == playerMovement.facingDirection)
                    {
                        Tile[i].tilemap.SetTile(targetPos, rotateTileForDirection(dir));
                    }
                    else
                    {
                        Tile[i].tilemap.SetTile(targetPos, moveTile);
                    }
                }
                else
                {
                    break; // Stop jika menemukan tile yang tidak bisa dilewati
                }
            }
        }
    }

    public TileBase rotateTileForDirection(Direction dir)
    {
        return dir switch
        {
            Direction.Up => RotateUpTile,
            Direction.Down => RotateDownTile,
            Direction.Left => RotateLeftTile,
            Direction.Right => RotateRightTile,
            _ => null
        };
    }

    public void CalculateTileTelegraphEnemy(Vector3Int gridPos, Direction dir, int range)
    {
        for (int i = 0; i < Tile.Length; i++)
        {
            if (Tile[i].tileType != TileType.Telegraph) continue;

            Vector3Int directionVector = dir switch
            {
                Direction.Up => Vector3Int.up,
                Direction.Down => Vector3Int.down,
                Direction.Left => Vector3Int.left,
                Direction.Right => Vector3Int.right,
                _ => Vector3Int.zero
            };

            for (int j = 1; j <= range; j++)
            {
                Vector3Int targetPos = gridPos + directionVector * j;
                if (CanMoveTo(targetPos))
                {
                    Tile[i].tilemap.SetTile(targetPos, moveTile);
                }
                else
                {
                    break; // Stop jika menemukan tile yang tidak bisa dilewati
                }
            }
        }
    }

    public void CalculateLayerForCharacter(Transform character, SpriteRenderer spr) // check apakah player atau enemy ada di tile paling atas atau bawah jadi kalkulasi semua baris y agar bisa tentukan sorting order player atau enemy
    {
        BoundsInt bounds = gridTilemap.cellBounds;

        Vector3Int cell = gridTilemap.WorldToCell(character.position);

        int sortingOrder = bounds.yMax - cell.y + 1; // agar sorting order dimulai dari 1, bukan 0, karena tilemap sorting order dimulai dari 1
        spr.sortingOrder = sortingOrder;
    }

    public bool IsTileWalkable(Vector3Int gridPos)
    {
        foreach (var t in Tile)
        {
            if (t.tileType != TileType.Move || !t.isActive || t.tilemapRenderer == null) continue;

            Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
            if (tm != null && tm.HasTile(gridPos)) return true;
        }
        return false;
    }

    public bool IsTileBlocked(Vector3Int gridPos)
    {
        foreach (var t in Tile)
        {
            if (t.tileType != TileType.Block || !t.isActive || t.tilemapRenderer == null) continue;

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

    public bool CheckMoveToPlayer(Vector3Int gridPos, EnemyMovement enemyMovement) // check apakah ada player di gridPos neighbor, jika ada return true, jika tidak ada return false
    {
        if (player == null || gridTilemap == null) return false;

        Vector3Int targetCheckPos = gridPos +
            new Vector3Int(
                enemyMovement.facingDirection.x,
                enemyMovement.facingDirection.y,
                0
            );

        Vector3Int playerGridPos = gridTilemap.WorldToCell(player.position);
        if (playerGridPos == targetCheckPos)
            return true;

        return false;
    }

    public bool CheckMoveToEnemy(Vector3Int gridPos)
    {
        if (enemy == null || gridTilemap == null)
            return false;

        Vector3Int targetCheckPos = gridPos +
            new Vector3Int(
                playerMovement.facingDirection.x,
                playerMovement.facingDirection.y,
                0
            );

        foreach (var e in enemy)
        {
            if (e == null) continue;

            Vector3Int enemyPos = gridTilemap.WorldToCell(e.position);

            if (enemyPos == targetCheckPos)
                return true;
        }

        return false;
    }

    public bool CheckEnemyIsThere(Vector3Int gridPos) // check apakah ada enemy di gridPos, jika ada return true, jika tidak ada return false
    {
        if (enemy == null || gridTilemap == null) return true;
        foreach (var e in enemy)
        {
            if (e == null) continue;
            Vector3Int enemyGridPos = gridTilemap.WorldToCell(e.position);
            if (enemyGridPos == gridPos) return true;
        }
        return false;
    }

    public bool CheckPlayerIsThere(Vector3Int gridPos) // check apakah ada player di gridPos, jika ada return true, jika tidak ada return false
    {
        if (player == null || gridTilemap == null) return true;
        Vector3Int playerGridPos = gridTilemap.WorldToCell(player.position);
        if (playerGridPos == gridPos) return true;
        return false;
    }

    public bool AttackPlayerAt(Vector3Int gridPos, int amount, EnemyType enemyType)
    {
        if (player == null || gridTilemap == null) return false;
        Vector3Int playerGridPos = gridTilemap.WorldToCell(player.position);
        if (playerGridPos == gridPos)
        {
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                print($"Attacking player at {gridPos}.");
                if (enemyType == EnemyType.Hook)
                {
                    playerMovement.DestroyCard(amount); // Misal damage 1
                }
                else if (enemyType == EnemyType.Nebelss)
                {
                    playerMovement.DiscardCardInHand(amount); // Misal discard 1 kartu
                }
                else
                {
                    print("Unknown enemy type. No action taken.");
                }
                return true;
            }
        }
        return false;
    }

    public bool AttackPlayerAtNeighbor(Vector3Int enemyGridPos, int amount, EnemyType enemyType, EnemyMovement enemyMovement)
    {
        if (player == null || gridTilemap == null)
            return false;

        Vector3Int playerGridPos = gridTilemap.WorldToCell(player.position);

        Vector3Int attackPos = enemyGridPos +
            new Vector3Int(
                enemyMovement.facingDirection.x,
                enemyMovement.facingDirection.y,
                0
            );

        if (attackPos != playerGridPos)
            return false;

        if (enemyType == EnemyType.Hook)
        {
            playerMovement.DestroyCard(amount);
        }
        else if (enemyType == EnemyType.Nebelss)
        {
            playerMovement.DiscardCardInHand(amount);
        }

        return true;
    }

    public bool AttackEnemyAt(Vector3Int gridPos, int damage)
    {
        if (enemy == null || gridTilemap == null) return false;
        foreach (var e in enemy)
        {
            if (e == null) continue;
            Vector3Int enemyGridPos = gridTilemap.WorldToCell(e.position);
            if (enemyGridPos == gridPos)
            {
                EnemyMovement enemyMovement = e.GetComponent<EnemyMovement>();
                if (enemyMovement != null)
                {
                    print($"Attacking enemy at {gridPos} for {damage} damage.");
                    return enemyMovement.TakeDamage(damage);
                }
                break;
            }
        }
        return false;
    }

    public bool AttackEnemyAtNeighbor(Vector3Int gridPos, int damage)
    {
        if (enemy == null || gridTilemap == null) return false;
        Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in neighbors)
        {
            if (dir == playerMovement.facingDirection) // Hanya cek arah yang sama dengan facing direction player
            {
                Vector3Int dir3 = new Vector3Int(dir.x, dir.y, 0);
                Vector3Int neighborPos = gridPos + dir3;
                foreach (var e in enemy)
                {
                    if (e == null) continue;
                    Vector3Int enemyGridPos = gridTilemap.WorldToCell(e.position);
                    if (enemyGridPos == neighborPos)
                    {
                        EnemyMovement enemyMovement = e.GetComponent<EnemyMovement>();
                        if (enemyMovement != null)
                        {
                            print($"Attacking enemy at {neighborPos} for {damage} damage.");
                            return enemyMovement.TakeDamage(damage);
                        }
                        break;
                    }
                }
            }
        }
        return false;
    }


    public bool IsTileType(Vector3Int gridPos, TileType type)
    {
        foreach (var t in Tile)
        {
            if (t.tileType != type || !t.isActive || t.tilemapRenderer == null) continue;
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
            if (t.tilemapRenderer == null || t.tileType == TileType.Move || !t.isActive) continue;
            Tilemap tm = t.tilemapRenderer.GetComponent<Tilemap>();
            if (tm != null && tm.HasTile(gridPos)) return t;
        }
        // Terakhir cek layer Move
        foreach (var t in Tile)
        {
            if (t.tileType == TileType.Move && t.tilemapRenderer != null && t.isActive)
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
                if (t.tilemapRenderer == null || !t.isActive) continue;
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
            Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var dir in neighbors)
            {
                if (dir == playerMovement.facingDirection)
                {
                    Vector3Int dir3 = new Vector3Int(dir.x, dir.y, 0);
                    Vector3Int targetPos = pGridPos + dir3;
                    if (CanMoveTo(targetPos))
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(gridTilemap.GetCellCenterWorld(targetPos), gridTilemap.cellSize * 1.05f);
                    }
                }
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(gridTilemap.GetCellCenterWorld(pGridPos), 0.3f);
        }

        if (enemy != null && gridTilemap != null)
        {
            foreach (var e in enemy)
            {
                if (e == null) continue;
                EnemyMovement enemyMovement = e.GetComponent<EnemyMovement>();
                for (int i = 0; i < enemyMovement.rangeNeighbor; i++)
                {

                    Vector3Int eGridPos = gridTilemap.WorldToCell(e.position);
                    Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                    foreach (var dir in neighbors)
                    {
                        if (dir == enemyMovement.facingDirection)
                        {
                            Vector3Int dir3 = new Vector3Int(dir.x, dir.y, 0);
                            Vector3Int targetPos = eGridPos + dir3 * (i + 1); // Jarak neighbor sesuai range
                            if (CanMoveTo(targetPos))
                            {
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawWireCube(gridTilemap.GetCellCenterWorld(targetPos), gridTilemap.cellSize * 1.05f);
                            }
                        }
                    }
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(gridTilemap.GetCellCenterWorld(eGridPos), 0.3f);
                }
            }
        }
    }


    public void CheckTileType() { }
    public void BlockTile(Tile tile) { Debug.Log("Blocking tile: " + tile.tilemapRenderer.name); }
    public void TelegraphTile(Tile tile) { Debug.Log("Telegraphing tile: " + tile.tilemapRenderer.name); }
    public void MoveTile(Tile tile) { Debug.Log("Moving tile: " + tile.tilemapRenderer.name); }
}


