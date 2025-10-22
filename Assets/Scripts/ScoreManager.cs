// ScoreManager.cs
using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Puan Ayarlarý")]
    [SerializeField] private int upgradeThreshold = 1000; // Her kaç puanda bir upgrade sunulacaðý
    [SerializeField] private GameObject floatingScoreTextPrefab; // Yüzen puan metni prefabý (TextMeshPro ve FloatingScoreText script'i içermeli)

    public int CurrentScore { get; private set; }
    public int ScoreForNextUpgradeLevel { get; private set; }

    // Event'ler: UI ve diðer sistemlerin dinlemesi için
    // Parametreler: mevcutPuan, birSonrakiUpgradeÝçinGerekenPuan
    public event Action<int, int> OnScoreUpdated;
    // Bu event, upgrade sunulmasý gerektiðinde tetiklenir
    public event Action OnUpgradeThresholdReached;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Sahneler arasý geçiþte korunmasý istenirse
        }
        else
        {
            Destroy(gameObject);
        }
        CurrentScore = 0;
        ScoreForNextUpgradeLevel = upgradeThreshold;
    }

    private void Start()
    {
        // Baþlangýçta UI'ý güncellemek için event'i tetikle
        OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);
    }

    public void AddScore(int amount, Vector3 textSpawnPosition)
    {
        if (amount <= 0) return;

        CurrentScore += amount;
        Debug.Log($"Puan eklendi: +{amount}. Toplam Puan: {CurrentScore}");

        // Yüzen Puan Metnini Göster
        if (floatingScoreTextPrefab != null)
        {
            GameObject textInstance = Instantiate(floatingScoreTextPrefab, textSpawnPosition, Quaternion.identity);
            FloatingScoreText scoreTextScript = textInstance.GetComponent<FloatingScoreText>();
            if (scoreTextScript != null)
            {
                scoreTextScript.Initialize($"+{amount}", Color.yellow); // Puan için örnek renk
            }
            else
            {
                // Fallback veya hata loglama
                Destroy(textInstance, 1f);
            }
        }

        OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);

        // Upgrade eþiðine ulaþýldý mý kontrol et
        if (CurrentScore >= ScoreForNextUpgradeLevel)
        {
            Debug.Log($"Upgrade eþiðine ulaþýldý! Puan: {CurrentScore}/{ScoreForNextUpgradeLevel}");
            OnUpgradeThresholdReached?.Invoke();
            // Bir sonraki upgrade için hedef puaný artýr
            ScoreForNextUpgradeLevel += upgradeThreshold;
            // UI'ý yeni hedefle tekrar güncelle
            OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);
        }
    }

    // Oyuncunun mevcut puanýný sýfýrlayýp, upgrade hedefini baþa almak için (örneðin yeni oyun)
    public void ResetScoreAndThreshold()
    {
        CurrentScore = 0;
        ScoreForNextUpgradeLevel = upgradeThreshold;
        OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);
    }
}