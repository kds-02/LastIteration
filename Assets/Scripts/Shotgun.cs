using System.Collections;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

public class Shotgun : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("샷건 발사 설정")]
    [SerializeField] private int pelletsPerShot = 8;         // 한번에 발사하는 산탄 수
    [SerializeField] private float spreadAngle = 15f;        // 산탄 퍼짐 각도
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;

    [Header("탄창 설정")]
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private bool autoReload = true;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    [Header("재장전 애니메이션")]
    [SerializeField] private Transform magTransform;
    [SerializeField] private float magDropDistance = 0.5f;
    [SerializeField] private float magDropSpeed = 2f;
    [SerializeField] private float magRiseSpeed = 3f;

    [Header("Fire Effect & Sound")]
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

    private int currentAmmo;
    public Text ammoText;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private Vector3 magOriginalPosition;
    private int shotSeq = 0;

    void Start()
    {
        currentAmmo = maxAmmo;
        if (magTransform != null) magOriginalPosition = magTransform.localPosition;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (ammoText == null)
        {
            ammoText = GameObject.Find("AmmoText")?.GetComponent<Text>();
        }

        UpdateAmmoUI();
    }

    void Update()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.HasInputAuthority)
            return;

        if (isReloading) return;

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
        if (bulletPrefab == null || firePoint == null || playerCamera == null)
        {
            Debug.LogError($"Missing: bulletPrefab={bulletPrefab}, firePoint={firePoint}, camera={playerCamera}");
            return;
        }

        Debug.Log("[Shotgun] Fire called!");

        if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound, fireSoundVolume);

        // 중앙 레이 계산
        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        for (int i = 0; i < pelletsPerShot; i++)
        {
            // Quaternion으로 spread 적용
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);

            Quaternion spreadRotation = Quaternion.Euler(randomY, randomX, 0f);
            Vector3 spreadDirection = spreadRotation * centerRay.direction;

            Ray ray = new Ray(centerRay.origin, spreadDirection);
            Vector3 targetPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
            {
                targetPoint = hit.point;

                if (hitEffectPrefab != null)
                {
                    GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(effect, 2f);
                }
            }
            else
            {
                targetPoint = ray.GetPoint(maxRayDistance);
            }

            // 총알이 firePoint에서 약간 오프셋을 주어 더 퍼져보이게
            Vector3 spawnOffset = Random.insideUnitSphere * 0.1f; // 0.1 반경 내에서 랜덤 생성
            spawnOffset.z = 0; // 앞뒤 방향은 오프셋 없음
            Vector3 spawnPosition = firePoint.position + firePoint.TransformDirection(spawnOffset);

            Vector3 direction = (targetPoint - spawnPosition).normalized;

            Debug.Log($"[Shotgun] Pellet {i}: spawnPos={spawnPosition}, targetPoint={targetPoint}, direction={direction}");

            var bulletGo = Instantiate(bulletPrefab, spawnPosition, Quaternion.LookRotation(direction));

            Debug.Log($"[Shotgun] Bullet instantiated: {bulletGo.name}, position={bulletGo.transform.position}, rotation={bulletGo.transform.rotation}");

            var b = bulletGo.GetComponent<Bullet>();
            if (b != null)
            {
                b.shooterId = GetShooterId();
                b.damage = 10f;
                shotSeq++;
                Debug.Log($"[Shotgun] Bullet component found, shooterId={b.shooterId}");
            }
            else
            {
                Debug.LogError("[Shotgun] No Bullet component on bullet!");
            }
        }

        currentAmmo--;
        UpdateAmmoUI();

        if (currentAmmo <= 0 && autoReload)
            StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        isReloading = true;

        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound, reloadSoundVolume);
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

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";

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

    private int GetShooterId()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null)
            return netObj.InputAuthority.RawEncoded;
        return -1;
    }
}