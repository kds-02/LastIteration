using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Respawn Spawn Points (8개)")]
    [SerializeField] private Transform[] spawnPoints;

    // 다른 스크립트(예: PlayerState)에서 쉽게 접근하기 위한 싱글톤
    public static Spawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Spawner] spawnPoints가 비어 있습니다. 인스펙터에서 설정하세요.");
        }
    }

    /// <summary>
    /// 8개의 스폰 포인트 중 하나를 랜덤으로 반환
    /// </summary>
    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        int idx = Random.Range(0, spawnPoints.Length);
        Debug.Log($"[Spawner] Respawn spawn index={idx}");
        return spawnPoints[idx];
    }

    /// <summary>
    /// position 값만 필요할 때
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        Transform t = GetRandomSpawnPoint();
        return t != null ? t.position : Vector3.zero;
    }
}
