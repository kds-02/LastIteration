using System.Threading.Tasks;
using Fusion.Photon.Realtime;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// 네트워크 전체를 관리하는 매니저
public class NetworkRunnerHandler : MonoBehaviour
{
    private NetworkRunner _runner;
    private PlayerSpawner _spawner;

    void Awake()
    {
        Debug.Log("[Fusion] NetworkRunnerHandler.Awake() 호출됨");

        // 중복 생성 방지
        if (FindObjectsOfType<NetworkRunnerHandler>().Length > 1) {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // 콜백용 플레이어 스포너 가져옴
        _spawner = GetComponent<PlayerSpawner>();

        // NetworkRunner 생성
        _runner = gameObject.AddComponent<NetworkRunner>();

        // 입력을 Fusion으로 보내도록 설정
        _runner.ProvideInput = true;

        Debug.Log("[Fusion] Runner 생성 완료 (Awake)");
    }

    async void Start()
    {
        Debug.Log("[Fusion] NetworkRunnerHandler.Start() 호출됨");

        // Runner와 Spawner 둘 다 있어야 함
        if (_runner == null) {
            Debug.LogError("[Fusion] Runner 초기화 실패!");
            return;
        }
        if (_spawner == null) {
            Debug.LogError("[Fusion] PlayerSpawner 스크립트 없음!");
            return;
        }

        // 콜백 등록
        _runner.AddCallbacks(_spawner);
        Debug.Log("[Fusion] PlayerSpawner Callback 등록 완료");

        // 현재 씬을 네트워크 씬으로 등록
        var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var appSettings = PhotonAppSettings.Global.AppSettings;

        // 실행 모드 자동 결정
        GameMode mode;

#if UNITY_EDITOR
        mode = GameMode.Host;     // ✦ 에디터에서는 무조건 Host
        Debug.Log("▶ Editor 실행 → Host(서버) 모드");
#else
        mode = GameMode.Client;   // ✦ 빌드된 앱에서는 Client
        Debug.Log("▶ Build 실행 → Client 모드");
#endif       
        // 게임 실행 설정
        var startArgs = new StartGameArgs()
        {
            // GameMode = GameMode.AutoHostOrClient,
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = sceneRef,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            CustomPhotonAppSettings = appSettings
        };

        // 세션 시작
        var result = await _runner.StartGame(startArgs);

        // 세션 상태 출력
        if (result.Ok) {
            Debug.Log($"✅ 세션 접속 성공: {startArgs.SessionName} | Mode: {_runner.GameMode}");
        } else {
            Debug.LogError($"❌ 세션 시작 실패: {result.ShutdownReason}");
        }
    }
}
