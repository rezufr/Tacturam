using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public TilemapController tilemapController;
    public GameManager gameManager;

    [Header("Settings")]
    public float moveSpeed = 7f;    // Kecepatan gerak (Velocity)
    public float stepDelay = 0.05f; // Jeda antar langkah
    public int damage = 1; // Jumlah damage player
    public int takenCardCount = 0; // Jumlah kartu yang diambil dari musuh

    [Header("Orientation")]
    public Vector2Int facingDirection = Vector2Int.down; // Arah hadap awal (Down)
    private bool isMoving = false;
    private bool isAttacking = false;
    public bool IsMoving => isMoving; // Getter untuk GameManager
    public bool IsAttacking => isAttacking; // Getter untuk GameManager

    [Header("Animation")]
    public Animator playerAnimator;
    public SpriteRenderer playerSpriteRenderer;

    /// <summary>
    /// Berputar 90 derajat. 
    /// rotDir: 1 untuk Kanan (Clockwise), -1 untuk Kiri (Counter-Clockwise)
    /// </summary>

    void Start()
    {
        tilemapController.CalculateLayerForCharacter(transform, playerSpriteRenderer); // Update sorting order saat start
    }

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

    public void UpdateVisualRotation()
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
                if (tilemapController.CheckEnemyIsThere(targetGridPos) && !isAttacking)
                {
                    Debug.Log("Enemy detected! Stopping movement for attack.");
                    yield return StartCoroutine(FaceTargetGridPosBeforeAttack(targetGridPos));
                    AnimationPlayer(3); // Set animasi attack
                    isAttacking = true;
                    if (tilemapController.AttackEnemyAt(targetGridPos, damage)) // Panggil fungsi attack di TilemapController
                    {
                        yield return new WaitForSeconds(0.9f); // Jeda durasi animasi attack
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.9f); // Jeda durasi animasi attack
                        break; // Enemy tidak berhasil diserang, hentikan pergerakan player
                    }
                }
                // Gerak Mulus ke Target
                Vector3 targetWorldPos = tilemapController.gridTilemap.GetCellCenterWorld(targetGridPos);
                yield return StartCoroutine(MoveToPosition(targetWorldPos));

                // cek tile neighbor apakah ada enemy, jika ada maka hentikan pergerakan player untuk attack lalu lanjutkan move ke target tile
                if (tilemapController.CheckMoveToEnemy(targetGridPos) && !isAttacking)
                {
                    Debug.Log("Enemy detected nearby! Stopping movement for attack.");

                    Vector3Int attackTargetGridPos = targetGridPos + new Vector3Int(facingDirection.x, facingDirection.y, 0);
                    yield return StartCoroutine(FaceTargetGridPosBeforeAttack(attackTargetGridPos));
                    isAttacking = true;
                    AnimationPlayer(3);
                    if (tilemapController.AttackEnemyAtNeighbor(targetGridPos, damage)) // Panggil fungsi attack di TilemapController
                    {
                        yield return new WaitForSeconds(0.9f); // Jeda durasi animasi attack
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.9f);
                        break; // Enemy tidak berhasil diserang, hentikan pergerakan player
                    }
                }

                if (tilemapController.IsTileType(targetGridPos, TileType.Finish))
                {
                    Debug.Log("Player reached the finish tile!");
                    gameManager.OnPlayerReachFinish();
                    break; // Hentikan pergerakan setelah mencapai tile Finish
                }

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

        isAttacking = false; // Reset status attack setelah selesai bergerak
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
        tilemapController.CalculateLayerForCharacter(transform, playerSpriteRenderer); // Update sorting order saat bergerak
        transform.position = targetPos;
    }

    private IEnumerator FaceTargetGridPosBeforeAttack(Vector3Int targetGridPos)
    {
        Vector3Int currentGridPos = tilemapController.gridTilemap.WorldToCell(transform.position);
        Vector3Int delta = targetGridPos - currentGridPos;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            facingDirection = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else if (delta != Vector3Int.zero)
        {
            facingDirection = delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        UpdateVisualRotation();
        yield return new WaitForSeconds(0.35f);
    }

    public void DestroyCard(int amount)
    {
        print($"Destroying {amount} card(s) from hand.");
        gameManager.DiscardCardPermanently(amount);
    }

    public void DiscardCardInHand(int amount)
    {
        print($"Discarding {amount} card(s) from hand.");
        gameManager.DiscardCardInHand(amount);
        takenCardCount += amount; // Update jumlah kartu yang diambil dari musuh
    }

    public void AnimationPlayer(int actionValue)
    {
        if (playerAnimator == null)
        {
            Debug.LogWarning("PlayerMovement: Animator belum di-assign!");
            return;
        }

        if (facingDirection == Vector2Int.up)
        {
            playerAnimator.SetBool("UpSide", true);
            playerAnimator.SetBool("DownSide", false);
            playerAnimator.SetBool("LeftSide", false);
            playerAnimator.SetBool("RightSide", false);
        }
        else if (facingDirection == Vector2Int.down)
        {
            playerAnimator.SetBool("DownSide", true);
            playerAnimator.SetBool("UpSide", false);
            playerAnimator.SetBool("LeftSide", false);
            playerAnimator.SetBool("RightSide", false);
        }
        else if (facingDirection == Vector2Int.left)
        {
            playerAnimator.SetBool("LeftSide", true);
            playerAnimator.SetBool("UpSide", false);
            playerAnimator.SetBool("DownSide", false);
            playerAnimator.SetBool("RightSide", false);
        }
        else if (facingDirection == Vector2Int.right)
        {
            playerAnimator.SetBool("RightSide", true);
            playerAnimator.SetBool("UpSide", false);
            playerAnimator.SetBool("DownSide", false);
            playerAnimator.SetBool("LeftSide", false);
        }

        // Reset semua trigger
        playerAnimator.ResetTrigger("Move");
        print("Resetting Move trigger and Attack trigger");
        // playerAnimator.ResetTrigger("Attack");

        // Set trigger sesuai actionValue
        switch (actionValue)
        {
            case 1:
                playerAnimator.SetBool("Idle", true);
                break;
            case 2:
                playerAnimator.SetTrigger("Move");
                break;
            case 3:
                print("Setting Attack trigger");
                playerAnimator.SetTrigger("Attack");
                break;
            default:
                Debug.LogWarning($"PlayerMovement: ActionValue {actionValue} tidak dikenali.");
                break;
        }
    }

    // --- TEST FUNCTIONS ---
    [ContextMenu("Test Move 2 Up")] public void Move2Up() => Move(Vector2Int.up, 2);
    [ContextMenu("Test Move Up")] public void TestMoveUp() => Move(Vector2Int.up, 1);
    [ContextMenu("Test Move Down")] public void TestMoveDown() => Move(Vector2Int.down, 1);
    [ContextMenu("Test Move Left")] public void TestMoveLeft() => Move(Vector2Int.left, 1);
    [ContextMenu("Test Move Right")] public void TestMoveRight() => Move(Vector2Int.right, 1);
}

