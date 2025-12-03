# Last Iteration

2025 게임소프트웨어 1조 프로젝트

Unity 기반 실시간 멀티플레이어 FPS 게임

## 게임 플레이 영상
https://youtu.be/CpXDIkhmwNo

## 개요

Last Iteration은 Unity 2022.3.62f1와 Photon Fusion 네트워크 프레임워크를 기반으로 개발된 실시간 멀티플레이 FPS 게임이다.

로그인 및 토큰 기반 인증을 통해 닉네임을 연동하고, 4인 매칭/멀티룸 생성, HUD, Combat Log, Scoreboard, 무기·전투 시스템 등 실제 온라인 FPS 구조를 그대로 구현한다.

또한 5분 매치 타이머, 킬 기반 무기 업그레이드(조건: 0/4/8킬), 탄약 UI, 리스폰, 데미지 판정, Host 기반 TickTimer 동기화, APN(region) 설정 등 네트워크 FPS 제작에 필수적인 요소가 모두 포함돼 있다.

---

# 기술 스택

| 구분 | 기술 |
| --- | --- |
| 엔진 | Unity 2022.3.6f1 / 2022.3.62f1 |
| 네트워크 | Photon Fusion v1.4.x (Host-Client) |
| UI | TextMeshPro |
| 언어 | C# |
| API 연동 | UnityWebRequest, PlayerPrefs에 토큰 저장 |
| 플랫폼 | Windows / macOS 빌드 |
| 기타 | APN(region) 설정 지원, REST API 인증, Resources 기반 prefab 관리 |

---

# 게임 특징

## 게임 규칙

- 경기 시간: 5분(300초)
- 승리 조건: 10킬 도달 or 시간 종료 시 최다 킬
- 최소 시작 인원: 2명
- 최대 플레이어: 4명

## 무기 시스템(킬 기반 업그레이드)

| 무기 | 획득 조건 | 데미지 | 탄창 | 특징 |
| --- | --- | --- | --- | --- |
| 라이플 | 0킬 (기본) | 20 | 10발 | 정밀 단발 |
| 샷건 | 4킬 이상 | 10×8 | 10발 | 15° 스프레드 |
| 피스톨 | 8킬 이상 | 20 | 10발 | 빠른 연사 |

## 조작 키

| 키 | 동작 |
| --- | --- |
| W/A/S/D | 이동 |
| Shift | 달리기 (6 m/s) |
| Ctrl | 앉기 (2 m/s) |
| Space | 점프 |
| 좌클릭 | 발사 |
| 우클릭 | 조준(ADS) |
| R | 재장전 |
| Tab | 스코어보드 표시 |

## 전투/게임 시스템

- HP: 100
- 리스폰 시간: 3초 (Timer UI)
- 탄약 UI 색상
    - 빨강: 0%
    - 노랑: 33% 이하
    - 흰색: 정상
- 조준: Camera.forward(화면 중앙) 기준 발사
- 타격 판정: DamageReceiver + Hitbox
- Host에서 데미지/킬/리스폰/승리 조건 최종 결정

---

# 전체 흐름(UX Flow)

```
BootScene → AppBootstrapper(토큰 검사)
   → 토큰 유효: menuScene 이동
   → 토큰 없음: AuthScene 이동

AuthScene: 로그인/회원가입
   → 성공 시 PlayerPrefs에 토큰 저장
   → menuScene 이동

menuScene
   → Session Name 입력
   → Start 버튼 클릭 → Matching 시작
   → Runner 생성 → 로비 접속
   → 동일 세션 있으면 Join, 포화면 SessionName-2/-3 자동 생성
   → SampleScene 로드

SampleScene
   → PlayerSpawner가 Host/Client 스폰
   → HUD / CombatLog / Scoreboard 활성화
   → GameManager가 인원 조건 충족 시 자동 시작(5분 타이머)

EndScene
   → 결과 표시, 커서 활성화

```

