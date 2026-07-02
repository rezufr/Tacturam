using UnityEngine;
using System.Collections;

public class AttackProjectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private int moveDistance = 3;

    private Vector3 targetPosition;

    public void Initialize(Vector2Int direction)
    {
        targetPosition = transform.position + new Vector3(direction.x * moveDistance, direction.y * moveDistance, 0);

        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Kasih damage ke player di sini
            // other.GetComponent<PlayerHealth>()?.TakeDamage(1);

            Destroy(gameObject);
        }
    }
}