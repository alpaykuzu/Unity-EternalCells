// Projectile.cs (Runtime Layer Ayarlarý ile Güncellenmiþ Hali)
using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    // Temel mermi özellikleri (hýz, yön vb.)
    private float speed;
    private Vector3 direction;
    private Rigidbody rb;
    private bool initialized = false;
    private float currentLifetime = 0f;

    // Inspector'dan ayarlanabilen varsayýlanlar
    [Header("Genel Ayarlar (Varsayýlanlar)")]
    [Tooltip("Merminin hiçbir þeye çarpmazsa yok olmadan önce ne kadar süre var olacaðý.")]
    [SerializeField] private float lifetime = 5f;
    // Bu varsayýlanlar, Initialize ile üzerine yazýlmazsa kullanýlabilir.
    // Ama genellikle Initialize ile her zaman belirttiðimiz için bu alanlar Projectile prefab'ýnda
    // çok kritik olmayabilir, daha çok bir fallback görevi görür.
    [Tooltip("Merminin varsayýlan olarak çarpýþacaðý katmanlar.")]
    [SerializeField] private LayerMask defaultCollisionLayers;
    [Tooltip("Merminin varsayýlan olarak AoE hasarý vereceði katmanlar.")]
    [SerializeField] private LayerMask defaultAoeTargetLayers;

    [Header("Hasar Ayarlarý")]
    public float damage = 10f;
    public bool causesHitStopOnImpact = true;
    public GameObject sourceGameObject { get; set; }

    [Header("Alan Etkisi (AoE) Ayarlarý")]
    public bool isAoEProjectile = false;
    [SerializeField] private float aoeRadius = 3f;
    [SerializeField] private float aoeDamage = 0f;
    [SerializeField] private bool aoeCausesHitStop = false;

    [Header("Efektler")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float effectDestroyDelay = 2f;

    // Runtime'da kullanýlacak LayerMask'lar
    private LayerMask _currentCollisionLayers;
    private LayerMask _currentAoeTargetLayers;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Projectile bir Rigidbody component'ine ihtiyaç duyar!", this);
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
        // AoE deðilse bile aoeLayersToTarget'ý ata, AoE kontrolü hasar verirken yapýlýr.
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

    // Opsiyonel: Eski Initialize çaðrýlarý için varsayýlan katmanlarý kullanan overload.
    // Eðer tüm ateþlemeler 5 parametreli Initialize ile yapýlacaksa bu silinebilir.
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