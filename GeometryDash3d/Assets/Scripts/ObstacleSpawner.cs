using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Références")]
    public GameObject obstaclePrefab;
    public GameObject jumpPadPrefab;
    public GameObject platformPrefab;

    [Header("Paramètres")]
    public int numberOfElements = 50;
    public float startZ = 15f;
    public float endZ = 300f;
    public Vector2 xRange = new Vector2(-2f, 2f);
    public float minSpacing = 0f;
    public float maxSpacing = 12f;

    void Start()
    {
        float currentZ = startZ;
        for (int i = 0; i < numberOfElements; i++)
        {
            currentZ += Random.Range(minSpacing, maxSpacing);
            float x = Random.Range(xRange.x, xRange.y);

            int type = Random.Range(0, 3); // 0 = obstacle, 1 = jump pad, 2 = plateforme
            GameObject prefabToSpawn = obstaclePrefab;

            if (type == 1 && jumpPadPrefab != null)
                prefabToSpawn = jumpPadPrefab;
            else if (type == 2 && platformPrefab != null)
                prefabToSpawn = platformPrefab;

            Vector3 pos = prefabToSpawn == platformPrefab ?
                new Vector3(x, 1.5f, currentZ) :
                new Vector3(x, 0.25f, currentZ);

            Instantiate(prefabToSpawn, pos, Quaternion.identity);
        }
    }
}
