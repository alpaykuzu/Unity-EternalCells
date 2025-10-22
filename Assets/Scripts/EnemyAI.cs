// EnemyAI.cs
// Düşman yapay zekasını yönetir. Devriye, takip, saldırı ve ölüm durumlarını içerir.
// Oda temizlendiğinde RoomController'a bilgi verir.
using UnityEngine;
using UnityEngine.AI; // Melee için gerekli, Kristal için değil
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyBehaviorType { Melee, StationaryRangedCrystal }
    [Header("AI Behavior Type")]
    [Tooltip("Bu düşmanın davranış tipini seçin.")]
    public EnemyBehaviorType behaviorType = EnemyBehaviorType.Melee;

    [Header("Common References")]
    public Transform player;
    private HealthSystem healthSystem;
    private Rigidbody rb; // Kristal de Rigidbody'ye sahip olabilir (örn: trigger için) ama kinematic olmalı.
    public RoomController roomController;

    [Header("Melee Specific References")]
    [Tooltip("Sadece Melee düşmanlar için NavMeshAgent.")]
    public NavMeshAgent agent; // Kristal ise bu null olabilir/olmalı
    [Tooltip("Sadece Melee düşmanlar veya animasyonlu Kristaller için Animator.")]
    public Animator animator; // Kristal basitse bu null olabilir

    [Header("Patrol Settings (Melee Only)")]
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange = 10f;
    public float patrolAreaRadius = 20f;
    private Vector3 initialSpawnPositionOnNavMesh;
    public float patrolPointWaitTime = 1f;
    private float waitTimer;

    [Header("Common Combat Settings")]
    public float timeBetweenAttacks = 2f;
    bool alreadyAttacked;
    public float sightRange = 15f;

    [Header("Melee Combat Settings")]
    public float attackRange = 2f;
    public float actualAttackTriggerRange = 1.8f;
    public float attackRotationAngleY = 0f;
    public float attackDamage = 10f;
    public Vector3 attackHitboxOffset = new Vector3(0, 1f, 1f);
    public float attackHitboxRadius = 0.7f;

    [Header("Crystal Ranged Attack Settings")]
    public GameObject crystalProjectilePrefab;
    public Transform crystalProjectileSpawnPoint;
    public float crystalProjectileSpeed = 15f;
    public float crystalAttackDamage = 12f;
    public float crystalFireDelay = 0.5f; // Animasyon yoksa, bu direkt mermi ateşleme gecikmesi
    public float crystalRotationSpeed = 10f;
    [Tooltip("Kristal mermisi AoE (alan etkili) mi olacak?")]
    public bool isCrystalProjectileAoE = false; // Bu EnemyAI'da projectile'ın AoE olup olmadığını belirler
    [Tooltip("Kristal mermisinin çarpışacağı katmanlar (Oyuncu, Çevre vb.).")]
    public LayerMask crystalProjectileCollisionLayers;
    [Tooltip("Kristal mermisinin (eğer AoE ise) AoE hasarı vereceği katmanlar (Oyuncu vb.).")]
    public LayerMask crystalProjectileAoeTargetLayers;


    [Header("States")]
    public bool playerInSightRange;
    public bool playerInAttackTriggerRange; // Melee için
    private bool isStunned = false;

    [Header("Melee Stats")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;

    [Header("Common Targeting")]
    public LayerMask whatIsPlayer; // Genellikle Melee hitbox için, Kristal direkt Player Transform'unu hedefler

    private bool isAgentInitialized = false; // Melee için veya AI'nın genel hazır olma durumu
    private Coroutine _knockbackCoroutine;

    // public bool isAoEProjectile = false; // Bu satır gereksiz, crystalProjectileAoe ile birleşti. EnemyAI'dan silebilirsin.
    [Header("UI References")] // YENİ BAŞLIK
    [Tooltip("Düşmanın üzerinde görünecek can barı prefabı (Canvas ve Slider içermeli).")]
    public GameObject healthBarPrefab;
    [Tooltip("Can barının düşmanın üzerinde ne kadar yükseklikte duracağı.")]
    public float healthBarOffsetY = 2.0f;
    private Slider healthBarSlider; // Düşmana ait can barının Slider component'i
    private GameObject healthBarInstance; // Oluşturulan can barı objesi

    [Header("Puan Ayarları")] // YENİ BAŞLIK
    [Tooltip("Bu düşman öldüğünde oyuncuya verilecek puan.")]
    public int scoreOnDeath = 50;

    private enum PatrolState { Idle, Searching, MovingToTarget, Waiting }
    private PatrolState currentPatrolState = PatrolState.Idle;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        rb = GetComponent<Rigidbody>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
            else
            {
                Debug.LogError("EnemyAI: Player Tag'ine sahip bir obje bulunamadı!", this);
                enabled = false; return;
            }
        }

        if (rb == null && behaviorType == EnemyBehaviorType.Melee) // Melee knockback için RB gerekebilir
        {
            Debug.LogWarning($"{name} (Melee) Rigidbody'ye sahip değil. Knockback için eklenebilir.", this);
            // rb = gameObject.AddComponent<Rigidbody>();
        }
        if (rb != null)
        {
            rb.isKinematic = true; // Genelde AI kontrolünde kinematic iyidir, knockback'te false yapılır.
        }


        if (behaviorType == EnemyBehaviorType.Melee)
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError($"{name} bir Melee düşman olarak ayarlandı ancak NavMeshAgent'ı yok! AI düzgün çalışmayabilir.", this);
                // enabled = false; // Agent yoksa Melee AI'ı devre dışı bırakabilirsin
            }
            else
            {
                agent.enabled = false;
            }
            if (animator == null) animator = GetComponentInChildren<Animator>(); // Melee'ler genelde animatörlü olur
            if (animator == null) Debug.LogWarning($"{name} (Melee) için Animator bulunamadı.", this);
        }
        else if (behaviorType == EnemyBehaviorType.StationaryRangedCrystal)
        {
            if (agent != null) // Kristalin agent'a ihtiyacı yok
            {
                Debug.LogWarning($"{name} bir Kristal düşman olarak ayarlandı ancak NavMeshAgent'ı vardı. Devre dışı bırakılıyor.", this);
                agent.enabled = false;
                // Destroy(agent); // İsteğe bağlı, editörde manuel kaldırılabilir
            }
            // Kristalin basit bir "ateş etme" animasyonu olabilir ama şart değil.
            // Eğer Animator component'i varsa ve atanmadıysa bulmayı deneyebilir.
            if (animator == null) animator = GetComponentInChildren<Animator>();
            // if (animator == null) Debug.LogWarning($"{name} (Crystal) için Animator bulunamadı. Saldırı animasyonu oynatılmayacak.", this);


            if (crystalProjectilePrefab == null) Debug.LogError($"{name} (Crystal) için Crystal Projectile Prefab atanmamış!", this);
            if (crystalProjectileSpawnPoint == null)
            {
                Debug.LogWarning($"{name} (Crystal) için Crystal Projectile Spawn Point atanmamış! Düşmanın kendi transform'u kullanılacak.", this);
                crystalProjectileSpawnPoint = transform;
            }
        }

        if (healthSystem == null) Debug.LogError($"{name} üzerinde HealthSystem bulunamadı!", this);
        else
        {
            // YENİ: Can değişim olayına abone ol
            healthSystem.OnHealthChanged += UpdateHealthBarUI;
            healthSystem.OnDeath += HideHealthBarOnDeath; // Ölünce can barını gizle/yok et
        }
    }

    void Start()
    {
        if (roomController == null)
        {
            roomController = GetComponentInParent<RoomController>();
            if (roomController == null) Debug.LogWarning($"{name} bir RoomController'a bağlı değil!", this);
        }

        if (behaviorType == EnemyBehaviorType.Melee)
        {
            if (agent != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(InitializeAgentOnNavMesh());
            }
            else if (agent == null)
            {
                isAgentInitialized = false; // Agent yoksa init olamaz
                Debug.LogWarning($"{name} (Melee) için NavMeshAgent atanmamış, AI hareket edemeyecek.", this);
            }
        }
        else if (behaviorType == EnemyBehaviorType.StationaryRangedCrystal)
        {
            isAgentInitialized = true; // Kristal için NavMesh init'e gerek yok, doğrudan hazır.
            if (rb != null) rb.isKinematic = true;
        }

        // YENİ: Can barını oluştur ve ayarla
        if (healthBarPrefab != null && healthSystem != null) // healthSystem null değilse can barı oluştur
        {
            // Düşmanın tepesine bir pozisyon belirle
            Vector3 healthBarPosition = transform.position + Vector3.up * healthBarOffsetY;
            healthBarInstance = Instantiate(healthBarPrefab, healthBarPosition, Quaternion.identity, transform); // Düşmanın çocuğu yap

            // Instantiate edilen prefab'daki EnemyHealthBarUI scriptini al (aşağıda tanımlanacak)
            EnemyHealthBarUI healthBarUIScript = healthBarInstance.GetComponent<EnemyHealthBarUI>();
            if (healthBarUIScript != null)
            {
                healthBarUIScript.Initialize(Camera.main, healthSystem.CurrentHealth, healthSystem.MaxHealth);
                healthBarSlider = healthBarUIScript.GetSlider(); // Slider referansını al
            }
            else
            {
                // Fallback: Eğer EnemyHealthBarUI scripti yoksa direkt Slider'ı bulmaya çalış
                healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
                if (healthBarSlider != null)
                {
                    healthBarSlider.maxValue = healthSystem.MaxHealth;
                    healthBarSlider.value = healthSystem.CurrentHealth;
                    // Billboarding için basit bir script can barı prefabında olabilir.
                }
                else Debug.LogWarning($"{name} için can barı prefabında Slider bulunamadı veya EnemyHealthBarUI scripti eksik.", this);
            }
            // Başlangıçta can barını güncelle
            UpdateHealthBarUI(healthSystem.CurrentHealth, healthSystem.MaxHealth);
        }
        else if (healthBarPrefab == null)
        {
            Debug.LogWarning($"{name} için Health Bar Prefab atanmamış. Can barı gösterilmeyecek.", this);
        }
    }

    // YENİ: OnDestroy'da event aboneliğini kaldır
    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= UpdateHealthBarUI;
            healthSystem.OnDeath -= HideHealthBarOnDeath;
        }
    }

    // YENİ: Can barını gizlemek için (ölüm durumunda çağrılır)
    private void HideHealthBarOnDeath()
    {
        if (healthBarInstance != null)
        {
            // healthBarInstance.SetActive(false); // Gizle
            Destroy(healthBarInstance); // Veya direkt yok et
        }
    }


    // YENİ METOT: Can barı UI'ını günceller
    private void UpdateHealthBarUI(float currentHealth, float maxHealth)
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
            // DEBUG: Slider'a atanan değerleri logla
            Debug.Log($"UpdateHealthBarUI: {gameObject.name} - Slider.maxValue: {healthBarSlider.maxValue}, Slider.value: {healthBarSlider.value} (Gelen: current={currentHealth}, max={maxHealth})");

            // Can barını sadece hasar aldığında veya iyileştiğinde görünür yap (isteğe bağlı)
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(currentHealth < maxHealth && currentHealth > 0);
            }
        }
    }

    void OnEnable()
    {
        // Player referansını tekrar kontrol et, özellikle obje pool kullanılıyorsa
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (behaviorType == EnemyBehaviorType.Melee)
        {
            if (agent != null && !isAgentInitialized && Application.isPlaying)
            {
                StartCoroutine(InitializeAgentOnNavMesh());
            }
        }
        else if (behaviorType == EnemyBehaviorType.StationaryRangedCrystal)
        {
            isAgentInitialized = true;
            if (rb != null) rb.isKinematic = true;
        }
    }

    public void AssignRoomController(RoomController controller)
    {
        roomController = controller;
    }

    System.Collections.IEnumerator InitializeAgentOnNavMesh()
    {
        if (isAgentInitialized || behaviorType != EnemyBehaviorType.Melee || agent == null) yield break;
        yield return null; // Bir frame bekle NavMesh hazır olsun

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
        {
            // Pozisyonu NavMesh üzerine taşıma Warp ile yapılmalı, agent aktifken.
            // transform.position = hit.position;
            agent.enabled = true;
            if (agent.isOnNavMesh) // Zaten NavMesh üzerindeyse direkt Warp et
            {
                agent.Warp(hit.position);
            }
            else // Değilse ve SamplePosition başarılıysa, bu biraz garip. Pozisyonu ayarla ve Warp dene.
            {
                transform.position = hit.position; // Önce pozisyonu ayarla
                agent.Warp(hit.position); // Sonra warp etmeyi dene
            }


            if (agent.isOnNavMesh)
            {
                agent.speed = 0;
                agent.isStopped = true;
                agent.stoppingDistance = 0.5f; // Patrol için varsayılan
                initialSpawnPositionOnNavMesh = agent.transform.position;
                isAgentInitialized = true;
                currentPatrolState = PatrolState.Searching;
                if (rb != null) rb.isKinematic = true;
            }
            else
            {
                Debug.LogError($"PATROL_DEBUG: {name} NavMeshAgent aktifleştirildi ANCAK NavMesh üzerinde DEĞİL (Pozisyon: {transform.position}, Hit: {hit.position}). AI devre dışı.", this);
                agent.enabled = false;
            }
        }
        else
        {
            Debug.LogError($"PATROL_DEBUG: {name} ({transform.position}) NavMesh üzerinde başlangıç pozisyonuna yakın geçerli bir nokta bulunamadı. EnemyAI devre dışı.", this);
            if (agent != null) agent.enabled = false; // Agent'ı kapat
        }
    }

    private void Update()
    {
        if (player == null || isStunned || (healthSystem != null && healthSystem.CurrentHealth <= 0))
        {
            if (isStunned || (healthSystem != null && healthSystem.CurrentHealth <= 0))
            {
                if (behaviorType == EnemyBehaviorType.Melee && agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                UpdateAnimationsInternal(true);
            }
            return;
        }

        if (!isAgentInitialized && behaviorType == EnemyBehaviorType.Melee) // Melee ve init olmadıysa bekle
        {
            return;
        }
        // isAgentInitialized artık kristal için de Start/OnEnable'da true yapılıyor.

        if (behaviorType == EnemyBehaviorType.StationaryRangedCrystal)
        {
            HandleCrystalBehavior();
        }
        else if (behaviorType == EnemyBehaviorType.Melee)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return; // Güvenlik kontrolü

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            playerInSightRange = distanceToPlayer <= sightRange;
            playerInAttackTriggerRange = distanceToPlayer <= actualAttackTriggerRange;

            if (playerInAttackTriggerRange && playerInSightRange) AttackPlayer();
            else if (playerInSightRange) ChasePlayer();
            else Patroling();
        }

        UpdateAnimationsInternal();
    }

    // --- MELEE DAVRANIŞLARI ---
    // Patroling, SearchWalkPoint, ChasePlayer, AttackPlayer, AnimationEvent_DealDamage
    // Bu metotlar büyük ölçüde aynı kalabilir, ancak agent ve animator null check'leri eklenebilir.
    // Örnek:
    private void Patroling()
    {
        if (behaviorType != EnemyBehaviorType.Melee || agent == null || !agent.isOnNavMesh || !agent.enabled) return;
        // ... (geri kalan patrol mantığı)
        if (agent.stoppingDistance != 0.5f) agent.stoppingDistance = 0.5f;
        agent.updateRotation = true;
        agent.speed = walkSpeed;

        switch (currentPatrolState)
        {
            case PatrolState.Idle:
                agent.isStopped = true;
                if (waitTimer > 0) currentPatrolState = PatrolState.Waiting;
                else currentPatrolState = PatrolState.Searching;
                break;
            case PatrolState.Waiting:
                agent.isStopped = true;
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0) currentPatrolState = PatrolState.Searching;
                break;
            case PatrolState.Searching:
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
                SearchWalkPoint();
                if (walkPointSet)
                {
                    if (agent.SetDestination(walkPoint))
                    {
                        currentPatrolState = PatrolState.MovingToTarget;
                        agent.isStopped = false;
                    }
                    else walkPointSet = false;
                }
                break;
            case PatrolState.MovingToTarget:
                agent.isStopped = false;
                if (!agent.pathPending && agent.hasPath)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance)
                    {
                        walkPointSet = false;
                        agent.isStopped = true;
                        waitTimer = patrolPointWaitTime;
                        currentPatrolState = PatrolState.Idle;
                    }
                }
                else if (!agent.pathPending && !agent.hasPath) // Hedefe ulaşılamıyorsa
                {
                    Debug.LogWarning($"{name} devriye noktasına ({walkPoint}) ulaşamadı. Yeni nokta aranıyor.");
                    walkPointSet = false;
                    currentPatrolState = PatrolState.Searching;
                }
                break;
        }
    }

    private void SearchWalkPoint()
    {
        if (behaviorType != EnemyBehaviorType.Melee || agent == null || !agent.enabled) return;
        // ... (geri kalan search mantığı)
        float minPatrolStepDistance = Mathf.Max(2.0f, agent.stoppingDistance * 4f);
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomRadiusForStep = Random.Range(minPatrolStepDistance, walkPointRange);
        Vector3 randomDirectionForStep = new Vector3(Mathf.Sin(randomAngle), 0, Mathf.Cos(randomAngle));
        Vector3 potentialWalkPoint = initialSpawnPositionOnNavMesh + randomDirectionForStep * randomRadiusForStep;

        if (Vector3.Distance(potentialWalkPoint, initialSpawnPositionOnNavMesh) > patrolAreaRadius)
        {
            potentialWalkPoint = initialSpawnPositionOnNavMesh + (potentialWalkPoint - initialSpawnPositionOnNavMesh).normalized * patrolAreaRadius;
        }
        potentialWalkPoint.y = transform.position.y;

        NavMeshHit hit;
        float sampleSearchRadius = Mathf.Max(2.0f, walkPointRange * 0.5f);
        if (NavMesh.SamplePosition(potentialWalkPoint, out hit, sampleSearchRadius, NavMesh.AllAreas))
        {
            if (Vector3.Distance(hit.position, transform.position) > agent.stoppingDistance + 0.5f)
            {
                walkPoint = hit.position;
                walkPointSet = true;
            }
            else walkPointSet = false;
        }
        else walkPointSet = false;
    }

    private void ChasePlayer()
    {
        if (behaviorType != EnemyBehaviorType.Melee || agent == null || !agent.isOnNavMesh || !agent.enabled || player == null) return;
        // ... (geri kalan chase mantığı)
        currentPatrolState = PatrolState.Idle;
        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.stoppingDistance = attackRange;
        agent.updateRotation = true;
        agent.SetDestination(player.position);
    }

    private void AttackPlayer() // Sadece Melee
    {
        if (behaviorType != EnemyBehaviorType.Melee || agent == null || !agent.isOnNavMesh || !agent.enabled || player == null) return;
        // ... (geri kalan attack mantığı)
        currentPatrolState = PatrolState.Idle;
        agent.isStopped = true;
        if (agent.hasPath) agent.ResetPath();
        agent.velocity = Vector3.zero;

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            targetRotation *= Quaternion.Euler(0, attackRotationAngleY, 0);
            transform.rotation = targetRotation;
        }

        if (!alreadyAttacked)
        {
            if (animator != null) animator.SetTrigger("Attack");
            else Debug.LogWarning($"{name} (Melee) saldırı animasyonu için Animator'e sahip değil.");
            // AnimationEvent_DealDamage hasarı tetikleyecek
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }
    public void AnimationEvent_DealDamage() // Sadece Melee için
    {
        if (behaviorType != EnemyBehaviorType.Melee || player == null || healthSystem == null || healthSystem.CurrentHealth <= 0) return;
        // whatIsPlayer LayerMask'ını kullanarak oyuncuyu bulmak daha doğru olabilir
        // ya da direkt player referansının collider'ını kontrol etmek.
        Collider[] hits = Physics.OverlapSphere(transform.TransformPoint(attackHitboxOffset), attackHitboxRadius, whatIsPlayer);
        foreach (Collider hitCollider in hits)
        {
            // Sadece player transformuna sahip olan collider'a hasar ver
            if (hitCollider.transform == player)
            {
                HealthSystem playerHealth = player.GetComponent<HealthSystem>(); // Zaten player referansımız var
                if (playerHealth != null)
                {
                    Vector3 hitPoint = player.position + Vector3.up * 0.5f;
                    Vector3 damageDirection = (player.position - transform.position).normalized;
                    DamageInfo damageInfo = new DamageInfo(attackDamage, damageDirection, gameObject, hitPoint, true);
                    playerHealth.TakeDamage(damageInfo);
                }
                break; // Oyuncuya bir kere hasar verince döngüden çık
            }
        }
    }

    // --- KRİSTAL DAVRANIŞLARI ---
    private void HandleCrystalBehavior()
    {
        if (behaviorType != EnemyBehaviorType.StationaryRangedCrystal || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        playerInSightRange = distanceToPlayer <= sightRange;

        if (playerInSightRange)
        {
            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0;
            if (directionToPlayer.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, crystalRotationSpeed * Time.deltaTime);
            }

            if (!alreadyAttacked)
            {
                if (animator != null) // Kristalin de basit bir saldırı animasyonu olabilir
                {
                    animator.SetTrigger("Attack");
                }
                StartCoroutine(FireCrystalProjectileAfterDelay(crystalFireDelay));
                alreadyAttacked = true;
                Invoke(nameof(ResetAttack), timeBetweenAttacks);
            }
        }
    }

    private IEnumerator FireCrystalProjectileAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FireCrystalProjectile();
    }

    private void FireCrystalProjectile()
    {
        if (behaviorType != EnemyBehaviorType.StationaryRangedCrystal || crystalProjectilePrefab == null || player == null) return;
        if (healthSystem != null && healthSystem.CurrentHealth <= 0) return;

        Transform spawnPointToUse = crystalProjectileSpawnPoint != null ? crystalProjectileSpawnPoint : transform;
        Vector3 directionToTarget = (player.position - spawnPointToUse.position).normalized;
        // Merminin Y ekseninde de oyuncuyu hedef almasını istiyorsak:
        // Vector3 targetPosForProjectile = player.position + Vector3.up * 1f; // Oyuncunun yaklaşık göğüs hizası
        // directionToTarget = (targetPosForProjectile - spawnPointToUse.position).normalized;


        GameObject projectileGO = Instantiate(crystalProjectilePrefab, spawnPointToUse.position, Quaternion.LookRotation(directionToTarget));
        Projectile projectileScript = projectileGO.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.Initialize(
                crystalProjectileSpeed,
                directionToTarget,
                gameObject, // owner
                crystalProjectileCollisionLayers, // Düşmanın mermisi için belirlenen çarpışma katmanları
                crystalProjectileAoeTargetLayers  // Düşmanın mermisi için belirlenen AoE hedef katmanları
            );
            projectileScript.damage = crystalAttackDamage;
            projectileScript.isAoEProjectile = isCrystalProjectileAoE; // EnemyAI'daki bool değeri Projectile'a aktarılır
        }
        else
        {
            Debug.LogWarning($"{name} tarafından ateşlenen {crystalProjectilePrefab.name} üzerinde Projectile scripti bulunamadı.", projectileGO);
            // Fallback: Eğer Projectile scripti yoksa temel Rigidbody ile hareket ettir
            Rigidbody rbProjectile = projectileGO.GetComponent<Rigidbody>();
            if (rbProjectile == null) rbProjectile = projectileGO.AddComponent<Rigidbody>(); // Yoksa ekle
            rbProjectile.useGravity = false;
            rbProjectile.linearVelocity = directionToTarget * crystalProjectileSpeed;
            // Bu durumda hasar verme mekanizması merminin kendisine eklenmeli (örn: ayrı bir DamageOnCollide scripti)
        }
    }

    // --- ORTAK METODLAR ---
    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    // UpdateAnimationsInternal olarak yeniden adlandırıldı, çünkü UpdateAnimations UnityEngine.Animator'da var.
    private void UpdateAnimationsInternal(bool forceIdle = false)
    {
        if (animator == null) return; // Animator yoksa hiçbir şey yapma

        if (behaviorType == EnemyBehaviorType.StationaryRangedCrystal)
        {
            animator.SetFloat("Speed", 0); // Kristal her zaman sabit
            if (forceIdle)
            {
                // Gerekirse kristal için özel idle durumu
            }
        }
        else if (behaviorType == EnemyBehaviorType.Melee)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                float speed = (agent.isStopped || forceIdle) ? 0 : agent.velocity.magnitude;
                animator.SetFloat("Speed", speed);
            }
            else
            {
                animator.SetFloat("Speed", 0); // Agent yoksa veya aktif değilse hız 0
            }
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (isStunned || (healthSystem != null && healthSystem.CurrentHealth <= 0)) return;

        if (behaviorType == EnemyBehaviorType.StationaryRangedCrystal)
        {
            if (animator != null) animator.SetTrigger("Hit");
            // Kristal hareket etmez, sadece animasyon
            return;
        }

        // Melee için knockback (Rigidbody gerektirir)
        if (rb == null)
        {
            Debug.LogWarning($"{name} (Melee) knockback alamaz çünkü Rigidbody'si yok.", this);
            //if (animator != null) animator.SetTrigger("Hit"); // Sadece animasyon
            return;
        }

        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutineInternal(direction, force, duration));
    }

    private IEnumerator KnockbackCoroutineInternal(Vector3 direction, float force, float duration)
    {
        isStunned = true;
        if (agent != null && agent.enabled) agent.enabled = false;

        rb.isKinematic = false;
        rb.AddForce(direction * force, ForceMode.Impulse);
        //if (animator != null) animator.SetTrigger("Hit");

        yield return new WaitForSeconds(duration);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        if (healthSystem != null && healthSystem.CurrentHealth > 0)
        {
            if (behaviorType == EnemyBehaviorType.Melee && agent != null)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                }
                if (!agent.enabled) agent.enabled = true; // Agent'ı tekrar aktifleştir

                // Agent'ın NavMesh üzerinde olup olmadığını tekrar kontrol et
                if (agent.isOnNavMesh)
                {
                    agent.Warp(transform.position); // Warp etmeden önce isOnNavMesh kontrolü
                    agent.isStopped = false;
                }
                else if (agent.enabled)
                { // enabled ama hala onNavMesh değilse sorun var
                    Debug.LogWarning($"{name} knockback sonrası NavMesh üzerinde değil! Agent tekrar devre dışı bırakılıyor.", this);
                    agent.enabled = false;
                }
            }
        }
        isStunned = false;
        _knockbackCoroutine = null;
    }

    public void HandleDeath()
    {
        if (!this.enabled || isStunned && healthSystem != null && healthSystem.CurrentHealth <= 0) return; // Script zaten disabled ise veya zaten ölüyse


        StopAllCoroutines();
        _knockbackCoroutine = null;
        isStunned = true;

        if (behaviorType == EnemyBehaviorType.Melee && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Die");
        }
        else
        {
            // Animatör yoksa, belki direkt objeyi bir süre sonra yok et
            // veya bir ölüm efekti oynat.
            Debug.LogWarning($"{name} için Animator bulunamadı, ölüm animasyonu oynatılamıyor.");
        }

        if (roomController != null)
        {
            roomController.OnEnemyDefeated(this);
        }
        else Debug.LogWarning($"{name} bir RoomController'a sahip değil, yenilgi bildirilemedi.", gameObject);


        // YENİ: Ölüm durumunda can barı örneğini yok et (eğer HideHealthBarOnDeath yetmiyorsa)
        // if (healthBarInstance != null)
        // {
        //     Destroy(healthBarInstance);
        // }

        // Bu script'i devre dışı bırakarak Update vb. çağrılarını durdur.
        // HealthSystem.Die() objeyi hemen yok etmiyorsa bu önemlidir.
        // Eğer healthBarInstance'ı düşmanın çocuğu yaptıysanız, düşman yok edildiğinde o da yok olacaktır.
        // Aksi takdirde burada ayrıca Destroy(healthBarInstance) gerekebilir.

        // YENİ: Puan Yöneticisine Puan Ekle ve Yüzen Puan Metnini Tetikle
        if (ScoreManager.Instance != null)
        {
            // Yüzen metnin çıkış pozisyonunu düşmanın biraz yukarısı olarak ayarlayabiliriz.
            Vector3 scoreTextPosition = transform.position + Vector3.up * 1.5f; // Örneğin 1.5 birim yukarıda
            ScoreManager.Instance.AddScore(scoreOnDeath, scoreTextPosition);
        }

        // Bu script'i devre dışı bırakarak Update vb. çağrılarını durdur.
        // HealthSystem.Die() objeyi hemen yok etmiyorsa bu önemlidir.
        this.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (behaviorType == EnemyBehaviorType.Melee)
        {
            if (agent != null) // Agent varsa çiz
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, actualAttackTriggerRange);
                if (agent.isOnNavMesh)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(transform.position, agent.stoppingDistance);
                }
                if (isAgentInitialized && initialSpawnPositionOnNavMesh != Vector3.zero)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(initialSpawnPositionOnNavMesh, patrolAreaRadius);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(initialSpawnPositionOnNavMesh, walkPointRange);
                }
                if (walkPointSet)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(walkPoint, 0.5f);
                }
            }
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Vector3 worldMeleeHitboxCenter = transform.TransformPoint(attackHitboxOffset);
            Gizmos.DrawSphere(worldMeleeHitboxCenter, attackHitboxRadius);
        }
        else if (behaviorType == EnemyBehaviorType.StationaryRangedCrystal)
        {
            if (crystalProjectileSpawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(crystalProjectileSpawnPoint.position, 0.2f);
                Gizmos.DrawLine(transform.position, crystalProjectileSpawnPoint.position);
            }
        }
    }
}