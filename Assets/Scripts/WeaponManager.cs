using Fusion;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform gunPoint;
    [SerializeField] private GameObject riflePrefab;
    [SerializeField] private GameObject shotgunPrefab;
    [SerializeField] private GameObject pistolPrefab;

    // 네트워크 동기화되는 현재 무기 타입
    [Networked] private int CurrentWeaponType { get; set; }

    private GameObject currentWeaponInstance;
    private PlayerState playerState;
    private int lastKillCount = -1;
    private int lastWeaponType = -1;

    public override void Spawned()
    {
        playerState = GetComponent<PlayerState>();

        if (Object.HasStateAuthority)
        {
            // 서버가 초기 무기 설정
            CurrentWeaponType = 0; // 소총으로 시작
        }

        // 모든 클라이언트가 무기 생성
        UpdateWeaponVisual();
    }

    private void Update()
    {
        // 테스트용 키 입력
        if (Object.HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("테스트: 소총으로 변경");
                RPC_RequestWeaponChange(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("테스트: 샷건으로 변경");
                RPC_RequestWeaponChange(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("테스트: 권총으로 변경");
                RPC_RequestWeaponChange(2);
            }
        }

        // 서버만 킬 수에 따른 무기 변경 처리
        if (Object.HasStateAuthority)
        {
            if (playerState != null)
            {
                int currentKills = (int)playerState.GetKill();
                if (currentKills != lastKillCount)
                {
                    lastKillCount = currentKills;
                    UpdateWeaponByKills();
                }
            }
        }

        // 모든 클라이언트: 무기 타입이 변경되면 시각적 업데이트
        if (CurrentWeaponType != lastWeaponType)
        {
            lastWeaponType = CurrentWeaponType;
            UpdateWeaponVisual();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestWeaponChange(int weaponType)
    {
        // 서버가 무기 타입 변경 승인
        CurrentWeaponType = weaponType;
    }

    private void UpdateWeaponByKills()
    {
        if (playerState == null) return;

        int kills = (int)playerState.GetKill();
        int newWeaponType = GetWeaponTypeByKills(kills);

        if (newWeaponType != CurrentWeaponType)
        {
            CurrentWeaponType = newWeaponType;
        }
    }

    private int GetWeaponTypeByKills(int kills)
    {
        if (kills >= 10 && kills <= 15)
            return 2; // 권총
        else if (kills >= 5 && kills <= 9)
            return 1; // 샷건
        else
            return 0; // 소총
    }

    private void UpdateWeaponVisual()
    {
        // 기존 무기 제거
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponInstance = null;
        }

        // 무기 타입에 따라 프리팹 선택
        GameObject weaponPrefab = null;
        string weaponName = "";

        switch (CurrentWeaponType)
        {
            case 0:
                weaponPrefab = riflePrefab;
                weaponName = "소총";
                break;
            case 1:
                weaponPrefab = shotgunPrefab;
                weaponName = "샷건";
                break;
            case 2:
                weaponPrefab = pistolPrefab;
                weaponName = "권총";
                break;
        }

        // 새 무기 생성 (로컬에서만)
        if (weaponPrefab != null && gunPoint != null)
        {
            currentWeaponInstance = Instantiate(weaponPrefab, gunPoint);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            Debug.Log($"[{(Object.HasStateAuthority ? "Server" : "Client")}] Weapon equipped: {weaponName}");
        }
    }

    public GameObject GetCurrentWeapon()
    {
        return currentWeaponInstance;
    }

    public int GetCurrentWeaponType()
    {
        return CurrentWeaponType;
    }
}