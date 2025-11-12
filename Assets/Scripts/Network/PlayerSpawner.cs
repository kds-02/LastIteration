using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private NetworkRunner _runner;

    // 네트워크 세션 시작 시 자동 호출됨
    async void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        _runner.ProvideInput = true;

        // 스폰 타이밍 딜레이 (Runner 초기화 대기)
        await System.Threading.Tasks.Task.Delay(1000);

        // 네트워크 스폰
        _runner.Spawn(playerPrefab, GetSpawnPoint(), Quaternion.identity, _runner.LocalPlayer);
    }

    private Vector3 GetSpawnPoint()
    {
        // 겹치지 않게 랜덤 위치
        return new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
    }
}
