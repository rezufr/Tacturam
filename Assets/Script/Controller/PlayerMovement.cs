using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public TilemapController tilemapController;

    [Header("Settings")]
    public float moveSpeed = 7f;    // Kecepatan gerak (Velocity)
    public float stepDelay = 0.05f; // Jeda antar langkah
    
    [Header("Orientation")]
    public Vector2Int facingDirection = Vector2Int.up; // Arah hadap awal (Up)
    
    private bool isMoving = false;
    public bool IsMoving => isMoving; // Getter untuk GameManager

    /// <summary>
    /// Berputar 90 derajat. 
    /// rotDir: 1 untuk Kanan (Clockwise), -1 untuk Kiri (Counter-Clockwise)
    /// </summary>
    public void RotatePlayer(int rotDir)
    {
        if (isMoving) return;
        StartCoroutine(RotateRoutine(rotDir));
    }

    private IEnumerator RotateRoutine(int rotDir)
    {
        isMoving = true;

        if (rotDir == 1) // Kanan
        {
            if (facingDirection == Vector2Int.up) facingDirection = Vector2Int.right;
            else if (facingDirection == Vector2Int.right) facingDirection = Vector2Int.down;
            else if (facingDirection == Vector2Int.down) facingDirection = Vector2Int.left;
            else if (facingDirection == Vector2Int.left) facingDirection = Vector2Int.up;
        }
        else // Kiri
        {
            if (facingDirection == Vector2Int.up) facingDirection = Vector2Int.left;
            else if (facingDirection == Vector2Int.left) facingDirection = Vector2Int.down;
            else if (facingDirection == Vector2Int.down) facingDirection = Vector2Int.right;
            else if (facingDirection == Vector2Int.right) facingDirection = Vector2Int.up;
        }

        UpdateVisualRotation();
        yield return new WaitForSeconds(0.35f); // Jeda durasi animasi rotasi

        isMoving = false;
    }

    private void UpdateVisualRotation()
    {
        float targetAngle = 0;
        if (facingDirection == Vector2Int.up) targetAngle = 0;
        else if (facingDirection == Vector2Int.right) targetAngle = -90f;
        else if (facingDirection == Vector2Int.down) targetAngle = -180f;
        else if (facingDirection == Vector2Int.left) targetAngle = -270f;

        transform.DORotate(new Vector3(0, 0, targetAngle), 0.3f).SetEase(Ease.OutBack).SetLink(gameObject);
    }

    /// <summary>
    /// Mendapatkan arah samping berdasarkan arah hadap saat ini.
    /// isRight: true untuk arah kanan player, false untuk kiri player.
    /// </summary>
    public Vector2Int GetSideDirection(bool isRight)
    {
        if (isRight)
        {
            // Facing Up (0,1) -> Right (1,0)
            return new Vector2Int(facingDirection.y, -facingDirection.x);
        }
        else
        {
            // Facing Up (0,1) -> Left (-1,0)
            return new Vector2Int(-facingDirection.y, facingDirection.x);
        }
    }

    /// <summary>
    /// Memulai proses pergerakan bertahap (sequential).
    /// </summary>
    public void Move(Vector2Int direction, int distance = 1)
    {
        if (isMoving) return;
        if (tilemapController == null || tilemapController.gridTilemap == null)
        {
            Debug.LogError("PlayerMovement: TilemapController belum di-assign!");
            return;
        }

        StartCoroutine(MoveRoutine(direction, distance));
    }

    private IEnumerator MoveRoutine(Vector2Int direction, int distance)
    {
        isMoving = true;

        for (int i = 0; i < distance; i++)
        {
            Vector3Int currentGridPos = tilemapController.gridTilemap.WorldToCell(transform.position);
            Vector3Int targetGridPos = currentGridPos + (Vector3Int)direction;

            if (tilemapController.CanMoveTo(targetGridPos))
            {
                // Gerak Mulus ke Target
                Vector3 targetWorldPos = tilemapController.gridTilemap.GetCellCenterWorld(targetGridPos);
                yield return StartCoroutine(MoveToPosition(targetWorldPos));

                // Cek efek tile setelah sampai
                if (tilemapController.IsTileType(targetGridPos, TileType.Stop))
                {
                    Debug.Log("FORCE STOP!");
                    break; 
                }

                if (tilemapController.IsTileType(targetGridPos, TileType.Slip))
                {
                    Debug.Log("SLIP! Maju ekstra.");
                    Vector3Int slipTargetGridPos = targetGridPos + (Vector3Int)direction;
                    if (tilemapController.CanMoveTo(slipTargetGridPos))
                    {
                        Vector3 slipTargetWorldPos = tilemapController.gridTilemap.GetCellCenterWorld(slipTargetGridPos);
                        yield return StartCoroutine(MoveToPosition(slipTargetWorldPos));
                    }
                }

                yield return new WaitForSeconds(stepDelay);
            }
            else
            {
                break; // Terpentok tembok/batas
            }
        }

        isMoving = false;
    }

    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        // Pastikan Z tetap sama (penting buat Game 2D agar distance check akurat)
        targetPos.z = transform.position.z;

        // Geser posisi secara bertahap (Velocity-based)
        while (Vector2.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
    }

    // --- TEST FUNCTIONS ---
    [ContextMenu("Test Move 2 Up")] public void Move2Up() => Move(Vector2Int.up, 2);
    [ContextMenu("Test Move Up")] public void TestMoveUp() => Move(Vector2Int.up, 1);
    [ContextMenu("Test Move Down")] public void TestMoveDown() => Move(Vector2Int.down, 1);
    [ContextMenu("Test Move Left")] public void TestMoveLeft() => Move(Vector2Int.left, 1);
    [ContextMenu("Test Move Right")] public void TestMoveRight() => Move(Vector2Int.right, 1);
}

