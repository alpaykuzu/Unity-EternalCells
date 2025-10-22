// TimeStopper.cs
using UnityEngine;
using System.Collections;

public class TimeStopper : MonoBehaviour
{
    public static TimeStopper Instance { get; private set; }
    private Coroutine _activeTimeStopCoroutine;

    void Awake()
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
    }

    /// <summary>
    /// Zaman� k�sa bir s�reli�ine yava�lat�r veya durdurur.
    /// </summary>
    /// <param name="stopDuration">Yava�latman�n/durman�n ana s�resi (ger�ek zaman).</param>
    /// <param name="targetTimeScale">Zaman�n ne kadar yava�layaca�� (0 = tam durma, 1 = normal h�z).</param>
    /// <param name="fadeDuration">Yava�lamaya giri� ve ��k���n yumu�atma s�resi.</param>
    public void StopTime(float stopDuration = 0.07f, float targetTimeScale = 0.05f, float fadeDuration = 0.02f)
    {
        if (!gameObject.activeInHierarchy) return; // Obje aktif de�ilse ba�latma

        if (_activeTimeStopCoroutine != null)
        {
            StopCoroutine(_activeTimeStopCoroutine);
        }
        _activeTimeStopCoroutine = StartCoroutine(StopTimeSequence(stopDuration, targetTimeScale, fadeDuration));
    }

    private IEnumerator StopTimeSequence(float stopDuration, float targetTimeScale, float fadeDuration)
    {
        float originalTimeScale = 1.0f;

        // ... (Yava�lamaya yumu�ak ge�i� k�sm� ayn�) ...
        float t = 0f;
        while (t < fadeDuration)
        {
            // E�er upgrade s�reci aktifse ve zaman� 0'a �ekmi�se, TimeStopper m�dahale etmesin
            if (UpgradePresenter.Instance != null && UpgradePresenter.Instance.IsUpgradeProcessCurrentlyActive() && Time.timeScale == 0f) // IsUpgradeProcessCurrentlyActive() diye bir public metot eklemen gerekebilir UpgradePresenter'a
            {
                Debug.LogWarning("[TimeStopper] Upgrade s�reci aktif ve zaman durmu�, TimeStopper zaman� normale d�nd�rm�yor.");
                _activeTimeStopCoroutine = null; // Coroutine'i sonland�r
                yield break; // Coroutine'den ��k
            }
            Time.timeScale = Mathf.Lerp(originalTimeScale, targetTimeScale, t / fadeDuration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = targetTimeScale;

        yield return new WaitForSecondsRealtime(stopDuration);

        // Normal h�za yumu�ak d�n�� k�sm�nda da ayn� kontrol
        t = 0f;
        while (t < fadeDuration)
        {
            if (UpgradePresenter.Instance != null && UpgradePresenter.Instance.IsUpgradeProcessCurrentlyActive() && Time.timeScale == 0f)
            {
                Debug.LogWarning("[TimeStopper] Upgrade s�reci aktif ve zaman durmu�, TimeStopper zaman� normale d�nd�rm�yor.");
                _activeTimeStopCoroutine = null;
                yield break;
            }
            Time.timeScale = Mathf.Lerp(targetTimeScale, originalTimeScale, t / fadeDuration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Son kontrol: E�er hala upgrade s�reci aktif de�ilse normale d�n
        if (UpgradePresenter.Instance == null || !UpgradePresenter.Instance.IsUpgradeProcessCurrentlyActive() || Time.timeScale != 0f)
        {
            Time.timeScale = originalTimeScale;
        }
        else
        {
            Debug.LogWarning("[TimeStopper] Upgrade s�reci aktif ve zaman durmu�, TimeStopper son normale d�nd�rme i�lemini atl�yor.");
        }
        _activeTimeStopCoroutine = null;
    }

    public void CancelCurrentStopTime()
    {
        if (_activeTimeStopCoroutine != null)
        {
            StopCoroutine(_activeTimeStopCoroutine);
            _activeTimeStopCoroutine = null;
            // Zaman� direkt normale d�nd�rebilirsin ya da oldu�u gibi b�rakabilirsin,
            // ��nk� UpgradePresenter zaten Time.timeScale = 0f yapacak.
            // Time.timeScale = 1.0f; // �ste�e ba�l�, e�er hemen normale d�nmesi gerekiyorsa
            Debug.Log("[TimeStopper] Aktif zaman durdurma i�lemi iptal edildi.");
        }
    }
}