---

# 프로젝트 구조

```
Assets/
├── Animations/              # 캐릭터 애니메이션
├── Materials/               # 머티리얼
├── Photon/                  # Fusion 관련 파일
├── Resources/
│   ├── Player.prefab        # 플레이어 프리팹(네트워크 오브젝트)
│   ├── ApiConfig.asset      # API BaseUrl/Timeout 설정
│   └── ...
├── Scenes/
│   ├── BootScene
│   ├── AuthScene
│   ├── menuScene
│   ├── SampleScene
│   └── EndScene
├── Scripts/
│   ├── Api/                 # 서버 API 호출
│   ├── Auth/                # 로그인/회원가입/토큰 저장
│   ├── Camera/              # FPS 카메라 컨트롤
│   ├── EndScene/            # 종료 화면 UI
│   ├── Network/             # Photon Fusion 핵심 로직
│   ├── Player/              # 플레이어 상태·이동·무기·히트박스
│   └── UI/                  # HUD, CombatLog, Scoreboard
└── TextMesh Pro/

```

---

# 핵심 스크립트 상세 설명

## 1. Auth / API 영역

### AppBootstrapper

- 게임 시작 직후 실행
- PlayerPrefs 토큰 존재 여부 확인
- 토큰 있으면 menuScene, 없으면 AuthScene 이동

### AuthManager / AuthUI

- 로그인/회원가입 처리
- 서버 응답 토큰 저장
- UI 입력 → API 호출 → Scene 전환

### AuthApiClient

- BaseUrl 기반 POST/GET 요청
- 로그인/회원가입 API 호출

### ApiConfig

- `ApiConfig.asset` 통해 서버 주소 변경
- 기본값: `127.0.0.1:3000` (로컬 테스트용)

---

## 2. Network 영역

### NetworkRunnerHandler

Fusion Runner 전체를 관리하는 핵심 스크립트.

**기능 요약**

- 로비 연결
- 세션 생성/입장
- SessionName 포화 시 자동 증가(`2`, `3`, …)
- APN(region) 설정 지원
- ConnectionToken에 로그인 토큰 전달
- Host/Client 분기
- SampleScene 자동 로드

**콜백 처리**

- `OnSessionListUpdated`
    
    ▷ 현재 세션 목록 확인
    
    ▷ 동일 이름 + 포화 → 새 세션 이름 생성
    
- `OnConnectedToServer`
- `OnPlayerJoined` / `OnPlayerLeft`

---

### PlayerSpawner

- Host/Client별 스폰 처리
- 스폰 포인트 배열 지정 필요
- 중복 스폰 방지
- 스폰 후 반드시 `SetPlayerObject(playerRef, NetworkObject)` 호출
- 스폰 실패 시 자기 transform 사용(임시값)

---

### NetworkInputData

입력 데이터 구조체.

필드:

- moveX, moveY
- mouseDeltaX
- jump, run, crouch

Host가 입력을 받아 서버에서 이동 로직을 처리한다.

---

## 3. Player 영역

### PlayerState

Networked 변수 포함:

- HP / MaxHP
- Kill / Death
- IsDead
- RespawnTimer
- UserId / Nickname
- Kill 발생 시 CombatLog 브로드캐스트

### PlayerMovement

- WASD 이동
- 중력/점프/가속 처리
- 앉기/달리기
- 서버 측 Yaw 적용 → 회전 desync 방지
- InputAuthority 있는 경우에만 카메라 연결

### CameraController

- 1인칭 카메라
- ADS(FOV 변경 60° → 40°)
- Camera pivot 자동 연결
- Proxy/Host의 카메라는 비활성
- Host 3인칭 오류 발생 해결(카메라 활성 조건 수정)

### WeaponManager

- 현재 무기 선택
- Kill 카운트 기반 무기 업그레이드
- 재장전, 총알 수 UI 갱신
- 발사 애니메이션 트리거

