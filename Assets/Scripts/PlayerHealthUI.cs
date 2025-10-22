using UnityEngine;
using UnityEngine.UI; // UI elemanlarýný kullanmak için bu satýr gerekli
using System.Collections; // IEnumerator için

public class PlayerHealthUI : MonoBehaviour
{
    [Tooltip("Oyuncunun canýný göstermek için kullanýlacak UI Slider.")]
    public Slider healthSlider;

    [Tooltip("Oyuncunun GameObject'inin Tag'i. Bu Tag ile oyuncu bulunacak.")]
    public string playerTag = "Player";

    private HealthSystem playerHealthSystem;

    // IEnumerator Start metodu, oyuncu bulunduktan sonra iþlemlere devam etmek için kullanýlýr
    IEnumerator Start()
    {
        if (healthSlider == null)
        {
            Debug.LogError("Health Slider atanmamýþ! Lütfen PlayerHealthUI script'ine bir Slider atayýn.", this);
            enabled = false; // Script'i devre dýþý býrak
            yield break; // Coroutine'i sonlandýr
        }

        GameObject playerObject = null;
        // Oyuncu bulunana kadar bekle (sahne yüklenirken hemen bulunamayabilir)
        while (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null)
            {
                // Debug.LogWarning("PlayerHealthUI: Oyuncu '" + playerTag + "' Tag'i ile bulunamadý. 1 saniye sonra tekrar denenecek...");
                yield return new WaitForSeconds(1f); // 1 saniye bekle ve tekrar dene
            }
        }

        // Debug.Log("PlayerHealthUI: Oyuncu bulundu: " + playerObject.name);
        playerHealthSystem = playerObject.GetComponent<HealthSystem>();

        if (playerHealthSystem == null)
        {
            Debug.LogError("Oyuncuda HealthSystem componenti bulunamadý!", playerObject);
            enabled = false;
            yield break;
        }

        // Can deðiþtiðinde UpdateHealthBar metodunu çaðýracak þekilde event'e abone ol
        playerHealthSystem.OnHealthChanged += UpdateHealthBar;

        // Baþlangýçta can barýný ayarla
        InitializeHealthBar();
    }

    void InitializeHealthBar()
    {
        if (playerHealthSystem != null && healthSlider != null)
        {
            healthSlider.maxValue = playerHealthSystem.MaxHealth;
            healthSlider.value = playerHealthSystem.CurrentHealth;
            // Debug.Log($"Health bar initialized. Max: {healthSlider.maxValue}, Current: {healthSlider.value}");
        }
    }

    // HealthSystem'deki OnHealthChanged event'i tarafýndan çaðrýlacak
    void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth; // Max can deðiþebileceði durumlar için
            healthSlider.value = currentHealth;
            // Debug.Log($"Health bar updated. Max: {healthSlider.maxValue}, Current: {healthSlider.value}");
        }
    }

    // Script yok edildiðinde event aboneliðinden çýkmak önemlidir
    private void OnDestroy()
    {
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnHealthChanged -= UpdateHealthBar;
        }
    }
}