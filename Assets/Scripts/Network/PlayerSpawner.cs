using Fusion;
using UnityEngine;

// 네트워크 방에 입장할 때 Player를 자동으로 스폰
// NetworkRunnerHandler 오브젝트에 PlayerSpawner 스크립트 붙여 실현함
public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority);
        }
    }
}
