// UpgradePresenter.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UpgradePresenter : MonoBehaviour
{
    public static UpgradePresenter Instance { get; private set; }

    [Header("Upgrade Havuzu ve Efektler")]
    public List<UpgradeData> upgradePool;
    [SerializeField] private GameObject playerUpgradeStartEffectPrefab;
    [SerializeField] private Transform playerEffectSpawnPointOverride;
    [SerializeField] private float uiDelayAfterEffect = 1.0f; // Efekt varsa UI gecikmesi

    [Header("Referanslar")]
    [SerializeField] private UpgradeUIManager uiManager;
    private TopDownController playerController;
    private HealthSystem playerHealthSystem;

    private bool isUpgradeProcessActive = false;
    private bool initialWeaponOffered = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UpgradeUIManager>();
            if (uiManager == null) Debug.LogError("[UP] Awake: UpgradeUIManager ATANMAMIÞ/BULUNAMADI!", this);
        }
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<TopDownController>();
            playerHealthSystem = playerObj.GetComponent<HealthSystem>();
        }

        if (playerController == null || playerHealthSystem == null)
        {
            Debug.LogError("[UP] Start: Oyuncu veya HealthSystem BULUNAMADI! Script devre dýþý býrakýlýyor.", this);
            enabled = false; return;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnUpgradeThresholdReached += HandleScoreBasedUpgradeTrigger;
        }
        else Debug.LogError("[UP] Start: ScoreManager BULUNAMADI!", this);

        if (!initialWeaponOffered && playerController.GetCurrentWeaponType() == WeaponType.None)
        {
            if (!isUpgradeProcessActive)
            {
                Debug.Log("[UP] Start: Oyun baþýnda ilk silah upgrade'i tetikleniyor.");
                TriggerUpgradePresentation(true);
            }
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnUpgradeThresholdReached -= HandleScoreBasedUpgradeTrigger;
        }
    }
    // UpgradePresenter.cs
    public bool IsUpgradeProcessCurrentlyActive()
    {
        return isUpgradeProcessActive;
    }

    private void HandleScoreBasedUpgradeTrigger()
    {
        Debug.Log($"[UP] HandleScoreBasedUpgradeTrigger: Çaðrýldý. Mevcut isUpgradeProcessActive = {isUpgradeProcessActive}");
        bool isItEffectivelyAnInitialOffer = !initialWeaponOffered && playerController.GetCurrentWeaponType() == WeaponType.None;
        TriggerUpgradePresentation(isItEffectivelyAnInitialOffer);
    }

    public void TriggerUpgradePresentation(bool isThisAnInitialWeaponOffer)
    {
        Debug.Log($"[UP] TriggerUpgradePresentation GÝRÝÞ. isInitialOffer: {isThisAnInitialWeaponOffer}, Mevcut isUpgradeProcessActive: {isUpgradeProcessActive}");
        if (isUpgradeProcessActive)
        {
            Debug.LogError($"[UP] TriggerUpgradePresentation: ENGEL! isUpgradeProcessActive ZATEN TRUE ({isUpgradeProcessActive}).");
            return;
        }
        if (playerController == null || uiManager == null) // playerHealthSystem null olabilir (bazý upgradeler için)
        {
            Debug.LogError("[UP] TriggerUpgradePresentation: KRÝTÝK HATA! PlayerController veya UIManager eksik.");
            return;
        }

        if (TimeStopper.Instance != null) // Önce TimeStopper'ý iptal et
        {
            TimeStopper.Instance.CancelCurrentStopTime();
        }

        isUpgradeProcessActive = true;
        Time.timeScale = 0f;
        Debug.LogError($"[UP_STATE] OYUN DURAKLATILDI (TriggerUpgradePresentation). TimeScale: {Time.timeScale}, isUpgradeActive: {isUpgradeProcessActive}");

        float delayForCoroutine = uiDelayAfterEffect;
        bool effectPlayed = false;

        if (!isThisAnInitialWeaponOffer && playerUpgradeStartEffectPrefab != null)
        {
            Instantiate(playerUpgradeStartEffectPrefab, playerController.transform.position, Quaternion.identity, playerController.transform);
            effectPlayed = true;
        }

        if (!effectPlayed || isThisAnInitialWeaponOffer)
        {
            delayForCoroutine = 0.05f;
        }
        StartCoroutine(ShowUpgradeUICoroutine(isThisAnInitialWeaponOffer, delayForCoroutine));
    }

    private IEnumerator ShowUpgradeUICoroutine(bool isThisAnInitialWeaponOfferContext, float delayBeforeShowUI)
    {
        Debug.Log($"[UP] ShowUpgradeUICoroutine: Baþladý. {delayBeforeShowUI}s beklenecek. (TimeScale: {Time.timeScale})");
        if (delayBeforeShowUI > 0) yield return new WaitForSecondsRealtime(delayBeforeShowUI);

        Debug.Log("[UP] ShowUpgradeUICoroutine: Adaylar seçiliyor.");
        List<UpgradeData> candidatesForSelection;
        if (isThisAnInitialWeaponOfferContext || playerController.GetCurrentWeaponType() == WeaponType.None)
        {
            candidatesForSelection = upgradePool.OfType<GrantAttackAbilityUpgradeData>().Where(u => u != null && u.IsAvailable(playerController, playerHealthSystem)).Cast<UpgradeData>().ToList();
            if (candidatesForSelection.Count == 0)
            {
                Debug.LogError("[UP] ShowUpgradeUICoroutine: KRÝTÝK! Silah grant adayý yok! Süreç sonlandýrýlýyor.");
                HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // Hata durumunda süreci sonlandýr
                yield break;
            }
        }
        else
        {
            candidatesForSelection = upgradePool.Where(u => u != null && u.IsAvailable(playerController, playerHealthSystem)).ToList();
            if (candidatesForSelection.Count == 0)
            {
                Debug.LogWarning("[UP] ShowUpgradeUICoroutine: Uygun genel upgrade adayý yok. Süreç sonlandýrýlýyor.");
                HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // Hata durumunda süreci sonlandýr
                yield break;
            }
        }

        List<UpgradeData> weightedList = new List<UpgradeData>();
        foreach (var upgrade in candidatesForSelection) { for (int i = 0; i < Mathf.Max(1, upgrade.weight); i++) weightedList.Add(upgrade); }
        if (weightedList.Count == 0)
        {
            Debug.LogWarning("[UP] ShowUpgradeUICoroutine: Aðýrlýklý liste boþ. Süreç sonlandýrýlýyor.");
            HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // Hata durumunda süreci sonlandýr
            yield break;
        }

        System.Random rng = new System.Random();
        UpgradeData option1 = weightedList[rng.Next(weightedList.Count)];
        UpgradeData option2 = null;
        if (weightedList.Count > 1) { List<UpgradeData> distinctSecondOptions = weightedList.Where(u => u != option1).ToList(); if (distinctSecondOptions.Count > 0) option2 = distinctSecondOptions[rng.Next(distinctSecondOptions.Count)]; }

        if (uiManager != null)
        {
            Debug.Log($"[UP] ShowUpgradeUICoroutine: uiManager.DisplayUpgradeChoices çaðrýlýyor. Context: {isThisAnInitialWeaponOfferContext}");
            uiManager.DisplayUpgradeChoices(option1, option2, this, playerController, playerHealthSystem, isThisAnInitialWeaponOfferContext);
        }
        else
        {
            Debug.LogError("[UP] ShowUpgradeUICoroutine: UIManager null! Süreç sonlandýrýlýyor.");
            HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // UI Manager yoksa süreci sonlandýr
        }
    }

    /// <summary>
    /// UpgradeUIManager'dan bir seçim yapýldýðýnda (veya UI'ýn kapatýlmasý gerektiðinde) çaðrýlýr.
    /// </summary>
    public void HandleUpgradeChoiceFromUI(UpgradeData chosenUpgrade, bool wasThisChoiceFromAnInitialOfferContext)
    {
        Debug.Log($"[UP] HandleUpgradeChoiceFromUI BAÞLADI. Seçilen: {chosenUpgrade?.upgradeName ?? "NULL"}, BaþlangýçMý: {wasThisChoiceFromAnInitialOfferContext}. Metoda girerken isUpgradeProcessActive = {isUpgradeProcessActive}");

        if (uiManager != null)
        {
            uiManager.HidePanel(); // UI Manager'a panelini gizlemesini söyle
        }

        if (!isUpgradeProcessActive && Time.timeScale == 1f)
        {
            // Eðer süreç zaten aktif deðilse ve oyun normal hýzdaysa, bu beklenmedik bir çaðrý olabilir.
            // Veya Presenter bir þekilde devre dýþý kalmýþ ama UI hala callback yapmaya çalýþýyor.
            Debug.LogWarning($"[UP] HandleUpgradeChoiceFromUI: Çaðrýldý ancak isUpgradeProcessActive ZATEN false idi ve Time.timeScale = 1. Geri dönülüyor.");
            return; // Fazladan iþlem yapma
        }
        // Eðer isUpgradeProcessActive false ama Time.timeScale 0 ise, bir tutarsýzlýk var demektir, yine de düzeltmeye çalýþalým.

        if (chosenUpgrade != null && playerController != null) // playerHealthSystem opsiyonel olabilir
        {
            chosenUpgrade.ApplyUpgrade(playerController, playerHealthSystem);
            // Debug.Log($"[UP] HandleUpgradeChoiceFromUI: Upgrade uygulandý: {chosenUpgrade.upgradeName}");
        }
        // else Debug.Log("[UP] HandleUpgradeChoiceFromUI: Upgrade seçilmedi veya gösterilecek bir þey yoktu.");

        Time.timeScale = 1f;
        isUpgradeProcessActive = false;
        Debug.LogError($"[UP_STATE] OYUN DEVAM ETTÝRÝLDÝ (HandleUpgradeChoiceFromUI). TimeScale: {Time.timeScale}, isUpgradeActive: {isUpgradeProcessActive}");

        if (wasThisChoiceFromAnInitialOfferContext)
        {
            if (!initialWeaponOffered)
            {
                initialWeaponOffered = true;
                Debug.Log($"[UP] HandleUpgradeChoiceFromUI: Baþlangýç süreci tamamlandý. initialWeaponOffered = {initialWeaponOffered}");
            }
        }
        Debug.Log($"[UP] HandleUpgradeChoiceFromUI BÝTTÝ.");
    }
}