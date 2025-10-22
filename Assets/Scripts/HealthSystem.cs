// HealthSystem.cs

using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using TMPro; // TextMeshPro için eklendi

// DamageInfo struct'ı aynı kalır
public struct DamageInfo
{
    public float amount;
    public Vector3 direction;
    public GameObject dealer;
    public Vector3 hitPoint;
    public bool causesHitStop;

    public DamageInfo(float amt, Vector3 dir, GameObject dlr, Vector3 hitPt, bool hitStop = true)
    {
        amount = amt;
        direction = dir;
        dealer = dlr;
        hitPoint = hitPt;
        causesHitStop = hitStop;
    }
}

public class HealthSystem : MonoBehaviour
{
    [Header("Temel Can Ayarları")]
    [SerializeField] private float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;

    private bool isDead = false;

    [Header("Hasar Aldığında Geri Bildirim")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Renderer[] characterRenderers;
    private Material[] originalMaterials;
    private Coroutine _flashCoroutine;

    [Header("Geri İtme (Knockback)")]
    [SerializeField] private float knockbackForceMultiplier = 0.5f;
    [SerializeField] private float knockbackStunDuration = 0.2f;
    private Rigidbody rb;

    [Header("Zaman Durması (Hit Stop)")]
    [SerializeField] private bool triggerGlobalHitStopOnDamageTaken = true;
    [SerializeField] private float hitStopDurationOnTaken = 0.07f;

    [Header("Ölüm")]
    [Tooltip("Öldüğünde ortaya çıkacak efekt prefabı.")]
    [SerializeField] private GameObject deathEffectPrefab;
    [Tooltip("Ölüm efektinin ana objenin pozisyonuna göre uygulanacak offset'i.")]
    [SerializeField] private Vector3 deathEffectOffset = Vector3.zero;
    [Tooltip("Öldüğünde script, collider gibi bileşenler devre dışı bırakılsın mı?")]
    [SerializeField] private bool disableComponentsOnDeath = true;

    // YENİ: Yüzen Hasar Metni Ayarları
    [Header("Yüzen Hasar Metni")]
    [SerializeField] private GameObject floatingDamageTextPrefab; // TextMeshPro içeren bir prefab olmalı
    [SerializeField] private float floatingTextSpawnOffsetY = 1.5f; // Metnin düşmanın üzerinde ne kadar yükseklikte çıkacağı
    [SerializeField] private float floatingTextRandomSpread = 0.5f; // Metnin X ve Z eksenlerinde ne kadar rastgele dağılacağı
    [SerializeField] private Color[] damageTextColors; // Hasar metni için potansiyel renkler

    public event Action<DamageInfo> OnDamaged;
    public event Action<float, float> OnHealthChanged; // Parametreler: currentHealth, maxHealth
    public event Action OnDeath;

    private TopDownController playerController;
    private EnemyAI enemyAI;
    private NavMeshAgent agent;

    void Awake()
    {
        CurrentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<TopDownController>();
        enemyAI = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();

        if (characterRenderers == null || characterRenderers.Length == 0)
        {
            characterRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (characterRenderers.Length > 0)
        {
            originalMaterials = new Material[characterRenderers.Length];
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null)
                {
                    originalMaterials[i] = characterRenderers[i].material;
                }
            }
        }
    }

