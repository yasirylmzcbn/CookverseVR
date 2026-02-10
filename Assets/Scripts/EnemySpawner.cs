using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawners;
    [SerializeField] private GameObject enemy;

    private void Start()
    {
        StartCoroutine(SpawnEnemiesRoutine());
    }

    private IEnumerator SpawnEnemiesRoutine()
    {
        float duration = 10f;
        float interval = 5f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Spawn 10 enemies
            for (int i = 0; i < 10; i++)
            {
                SpawnEnemy();
            }

            // Wait 5 seconds before next wave
            yield return new WaitForSeconds(interval);
            elapsedTime += interval;
        }
    }

    private void SpawnEnemy()
    {
        int randomInt = Random.Range(0, spawners.Length);
        Transform randomSpawner = spawners[randomInt];
        Instantiate(enemy, randomSpawner.position, randomSpawner.rotation);
    }
}
