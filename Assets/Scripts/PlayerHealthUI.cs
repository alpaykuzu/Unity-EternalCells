using UnityEngine;
using UnityEngine.UI; // UI elemanlar�n� kullanmak i�in bu sat�r gerekli
using System.Collections; // IEnumerator i�in

public class PlayerHealthUI : MonoBehaviour
{
    [Tooltip("Oyuncunun can�n� g�stermek i�in kullan�lacak UI Slider.")]
    public Slider healthSlider;

    [Tooltip("Oyuncunun GameObject'inin Tag'i. Bu Tag ile oyuncu bulunacak.")]
    public string playerTag = "Player";

    private HealthSystem playerHealthSystem;

    // IEnumerator Start metodu, oyuncu bulunduktan sonra i�lemlere devam etmek i�in kullan�l�r
    IEnumerator Start()
    {
        if (healthSlider == null)
        {
            Debug.LogError("Health Slider atanmam��! L�tfen PlayerHealthUI script'ine bir Slider atay�n.", this);
            enabled = false; // Script'i devre d��� b�rak
            yield break; // Coroutine'i sonland�r
        }

        GameObject playerObject = null;
        // Oyuncu bulunana kadar bekle (sahne y�klenirken hemen bulunamayabilir)
        while (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null)
            {
                // Debug.LogWarning("PlayerHealthUI: Oyuncu '" + playerTag + "' Tag'i ile bulunamad�. 1 saniye sonra tekrar denenecek...");
                yield return new WaitForSeconds(1f); // 1 saniye bekle ve tekrar dene
            }
        }

        // Debug.Log("PlayerHealthUI: Oyuncu bulundu: " + playerObject.name);
        playerHealthSystem = playerObject.GetComponent<HealthSystem>();

        if (playerHealthSystem == null)
        {
            Debug.LogError("Oyuncuda HealthSystem componenti bulunamad�!", playerObject);
            enabled = false;
            yield break;
        }

        // Can de�i�ti�inde UpdateHealthBar metodunu �a��racak �ekilde event'e abone ol
        playerHealthSystem.OnHealthChanged += UpdateHealthBar;

        // Ba�lang��ta can bar�n� ayarla
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

    // HealthSystem'deki OnHealthChanged event'i taraf�ndan �a�r�lacak
    void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth; // Max can de�i�ebilece�i durumlar i�in
            healthSlider.value = currentHealth;
            // Debug.Log($"Health bar updated. Max: {healthSlider.maxValue}, Current: {healthSlider.value}");
        }
    }

    // Script yok edildi�inde event aboneli�inden ��kmak �nemlidir
    private void OnDestroy()
    {
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnHealthChanged -= UpdateHealthBar;
        }
    }
}