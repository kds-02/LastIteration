using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// 네트워크 전체를 관리하는 매니저 (싱글룸 매칭 UI 버튼 기반)
public class NetworkRunnerHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Session Settings")]
    [SerializeField] private string sessionName = "SingleRoom";
    [SerializeField] private SceneRef gameScene; // 실제 플레이 씬 (메인 메뉴-> 이동)
    [SerializeField] private int maxPlayers = 4; // 방 인원 제한

    private NetworkRunner _runner;
    private PlayerSpawner _spawner;

    private bool _isStarting = false;           // 중복 세션 시작 방지용
    private bool _waitingForSessionList = false; // 세션 리스트 대기 플래그

    void Awake()
    {
        // 중복 생성 방지
        if (FindObjectsOfType<NetworkRunnerHandler>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // PlayerSpawner 가져오기
        _spawner = GetComponent<PlayerSpawner>();

        // Runner 생성
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // 콜백 등록
        _runner.AddCallbacks(this);

        Debug.Log("[Fusion] NetworkRunnerHandler Awake 완료");
    }

    // UI Start 버튼 클릭할 때 실행되는 함수
    public async void StartSession()
    {
        if (_isStarting)
        {
            Debug.LogWarning("[Fusion] 이미 매칭 중입니다.");
            return;
        }

        _isStarting = true;
        Debug.Log("[Fusion] Start 버튼 → 매칭 시작");

        // 1) 로비 접속
        await _runner.JoinSessionLobby(SessionLobby.Shared);

        Debug.Log("[Fusion] 로비 접속 완료 → 세션 리스트 대기");
        _waitingForSessionList = true; // 세션 리스트 콜백 대기 시작
    }

    // 로비 세션 목록이 갱신될 때 Host/Client 자동 선택
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!_waitingForSessionList) return;
        _waitingForSessionList = false;

        Debug.Log($"[Fusion] 세션 리스트 업데이트 (총 {sessionList.Count}개)");

        // 메인 메뉴에서 버튼을 누를 때, 실제 게임 씬을 네트워크 씬으로 등록
        var targetScene = gameScene.IsValid
            ? gameScene
            : SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        StartGameArgs args = new StartGameArgs()
        {
            SessionName = sessionName,
            CustomPhotonAppSettings = PhotonAppSettings.Global.AppSettings,
            Scene = targetScene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        // 2) TestRoom 존재 여부 판단 → Host/Client 결정
        bool roomExists = false;
        bool roomFull = false;

        SessionInfo matchedInfo = default;
        foreach (var info in sessionList)
        {
            if (info.Name == sessionName)
            {
                roomExists = true;
                matchedInfo = info;
                if (info.PlayerCount >= maxPlayers)
                {
                    roomFull = true;
                }
                break;
            }
        }

        if (roomExists)
        {
            if (roomFull) // 방 다 찼으면 접속X 다시 시도하도록
            {
                Debug.LogWarning($"[Fusion] 방({sessionName}) 인원 초과({matchedInfo.PlayerCount}/{maxPlayers}) → 접속 거절");
                _isStarting = false;
                return;
            }
            args.GameMode = GameMode.Client;
            Debug.Log("기존 방 존재 → Client로 접속");
        }
        else
        {
            args.GameMode = GameMode.Host;
            args.PlayerCount = maxPlayers; // 호스트 생성 시 최대 인원 설정
            Debug.Log("방 없음 → Host로 생성");
        }

        _ = StartRunner(args);
    }

    // Runner 실행 처리
    private async Task StartRunner(StartGameArgs args)
    {
        var result = await _runner.StartGame(args);

        if (result.Ok)
        {
            Debug.Log($"네트워크 세션 성공! Mode = {args.GameMode}");
        }
        else
        {
            Debug.LogError($"세션 실행 실패: {result.ShutdownReason}");
            _isStarting = false; // 실패했으므로 다시 StartSession 가능
        }
    }

    // ===== 필요 없는 콜백들 (전부 빈 함수로 구현) =====
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { } // ← 버전 필수
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
}
