using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum WeaponType
{
    None, // Oyuncunun baþlangýçta bir silahý olmadýðýný belirtir
    Sword,
    Bow,
    Magic
}

public class TopDownController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float bowCharacterRotationSpeed = 15f;
    [SerializeField] private float magicCharacterRotationSpeed = 15f;

    [Header("Dodge Settings")]
    [SerializeField] private float dodgeForce = 15f;
    [SerializeField] private float dodgeDuration = 0.3f;
    [SerializeField] private float dodgeCooldown = 1f;
    [SerializeField] private Collider mainCollider;

    [Header("Weapon Settings")]
    [SerializeField] public WeaponType currentWeapon = WeaponType.None;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("Weapon Visuals (Held Objects)")]
    [SerializeField] private GameObject swordVisualPrefab;
    [SerializeField] private GameObject bowVisualPrefab;
    [SerializeField] private GameObject magicItemVisualPrefab;
    [SerializeField] private Transform weaponHoldPoint;
    private GameObject currentWeaponInstance;

    [Header("Bow Specific Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private float arrowSpeed = 25f;
    [SerializeField] private LineRenderer aimIndicator;
    [SerializeField] private float aimIndicatorRange = 15f;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float bowChargeToHoldAnimTime = 0.9f;
    [SerializeField] private float bowAimRotationOffset = 90f;
    [SerializeField] private float bowDamage = 15f;

    [Header("Magic Specific Settings")]
    [SerializeField] private GameObject magicProjectilePrefab;
    [SerializeField] private Transform magicSpawnPoint;
    [SerializeField] private float magicProjectileSpeed = 20f;
    [SerializeField] private float magicChargeToHoldAnimTime = 0.9f;
    [SerializeField] private bool useAimIndicatorForMagic = true;
    [SerializeField] private float magicAimRotationOffset = 90f;
    [SerializeField] private float magicDamage = 20f;

    [Header("Sword Attack Effect Settings")]
    [SerializeField] private GameObject outwardSlashEffectPrefab;
    [SerializeField] private Transform outwardSlashEffectSpawnPoint;
    [SerializeField] private Vector3 outwardSlashEffectOffset = Vector3.zero;
    [SerializeField] private GameObject inwardSlashEffectPrefab;
    [SerializeField] private Transform inwardSlashEffectSpawnPoint;
    [SerializeField] private Vector3 inwardSlashEffectOffset = Vector3.zero;
    [SerializeField] private float swordEffectDuration = 1f;

    [Header("Sword Attack Lunge & Damage Settings")]
    [SerializeField] private float swordLungeForce = 2f;
    [SerializeField] private bool enableSwordLockOn = true;
    [SerializeField] private float swordLockOnRange = 7f;
    [SerializeField] private float swordLockOnAngle = 90f;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float swordLockOnLungeForce = 5f;
    [SerializeField] private float swordDamage = 25f;
    [SerializeField] private Vector3 swordHitboxOffset = new Vector3(0, 1f, 0.75f);
    [SerializeField] private float swordHitboxRadius = 0.8f;

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerInput playerInput;
    private HealthSystem healthSystem;
    private bool isStunned = false;
    private Coroutine _knockbackCoroutine;

    [Header("Game Flow Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float delayBeforeLoadMainMenuOnDeath = 2.5f;

    private const string AttackAnimationTag = "Attack";
    private const string DodgeAnimationTag = "Dodge"; // Kullanýlmýyorsa kaldýrýlabilir
    private const string BowDrawAimAnimationTag = "BowDrawAim";
    private const string MagicChargeHoldAnimationTag = "MagicChargeHold";

    private const string DodgeAnimatorTrigger = "dodge";
    private const string WalkAnimatorBool = "walk";
    private const string SwordOutwardSlashAnimatorBool = "sword_outward_slash";
    private const string SwordInwardSlashAnimatorTrigger = "sword_inward_slash";
    private const string BowAttackAnimatorTrigger = "bow_attack";
    private const string IsAimingBowAnimatorBool = "is_aiming_bow";
    private const string MagicChargeAnimatorTrigger = "magic_charge";
    private const string IsAimingMagicAnimatorBool = "is_aiming_magic";

    private Vector2 rawMoveInput;
    private Vector2 moveInput;
    private Rigidbody rb;
    private RigidbodyConstraints _originalConstraints; // Dodge için orijinal Rigidbody kýsýtlamalarýný saklar

    private bool isAttacking = false;
    private bool isDodging = false;
    private bool isHoldingAttackButton = false;

    private float lastDodgeTime = -Mathf.Infinity;
    private float currentDodgeTimer = 0f;
    private float lastAttackActionTime = -Mathf.Infinity;

    private InputAction attackInputAction;
    private InputAction moveInputAction;

    private bool isRotatingForBowAim = false;
    private bool isResettingBowRotation = false;
    private Quaternion bowCharacterTargetVisualRotation;
    private Quaternion bowArrowActualTargetRotation;
    private Quaternion bowPostShotReturnRotation;

    private bool isRotatingForMagicAim = false;
    private bool isResettingMagicRotation = false;
    private Quaternion magicCharacterTargetVisualRotation;
    private Quaternion magicProjectileActualTargetRotation;
    private Quaternion magicPostCastReturnRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) { Debug.LogError("Player üzerinde Rigidbody component'i bulunamadý!", this); enabled = false; return; }
        _originalConstraints = rb.constraints; // Orijinal kýsýtlamalarý kaydet

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) { Debug.LogError("Player veya çocuklarýnda Animator component'i bulunamadý!", this); enabled = false; return; }

        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) { Debug.LogError("Player üzerinde PlayerInput component'i bulunamadý!", this); enabled = false; return; }
        else
        {
            attackInputAction = playerInput.actions["Attack"];
            moveInputAction = playerInput.actions["Move"];
            if (attackInputAction == null) Debug.LogError("PlayerInput actions içinde 'Attack' aksiyonu bulunamadý.");
            if (moveInputAction == null) Debug.LogError("PlayerInput actions içinde 'Move' aksiyonu bulunamadý.");
        }

        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogWarning("Player üzerinde HealthSystem bulunamadý! Can sistemi çalýþmayacak. Otomatik ekleniyor...", this);
            healthSystem = gameObject.AddComponent<HealthSystem>();
        }
        healthSystem.OnDeath += HandlePlayerDeath;

        if (mainCollider == null) mainCollider = GetComponentInChildren<Collider>(); // Ana collider'ý bulmaya çalýþ
        if (mainCollider == null) Debug.LogWarning("Ana Collider (Player) atanmamýþ. Dodge sýrasýnda geçirgenlik düzgün çalýþmayabilir.", this);

        if (aimIndicator != null) { aimIndicator.gameObject.SetActive(false); aimIndicator.positionCount = 2; }

        lastDodgeTime = -dodgeCooldown; // Oyun baþlar baþlamaz dodge yapabilmek için
        lastAttackActionTime = -attackCooldown; // Oyun baþlar baþlamaz saldýrý yapabilmek için

        UpdateHeldWeaponVisual(currentWeapon);
    }

    private void UpdateHeldWeaponVisual(WeaponType weaponToDisplay)
    {
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponInstance = null;
        }

        if (weaponHoldPoint == null)
        {
            if (weaponToDisplay != WeaponType.None) Debug.LogWarning("WeaponHoldPoint atanmamýþ. Silah görselleri gösterilemiyor.", this);
            return;
        }

        GameObject prefabToInstantiate = null;
        switch (weaponToDisplay)
        {
            case WeaponType.Sword: prefabToInstantiate = swordVisualPrefab; break;
            case WeaponType.Bow: prefabToInstantiate = bowVisualPrefab; break;
            case WeaponType.Magic: prefabToInstantiate = magicItemVisualPrefab; break;
        }

        if (prefabToInstantiate != null)
        {
            currentWeaponInstance = Instantiate(prefabToInstantiate, weaponHoldPoint);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;
        }
        else if (weaponToDisplay != WeaponType.None)
        {
            Debug.LogWarning($"{weaponToDisplay} için görsel prefab atanmamýþ.", this);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (isStunned || (healthSystem != null && healthSystem.CurrentHealth <= 0))
        {
            rawMoveInput = Vector2.zero;
            return;
        }
        rawMoveInput = context.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (currentWeapon == WeaponType.None) return;
        if (isDodging || isStunned || (healthSystem != null && healthSystem.CurrentHealth <= 0)) return;

        if (currentWeapon == WeaponType.Sword)
        {
            if (context.performed)
            {
                bool isPerformingOutwardSlash = animator.GetBool(SwordOutwardSlashAnimatorBool);
                if (isPerformingOutwardSlash && Time.time < lastAttackActionTime + attackCooldown * 0.7f) return;
                if (!isPerformingOutwardSlash && Time.time < lastAttackActionTime + attackCooldown) return;

                if (rawMoveInput.sqrMagnitude > 0.1f)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }

                isAttacking = true;
                animator.speed = 1f;

                bool lockedOn = false;
                if (enableSwordLockOn)
                {
                    Transform potentialTarget = FindSwordTarget();
                    if (potentialTarget != null)
                    {
                        Vector3 directionToTarget = (potentialTarget.position - transform.position);
                        directionToTarget.y = 0;
                        transform.rotation = Quaternion.LookRotation(directionToTarget.normalized);
                        rb.AddForce(directionToTarget.normalized * swordLockOnLungeForce, ForceMode.Impulse);
                        lockedOn = true;
                    }
                }
                if (!lockedOn && swordLungeForce > 0f)
                {
                    rb.AddForce(transform.forward * swordLungeForce, ForceMode.Impulse);
                }

                if (animator.GetBool(SwordOutwardSlashAnimatorBool)) animator.SetTrigger(SwordInwardSlashAnimatorTrigger);
                else animator.SetBool(SwordOutwardSlashAnimatorBool, true);

                lastAttackActionTime = Time.time;
            }
        }
        else if (currentWeapon == WeaponType.Bow)
        {
            bool canBypassCooldownForRelease = isHoldingAttackButton && animator.GetBool(IsAimingBowAnimatorBool);
            if (!canBypassCooldownForRelease && Time.time < lastAttackActionTime + attackCooldown) return;

            if (context.started)
            {
                isHoldingAttackButton = true;
                if (!animator.GetBool(IsAimingBowAnimatorBool) && !isAttacking) StartBowAttack();
            }
            else if (context.canceled && isHoldingAttackButton)
            {
                isHoldingAttackButton = false;
                if (animator.GetBool(IsAimingBowAnimatorBool)) ReleaseBowAttack();
            }
        }
        else if (currentWeapon == WeaponType.Magic)
        {
            bool canBypassCooldownForRelease = isHoldingAttackButton && animator.GetBool(IsAimingMagicAnimatorBool);
            if (!canBypassCooldownForRelease && Time.time < lastAttackActionTime + attackCooldown) return;

            if (context.started)
            {
                isHoldingAttackButton = true;
                if (!animator.GetBool(IsAimingMagicAnimatorBool) && !isAttacking) StartMagicAttack();
            }
            else if (context.canceled && isHoldingAttackButton)
            {
                isHoldingAttackButton = false;
                if (animator.GetBool(IsAimingMagicAnimatorBool)) ReleaseMagicAttack();
            }
        }
    }

    public WeaponType GetCurrentWeaponType() { return currentWeapon; }

    public void EquipNewAttackAbility(WeaponType typeToEquip, GameObject primaryPrefab, GameObject secondaryPrefab, float newDamage, float newCooldown)
    {
        CancelCurrentActionsOnWeaponSwitch();
        currentWeapon = typeToEquip;
        attackCooldown = newCooldown;
        outwardSlashEffectPrefab = null;
        inwardSlashEffectPrefab = null;
        arrowPrefab = null;
        magicProjectilePrefab = null;

        switch (typeToEquip)
        {
            case WeaponType.Sword:
                outwardSlashEffectPrefab = primaryPrefab;
                inwardSlashEffectPrefab = secondaryPrefab;
                swordDamage = newDamage;
                break;
            case WeaponType.Bow:
                arrowPrefab = primaryPrefab;
                bowDamage = newDamage;
                break;
            case WeaponType.Magic:
                magicProjectilePrefab = primaryPrefab;
                magicDamage = newDamage;
                break;
            case WeaponType.None:
                swordDamage = 0; bowDamage = 0; magicDamage = 0;
                break;
        }
        lastAttackActionTime = Time.time - attackCooldown;
        UpdateHeldWeaponVisual(currentWeapon);
    }

    private Transform FindSwordTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, swordLockOnRange, enemyLayerMask);
        Transform bestTarget = null;
        float minAngle = swordLockOnAngle / 2f;
        float closestDistanceSqrToBestTargetInAngle = float.MaxValue;
        Vector3 forward = transform.forward;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform == transform || !hitCollider.CompareTag("Enemy")) continue;
            Vector3 directionToHit = (hitCollider.transform.position - transform.position);
            directionToHit.y = 0;
            float angleToTarget = Vector3.Angle(forward, directionToHit.normalized);
            float distanceSqr = directionToHit.sqrMagnitude;
            if (angleToTarget < minAngle && distanceSqr < closestDistanceSqrToBestTargetInAngle)
            {
                closestDistanceSqrToBestTargetInAngle = distanceSqr;
                bestTarget = hitCollider.transform;
            }
        }
        return bestTarget;
    }

    public void AnimationEvent_DealSwordDamage()
    {
        if (healthSystem != null && healthSystem.CurrentHealth <= 0) return;
        Vector3 hitboxCenter = transform.TransformPoint(swordHitboxOffset);
        Collider[] hitEnemies = Physics.OverlapSphere(hitboxCenter, swordHitboxRadius, enemyLayerMask);
        foreach (Collider enemyCollider in hitEnemies)
        {
            if (enemyCollider.CompareTag("Enemy"))
            {
                HealthSystem enemyHealth = enemyCollider.GetComponent<HealthSystem>();
                if (enemyHealth != null)
                {
                    Vector3 damageDirection = (enemyCollider.transform.position - transform.position).normalized;
                    if (damageDirection == Vector3.zero) damageDirection = transform.forward;
                    DamageInfo damageInfo = new DamageInfo(swordDamage, damageDirection, gameObject, enemyCollider.ClosestPoint(hitboxCenter), true);
                    enemyHealth.TakeDamage(damageInfo);
                }
            }
        }
    }

    public void TriggerOutwardSlashEffect()
    {
        if (outwardSlashEffectPrefab != null)
        {
            Transform spawnOrigin = outwardSlashEffectSpawnPoint != null ? outwardSlashEffectSpawnPoint : transform;
            Vector3 spawnPosition = spawnOrigin.position + (spawnOrigin.rotation * outwardSlashEffectOffset);
            GameObject effect = Instantiate(outwardSlashEffectPrefab, spawnPosition, transform.rotation);
            if (swordEffectDuration > 0) Destroy(effect, swordEffectDuration);
        }
    }

    public void TriggerInwardSlashEffect()
    {
        if (inwardSlashEffectPrefab != null)
        {
            Transform spawnOrigin = inwardSlashEffectSpawnPoint != null ? inwardSlashEffectSpawnPoint : transform;
            Vector3 spawnPosition = spawnOrigin.position + (spawnOrigin.rotation * inwardSlashEffectOffset);
            GameObject effect = Instantiate(inwardSlashEffectPrefab, spawnPosition, transform.rotation);
            if (swordEffectDuration > 0) Destroy(effect, swordEffectDuration);
        }
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed && CanDodge())
        {
            StartDodge();
        }
    }

    private bool CanDodge()
    {
        return !isDodging &&
               !isStunned &&
               (healthSystem == null || healthSystem.CurrentHealth > 0) &&
               Time.time >= lastDodgeTime + dodgeCooldown;
    }

    private void StartDodge()
    {
        if (isAttacking || animator.GetBool(IsAimingBowAnimatorBool) || animator.GetBool(IsAimingMagicAnimatorBool))
        {
            isAttacking = false;
            isHoldingAttackButton = false;
            animator.speed = 1f;
            animator.SetBool(SwordOutwardSlashAnimatorBool, false);
            if (currentWeapon == WeaponType.Bow && animator.GetBool(IsAimingBowAnimatorBool)) CancelBowAim();
            if (currentWeapon == WeaponType.Magic && animator.GetBool(IsAimingMagicAnimatorBool)) CancelMagicAim();
            Debug.Log("Dodge, mevcut saldýrýyý/niþan almayý iptal etti.");
        }

        isDodging = true;
        currentDodgeTimer = 0f;
        animator.SetTrigger(DodgeAnimatorTrigger);
        lastDodgeTime = Time.time;

        if (mainCollider != null) mainCollider.isTrigger = true;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        Vector3 dodgeDirection = transform.forward;
        if (rawMoveInput.sqrMagnitude > 0.1f)
        {
            dodgeDirection = new Vector3(rawMoveInput.x, 0, rawMoveInput.y).normalized;
        }
        transform.rotation = Quaternion.LookRotation(dodgeDirection);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dodgeDirection * dodgeForce, ForceMode.VelocityChange);
    }

    private void StartBowAttack()
    {
        if (rawMoveInput.sqrMagnitude > 0.1f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
        isAttacking = true;
        isRotatingForBowAim = true;
        isResettingBowRotation = false;
        animator.speed = 1f;
        animator.SetBool(IsAimingBowAnimatorBool, true);
        animator.SetTrigger(BowAttackAnimatorTrigger);
        if (aimIndicator != null) aimIndicator.gameObject.SetActive(true);
        lastAttackActionTime = Time.time;
    }

    private void UpdateBowAimAndAnimation()
    {
        if (!animator.GetBool(IsAimingBowAnimatorBool))
        {
            isRotatingForBowAim = false; return;
        }

        Plane groundPlane = new Plane(Vector3.up, transform.position);
        Ray cameraRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (groundPlane.Raycast(cameraRay, out float rayDistance))
        {
            Vector3 targetPoint = cameraRay.GetPoint(rayDistance);
            Vector3 directionToTarget = (targetPoint - transform.position);
            directionToTarget.y = 0;
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                bowArrowActualTargetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                if (isRotatingForBowAim)
                {
                    bowCharacterTargetVisualRotation = bowArrowActualTargetRotation * Quaternion.Euler(0, bowAimRotationOffset, 0);
                }
            }
        }

        if (aimIndicator != null && aimIndicator.gameObject.activeSelf && arrowSpawnPoint != null)
        {
            aimIndicator.SetPosition(0, arrowSpawnPoint.position);
            aimIndicator.SetPosition(1, arrowSpawnPoint.position + (bowArrowActualTargetRotation * Vector3.forward) * aimIndicatorRange);
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag(BowDrawAimAnimationTag))
        {
            if (stateInfo.normalizedTime >= bowChargeToHoldAnimTime && animator.speed != 0f)
            {
                animator.speed = 0f;
            }
        }
    }

    private void HandleBowCharacterRotation()
    {
        if (isRotatingForBowAim && animator.GetBool(IsAimingBowAnimatorBool))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, bowCharacterTargetVisualRotation, bowCharacterRotationSpeed * Time.deltaTime);
        }
        else if (isResettingBowRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, bowPostShotReturnRotation, bowCharacterRotationSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, bowPostShotReturnRotation) < 1.0f) isResettingBowRotation = false;
        }
    }

    private void CancelBowAim()
    {
        if (animator.GetBool(IsAimingBowAnimatorBool))
        {
            isHoldingAttackButton = false;
            animator.speed = 1f;
            animator.SetBool(IsAimingBowAnimatorBool, false);
            animator.ResetTrigger(BowAttackAnimatorTrigger);
            if (aimIndicator != null) aimIndicator.gameObject.SetActive(false);
            isRotatingForBowAim = false;
            isResettingBowRotation = false;
            isAttacking = false;
        }
    }

    private void ReleaseBowAttack()
    {
        animator.speed = 1f;
        FireArrow();
        if (aimIndicator != null) aimIndicator.gameObject.SetActive(false);
        bowPostShotReturnRotation = bowArrowActualTargetRotation;
        isResettingBowRotation = true;
        isRotatingForBowAim = false;
        lastAttackActionTime = Time.time;
        animator.SetBool(IsAimingBowAnimatorBool, false);
    }

    private void FireArrow()
    {
        if (arrowPrefab != null && arrowSpawnPoint != null)
        {
            GameObject arrowGO = Instantiate(arrowPrefab, arrowSpawnPoint.position, bowArrowActualTargetRotation);
            Projectile projectileScript = arrowGO.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(arrowSpeed, bowArrowActualTargetRotation * Vector3.forward, gameObject);
                projectileScript.damage = bowDamage;
                projectileScript.causesHitStopOnImpact = true;
            }
            else
            {
                Rigidbody arrowRb = arrowGO.GetComponent<Rigidbody>();
                if (arrowRb != null) arrowRb.linearVelocity = (bowArrowActualTargetRotation * Vector3.forward) * arrowSpeed;
            }
        }
    }

    private void StartMagicAttack()
    {
        if (rawMoveInput.sqrMagnitude > 0.1f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
        isAttacking = true;
        isRotatingForMagicAim = true;
        isResettingMagicRotation = false;
        animator.speed = 1f;
        animator.SetBool(IsAimingMagicAnimatorBool, true);
        animator.SetTrigger(MagicChargeAnimatorTrigger);
        if (aimIndicator != null && useAimIndicatorForMagic) aimIndicator.gameObject.SetActive(true);
        lastAttackActionTime = Time.time;
    }

    private void UpdateMagicAimAndAnimation()
    {
        if (!animator.GetBool(IsAimingMagicAnimatorBool))
        {
            isRotatingForMagicAim = false; return;
        }

        Plane groundPlane = new Plane(Vector3.up, transform.position);
        Ray cameraRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (groundPlane.Raycast(cameraRay, out float rayDistance))
        {
            Vector3 targetPoint = cameraRay.GetPoint(rayDistance);
            Vector3 directionToTarget = (targetPoint - transform.position);
            directionToTarget.y = 0;
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                magicProjectileActualTargetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                if (isRotatingForMagicAim)
                {
                    magicCharacterTargetVisualRotation = magicProjectileActualTargetRotation * Quaternion.Euler(0, magicAimRotationOffset, 0);
                }
            }
        }

        if (aimIndicator != null && useAimIndicatorForMagic && aimIndicator.gameObject.activeSelf && magicSpawnPoint != null)
        {
            aimIndicator.SetPosition(0, magicSpawnPoint.position);
            aimIndicator.SetPosition(1, magicSpawnPoint.position + (magicProjectileActualTargetRotation * Vector3.forward) * aimIndicatorRange);
        }
        else if (aimIndicator != null && useAimIndicatorForMagic && magicSpawnPoint == null)
        {
            aimIndicator.gameObject.SetActive(false);
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsTag(MagicChargeHoldAnimationTag))
        {
            if (stateInfo.normalizedTime >= magicChargeToHoldAnimTime && animator.speed != 0f)
            {
                animator.speed = 0f;
            }
        }
    }

    private void HandleMagicCharacterRotation()
    {
        if (isRotatingForMagicAim && animator.GetBool(IsAimingMagicAnimatorBool))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, magicCharacterTargetVisualRotation, magicCharacterRotationSpeed * Time.deltaTime);
        }
        else if (isResettingMagicRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, magicPostCastReturnRotation, magicCharacterRotationSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, magicPostCastReturnRotation) < 1.0f) isResettingMagicRotation = false;
        }
    }

    private void CancelMagicAim()
    {
        if (animator.GetBool(IsAimingMagicAnimatorBool))
        {
            isHoldingAttackButton = false;
            animator.speed = 1f;
            animator.SetBool(IsAimingMagicAnimatorBool, false);
            animator.ResetTrigger(MagicChargeAnimatorTrigger);
            if (aimIndicator != null && useAimIndicatorForMagic) aimIndicator.gameObject.SetActive(false);
            isRotatingForMagicAim = false;
            isResettingMagicRotation = false;
            isAttacking = false;
        }
    }

    private void ReleaseMagicAttack()
    {
        animator.speed = 1f;
        FireMagicProjectile();
        if (aimIndicator != null && useAimIndicatorForMagic) aimIndicator.gameObject.SetActive(false);
        magicPostCastReturnRotation = magicProjectileActualTargetRotation;
        isResettingMagicRotation = true;
        isRotatingForMagicAim = false;
        lastAttackActionTime = Time.time;
        animator.SetBool(IsAimingMagicAnimatorBool, false);
    }

    private void FireMagicProjectile()
    {
        if (magicProjectilePrefab != null && magicSpawnPoint != null)
        {
            GameObject projectileGO = Instantiate(magicProjectilePrefab, magicSpawnPoint.position, magicProjectileActualTargetRotation);
            Projectile projectileScript = projectileGO.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(magicProjectileSpeed, magicProjectileActualTargetRotation * Vector3.forward, gameObject);
                projectileScript.damage = magicDamage;
                projectileScript.causesHitStopOnImpact = true;
            }
            else
            {
                Rigidbody projectileRb = projectileGO.GetComponent<Rigidbody>();
                if (projectileRb != null) projectileRb.linearVelocity = (magicProjectileActualTargetRotation * Vector3.forward) * magicProjectileSpeed;
            }
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (isStunned || isDodging || (healthSystem != null && healthSystem.CurrentHealth <= 0)) return;

        if (isAttacking || animator.GetBool(IsAimingBowAnimatorBool) || animator.GetBool(IsAimingMagicAnimatorBool))
        {
            isAttacking = false; isHoldingAttackButton = false; animator.speed = 1f;
            animator.SetBool(SwordOutwardSlashAnimatorBool, false);
            if (currentWeapon == WeaponType.Bow && animator.GetBool(IsAimingBowAnimatorBool)) CancelBowAim();
            if (currentWeapon == WeaponType.Magic && animator.GetBool(IsAimingMagicAnimatorBool)) CancelMagicAim();
        }

        if (_knockbackCoroutine != null) StopCoroutine(_knockbackCoroutine);
        _knockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, force, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        isStunned = true;
        rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        yield return new WaitForSeconds(duration);
        isStunned = false;
        _knockbackCoroutine = null;
    }

    private void HandlePlayerDeath()
    {
        if (animator != null) animator.SetTrigger("Die");
        isStunned = true; isAttacking = false;
        moveInput = Vector2.zero; rawMoveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector3.zero;
        if (currentWeaponInstance != null) currentWeaponInstance.SetActive(false);
        if (aimIndicator != null) aimIndicator.gameObject.SetActive(false);
        StartCoroutine(LoadMainMenuAfterDelay());
    }

    private IEnumerator LoadMainMenuAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeLoadMainMenuOnDeath);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void Update()
    {
        if (animator == null || playerInput == null || (healthSystem != null && healthSystem.CurrentHealth <= 0))
        {
            if ((healthSystem != null && healthSystem.CurrentHealth <= 0) && rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        UpdateMoveInputBasedOnState();

        if (!isStunned && !isDodging)
        {
            if (currentWeapon == WeaponType.Bow && animator.GetBool(IsAimingBowAnimatorBool)) UpdateBowAimAndAnimation();
            else if (currentWeapon == WeaponType.Magic && animator.GetBool(IsAimingMagicAnimatorBool)) UpdateMagicAimAndAnimation();

            if (currentWeapon == WeaponType.Bow) HandleBowCharacterRotation();
            else if (currentWeapon == WeaponType.Magic) HandleMagicCharacterRotation();
        }

        UpdateAnimationStateFlags();
        HandleWalkingAnimation();
    }

    private void SwitchToNextWeapon()
    {
        CancelCurrentActionsOnWeaponSwitch();
        int currentWeaponIndex = (int)currentWeapon;
        currentWeaponIndex++;
        if (currentWeaponIndex >= Enum.GetValues(typeof(WeaponType)).Length) currentWeaponIndex = 0;
        currentWeapon = (WeaponType)currentWeaponIndex;
        lastAttackActionTime = Time.time - attackCooldown;
        UpdateHeldWeaponVisual(currentWeapon);
    }

    private void CancelCurrentActionsOnWeaponSwitch()
    {
        isAttacking = false; isHoldingAttackButton = false; animator.speed = 1f;
        animator.SetBool(SwordOutwardSlashAnimatorBool, false);
        if (currentWeapon == WeaponType.Bow && animator.GetBool(IsAimingBowAnimatorBool)) CancelBowAim();
        if (currentWeapon == WeaponType.Magic && animator.GetBool(IsAimingMagicAnimatorBool)) CancelMagicAim();
        if (aimIndicator != null && aimIndicator.gameObject.activeSelf) aimIndicator.gameObject.SetActive(false);
    }

    private void UpdateMoveInputBasedOnState()
    {
        bool canProcessInput = !isStunned && (healthSystem == null || healthSystem.CurrentHealth > 0);
        if (!canProcessInput) { moveInput = Vector2.zero; return; }

        bool isAimingOrAttackingForMovementStop =
            (currentWeapon == WeaponType.Bow && animator.GetBool(IsAimingBowAnimatorBool)) ||
            (currentWeapon == WeaponType.Magic && animator.GetBool(IsAimingMagicAnimatorBool)) ||
            (isAttacking && currentWeapon == WeaponType.Sword);

        if (isDodging || isStunned)
        {
            moveInput = rawMoveInput;
        }
        else if (isAimingOrAttackingForMovementStop)
        {
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = rawMoveInput;
        }
    }

    private void UpdateAnimationStateFlags()
    {
        if (isStunned || (healthSystem != null && healthSystem.CurrentHealth <= 0))
        {
            isAttacking = false;
            return;
        }

        AnimatorStateInfo currentAnimatorState = animator.GetCurrentAnimatorStateInfo(0);
        bool isCurrentlyConsideredAttackingByAnimator = false;

        if (isDodging)
        {
            isAttacking = false;
            animator.SetBool(SwordOutwardSlashAnimatorBool, false);
            animator.SetBool(IsAimingBowAnimatorBool, false);
            animator.SetBool(IsAimingMagicAnimatorBool, false);
            if (aimIndicator != null) aimIndicator.gameObject.SetActive(false);
            return;
        }

        switch (currentWeapon)
        {
            case WeaponType.Sword:
                isCurrentlyConsideredAttackingByAnimator = animator.GetBool(SwordOutwardSlashAnimatorBool) ||
                                                           (isAttacking && currentAnimatorState.IsTag(AttackAnimationTag) && !currentAnimatorState.IsName("Idle"));
                break;
            case WeaponType.Bow:
                isCurrentlyConsideredAttackingByAnimator = animator.GetBool(IsAimingBowAnimatorBool) ||
                                                           (isAttacking && currentAnimatorState.IsTag(AttackAnimationTag) && !currentAnimatorState.IsName("Idle_Bow"));
                break;
            case WeaponType.Magic:
                isCurrentlyConsideredAttackingByAnimator = animator.GetBool(IsAimingMagicAnimatorBool) ||
                                                           (isAttacking && currentAnimatorState.IsTag(AttackAnimationTag) && !currentAnimatorState.IsName("Idle_Magic"));
                break;
        }

        if (isCurrentlyConsideredAttackingByAnimator)
        {
            if (!isAttacking) isAttacking = true;
        }
        else
        {
            if (isAttacking)
            {
                isAttacking = false;
                animator.speed = 1f;
            }
        }
    }

    private void HandleWalkingAnimation()
    {
        if (isStunned || (healthSystem != null && healthSystem.CurrentHealth <= 0))
        {
            if (animator.GetBool(WalkAnimatorBool)) animator.SetBool(WalkAnimatorBool, false);
            return;
        }

        bool shouldPreventWalkAnim = isDodging || isAttacking ||
                                     (currentWeapon == WeaponType.Bow && animator.GetBool(IsAimingBowAnimatorBool)) ||
                                     (currentWeapon == WeaponType.Magic && animator.GetBool(IsAimingMagicAnimatorBool));

        if (shouldPreventWalkAnim)
        {
            if (animator.GetBool(WalkAnimatorBool)) animator.SetBool(WalkAnimatorBool, false);
        }
        else
        {
            bool isMoving = moveInput.magnitude > 0.1f;
            if (animator.GetBool(WalkAnimatorBool) != isMoving) animator.SetBool(WalkAnimatorBool, isMoving);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || (healthSystem != null && healthSystem.CurrentHealth <= 0)) return;

        if (isDodging)
        {
            currentDodgeTimer += Time.fixedDeltaTime;
            if (currentDodgeTimer >= dodgeDuration)
            {
                isDodging = false;
                if (mainCollider != null) mainCollider.isTrigger = false;
                rb.constraints = _originalConstraints;
            }
            return;
        }

        if (isStunned) return;

        bool isPerformingActionThatPreventsMovement =
            isAttacking ||
            (currentWeapon == WeaponType.Bow && animator.GetBool(IsAimingBowAnimatorBool)) ||
            (currentWeapon == WeaponType.Magic && animator.GetBool(IsAimingMagicAnimatorBool));

        if (!isPerformingActionThatPreventsMovement)
        {
            MovePlayer();
        }
        // Hareket etmiyorsa veya saldýrý/niþan alýyorsa, MovePlayer() zaten yatay hýzý sýfýrlar veya hiç çaðýrmaz.
        // Dolayýsýyla burada ek bir rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); çaðýrmaya gerek yok.
    }

    public void MovePlayer()
    {
        if (moveInput.magnitude > 0.01f)
        {
            Vector3 movementDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            Vector3 targetVelocity = movementDirection * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

            if (!((currentWeapon == WeaponType.Bow && (isRotatingForBowAim || isResettingBowRotation || animator.GetBool(IsAimingBowAnimatorBool))) ||
                  (currentWeapon == WeaponType.Magic && (isRotatingForMagicAim || isResettingMagicRotation || animator.GetBool(IsAimingMagicAnimatorBool)))))
            {
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (!((currentWeapon == WeaponType.Bow && (isRotatingForBowAim || isResettingBowRotation || animator.GetBool(IsAimingBowAnimatorBool))) ||
                 (currentWeapon == WeaponType.Magic && (isRotatingForMagicAim || isResettingMagicRotation || animator.GetBool(IsAimingMagicAnimatorBool)))))
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); // Hareketsizken yatay hýzý sýfýrla
            }
        }
    }

    // YENÝ EKLENEN STAT DEÐÝÞTÝRME FONKSÝYONLARI
    public void ModifyMoveSpeed(float amount, bool isPercentage)
    {
        if (isPercentage)
        {
            moveSpeed *= (1f + amount / 100f);
        }
        else
        {
            moveSpeed += amount;
        }
        moveSpeed = Mathf.Max(0.1f, moveSpeed); // Negatif veya çok düþük hýzý engelle
        Debug.Log($"Hareket Hýzý Deðiþtirildi. Yeni Hýz: {moveSpeed}");
    }

    public void ModifyDodgeCooldown(float amount) // amount zaten negatif veya pozitif gelebilir
    {
        dodgeCooldown += amount;
        dodgeCooldown = Mathf.Max(0.1f, dodgeCooldown); // Cooldown'un çok düþmesini engelle
        Debug.Log($"Dodge Cooldown Deðiþtirildi. Yeni Cooldown: {dodgeCooldown}");
    }

    public void ModifyWeaponDamage(WeaponType weapon, float amount, bool isPercentage)
    {
        switch (weapon)
        {
            case WeaponType.Sword:
                if (isPercentage) swordDamage *= (1f + amount / 100f);
                else swordDamage += amount;
                swordDamage = Mathf.Max(1f, swordDamage); // Minimum hasar
                Debug.Log($"Kýlýç Hasarý Deðiþtirildi. Yeni Hasar: {swordDamage}");
                break;
            case WeaponType.Bow:
                if (isPercentage) bowDamage *= (1f + amount / 100f);
                else bowDamage += amount;
                bowDamage = Mathf.Max(1f, bowDamage);
                Debug.Log($"Yay Hasarý Deðiþtirildi. Yeni Hasar: {bowDamage}");
                break;
            case WeaponType.Magic:
                if (isPercentage) magicDamage *= (1f + amount / 100f);
                else magicDamage += amount;
                magicDamage = Mathf.Max(1f, magicDamage);
                Debug.Log($"Büyü Hasarý Deðiþtirildi. Yeni Hasar: {magicDamage}");
                break;
        }
    }
    public void SetSwordOutwardSlashPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            outwardSlashEffectPrefab = prefab;
            Debug.Log($"Outward Slash Prefab changed to: {prefab.name}");
        }
    }
    public void SetSwordInwardSlashPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            inwardSlashEffectPrefab = prefab;
            Debug.Log($"Inward Slash Prefab changed to: {prefab.name}");
        }
    }
    public void SetSwordSkillSetPrefabs(GameObject outwardPrefab, GameObject inwardPrefab)
    {
        bool changed = false;
        if (outwardPrefab != null)
        {
            outwardSlashEffectPrefab = outwardPrefab;
            Debug.Log($"Outward Slash Prefab kýlýç setiyle deðiþti: {outwardPrefab.name}");
            changed = true;
        }
        if (inwardPrefab != null)
        {
            inwardSlashEffectPrefab = inwardPrefab;
            Debug.Log($"Inward Slash Prefab kýlýç setiyle deðiþti: {inwardPrefab.name}");
            changed = true;
        }
        if (!changed)
        {
            Debug.LogWarning("SetSwordSkillSetPrefabs çaðrýldý ancak atanacak geçerli prefab yoktu.");
        }
    }

    public GameObject GetCurrentOutwardSlashPrefab() { return outwardSlashEffectPrefab; }
    public GameObject GetCurrentInwardSlashPrefab() { return inwardSlashEffectPrefab; }

    public void SetArrowPrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            arrowPrefab = prefab;
            Debug.Log($"Arrow Prefab changed to: {prefab.name}");
        }
    }
    public void SetMagicProjectilePrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            magicProjectilePrefab = prefab;
            Debug.Log($"Magic Projectile Prefab changed to: {prefab.name}");
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (currentWeapon == WeaponType.Sword)
        {
            Gizmos.color = Color.red;
            Vector3 worldHitboxCenter = transform.TransformPoint(swordHitboxOffset);
            Gizmos.DrawWireSphere(worldHitboxCenter, swordHitboxRadius);
        }

        if (enableSwordLockOn)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, swordLockOnRange);
            Vector3 forward = transform.forward;
            Vector3 leftRayDirection = Quaternion.AngleAxis(-swordLockOnAngle / 2, Vector3.up) * forward;
            Vector3 rightRayDirection = Quaternion.AngleAxis(swordLockOnAngle / 2, Vector3.up) * forward;
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.25f);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, leftRayDirection * swordLockOnRange);
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, rightRayDirection * swordLockOnRange);
        }
    }
}
