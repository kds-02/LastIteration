using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("발사 설정")]
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

    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private Vector3 magOriginalPosition;

    void Start()
    {
        currentAmmo = maxAmmo;

        if (magTransform != null)
        {
            magOriginalPosition = magTransform.localPosition;
        }
    }

    void Update()
    {

        if (isReloading)
            return;

        // 수동 재장전   
        if (Input.GetKeyDown(reloadKey))
        {
            if (currentAmmo < maxAmmo)
            {
                StartCoroutine(Reload());
            }
            return;
        }

        if (Input.GetKey(fireKey) && Time.time >= nextFireTime)
        {
            if (currentAmmo > 0)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
            else
            {
                if (autoReload)
                {
                    StartCoroutine(Reload());
                }
            }
        }
    }

    void Fire()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        currentAmmo--; 

        if (currentAmmo <= 0 && autoReload)
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;

        Debug.Log("재장전 시작...");

        if (magTransform != null)
        {
            Vector3 originalPos = magOriginalPosition;
            Vector3 dropPos = originalPos + Vector3.down * magDropDistance;

            float dropTime = 1f / magDropSpeed;
            float elapsed = 0f;

            while (elapsed < dropTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dropTime;
                magTransform.localPosition = Vector3.Lerp(originalPos, dropPos, t);
                yield return null;
            }

            magTransform.localPosition = dropPos;

            yield return new WaitForSeconds(0.3f);

            float riseTime = 1f / magRiseSpeed;
            elapsed = 0f;

            while (elapsed < riseTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / riseTime;
                magTransform.localPosition = Vector3.Lerp(dropPos, originalPos, t);
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

        Debug.Log("재장전 완료!");
    }
}
