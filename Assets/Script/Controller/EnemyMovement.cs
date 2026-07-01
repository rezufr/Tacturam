using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Groom,
    Nebelss,
    Hook
}

public enum EnemyState
{
    Idle,
    Moving,
    Attacking,
    Dead,
    Rotating
}

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    public TilemapController tilemapController;
    public GameManager gameManager;

    [Header("Settings")]
    public float moveSpeed = 7f;    // Kecepatan gerak (Velocity)
    public float stepDelay = 0.05f; // Jeda antar langkah
    public int health = 1; // Jumlah nyawa musuh
    public EnemyType enemyType; // Tipe musuh
    public int turnOrder; // Urutan giliran musuh dalam satu ronde
    public int rangeNeighbor = 3; // Jarak neighbor yang diperiksa (default 1)
    public bool isAttacking = false; // Status apakah musuh sedang menyerang
    public int damage = 1; // Damage yang diberikan musuh saat menyerang
    public int takenCardCount = 0; // Jumlah kartu yang diambil dari player (default 0)

    [Header("References")]
    [Tooltip("Parent object tempat kartu-kartu berada")]
    public Transform handContainer;

    [Header("Orientation")]
    public Vector2Int facingDirection = Vector2Int.down; // Arah hadap awal (Down)
    private bool isMoving = false;
    public bool IsMoving => isMoving; // Getter untuk GameManager

    [Header("Animation")]
    public Animator enemyAnimator;
    public SpriteRenderer enemySpriteRenderer;

    void Start()
    {
        tilemapController.CalculateLayerForCharacter(transform, enemySpriteRenderer); // Update sorting order saat start
    }

    /// <summary>
    /// Berputar 90 derajat.
    /// rotDir: 1 untuk Kanan (Clockwise), -1 untuk Kiri (Counter-Clockwise)
    /// </summary>
    public void RotateEnemy(int rotDir)
    {
        print($"Enemy is rotating. Current facing: {facingDirection}, Rotation direction: {(rotDir == 1 ? "Right" : "Left")}");
        if (isMoving)
        {
            print("Enemy is already moving.");
            return;
        }
        StartCoroutine(RotateRoutine(rotDir));
    }

    private IEnumerator RotateRoutine(int rotDir)
    {
        print($"Starting rotation coroutine. Current facing: {facingDirection}, Rotation direction: {(rotDir == 1 ? "Right" : "Left")}");
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
        print($"Updating visual rotation. Current facing: {facingDirection}");
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

    public void SetFacingDirection(Vector2Int newDirection)
    {
        facingDirection = newDirection;
        UpdateVisualRotation();
    }

    public void SetFacingDirectionToPlayer(Vector2Int playerPosition)
    {
        print($"Setting facing direction towards player at {playerPosition}");
        Vector2Int currentPosition = Vector2Int.RoundToInt(transform.position);

        Vector2Int delta = playerPosition - currentPosition;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            facingDirection = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else if (delta != Vector2Int.zero)
        {
            facingDirection = delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        UpdateVisualRotation();

        print($"Enemy facing direction set to: {facingDirection}");
    }

    public bool TakeDamage(int damageAmount)
    {
        health -= damageAmount;
        Debug.Log($"Enemy took {damageAmount} damage. Remaining health: {health}");

        if (enemyType == EnemyType.Nebelss)
        {
            Debug.Log("Nebelss: Mengambalikan kartu yang diambil");
            gameManager.DrawTakenCards(); // Kembalikan kartu yang diambil ke tangan player
        }

        if (health <= 0)
        {
            Die();
            return true; // Enemy mati
        }

        return false; // Enemy masih hidup
    }

    public void Die()
    {
        Debug.Log("Enemy died!");
        // Tambahkan efek kematian atau animasi di sini jika diperlukan
        Destroy(gameObject);
    }

    public void SetMove(EnemyState state)
    {
        if (state == EnemyState.Moving)
        {
            if (enemyType == EnemyType.Nebelss)
            {
                Move(facingDirection, 2); // Nebelss bergerak 2 langkah
            }

            if (enemyType == EnemyType.Hook)
            {
                Move(facingDirection, 1); // Groom bergerak 1 langkah
            }
        }
        else if (state == EnemyState.Idle)
        {
            AnimationPlayer(1); // Set animasi idle
        }
        else if (state == EnemyState.Rotating)
        {
            SetFacingDirectionToPlayer(Vector2Int.RoundToInt(gameManager.player.transform.position)); // Menghadap ke player
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
                if (tilemapController.CheckEnemyIsThere(targetGridPos))
                {
                    Debug.Log("Enemy detected! Stopping movement.");
                    AnimationPlayer(1); // Set animasi idle
                    break; // Terpentok musuh lain
                }
                if (tilemapController.CheckPlayerIsThere(targetGridPos) && !isAttacking)
                {
                    Debug.Log("Player detected! Stopping movement for attack.");
                    AnimationPlayer(3); // Set animasi attack
                    isAttacking = true;
                    if (tilemapController.AttackPlayerAt(targetGridPos, damage, enemyType)) // Panggil fungsi attack di TilemapController
                    {
                        if (enemyType == EnemyType.Nebelss)
                        {
                            Debug.Log("Nebelss: Mengambil kartu dari player.");
                            takenCardCount += 1; // Update jumlah kartu yang diambil dari player
                        }
                        yield return new WaitForSeconds(0.9f); // Jeda durasi animasi attack
                        break; // Player berhasil menyerang, hentikan pergerakan enemy
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.9f); // Jeda durasi animasi attack
                        break; // Player tidak berhasil diserang, hentikan pergerakan enemy
                    }
                }
                // Gerak Mulus ke Target
                Vector3 targetWorldPos = tilemapController.gridTilemap.GetCellCenterWorld(targetGridPos);
                yield return StartCoroutine(MoveToPosition(targetWorldPos));

                // cek tile neighbor apakah ada player, jika ada maka hentikan pergerakan player untuk attack lalu lanjutkan move ke target tile
                if (tilemapController.CheckMoveToPlayer(targetGridPos, this) && !isAttacking)
                {
                    Debug.Log("Player detected nearby! Stopping movement for attack." + enemyType);

                    isAttacking = true;
                    if (tilemapController.AttackPlayerAtNeighbor(targetGridPos, damage, enemyType, this)) // Panggil fungsi attack di TilemapController
                    {
                        if (enemyType == EnemyType.Nebelss)
                        {
                            Debug.Log("Nebelss: Mengambil kartu dari player.");
                            takenCardCount += 1; // Update jumlah kartu yang diambil dari player
                        }
                        AnimationPlayer(3);
                        yield return new WaitForSeconds(0.9f); // Jeda durasi animasi attack
                        break; // Player berhasil diserang, hentikan pergerakan enemy
                    }
                    else
                    {
                        AnimationPlayer(3);
                        yield return new WaitForSeconds(0.9f);
                        break; // Player tidak berhasil diserang, hentikan pergerakan enemy
                    }
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

        isMoving = false;
        isAttacking = false; // Reset status menyerang setelah selesai bergerak

        if (enemyType == EnemyType.Nebelss)
        {
            print("Nebelss: Menghadap random setelah bergerak.");
            int randomRotation = Random.Range(0, 2); // 0 = Kiri, 1 = Kanan
            if (randomRotation == 0)
            {
                RotateEnemy(-1); // Putar ke kiri jika terhalang
            }
            else
            {
                RotateEnemy(1); // Putar ke kanan jika terhalang
            }
        }
        else if (enemyType == EnemyType.Hook)
        {
            print("Hook: Menghadap ke player setelah bergerak.");
            SetFacingDirectionToPlayer(Vector2Int.RoundToInt(gameManager.player.transform.position)); // Menghadap ke player
        }
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
        tilemapController.CalculateLayerForCharacter(transform, enemySpriteRenderer); // Update sorting order saat bergerak
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
