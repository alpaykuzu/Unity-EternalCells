// ScoreUI.cs
using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreUpdated += UpdateScoreDisplay;
            // Ba�lang�� de�erini ayarla
            UpdateScoreDisplay(ScoreManager.Instance.CurrentScore, ScoreManager.Instance.ScoreForNextUpgradeLevel);
        }
        else
        {
            Debug.LogError("ScoreUI: ScoreManager bulunamad�!");
            if (scoreText != null) scoreText.text = "HATA";
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreUpdated -= UpdateScoreDisplay;
        }
    }

    private void UpdateScoreDisplay(int currentScore, int nextUpgradeScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SKOR: {currentScore} / {nextUpgradeScore}";
        }
    }
}