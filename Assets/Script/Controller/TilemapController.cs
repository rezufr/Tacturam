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

    [Header("Telegraph Preview Tilemap")]
    public Tilemap telegraphPreviewTilemap;

    [Header("Enemy Telegraph Preview Tilemap")]
    public Tilemap enemyTelegraphPreviewTilemap;

    [Header("Gizmos Settings")]
    public Transform player; // Tetap ada cuma buat visualisasi Gizmos saja
    public PlayerMovement playerMovement; // Tetap ada cuma buat visualisasi Gizmos saja
    public EnemyMovement[] enemyMovement; // Tetap ada cuma buat visualisasi Gizmos saja
    public Transform[] enemy;

    public int neighborRange = 1; // Jarak neighbor yang diperiksa (default 1)
    public Dir[] neighborDirections; // Arah neighbor yang diperiksa

    [Header("Telegraph Preview (Tilemap)")]
    [SerializeField] private TileBase previewPathTile;
    [SerializeField] private TileBase previewEndTile;
    [SerializeField] private Color enemyTelegraphColor = new Color(1f, 0.2f, 0.2f, 0.85f);

    private readonly Dictionary<Vector3Int, TileBase> telegraphPreviewTiles = new Dictionary<Vector3Int, TileBase>();
    private readonly Dictionary<Vector3Int, TileBase> enemyTelegraphPreviewTiles = new Dictionary<Vector3Int, TileBase>();
    private bool hasTelegraphPreview;

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

    public void BuildEnemyTelegraphPreview(EnemyMovement enemyMovement)
    {
        ClearEnemyTelegraphPreview();

        if (gridTilemap == null || enemyTelegraphPreviewTilemap == null || enemyMovement == null)
            return;

        BuildEnemyTelegraphPreviewInternal(enemyMovement);
        hasTelegraphPreview = enemyTelegraphPreviewTiles.Count > 0;
    }

    public void BuildAllEnemyTelegraphPreview(IList<EnemyMovement> enemyMovements)
    {
        ClearEnemyTelegraphPreview();

        if (gridTilemap == null || enemyTelegraphPreviewTilemap == null || enemyMovements == null)
            return;

        for (int i = 0; i < enemyMovements.Count; i++)
        {
            if (enemyMovements[i] == null)
                continue;

            BuildEnemyTelegraphPreviewInternal(enemyMovements[i]);
        }

        hasTelegraphPreview = enemyTelegraphPreviewTiles.Count > 0;
    }

    public void ClearTelegraphPreview()
    {
        if (telegraphPreviewTilemap != null)
        {
            foreach (var cell in telegraphPreviewTiles.Keys)
            {
                telegraphPreviewTilemap.SetTile(cell, null);
            }
        }

        telegraphPreviewTiles.Clear();
        hasTelegraphPreview = false;
    }

    public void ClearEnemyTelegraphPreview()
    {
        if (enemyTelegraphPreviewTilemap != null)
        {
            foreach (var cell in enemyTelegraphPreviewTiles.Keys)
            {
                enemyTelegraphPreviewTilemap.SetTile(cell, null);
                enemyTelegraphPreviewTilemap.SetColor(cell, Color.white);
            }
        }

        enemyTelegraphPreviewTiles.Clear();
    }

    public void BuildTelegraphPreview(Vector3Int startGridPos, Vector2Int startFacing, IList<CardController> selectedCards)
    {
        ClearTelegraphPreview();

        if (gridTilemap == null || telegraphPreviewTilemap == null || selectedCards == null || selectedCards.Count == 0)
            return;

        Vector3Int currentPos = startGridPos;
        Vector2Int currentFacing = startFacing;

        CardAction lastActionType = default;
        int lastActionValue = 0;
        bool lastActionFlipped = false;
        bool hasLastAction = false;
        bool lastActionWasRotate = false;

        for (int i = 0; i < selectedCards.Count; i++)
        {
            CardController card = selectedCards[i];
            if (card == null) continue;

            SimulateCardPreview(
                card.actionType,
                card.actionValue,
                card.IsFlipped,
                ref currentPos,
                ref currentFacing,
                ref lastActionType,
                ref lastActionValue,
                ref lastActionFlipped,
                ref hasLastAction,
                ref lastActionWasRotate);
        }

        if (!lastActionWasRotate)
        {
            AddTelegraphPreviewTile(currentPos, GetPreviewEndTile());
        }

        hasTelegraphPreview = telegraphPreviewTiles.Count > 0;
    }

    private void SimulateCardPreview(
        CardAction action,
        int value,
        bool flipped,
        ref Vector3Int currentPos,
        ref Vector2Int currentFacing,
        ref CardAction lastActionType,
        ref int lastActionValue,
        ref bool lastActionFlipped,
        ref bool hasLastAction,
        ref bool lastActionWasRotate)
    {
        if (action == CardAction.Copy)
        {
            if (hasLastAction)
            {
                SimulateCardPreview(
                    lastActionType,
                    lastActionValue,
                    lastActionFlipped,
                    ref currentPos,
                    ref currentFacing,
                    ref lastActionType,
                    ref lastActionValue,
                    ref lastActionFlipped,
                    ref hasLastAction,
                    ref lastActionWasRotate);
            }

            return;
        }

        switch (action)
        {
            case CardAction.Move:
            case CardAction.Dash:
                SimulateLinearPreviewMove(ref currentPos, currentFacing, value, ref currentFacing, true);
                lastActionWasRotate = false;
                break;

            case CardAction.Back:
                SimulateLinearPreviewMove(ref currentPos, new Vector2Int(-currentFacing.x, -currentFacing.y), value, ref currentFacing, true);
                lastActionWasRotate = false;
                break;

            case CardAction.Rotate:
                currentFacing = RotateFacing(currentFacing, flipped ? -1 : 1);
                AddTelegraphPreviewTile(currentPos, GetRotatePreviewTile(currentFacing));
                lastActionWasRotate = true;
                break;

            case CardAction.Side:
                SimulateLinearPreviewMove(ref currentPos, GetSideDirection(currentFacing, !flipped), value, ref currentFacing, true);
                lastActionWasRotate = false;
                break;
        }

        lastActionType = action;
        lastActionValue = value;
        lastActionFlipped = flipped;
        hasLastAction = true;
    }

    private void SimulateLinearPreviewMove(ref Vector3Int currentPos, Vector2Int direction, int distance, ref Vector2Int facingForAttack, bool allowAttackPreview)
    {
        Vector3Int step = new Vector3Int(direction.x, direction.y, 0);

        for (int i = 0; i < Mathf.Max(0, distance); i++)
        {
            Vector3Int targetPos = currentPos + step;
            if (!CanMoveTo(targetPos))
                break;

            TileBase previewTile = GetPreviewTileForStep(targetPos, facingForAttack, allowAttackPreview);
            AddTelegraphPreviewTile(targetPos, previewTile);

            if (previewTile == AttackTile)
            {
                UpdatePreviewFacingAfterAttack(targetPos, direction, ref facingForAttack);
            }

            currentPos = targetPos;

            if (IsTileType(targetPos, TileType.Stop))
                break;

            if (IsTileType(targetPos, TileType.Slip))
            {
                Vector3Int slipTargetPos = currentPos + step;
                if (CanMoveTo(slipTargetPos))
                {
                    TileBase slipPreviewTile = GetPreviewTileForStep(slipTargetPos, facingForAttack, allowAttackPreview);
                    AddTelegraphPreviewTile(slipTargetPos, slipPreviewTile);

                    if (slipPreviewTile == AttackTile)
                    {
                        UpdatePreviewFacingAfterAttack(slipTargetPos, direction, ref facingForAttack);
                    }

                    currentPos = slipTargetPos;

                    if (IsTileType(slipTargetPos, TileType.Stop))
                        break;
                }
            }
        }
    }

    private Vector2Int RotateFacing(Vector2Int facing, int rotDir)
    {
        if (rotDir == 1)
        {
            if (facing == Vector2Int.up) return Vector2Int.right;
            if (facing == Vector2Int.right) return Vector2Int.down;
            if (facing == Vector2Int.down) return Vector2Int.left;
            return Vector2Int.up;
        }

        if (facing == Vector2Int.up) return Vector2Int.left;
        if (facing == Vector2Int.left) return Vector2Int.down;
        if (facing == Vector2Int.down) return Vector2Int.right;
        return Vector2Int.up;
    }

    private Vector2Int GetSideDirection(Vector2Int facing, bool isRight)
    {
        if (isRight)
            return new Vector2Int(facing.y, -facing.x);

        return new Vector2Int(-facing.y, facing.x);
    }

    private void AddTelegraphPreviewTile(Vector3Int cell, TileBase tile)
    {
        if (telegraphPreviewTilemap == null || tile == null)
            return;

        if (telegraphPreviewTiles.TryGetValue(cell, out TileBase existingTile))
        {
            if (existingTile == AttackTile && tile != AttackTile)
                return;

            if (existingTile != AttackTile && tile == GetMovePreviewTile())
                return;
        }

        telegraphPreviewTiles[cell] = tile;
        telegraphPreviewTilemap.SetTile(cell, tile);
    }

    private void AddEnemyTelegraphPreviewTile(Vector3Int cell, TileBase tile)
    {
        if (enemyTelegraphPreviewTilemap == null || tile == null)
            return;

        if (enemyTelegraphPreviewTiles.TryGetValue(cell, out TileBase existingTile))
        {
            if (existingTile == AttackTile && tile != AttackTile)
                return;

            if (existingTile != AttackTile && tile == GetMovePreviewTile())
                return;
        }

        enemyTelegraphPreviewTiles[cell] = tile;
        enemyTelegraphPreviewTilemap.SetTile(cell, tile);
        enemyTelegraphPreviewTilemap.SetColor(cell, enemyTelegraphColor);
    }

    private TileBase GetMovePreviewTile()
    {
        if (previewPathTile != null)
            return previewPathTile;

        return moveTile;
    }

    private TileBase GetPreviewTileForStep(Vector3Int targetPos, Vector2Int facingForAttack, bool allowAttackPreview)
    {
        if (!allowAttackPreview)
            return GetMovePreviewTile();

        if (CheckEnemyIsThere(targetPos))
            return GetPreviewAttackTile();

        if (HasEnemyInFacingNeighbor(targetPos, facingForAttack))
            return GetPreviewAttackTile();

        return GetMovePreviewTile();
    }

    private bool HasEnemyInFacingNeighbor(Vector3Int gridPos, Vector2Int facingForAttack)
    {
        if (enemy == null || gridTilemap == null)
            return false;

        Vector3Int neighborPos = gridPos + new Vector3Int(facingForAttack.x, facingForAttack.y, 0);
        for (int i = 0; i < enemy.Length; i++)
        {
            if (enemy[i] == null)
                continue;

            Vector3Int enemyGridPos = gridTilemap.WorldToCell(enemy[i].position);
            if (enemyGridPos == neighborPos)
                return true;
        }

        return false;
    }

    private TileBase GetPreviewAttackTile()
    {
        if (AttackTile != null)
            return AttackTile;

        if (previewEndTile != null)
            return previewEndTile;

        Debug.LogWarning("TilemapController: AttackTile belum di-assign, telegraph attack akan terlihat seperti move tile.");
        return GetMovePreviewTile();
    }

    private bool HasPlayerAt(Vector3Int gridPos)
    {
        if (player == null || gridTilemap == null)
            return false;

        Vector3Int playerGridPos = gridTilemap.WorldToCell(player.position);
        return playerGridPos == gridPos;
    }

    private int GetEnemyMoveDistance(EnemyMovement enemyMovement)
    {
        if (enemyMovement == null)
            return 0;

        return enemyMovement.enemyType switch
        {
            EnemyType.Nebelss => 2,
            EnemyType.Hook => 1,
            _ => 0,
        };
    }

    private void BuildEnemyTelegraphPreviewInternal(EnemyMovement enemyMovement)
    {
        if (enemyMovement == null || gridTilemap == null)
            return;

        Vector3Int currentPos = gridTilemap.WorldToCell(enemyMovement.transform.position);
        Vector2Int moveDirection = enemyMovement.facingDirection;
        int moveDistance = GetEnemyMoveDistance(enemyMovement);

        for (int i = 0; i < moveDistance; i++)
        {
            Vector3Int targetPos = currentPos + new Vector3Int(moveDirection.x, moveDirection.y, 0);
            if (!CanMoveTo(targetPos))
                break;

            if (HasPlayerAt(targetPos))
            {
                AddEnemyTelegraphPreviewTile(targetPos, GetPreviewAttackTile());
                break;
            }

            AddEnemyTelegraphPreviewTile(targetPos, GetMovePreviewTile());

            if (CheckMoveToPlayer(targetPos, enemyMovement))
            {
                Vector3Int attackPos = targetPos + new Vector3Int(moveDirection.x, moveDirection.y, 0);
                AddEnemyTelegraphPreviewTile(attackPos, GetPreviewAttackTile());
                break;
            }

            currentPos = targetPos;

            if (IsTileType(targetPos, TileType.Stop))
                break;

            if (IsTileType(targetPos, TileType.Slip))
            {
                Vector3Int slipTargetPos = currentPos + new Vector3Int(moveDirection.x, moveDirection.y, 0);
                if (CanMoveTo(slipTargetPos))
                {
                    AddEnemyTelegraphPreviewTile(slipTargetPos, GetMovePreviewTile());
                    currentPos = slipTargetPos;

                    if (CheckMoveToPlayer(slipTargetPos, enemyMovement))
                    {
                        Vector3Int attackPos = slipTargetPos + new Vector3Int(moveDirection.x, moveDirection.y, 0);
                        AddEnemyTelegraphPreviewTile(attackPos, GetPreviewAttackTile());
                        break;
                    }

                    if (IsTileType(slipTargetPos, TileType.Stop))
                        break;
                }
            }
        }
    }

    private void UpdatePreviewFacingAfterAttack(Vector3Int targetPos, Vector2Int stepDirection, ref Vector2Int facingForAttack)
    {
        if (CheckEnemyIsThere(targetPos))
        {
            facingForAttack = stepDirection;
            return;
        }

        facingForAttack = stepDirection;
    }

    private TileBase GetRotatePreviewTile(Vector2Int facing)
    {
        if (facing == Vector2Int.up && RotateUpTile != null) return RotateUpTile;
        if (facing == Vector2Int.down && RotateDownTile != null) return RotateDownTile;
        if (facing == Vector2Int.left && RotateLeftTile != null) return RotateLeftTile;
        if (facing == Vector2Int.right && RotateRightTile != null) return RotateRightTile;

        return GetMovePreviewTile();
    }

    private TileBase GetPreviewEndTile()
    {
        if (previewEndTile != null)
            return previewEndTile;

        return GetMovePreviewTile();
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

        // Telegraph preview now renders on the telegraph Tilemap using TileBase tiles,
        // so there is no separate Gizmos overlay for the preview.
    }


    public void CheckTileType() { }
    public void BlockTile(Tile tile) { Debug.Log("Blocking tile: " + tile.tilemapRenderer.name); }
    public void TelegraphTile(Tile tile) { Debug.Log("Telegraphing tile: " + tile.tilemapRenderer.name); }
    public void MoveTile(Tile tile) { Debug.Log("Moving tile: " + tile.tilemapRenderer.name); }
}


