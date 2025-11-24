using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    // 플레이어가 룸에 입장했을 때 호출됨
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer) {
            Debug.Log("[Fusion] 서버가 플레이어 스폰");

            // 이미 스폰되어 있으면 중복 생성 방지
            if (spawnedPlayers.ContainsKey(player))
            {
                Debug.LogWarning($"[Fusion] Player {player} already spawned, skip duplicate.");
                return;
            }

            Vector3 spawnPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), 1f, UnityEngine.Random.Range(-5f, 5f));

            var obj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            spawnedPlayers[player] = obj;
            runner.SetPlayerObject(player, obj);
            Debug.Log($"[Fusion] Player spawned: {player} (loyalty: server)");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Fusion] Player left: {player} 나감");
        // 서버가 해당 플레이어의 오브젝트 정리
        if (runner.IsServer && spawnedPlayers.TryGetValue(player, out var obj))
        {
            if (obj != null && obj.IsValid)
            {
                runner.Despawn(obj);
            }
            spawnedPlayers.Remove(player);
        }

        // PlayerObject 매핑 해제
        if (runner.IsServer)
            runner.SetPlayerObject(player, null);
    }

    // 클라이언트의 입력을 Fusion 네트워크로 전달
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData();

        //  이동 입력 (WASD)
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        data.moveInput = new Vector2(x, y);

        data.jumpPressed = Input.GetKeyDown(KeyCode.Space); //  점프 입력
        data.runHeld = Input.GetKey(KeyCode.LeftShift); //  달리기 입력
        data.crouchHeld = Input.GetKey(KeyCode.LeftControl); //  앉기 입력
        data.mouseDeltaX = Input.GetAxisRaw("Mouse X"); // 마우스 X 회전 입력

        input.Set(data);
    }

    // 이하 콜백은 사용 안 함
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // 사용 안 함
    }
}
