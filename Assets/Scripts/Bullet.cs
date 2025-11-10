using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;      
    public float lifetime = 3f;    
    
    void Start()
    {
        // 3초 후 총알 삭제
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        Vector3 dir = transform.forward;
        transform.position += dir * speed * Time.deltaTime;
    }
    
    // 무언가와 충돌하면
    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);  // 총알 제거
    }
}