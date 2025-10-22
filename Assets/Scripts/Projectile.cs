// Projectile.cs (Runtime Layer Ayarlar� ile G�ncellenmi� Hali)
using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    // Temel mermi �zellikleri (h�z, y�n vb.)
    private float speed;
    private Vector3 direction;
    private Rigidbody rb;
    private bool initialized = false;
    private float currentLifetime = 0f;

    // Inspector'dan ayarlanabilen varsay�lanlar
    [Header("Genel Ayarlar (Varsay�lanlar)")]
    [Tooltip("Merminin hi�bir �eye �arpmazsa yok olmadan �nce ne kadar s�re var olaca��.")]
    [SerializeField] private float lifetime = 5f;
    // Bu varsay�lanlar, Initialize ile �zerine yaz�lmazsa kullan�labilir.
    // Ama genellikle Initialize ile her zaman belirtti�imiz i�in bu alanlar Projectile prefab'�nda
    // �ok kritik olmayabilir, daha �ok bir fallback g�revi g�r�r.
    [Tooltip("Merminin varsay�lan olarak �arp��aca�� katmanlar.")]
    [SerializeField] private LayerMask defaultCollisionLayers;
    [Tooltip("Merminin varsay�lan olarak AoE hasar� verece�i katmanlar.")]
    [SerializeField] private LayerMask defaultAoeTargetLayers;

    [Header("Hasar Ayarlar�")]
    public float damage = 10f;
    public bool causesHitStopOnImpact = true;
    public GameObject sourceGameObject { get; set; }

    [Header("Alan Etkisi (AoE) Ayarlar�")]
    public bool isAoEProjectile = false;
    [SerializeField] private float aoeRadius = 3f;
    [SerializeField] private float aoeDamage = 0f;
    [SerializeField] private bool aoeCausesHitStop = false;

    [Header("Efektler")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float effectDestroyDelay = 2f;

    // Runtime'da kullan�lacak LayerMask'lar
    private LayerMask _currentCollisionLayers;
    private LayerMask _currentAoeTargetLayers;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Projectile bir Rigidbody component'ine ihtiya� duyar!", this);
            Destroy(gameObject);
            return;
        }
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Initialize(float projectileSpeed, Vector3 shootingDirection, GameObject owner,
                           LayerMask layersToCollideWith, LayerMask aoeLayersToTarget)
    {
        this.speed = projectileSpeed;
        this.direction = shootingDirection.normalized;
        this.sourceGameObject = owner;
        this._currentCollisionLayers = layersToCollideWith;
        // AoE de�ilse bile aoeLayersToTarget'� ata, AoE kontrol� hasar verirken yap�l�r.
        this._currentAoeTargetLayers = aoeLayersToTarget;

        initialized = true;
        currentLifetime = 0f;

        if (rb != null)
        {
            rb.linearVelocity = this.direction * this.speed;
        }

        if (this.direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(this.direction);
        }
    }

    // Opsiyonel: Eski Initialize �a�r�lar� i�in varsay�lan katmanlar� kullanan overload.
    // E�er t�m ate�lemeler 5 parametreli Initialize ile yap�lacaksa bu silinebilir.
    public void Initialize(float projectileSpeed, Vector3 shootingDirection, GameObject owner)
    {
        Debug.LogWarning($"Projectile on {name} initialized without explicit LayerMasks. Using defaults. Consider updating the call.", this);
        Initialize(projectileSpeed, shootingDirection, owner, defaultCollisionLayers, defaultAoeTargetLayers);
    }

    void Update()
    {
        if (!initialized) return;

        currentLifetime += Time.deltaTime;
        if (currentLifetime >= lifetime)
        {
            HandleImpactOrExpiration(transform.position, transform.rotation, null);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!initialized) return;

        if (sourceGameObject != null && (collision.gameObject == sourceGameObject || collision.transform.IsChildOf(sourceGameObject.transform)))
        {
            return;
        }

        if ((_currentCollisionLayers.value & (1 << collision.gameObject.layer)) > 0)
        {
            HealthSystem targetHealth = collision.gameObject.GetComponentInParent<HealthSystem>();
            if (targetHealth != null)
            {
                DamageInfo directDamageInfo = new DamageInfo(
                    this.damage,
                    direction,
                    this.sourceGameObject,
                    collision.contacts[0].point,
                    causesHitStopOnImpact
                );
                targetHealth.TakeDamage(directDamageInfo);
            }
            ContactPoint contact = collision.contacts[0];
            HandleImpactOrExpiration(contact.point, Quaternion.LookRotation(contact.normal), collision.gameObject);
        }
    }

    private void HandleImpactOrExpiration(Vector3 impactPosition, Quaternion impactRotation, GameObject directlyHitObject)
    {
        if (!initialized) return;
        initialized = false;

        if (impactEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(impactEffectPrefab, impactPosition, impactRotation);
            if (effectDestroyDelay > 0) Destroy(effectInstance, effectDestroyDelay);
            else if (effectInstance.GetComponent<ParticleSystem>() == null) Destroy(effectInstance);
        }

        if (isAoEProjectile && aoeRadius > 0)
        {
            float damageToDealInAoE = (aoeDamage > 0) ? aoeDamage : this.damage;
            if (damageToDealInAoE > 0)
            {
                Collider[] aoeHits = Physics.OverlapSphere(impactPosition, aoeRadius, _currentAoeTargetLayers);
                List<HealthSystem> alreadyDamagedInThisAoE = new List<HealthSystem>();

                foreach (var hitCollider in aoeHits)
                {
                    if (sourceGameObject != null && (hitCollider.gameObject == sourceGameObject || hitCollider.transform.IsChildOf(sourceGameObject.transform)))
                    {
                        continue;
                    }

                    HealthSystem targetHealthInAoE = hitCollider.GetComponentInParent<HealthSystem>();
                    if (targetHealthInAoE != null && !alreadyDamagedInThisAoE.Contains(targetHealthInAoE))
                    {
                        Vector3 directionToAoeTarget = (hitCollider.transform.position - impactPosition).normalized;
                        if (directionToAoeTarget == Vector3.zero) directionToAoeTarget = transform.forward; // Fallback

                        DamageInfo aoeDmgInfo = new DamageInfo(
                            damageToDealInAoE,
                            directionToAoeTarget,
                            this.sourceGameObject,
                            hitCollider.ClosestPoint(impactPosition),
                            aoeCausesHitStop
                        );
                        targetHealthInAoE.TakeDamage(aoeDmgInfo);
                        alreadyDamagedInThisAoE.Add(targetHealthInAoE);
                    }
                }
            }
        }
        Destroy(gameObject);
    }
}