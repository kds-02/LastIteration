using System.Collections;
using UnityEngine;
using Fusion;

public class Gun : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("발사 설정")]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;

    [Header("장전 설정")]
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private bool autoReload = true;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    [Header("탄창 애니메이션")]
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

    private int currentAmmo;
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
    }

    void Update()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null && !netObj.HasInputAuthority)
            return; // 프록시에서 입력 처리/총알 생성 방지

        if (isReloading) return;

        // 수동 장전
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
        if (bulletPrefab == null || firePoint == null || playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        if (audioSource != null && fireSound != null)
            audioSource.PlayOneShot(fireSound, fireSoundVolume);

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

        Vector3 direction = (targetPoint - firePoint.position).normalized;
        var bulletGo = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));

        // 발사자/데미지 설정
        var b = bulletGo.GetComponent<Bullet>();
        if (b != null)
        {
            b.shooterId = GetShooterId();
            b.damage = 20f;
            Debug.Log($"[Gun] Fire shooterId={b.shooterId}");
            shotSeq++;
        }

        currentAmmo--;

        if (currentAmmo <= 0 && autoReload)
            StartCoroutine(Reload());
    }

    IEnumerator Reload()
    {
        isReloading = true;
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
        isReloading = false;
    }

    // 공격자 식별: PlayerRef.RawEncoded 사용 (없으면 -1)
    private int GetShooterId()
    {
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj != null)
            return netObj.InputAuthority.RawEncoded;
        return -1;
    }
}