---

## 4. 무기 영역

### Gun

- 기본 라이플
- 단발
- Camera.forward 기준 레이 발사

### Shotgun

- 8발 산탄 처리
- Spread 각도 15°
- 근거리 높은 화력

### Bullet

- 실제 탄환 이동(필요 시)
- 충돌/데미지 처리

### DamageReceiver / Hitbox / PlayerHitbox

- Collider 기반 hitbox
- 데미지를 PlayerState에 전달
- Headshot 등 구분 가능 확장 구조

---

## 5. UI / HUD 시스템

### CombatLogUI

- 입장/퇴장/킬 이벤트 텍스트 출력
- 30초 후 자동 삭제
- 색상 구분(킬/사망)

### HUDKillDeathUI

- Kill / Death 표시
- RespawnTimer 표시
- Nickname 자동 연결(`name` 포함 Text)

### ScoreboardUI

- Tab 누르면 표시
- PlayerId 순서대로 정렬
- "# Name K / D" 형태

### HPBar

- 현재 체력 표시

---

# GameManager – 서버 기반 게임 관리

### TickTimer 기반 5분 카운트다운

- Host가 TickTimer 시작
- 모든 Client는 동일 시간 표시
- 인원 조건(2명 이상) 충족 시 자동 시작

### 종료 조건

- 10 Kill 달성 플레이어 존재
- 시간이 0초 도달
- EndScene 로드

---

# 멀티룸 / 매칭 로직 상세

1. Start 클릭 → Runner 생성
2. 로비 접속 후 SessionList 조회
3. 동일 SessionName 존재
    - 빈 자리 → Join
    - Full → SessionName-2, -3… 새 방 생성
4. maxPlayers(4명) 초과 Join 시도 → 거절 → 새 방 자동 재시도
5. SampleScene 로드 → PlayerSpawner 동작

---

# 빌드 및 실행

1. Unity Hub에서 프로젝트 열기 (Unity 2022.3.62f1)
2. Photon App ID 설정 (Assets/Photon/Fusion/Resources)
3. `File → Build Settings`에서 빌드
4. 또는 에디터에서 `Play` 버튼으로 테스트

---

# 문제 해결 이력(트러블슈팅)

- 클라이언트 카메라 미작동 / Host 3인칭 문제
    
    → InputAuthority 체크 조건 수정
    
    → 카메라 pivot 자동 탐지
    
    → Proxy 카메라 비활성화 처리
    
- 스폰 포인트 미지정 NullReference
    
    → Spawner 기본 위치 반환 + 배열 강제 설정
    
- 네트워크 desync / 회전 미적용
    
    → Yaw 서버 적용으로 통일
    
    → NetworkYaw 공유
    
- GameManager 타이머 불일치
    
    → Host TickTimer로 통합 관리
    
- PlayerObject 매핑 누락
    
    → Spawn 직후 `SetPlayerObject` 필수 호출
    
- 총알이 화면 중앙이 아닌 곳으로 발사
    
    → Camera.forward 기준으로 발사 방향 통일
    
- 멀티룸에서 방 이름 충돌
    
    → sessionName-2/-3 자동 생성 로직 구현
    

---

# 게임 설정값 전체 모음

```csharp
// 매치
매치 시간: 300초
킬 제한: 10킬
최대 플레이어: 4명
최소 시작 인원: 2

// 이동
걷기 속도: 3f
달리기 속도: 6f
앉기 속도: 2f
점프 높이: 1.2f

// 카메라
기본 FOV: 60
ADS FOV: 40
마우스 감도: 2.0

// 무기
라이플: 20 Damage
샷건: 10*8 Damage, 15° Spread
피스톨: Damage 20

```

---

# 주의사항
Windows 기반으로 개발해서 Mac에서는 호환 문제 발생

---

# 2025 게임소프트웨어 1조
