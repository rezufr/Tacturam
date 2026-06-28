using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    public TilemapController tilemapController;

    [Header("Settings")]
    public float moveSpeed = 7f;    // Kecepatan gerak (Velocity)
    public float stepDelay = 0.05f; // Jeda antar langkah

    [Header("Orientation")]
    public Vector2Int facingDirection = Vector2Int.down; // Arah hadap awal (Down)
    private bool isMoving = false;
    public bool IsMoving => isMoving; // Getter untuk GameManager

    [Header("Animation")]
    public Animator enemyAnimator;

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
        if (facingDirection == Vector2Int.up) AnimationPlayer(1);
        else if (facingDirection == Vector2Int.right) AnimationPlayer(1);
        else if (facingDirection == Vector2Int.down) AnimationPlayer(1);
        else if (facingDirection == Vector2Int.left) AnimationPlayer(1);
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
            Debug.LogError("EnemyMovement: TilemapController belum di-assign!");
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
                    AnimationPlayer(1); // Set animasi idle
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
                AnimationPlayer(1); // Set animasi idle
                break; // Terpentok tembok/batas
            }
        }

        isMoving = false;
        AnimationPlayer(1); // Set animasi idle setelah selesai bergerak
    }

    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        // Pastikan Z tetap sama (penting buat Game 2D agar distance check akurat)
        targetPos.z = transform.position.z;

        AnimationPlayer(2); // Set animasi berjalan
        // Geser posisi secara bertahap (Velocity-based)
        while (Vector2.Distance(transform.position, targetPos) > 0.001f)
        {

            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
    }

    public void AnimationPlayer(int actionValue)
    {
        if (enemyAnimator == null)
        {
            Debug.LogWarning("EnemyMovement: Animator belum di-assign!");
            return;
        }

        if (facingDirection == Vector2Int.up)
        {
            enemyAnimator.SetBool("UpSide", true);
            enemyAnimator.SetBool("DownSide", false);
            enemyAnimator.SetBool("LeftSide", false);
            enemyAnimator.SetBool("RightSide", false);
        }
        else if (facingDirection == Vector2Int.down)
        {
            enemyAnimator.SetBool("DownSide", true);
            enemyAnimator.SetBool("UpSide", false);
            enemyAnimator.SetBool("LeftSide", false);
            enemyAnimator.SetBool("RightSide", false);
        }
        else if (facingDirection == Vector2Int.left)
        {
            enemyAnimator.SetBool("LeftSide", true);
            enemyAnimator.SetBool("UpSide", false);
            enemyAnimator.SetBool("DownSide", false);
            enemyAnimator.SetBool("RightSide", false);
        }
        else if (facingDirection == Vector2Int.right)
        {
            enemyAnimator.SetBool("RightSide", true);
            enemyAnimator.SetBool("UpSide", false);
            enemyAnimator.SetBool("DownSide", false);
            enemyAnimator.SetBool("LeftSide", false);
        }

        // Reset semua trigger
        enemyAnimator.ResetTrigger("Move");
        print("Resetting Move trigger");
        // enemyAnimator.ResetTrigger("Attack");

        // Set trigger sesuai actionValue
        switch (actionValue)
        {
            case 1:
                enemyAnimator.SetBool("Idle", true);
                break;
            case 2:
                enemyAnimator.SetBool("Idle", false);
                enemyAnimator.SetTrigger("Move");
                break;
            case 3:
                enemyAnimator.SetBool("Idle", false);
                enemyAnimator.SetTrigger("Attack");
                break;
            default:
                Debug.LogWarning($"EnemyMovement: ActionValue {actionValue} tidak dikenali.");
                break;
        }
    }
}