    void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }


    public void TakeDamage(DamageInfo damageInfo)
    {
        Debug.Log($"TakeDamage çağrıldı: {gameObject.name}, Hasar: {damageInfo.amount}");
        if (isDead || CurrentHealth <= 0) return;

        CurrentHealth -= damageInfo.amount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        // YENİ: Yüzen Hasar Metnini Göster
        if (floatingDamageTextPrefab != null && damageInfo.amount > 0)
        {
            Debug.Log("ShowFloatingDamageText çağrılacak."); // BU SATIRI EKLE
            ShowFloatingDamageText(damageInfo);
        }

        OnDamaged?.Invoke(damageInfo);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        HandleMaterialFlash();
        HandleKnockback(damageInfo);
        HandleGlobalTimeStop(damageInfo);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    // YENİ METOT: Yüzen Hasar Metnini Oluşturur ve Ayarlar
    private void ShowFloatingDamageText(DamageInfo damageInfo)
    {
        Vector3 spawnPosition = damageInfo.hitPoint; // Vuruş noktasını temel al
        spawnPosition.y += floatingTextSpawnOffsetY; // Biraz yukarıda çıksın
        Debug.Log($"ShowFloatingDamageText İÇİNDE. Spawn Pozisyonu: {spawnPosition}, Prefab: {floatingDamageTextPrefab.name}");
        // Rastgele yatay bir offset ekle
        spawnPosition.x += UnityEngine.Random.Range(-floatingTextRandomSpread, floatingTextRandomSpread);
        spawnPosition.z += UnityEngine.Random.Range(-floatingTextRandomSpread, floatingTextRandomSpread);

        GameObject textInstance = Instantiate(floatingDamageTextPrefab, spawnPosition, Quaternion.identity);
        FloatingDamageText textScript = textInstance.GetComponent<FloatingDamageText>();

        if (textScript != null)
        {
            Color textColor = Color.white; // Varsayılan renk
            if (damageTextColors != null && damageTextColors.Length > 0)
            {
                textColor = damageTextColors[UnityEngine.Random.Range(0, damageTextColors.Length)];
            }
            textScript.Initialize(Mathf.RoundToInt(damageInfo.amount).ToString(), textColor);
        }
        else
        {
            // Fallback eğer script yoksa (TextMeshPro component'ine direkt erişim)
            TextMeshPro tmPro = textInstance.GetComponentInChildren<TextMeshPro>();
            if (tmPro != null)
            {
                tmPro.text = Mathf.RoundToInt(damageInfo.amount).ToString();
                if (damageTextColors != null && damageTextColors.Length > 0)
                {
                    tmPro.color = damageTextColors[UnityEngine.Random.Range(0, damageTextColors.Length)];
                }
            }
            // Bu durumda metnin animasyonu ve yok olması prefab'da ayarlanmış olmalı veya basit bir Destroy eklenebilir.
            Destroy(textInstance, 1f); // Örnek: 1 saniye sonra yok et
            Debug.LogWarning("FloatingDamageText prefab'ında FloatingDamageText scripti bulunamadı. Basit gösterim yapılıyor.");
        }
    }

    private void HandleMaterialFlash()
    {
        if (flashMaterial != null && characterRenderers != null && characterRenderers.Length > 0 && originalMaterials != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashMaterialsCoroutine());
        }
    }

    private IEnumerator FlashMaterialsCoroutine()
    {
        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null && characterRenderers[i].enabled)
            {
                characterRenderers[i].material = flashMaterial;
            }
        }

        yield return new WaitForSecondsRealtime(flashDuration);

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null && characterRenderers[i].enabled &&
              originalMaterials[i] != null)
            {
                characterRenderers[i].material = originalMaterials[i];
            }
        }
        _flashCoroutine = null;
    }

    private void HandleKnockback(DamageInfo damageInfo)
    {
        if (rb == null || damageInfo.direction == Vector3.zero || knockbackForceMultiplier <= 0 || damageInfo.amount <= 0) return;

        if (playerController != null)
        {
            playerController.ApplyKnockback(damageInfo.direction, damageInfo.amount * knockbackForceMultiplier, knockbackStunDuration);
        }
        else if (enemyAI != null)
        {
            enemyAI.ApplyKnockback(damageInfo.direction, damageInfo.amount * knockbackForceMultiplier, knockbackStunDuration);
        }
    }

    private void HandleGlobalTimeStop(DamageInfo damageInfo)
    {
        bool shouldCauseHitStop = damageInfo.causesHitStop &&
                   (playerController != null ? triggerGlobalHitStopOnDamageTaken : (damageInfo.dealer != null && damageInfo.dealer.CompareTag("Player")));

        if (shouldCauseHitStop && TimeStopper.Instance != null && hitStopDurationOnTaken > 0)
        {
            TimeStopper.Instance.StopTime(hitStopDurationOnTaken);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (enemyAI != null) enemyAI.HandleDeath();

        OnDeath?.Invoke();

        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position + deathEffectOffset, transform.rotation);
        }

        if (disableComponentsOnDeath)
        {
            if (agent != null && agent.enabled) agent.enabled = false;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Collider[] childColliders = GetComponentsInChildren<Collider>();
            foreach (Collider childCol in childColliders)
            {
                if (childCol != col) childCol.enabled = false;
            }


            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (characterRenderers != null)
            {
                foreach (var rend in characterRenderers)
                {
                    if (rend != null) rend.enabled = false;
                }
            }
        }
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (isDead || amount <= 0) return;
        maxHealth += amount;
        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0 || CurrentHealth >= maxHealth) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void SetHealth(float newHealth, float newMaxHealth)
    {
        maxHealth = Mathf.Max(0, newMaxHealth);
        CurrentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        isDead = false;

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
    public void ForceKill()
    {
        if (isDead) return;
        CurrentHealth = 0;
        DamageInfo fatalDamage = new DamageInfo(maxHealth, Vector3.zero, null, transform.position, false);
        OnDamaged?.Invoke(fatalDamage);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        Die();
    }
}