// UpgradeUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UpgradeUIManager : MonoBehaviour
{
    [Header("UI Paneli")]
    [SerializeField] private GameObject upgradePanel;

    [Header("Seçenek 1 UI Elemanlarý")]
    [SerializeField] private GameObject option1DisplayGroup;
    [SerializeField] private Button option1Button;
    [SerializeField] private Image option1Icon;
    [SerializeField] private TextMeshProUGUI option1NameText;
    [SerializeField] private TextMeshProUGUI option1DescriptionText;

    [Header("Seçenek 2 UI Elemanlarý")]
    [SerializeField] private GameObject option2DisplayGroup;
    [SerializeField] private Button option2Button;
    [SerializeField] private Image option2Icon;
    [SerializeField] private TextMeshProUGUI option2NameText;
    [SerializeField] private TextMeshProUGUI option2DescriptionText;

    [Header("Durum Metinleri")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Diðer UI Kontrolü")]
    [Tooltip("Upgrade ekraný aktifken gizlenecek diðer UI panelleri/canvaslarý.")]
    public List<GameObject> otherUiElementsToHide;
    private List<bool> otherUiOriginalStates;


    private UpgradeData currentOption1Data; // Butonlara týklandýðýnda hangi datanýn seçildiðini bilmek için
    private UpgradeData currentOption2Data;
    private UpgradePresenter currentActivePresenter; // Hangi Presenter'a geri bildirim yapacaðýmýzý bilmek için
    private bool wasThisDisplayForInitialOffer; // Geri bildirimde bu context'i kullanmak için

    private void Awake()
    {
        otherUiOriginalStates = new List<bool>();
        if (upgradePanel == null) { Debug.LogError("[UI] Upgrade Panel ATANMAMIÞ!", this); enabled = false; return; }
        upgradePanel.SetActive(false);

        if (option1Button == null) Debug.LogError("[UI] Option 1 Button ATANMAMIÞ!", this);
        // option2Button null olabilir.
    }

    private void Start()
    {
        if (option1Button != null) option1Button.onClick.AddListener(OnOption1Clicked);
        if (option2Button != null) option2Button.onClick.AddListener(OnOption2Clicked);
    }

    public void DisplayUpgradeChoices(UpgradeData opt1, UpgradeData opt2, UpgradePresenter sourcePresenter, TopDownController playerCtrl, HealthSystem playerHealth, bool isInitialOfferContext)
    {
        // Gelen referanslarý sakla
        currentOption1Data = opt1;
        currentOption2Data = opt2;
        currentActivePresenter = sourcePresenter;
        wasThisDisplayForInitialOffer = isInitialOfferContext;

        if (upgradePanel == null || option1DisplayGroup == null || currentActivePresenter == null)
        {
            Debug.LogError("[UI] DisplayUpgradeChoices: Kritik referanslar eksik (Panel, Option1Group veya Presenter). Sunum iptal ediliyor.");
            // Eðer Presenter varsa ama diðerleri yoksa, Presenter'a yine de durumu bildirip süreci sonlandýrmasýný isteyebiliriz.
            currentActivePresenter?.HandleUpgradeChoiceFromUI(null, isInitialOfferContext);
            return;
        }
        if (opt1 == null) // opt1 her zaman dolu gelmeli
        {
            Debug.LogError("[UI] DisplayUpgradeChoices: opt1 null geldi! Sunum iptal ediliyor.");
            currentActivePresenter.HandleUpgradeChoiceFromUI(null, isInitialOfferContext);
            return;
        }

        // Diðer UI'larý gizle
        if (otherUiElementsToHide != null)
        {
            otherUiOriginalStates.Clear();
            foreach (GameObject uiElement in otherUiElementsToHide)
            {
                if (uiElement != null) { otherUiOriginalStates.Add(uiElement.activeSelf); uiElement.SetActive(false); }
                else { otherUiOriginalStates.Add(false); }
            }
        }

        upgradePanel.SetActive(true);

        // Seçenek 1'i UI'da ayarla
        option1DisplayGroup.SetActive(true);
        if (option1Icon != null) { option1Icon.sprite = opt1.upgradeIcon; option1Icon.enabled = (opt1.upgradeIcon != null); }
        if (option1NameText != null) option1NameText.text = opt1.upgradeName;
        if (option1DescriptionText != null) option1DescriptionText.text = opt1.upgradeDescription;
        if (option1Button != null) option1Button.gameObject.SetActive(true);

        // Seçenek 2'yi UI'da ayarla (varsa)
        bool hasOption2 = (opt2 != null);
        if (option2DisplayGroup != null)
        {
            option2DisplayGroup.SetActive(hasOption2);
            if (hasOption2)
            {
                if (option2Icon != null) { option2Icon.sprite = opt2.upgradeIcon; option2Icon.enabled = (opt2.upgradeIcon != null); }
                if (option2NameText != null) option2NameText.text = opt2.upgradeName;
                if (option2DescriptionText != null) option2DescriptionText.text = opt2.upgradeDescription;
                if (option2Button != null) option2Button.gameObject.SetActive(true);
            }
        }
        else if (hasOption2) Debug.LogWarning("[UI] Ýkinci seçenek verisi var ama Option 2 Display Group atanmamýþ.");

        // Durum metnini ayarla
        if (statusText != null)
        {
            bool showStatus = !(hasOption2 && option2DisplayGroup != null && option2Button != null && option2Button.gameObject.activeSelf);
            statusText.gameObject.SetActive(showStatus);
            if (showStatus) statusText.text = "Baþka bir seçenek mevcut deðil.";
        }
    }

    private void OnOption1Clicked()
    {
        Debug.Log($"[UI] OnOption1Clicked. Seçilen: {currentOption1Data?.upgradeName ?? "NULL"}. Presenter'a bildiriliyor.");
        if (currentActivePresenter != null)
        {
            currentActivePresenter.HandleUpgradeChoiceFromUI(currentOption1Data, wasThisDisplayForInitialOffer);
        }
        else Debug.LogError("[UI] OnOption1Clicked: currentActivePresenter NULL!");
        // Paneli burada gizleme, Presenter HidePanel'i çaðýracak.
    }

    private void OnOption2Clicked()
    {
        Debug.Log($"[UI] OnOption2Clicked. Seçilen: {currentOption2Data?.upgradeName ?? "NULL"}. Presenter'a bildiriliyor.");
        if (currentActivePresenter != null)
        {
            // Eðer currentOption2Data null ise (UI'da sadece 1 seçenek gösteriliyordu),
            // bu butona týklanmamalýydý. Ama yine de Presenter'a null data ile bildirim yapabiliriz.
            currentActivePresenter.HandleUpgradeChoiceFromUI(currentOption2Data, wasThisDisplayForInitialOffer);
        }
        else Debug.LogError("[UI] OnOption2Clicked: currentActivePresenter NULL!");
    }

    /// <summary>
    /// UpgradePresenter tarafýndan çaðrýlýr, bu UI panelini gizler.
    /// </summary>
    public void HidePanel()
    {
        Debug.Log("[UI] HidePanel: Çaðrýldý. Panel gizleniyor ve diðer UI'lar geri yükleniyor.");
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        // Diðer UI elemanlarýný orijinal durumlarýna geri getir
        if (otherUiElementsToHide != null && otherUiOriginalStates != null)
        {
            for (int i = 0; i < otherUiElementsToHide.Count; i++)
            {
                if (otherUiElementsToHide[i] != null && i < otherUiOriginalStates.Count)
                {
                    otherUiElementsToHide[i].SetActive(otherUiOriginalStates[i]);
                }
            }
            otherUiOriginalStates.Clear();
        }

        // Bu etkileþim döngüsü için referanslarý temizle
        currentOption1Data = null;
        currentOption2Data = null;
        currentActivePresenter = null;
        // wasThisDisplayForInitialOffer'ý burada sýfýrlamaya gerek yok, DisplayUpgradeChoices'da tekrar set edilecek.
    }
}