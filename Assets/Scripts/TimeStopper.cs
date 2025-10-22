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
            // DontDestroyOnLoad(gameObject); // Sahneler arasý geçiþte korunmasý istenirse
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Zamaný kýsa bir süreliðine yavaþlatýr veya durdurur.
    /// </summary>
    /// <param name="stopDuration">Yavaþlatmanýn/durmanýn ana süresi (gerçek zaman).</param>
    /// <param name="targetTimeScale">Zamanýn ne kadar yavaþlayacaðý (0 = tam durma, 1 = normal hýz).</param>
    /// <param name="fadeDuration">Yavaþlamaya giriþ ve çýkýþýn yumuþatma süresi.</param>
    public void StopTime(float stopDuration = 0.07f, float targetTimeScale = 0.05f, float fadeDuration = 0.02f)
    {
        if (!gameObject.activeInHierarchy) return; // Obje aktif deðilse baþlatma

        if (_activeTimeStopCoroutine != null)
        {
            StopCoroutine(_activeTimeStopCoroutine);
        }
        _activeTimeStopCoroutine = StartCoroutine(StopTimeSequence(stopDuration, targetTimeScale, fadeDuration));
    }

    private IEnumerator StopTimeSequence(float stopDuration, float targetTimeScale, float fadeDuration)
    {
        float originalTimeScale = 1.0f;

        // ... (Yavaþlamaya yumuþak geçiþ kýsmý ayný) ...
        float t = 0f;
        while (t < fadeDuration)
        {
            // Eðer upgrade süreci aktifse ve zamaný 0'a çekmiþse, TimeStopper müdahale etmesin
            if (UpgradePresenter.Instance != null && UpgradePresenter.Instance.IsUpgradeProcessCurrentlyActive() && Time.timeScale == 0f) // IsUpgradeProcessCurrentlyActive() diye bir public metot eklemen gerekebilir UpgradePresenter'a
            {
                Debug.LogWarning("[TimeStopper] Upgrade süreci aktif ve zaman durmuþ, TimeStopper zamaný normale döndürmüyor.");
                _activeTimeStopCoroutine = null; // Coroutine'i sonlandýr
                yield break; // Coroutine'den çýk
            }
            Time.timeScale = Mathf.Lerp(originalTimeScale, targetTimeScale, t / fadeDuration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = targetTimeScale;

        yield return new WaitForSecondsRealtime(stopDuration);

        // Normal hýza yumuþak dönüþ kýsmýnda da ayný kontrol
        t = 0f;
        while (t < fadeDuration)
        {
            if (UpgradePresenter.Instance != null && UpgradePresenter.Instance.IsUpgradeProcessCurrentlyActive() && Time.timeScale == 0f)
            {
                Debug.LogWarning("[TimeStopper] Upgrade süreci aktif ve zaman durmuþ, TimeStopper zamaný normale döndürmüyor.");
                _activeTimeStopCoroutine = null;
                yield break;
            }
            Time.timeScale = Mathf.Lerp(targetTimeScale, originalTimeScale, t / fadeDuration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Son kontrol: Eðer hala upgrade süreci aktif deðilse normale dön
        if (UpgradePresenter.Instance == null || !UpgradePresenter.Instance.IsUpgradeProcessCurrentlyActive() || Time.timeScale != 0f)
        {
            Time.timeScale = originalTimeScale;
        }
        else
        {
            Debug.LogWarning("[TimeStopper] Upgrade süreci aktif ve zaman durmuþ, TimeStopper son normale döndürme iþlemini atlýyor.");
        }
        _activeTimeStopCoroutine = null;
    }

    public void CancelCurrentStopTime()
    {
        if (_activeTimeStopCoroutine != null)
        {
            StopCoroutine(_activeTimeStopCoroutine);
            _activeTimeStopCoroutine = null;
            // Zamaný direkt normale döndürebilirsin ya da olduðu gibi býrakabilirsin,
            // çünkü UpgradePresenter zaten Time.timeScale = 0f yapacak.
            // Time.timeScale = 1.0f; // Ýsteðe baðlý, eðer hemen normale dönmesi gerekiyorsa
            Debug.Log("[TimeStopper] Aktif zaman durdurma iþlemi iptal edildi.");
        }
    }
}