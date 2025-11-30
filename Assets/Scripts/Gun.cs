using System.Collections;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class Gun : MonoBehaviour
{
    [Header("�Ѿ� ����")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("�߻� ����")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;

    [Header("źâ ����")]
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private bool autoReload = true;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    [Header("������ �ִϸ��̼�")]
    [SerializeField] private Transform magTransform;
    [SerializeField] private float magDropDistance = 0.5f;
    [SerializeField] private float magDropSpeed = 2f;
    [SerializeField] private float magRiseSpeed = 3f;

    [Header("Fire Effect & Sound")]
    //public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public AudioClip fireSound;
    [Range(0f, 1f)] public float fireSoundVolume = 0.5f;

    [Header("Camera Raycast")]
    public Camera playerCamera;
    public float maxRayDistance = 100f;

    [Header("Hit Effect")]
    public GameObject hitEffectPrefab;

    [Header("Reload Sound")]
    public AudioClip reloadSound;
    [Range(0f, 1f)]
    public float reloadSoundVolume = 0.5f;

    [Header("Ammo UI")]
    public Text ammoText;

    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private Vector3 magOriginalPosition;

    private PlayerState playerState;

    private int shotSeq = 0;
    private WeaponManager weaponManager;

    void Start()
    {
        currentAmmo = maxAmmo;
        if (magTransform != null) magOriginalPosition = magTransform.localPosition;

        if (playerCamera == null)
        {
            var camController = GetComponentInParent<NetworkObject>()?.GetComponentInChildren<CameraController>(true);
            if (camController != null)
                playerCamera = camController.GetComponent<Camera>();
            else
                playerCamera = Camera.main;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        playerState = GetComponentInParent<PlayerState>();
        weaponManager = GetComponentInParent<WeaponManager>();

        if (ammoText == null)
        {
            GameObject ammoObj = GameObject.Find("AmmoText");
            if (ammoObj != null)
            {
                ammoText = ammoObj.GetComponent<Text>();
            }
        }
        UpdateAmmoUI();
    }

    void Update()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.HasInputAuthority)
            return;  // ���� �κп��� �Է� ó�� / ��ü �迭 ���� �κ�


        //�÷��̾ �׾������� �ѱⰡ �߻���� �ʵ��� ����
        if (playerState != null && playerState.IsDead)
            return;

        if (isReloading) return;

        // ?�동 ?�전
        if (Input.GetKeyDown(reloadKey))
        {
            if (currentAmmo < maxAmmo) StartCoroutine(Reload());
            return;
        }

        if (Input.GetKey(fireKey) && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
            else if (autoReload)
            {
                StartCoroutine(Reload());
            }
        }
    }

    void Fire()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 총구 방향으로 발사
        Vector3 direction = firePoint.forward;

        // 발사 소리를 모든 클라이언트에 브로드캐스트
        if (weaponManager != null)
            weaponManager.BroadcastFireSound(fireSound, fireSoundVolume);
        else if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound, fireSoundVolume);

        // 총구 방향으로 레이캐스트하여 히트 이펙트 표시
        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, maxRayDistance))
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 2f);
            }
        }

        var bulletGo = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));

        // 발사???��?지 ?�정
        var b = bulletGo.GetComponent<Bullet>();
        if (b != null)
        {
            b.shooterId = GetShooterId();
            b.damage = 20f;
            Debug.Log($"[Gun] Fire shooterId={b.shooterId}");
            shotSeq++;
        }

        currentAmmo--;
        UpdateAmmoUI();

        if (currentAmmo <= 0 && autoReload)
            StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        isReloading = true;

        // ������ ���� ��� (����� �߰�)
        Debug.Log("������ ����!");
        if (audioSource != null && reloadSound != null)
        {
            Debug.Log("������ ���� ���!");
            audioSource.PlayOneShot(reloadSound, reloadSoundVolume);
        }
        else
        {
            Debug.LogWarning($"AudioSource: {audioSource != null}, ReloadSound: {reloadSound != null}");
        }

        if (magTransform != null)
        {
            Vector3 originalPos = magOriginalPosition;
            Vector3 dropPos = originalPos + Vector3.down * magDropDistance;

            float dropTime = 1f / magDropSpeed;
            float elapsed = 0f;
            while (elapsed < dropTime)
            {
                elapsed += Time.deltaTime;
                magTransform.localPosition = Vector3.Lerp(originalPos, dropPos, elapsed / dropTime);
                yield return null;
            }
            magTransform.localPosition = dropPos;

            yield return new WaitForSeconds(0.3f);

            float riseTime = 1f / magRiseSpeed;
            elapsed = 0f;
            while (elapsed < riseTime)
            {
                elapsed += Time.deltaTime;
                magTransform.localPosition = Vector3.Lerp(dropPos, originalPos, elapsed / riseTime);
                yield return null;
            }
            magTransform.localPosition = originalPos;
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        currentAmmo = maxAmmo;
        UpdateAmmoUI();
        isReloading = false;
    }

    // ź�� UI ������Ʈ �Լ�
    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";

            // �Ѿ��� ���� �� ���������� ǥ�� (���û���)
            if (currentAmmo == 0)
            {
                ammoText.color = Color.red;
            }
            else if (currentAmmo <= maxAmmo / 3)
            {
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.color = Color.white;
            }
        }
    }

    // 공격???�별: PlayerRef.RawEncoded ?�용 (?�으�?-1)
    private int GetShooterId()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null)
            return netObj.InputAuthority.RawEncoded;
        return -1;
    }
}
