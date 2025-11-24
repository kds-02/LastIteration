using Fusion;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform gunPoint; // 무기가 장착될 위치
    [SerializeField] private GameObject riflePrefab;   // 소총 (Kill 0~4)
    [SerializeField] private GameObject shotgunPrefab; // 샷건 (Kill 5~9)
    [SerializeField] private GameObject pistolPrefab;  // 권총 (Kill 10~15)

    private GameObject currentWeaponInstance;
    private PlayerState playerState;
    private int lastKillCount = -1;

    public override void Spawned()
    {
        playerState = GetComponent<PlayerState>();

        // 초기 무기 장착
        UpdateWeaponByKills();
    }

    private void Update()
    {
        if (Object.HasInputAuthority)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("테스트: 소총으로 변경");
                RPC_TestChangeWeapon(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("테스트: 샷건으로 변경");
                RPC_TestChangeWeapon(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("테스트: 권총으로 변경");
                RPC_TestChangeWeapon(2);
            }
        }
        if (!Object.HasStateAuthority) return;

        // Kill 수가 변경되었는지 확인
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

    private void UpdateWeaponByKills()
    {
        if (playerState == null) return;

        int kills = (int)playerState.GetKill();
        GameObject newWeaponPrefab = GetWeaponPrefabByKills(kills);

        if (newWeaponPrefab != null)
        {
            RPC_UpdateWeaponVisual(newWeaponPrefab.name);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_TestChangeWeapon(int weaponType)
    {
        GameObject weaponPrefab = null;

        switch (weaponType)
        {
            case 0: weaponPrefab = riflePrefab; break;   // 소총
            case 1: weaponPrefab = shotgunPrefab; break; // 샷건
            case 2: weaponPrefab = pistolPrefab; break;  // 권총
        }

        if (weaponPrefab != null)
        {
            RPC_UpdateWeaponVisual(weaponPrefab.name);
        }
    }

    private GameObject GetWeaponPrefabByKills(int kills)
    {
        if (kills >= 10 && kills <= 15)
            return pistolPrefab;  // 권총
        else if (kills >= 5 && kills <= 9)
            return shotgunPrefab; // 샷건
        else
            return riflePrefab;   // 소총 (0~4 또는 16+)
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateWeaponVisual(string weaponName)
    {
        // 기존 무기 제거
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
        }

        // 무기 이름으로 프리팹 찾기
        GameObject weaponPrefab = null;
        if (weaponName == riflePrefab?.name)
            weaponPrefab = riflePrefab;
        else if (weaponName == shotgunPrefab?.name)
            weaponPrefab = shotgunPrefab;
        else if (weaponName == pistolPrefab?.name)
            weaponPrefab = pistolPrefab;

        // 새 무기 생성
        if (weaponPrefab != null && gunPoint != null)
        {
            currentWeaponInstance = Instantiate(weaponPrefab, gunPoint);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            Debug.Log($"Weapon equipped: {weaponPrefab.name}");
        }
    }

    public GameObject GetCurrentWeapon()
    {
        return currentWeaponInstance;
    }
}