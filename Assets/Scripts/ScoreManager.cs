// ScoreManager.cs
using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Puan Ayarlar�")]
    [SerializeField] private int upgradeThreshold = 1000; // Her ka� puanda bir upgrade sunulaca��
    [SerializeField] private GameObject floatingScoreTextPrefab; // Y�zen puan metni prefab� (TextMeshPro ve FloatingScoreText script'i i�ermeli)

    public int CurrentScore { get; private set; }
    public int ScoreForNextUpgradeLevel { get; private set; }

    // Event'ler: UI ve di�er sistemlerin dinlemesi i�in
    // Parametreler: mevcutPuan, birSonrakiUpgrade��inGerekenPuan
    public event Action<int, int> OnScoreUpdated;
    // Bu event, upgrade sunulmas� gerekti�inde tetiklenir
    public event Action OnUpgradeThresholdReached;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Sahneler aras� ge�i�te korunmas� istenirse
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
        // Ba�lang��ta UI'� g�ncellemek i�in event'i tetikle
        OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);
    }

    public void AddScore(int amount, Vector3 textSpawnPosition)
    {
        if (amount <= 0) return;

        CurrentScore += amount;
        Debug.Log($"Puan eklendi: +{amount}. Toplam Puan: {CurrentScore}");

        // Y�zen Puan Metnini G�ster
        if (floatingScoreTextPrefab != null)
        {
            GameObject textInstance = Instantiate(floatingScoreTextPrefab, textSpawnPosition, Quaternion.identity);
            FloatingScoreText scoreTextScript = textInstance.GetComponent<FloatingScoreText>();
            if (scoreTextScript != null)
            {
                scoreTextScript.Initialize($"+{amount}", Color.yellow); // Puan i�in �rnek renk
            }
            else
            {
                // Fallback veya hata loglama
                Destroy(textInstance, 1f);
            }
        }

        OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);

        // Upgrade e�i�ine ula��ld� m� kontrol et
        if (CurrentScore >= ScoreForNextUpgradeLevel)
        {
            Debug.Log($"Upgrade e�i�ine ula��ld�! Puan: {CurrentScore}/{ScoreForNextUpgradeLevel}");
            OnUpgradeThresholdReached?.Invoke();
            // Bir sonraki upgrade i�in hedef puan� art�r
            ScoreForNextUpgradeLevel += upgradeThreshold;
            // UI'� yeni hedefle tekrar g�ncelle
            OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);
        }
    }

    // Oyuncunun mevcut puan�n� s�f�rlay�p, upgrade hedefini ba�a almak i�in (�rne�in yeni oyun)
    public void ResetScoreAndThreshold()
    {
        CurrentScore = 0;
        ScoreForNextUpgradeLevel = upgradeThreshold;
        OnScoreUpdated?.Invoke(CurrentScore, ScoreForNextUpgradeLevel);
    }
}