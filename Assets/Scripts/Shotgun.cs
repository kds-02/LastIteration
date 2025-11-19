using System.Collections;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("샷건 발사 설정")]
    [SerializeField] private int pelletsPerShot = 8; // 한 번에 발사되는 총알 개수
    [SerializeField] private float spreadAngle = 15f; // 총알 퍼지는 각도
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
    [Range(0f, 1f)]
    public float fireSoundVolume = 0.5f;

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
        {
            playerCamera = Camera.main;
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (isReloading) return;

        // 수동 재장전
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

        // 사운드는 한 번만 재생
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound, fireSoundVolume);
        }

        // 카메라 중앙 레이 (기준점)
        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 centerTarget;

        if (Physics.Raycast(centerRay, out RaycastHit centerHit, maxRayDistance))
        {
            centerTarget = centerHit.point;
        }
        else
        {
            centerTarget = centerRay.GetPoint(maxRayDistance);
        }

        // 여러 발의 총알 발사
        for (int i = 0; i < pelletsPerShot; i++)
        {
            FireSinglePellet(centerTarget);
        }

        currentAmmo--;

        if (currentAmmo <= 0 && autoReload)
        {
            StartCoroutine(Reload());
        }
    }

    void FireSinglePellet(Vector3 centerTarget)
    {
        // Gun과 동일하게: firePoint에서 타겟으로의 방향
        Vector3 direction = (centerTarget - firePoint.position).normalized;

        // 스프레드를 방향 벡터에 직접 추가
        Vector3 spread = new Vector3(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        );

        // 방향에 스프레드 각도를 더한 후 정규화
        direction = Quaternion.Euler(spread) * direction;

        // 스프레드가 적용된 방향으로 레이캐스트
        Ray spreadRay = new Ray(firePoint.position, direction);

        if (Physics.Raycast(spreadRay, out RaycastHit hit, maxRayDistance))
        {
            // 히트 이펙트 생성
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 2f);
            }
        }

        // Gun과 완전히 동일한 방식으로 총알 생성
        var bulletGo = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));

        // 발사자/데미지 정보 주입
        var b = bulletGo.GetComponent<Bullet>();
        if (b != null)
        {
            b.shooterId = GetShooterId();
            b.damage = 20f;
            shotSeq++;
        }
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

    private int GetShooterId()
    {
        return 0;
    }
}