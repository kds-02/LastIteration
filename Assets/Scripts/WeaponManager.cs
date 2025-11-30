using Fusion;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform gunPoint;
    [SerializeField] private GameObject riflePrefab;
    [SerializeField] private GameObject shotgunPrefab;
    [SerializeField] private GameObject pistolPrefab;

    [Header("Hand Bone Settings")]
    [SerializeField] private string handBoneName = "B-handProp.R";
    [SerializeField] private Vector3 weaponPositionOffset = new Vector3(0f, 0f, 0.15f);
    [SerializeField] private Vector3 weaponRotationOffset = new Vector3(0f, 90f, 0f);

    [Header("ADS (Aim Down Sights) Settings")]
    [SerializeField] private Vector3 adsPositionOffset = new Vector3(0f, -0.1f, 0.3f);
    [SerializeField] private float adsSpeed = 10f;

    // 네트워크 동기화되는 현재 무기 타입
    [Networked] private int CurrentWeaponType { get; set; }

    private GameObject currentWeaponInstance;
    private PlayerState playerState;
    private int lastKillCount = -1;
    private int lastWeaponType = -1;
    private Transform handBone;
    private Transform cameraPivot;
    private bool isAiming = false;

    public override void Spawned()
    {
        playerState = GetComponent<PlayerState>();

        weaponPositionOffset = new Vector3(0.1f, 0f, 0f);
        weaponRotationOffset = new Vector3(0f, 90f, 0f);
        adsPositionOffset = new Vector3(0f, -0.2f, 0.3f);

        handBone = FindChildRecursive(transform, handBoneName);
        if (handBone == null)
        {
            Debug.LogWarning($"[WeaponManager] Hand bone '{handBoneName}' not found, using gunPoint fallback");
        }

        cameraPivot = FindChildRecursive(transform, "FPSCamera");
        if (cameraPivot == null)
            cameraPivot = transform.Find("CameraPivot");

        if (Object.HasStateAuthority)
        {
            // 서버에서 초기 무기 설정
            CurrentWeaponType = 0; // 라이플로 시작
        }

        // 모든 클라이언트가 무기 생성
        UpdateWeaponVisual();
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            var result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void Update()
    {
        // 로컬 플레이어 입력 처리
        if (Object.HasInputAuthority)
        {
            // 무기 교체 테스트 키
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                RPC_RequestWeaponChange(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                RPC_RequestWeaponChange(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                RPC_RequestWeaponChange(2);
            }

            isAiming = Input.GetMouseButton(1);
        }

        // 서버: 킬 수에 따른 무기 변경 처리
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

    private void LateUpdate()
    {
        if (!Object.HasInputAuthority) return;
        if (currentWeaponInstance == null || cameraPivot == null) return;

        if (isAiming)
        {
            Vector3 targetPos = cameraPivot.position + cameraPivot.forward * adsPositionOffset.z
                              + cameraPivot.up * adsPositionOffset.y
                              + cameraPivot.right * adsPositionOffset.x;
            Quaternion targetRot = cameraPivot.rotation;

            currentWeaponInstance.transform.position = Vector3.Lerp(
                currentWeaponInstance.transform.position, targetPos, adsSpeed * Time.deltaTime);
            currentWeaponInstance.transform.rotation = Quaternion.Slerp(
                currentWeaponInstance.transform.rotation, targetRot, adsSpeed * Time.deltaTime);
        }
        else
        {
            if (handBone != null)
            {
                Vector3 targetPos = handBone.TransformPoint(weaponPositionOffset);
                Quaternion targetRot = handBone.rotation * Quaternion.Euler(weaponRotationOffset);

                currentWeaponInstance.transform.position = Vector3.Lerp(
                    currentWeaponInstance.transform.position, targetPos, adsSpeed * Time.deltaTime);
                currentWeaponInstance.transform.rotation = Quaternion.Slerp(
                    currentWeaponInstance.transform.rotation, targetRot, adsSpeed * Time.deltaTime);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestWeaponChange(int weaponType)
    {
        // ������ ���� Ÿ�� ���� ����
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
            return 2; // ����
        else if (kills >= 5 && kills <= 9)
            return 1; // ����
        else
            return 0; // ����
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
                weaponName = "라이플";
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

        // 부착할 부모 결정 (손 본 우선, 없으면 gunPoint 폴백)
        Transform attachPoint = handBone != null ? handBone : gunPoint;

        // 새 무기 생성
        if (weaponPrefab != null && attachPoint != null)
        {
            currentWeaponInstance = Instantiate(weaponPrefab, attachPoint);
            currentWeaponInstance.transform.localPosition = weaponPositionOffset;
            currentWeaponInstance.transform.localRotation = Quaternion.Euler(weaponRotationOffset);

            Debug.Log($"[{(Object.HasStateAuthority ? "Server" : "Client")}] Weapon equipped: {weaponName} on {attachPoint.name}");
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

    // 발사 소리를 모든 클라이언트에 브로드캐스트
    public void BroadcastFireSound(AudioClip clip, float volume)
    {
        if (clip == null) return;
        RPC_PlayFireSound(CurrentWeaponType, volume);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayFireSound(int weaponType, float volume)
    {
        if (currentWeaponInstance == null) return;

        AudioSource audio = currentWeaponInstance.GetComponent<AudioSource>();
        if (audio == null) return;

        AudioClip clip = null;
        var gun = currentWeaponInstance.GetComponent<Gun>();
        var shotgun = currentWeaponInstance.GetComponent<Shotgun>();

        if (gun != null)
            clip = gun.fireSound;
        else if (shotgun != null)
            clip = shotgun.fireSound;

        if (clip != null)
            audio.PlayOneShot(clip, volume);
    }
}