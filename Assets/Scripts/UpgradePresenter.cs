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
            if (uiManager == null) Debug.LogError("[UP] Awake: UpgradeUIManager ATANMAMI�/BULUNAMADI!", this);
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
            Debug.LogError("[UP] Start: Oyuncu veya HealthSystem BULUNAMADI! Script devre d��� b�rak�l�yor.", this);
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
                Debug.Log("[UP] Start: Oyun ba��nda ilk silah upgrade'i tetikleniyor.");
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
        Debug.Log($"[UP] HandleScoreBasedUpgradeTrigger: �a�r�ld�. Mevcut isUpgradeProcessActive = {isUpgradeProcessActive}");
        bool isItEffectivelyAnInitialOffer = !initialWeaponOffered && playerController.GetCurrentWeaponType() == WeaponType.None;
        TriggerUpgradePresentation(isItEffectivelyAnInitialOffer);
    }

    public void TriggerUpgradePresentation(bool isThisAnInitialWeaponOffer)
    {
        Debug.Log($"[UP] TriggerUpgradePresentation G�R��. isInitialOffer: {isThisAnInitialWeaponOffer}, Mevcut isUpgradeProcessActive: {isUpgradeProcessActive}");
        if (isUpgradeProcessActive)
        {
            Debug.LogError($"[UP] TriggerUpgradePresentation: ENGEL! isUpgradeProcessActive ZATEN TRUE ({isUpgradeProcessActive}).");
            return;
        }
        if (playerController == null || uiManager == null) // playerHealthSystem null olabilir (baz� upgradeler i�in)
        {
            Debug.LogError("[UP] TriggerUpgradePresentation: KR�T�K HATA! PlayerController veya UIManager eksik.");
            return;
        }

        if (TimeStopper.Instance != null) // �nce TimeStopper'� iptal et
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
        Debug.Log($"[UP] ShowUpgradeUICoroutine: Ba�lad�. {delayBeforeShowUI}s beklenecek. (TimeScale: {Time.timeScale})");
        if (delayBeforeShowUI > 0) yield return new WaitForSecondsRealtime(delayBeforeShowUI);

        Debug.Log("[UP] ShowUpgradeUICoroutine: Adaylar se�iliyor.");
        List<UpgradeData> candidatesForSelection;
        if (isThisAnInitialWeaponOfferContext || playerController.GetCurrentWeaponType() == WeaponType.None)
        {
            candidatesForSelection = upgradePool.OfType<GrantAttackAbilityUpgradeData>().Where(u => u != null && u.IsAvailable(playerController, playerHealthSystem)).Cast<UpgradeData>().ToList();
            if (candidatesForSelection.Count == 0)
            {
                Debug.LogError("[UP] ShowUpgradeUICoroutine: KR�T�K! Silah grant aday� yok! S�re� sonland�r�l�yor.");
                HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // Hata durumunda s�reci sonland�r
                yield break;
            }
        }
        else
        {
            candidatesForSelection = upgradePool.Where(u => u != null && u.IsAvailable(playerController, playerHealthSystem)).ToList();
            if (candidatesForSelection.Count == 0)
            {
                Debug.LogWarning("[UP] ShowUpgradeUICoroutine: Uygun genel upgrade aday� yok. S�re� sonland�r�l�yor.");
                HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // Hata durumunda s�reci sonland�r
                yield break;
            }
        }

        List<UpgradeData> weightedList = new List<UpgradeData>();
        foreach (var upgrade in candidatesForSelection) { for (int i = 0; i < Mathf.Max(1, upgrade.weight); i++) weightedList.Add(upgrade); }
        if (weightedList.Count == 0)
        {
            Debug.LogWarning("[UP] ShowUpgradeUICoroutine: A��rl�kl� liste bo�. S�re� sonland�r�l�yor.");
            HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // Hata durumunda s�reci sonland�r
            yield break;
        }

        System.Random rng = new System.Random();
        UpgradeData option1 = weightedList[rng.Next(weightedList.Count)];
        UpgradeData option2 = null;
        if (weightedList.Count > 1) { List<UpgradeData> distinctSecondOptions = weightedList.Where(u => u != option1).ToList(); if (distinctSecondOptions.Count > 0) option2 = distinctSecondOptions[rng.Next(distinctSecondOptions.Count)]; }

        if (uiManager != null)
        {
            Debug.Log($"[UP] ShowUpgradeUICoroutine: uiManager.DisplayUpgradeChoices �a�r�l�yor. Context: {isThisAnInitialWeaponOfferContext}");
            uiManager.DisplayUpgradeChoices(option1, option2, this, playerController, playerHealthSystem, isThisAnInitialWeaponOfferContext);
        }
        else
        {
            Debug.LogError("[UP] ShowUpgradeUICoroutine: UIManager null! S�re� sonland�r�l�yor.");
            HandleUpgradeChoiceFromUI(null, isThisAnInitialWeaponOfferContext); // UI Manager yoksa s�reci sonland�r
        }
    }

    /// <summary>
    /// UpgradeUIManager'dan bir se�im yap�ld���nda (veya UI'�n kapat�lmas� gerekti�inde) �a�r�l�r.
    /// </summary>
    public void HandleUpgradeChoiceFromUI(UpgradeData chosenUpgrade, bool wasThisChoiceFromAnInitialOfferContext)
    {
        Debug.Log($"[UP] HandleUpgradeChoiceFromUI BA�LADI. Se�ilen: {chosenUpgrade?.upgradeName ?? "NULL"}, Ba�lang��M�: {wasThisChoiceFromAnInitialOfferContext}. Metoda girerken isUpgradeProcessActive = {isUpgradeProcessActive}");

        if (uiManager != null)
        {
            uiManager.HidePanel(); // UI Manager'a panelini gizlemesini s�yle
        }

        if (!isUpgradeProcessActive && Time.timeScale == 1f)
        {
            // E�er s�re� zaten aktif de�ilse ve oyun normal h�zdaysa, bu beklenmedik bir �a�r� olabilir.
            // Veya Presenter bir �ekilde devre d��� kalm�� ama UI hala callback yapmaya �al���yor.
            Debug.LogWarning($"[UP] HandleUpgradeChoiceFromUI: �a�r�ld� ancak isUpgradeProcessActive ZATEN false idi ve Time.timeScale = 1. Geri d�n�l�yor.");
            return; // Fazladan i�lem yapma
        }
        // E�er isUpgradeProcessActive false ama Time.timeScale 0 ise, bir tutars�zl�k var demektir, yine de d�zeltmeye �al��al�m.

        if (chosenUpgrade != null && playerController != null) // playerHealthSystem opsiyonel olabilir
        {
            chosenUpgrade.ApplyUpgrade(playerController, playerHealthSystem);
            // Debug.Log($"[UP] HandleUpgradeChoiceFromUI: Upgrade uyguland�: {chosenUpgrade.upgradeName}");
        }
        // else Debug.Log("[UP] HandleUpgradeChoiceFromUI: Upgrade se�ilmedi veya g�sterilecek bir �ey yoktu.");

        Time.timeScale = 1f;
        isUpgradeProcessActive = false;
        Debug.LogError($"[UP_STATE] OYUN DEVAM ETT�R�LD� (HandleUpgradeChoiceFromUI). TimeScale: {Time.timeScale}, isUpgradeActive: {isUpgradeProcessActive}");

        if (wasThisChoiceFromAnInitialOfferContext)
        {
            if (!initialWeaponOffered)
            {
                initialWeaponOffered = true;
                Debug.Log($"[UP] HandleUpgradeChoiceFromUI: Ba�lang�� s�reci tamamland�. initialWeaponOffered = {initialWeaponOffered}");
            }
        }
        Debug.Log($"[UP] HandleUpgradeChoiceFromUI B�TT�.");
    }
}