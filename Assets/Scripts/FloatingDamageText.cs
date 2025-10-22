using UnityEngine;
using TMPro; // TextMeshPro i�in
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.2f; // Metnin ekranda kalma s�resi
    [SerializeField] private float moveSpeedY = 1.5f; // Metnin yukar� do�ru hareket h�z�
    [SerializeField] private float fadeOutStartTimeRatio = 0.6f; // �m�r s�resinin % ka��nda solmaya ba�layaca�� (0-1 aras�)
    [SerializeField] private AnimationCurve scaleCurve; // Opsiyonel: Metnin b�y�kl���n� animasyonla de�i�tirmek i�in Curve

    private TextMeshProUGUI textMesh;
    private Color initialColor;
    private float startTime;
    private Vector3 initialScale;
    private Transform cameraTransform;


    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh == null)
        {
            Debug.LogError("FloatingDamageText: TextMeshProUGUI component'i bulunamad�!", this);
            enabled = false;
        }
        initialScale = transform.localScale;
    }

    public void Initialize(string text, Color color)
    {
        Debug.Log($"FloatingDamageText Initialize �a�r�ld�. Text: {text}, Color: {color}");
        if (textMesh == null) Debug.LogError("Initialize i�inde textMesh NULL!");
        if (textMesh == null) return;

        textMesh.text = text;
        textMesh.color = color;
        initialColor = color;
        startTime = Time.time;

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("FloatingDamageText: Ana kamera bulunamad�. Billboard d�zg�n �al��mayabilir.");
        }

        // Ba�lang��ta rastgele k���k bir X ekseni hareketi (daha dinamik g�r�n�m i�in)
        float randomXJitter = Random.Range(-0.3f, 0.3f);
        transform.position += new Vector3(randomXJitter, 0, 0);

        // Yok olma s�resini ba�lat
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (textMesh == null) return;

        // Yukar� hareket
        transform.position += Vector3.up * moveSpeedY * Time.deltaTime;

        // Billboard (her zaman kameraya d�n�k olma)
        if (cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }

        float timePassed = Time.time - startTime;
        float lifeRatio = timePassed / lifetime;

        // �l�ek Animasyonu (opsiyonel)
        if (scaleCurve != null && scaleCurve.length > 0)
        {
            transform.localScale = initialScale * scaleCurve.Evaluate(lifeRatio);
        }

        // Solma Animasyonu
        if (lifeRatio >= fadeOutStartTimeRatio)
        {
            float fadeRatio = (lifeRatio - fadeOutStartTimeRatio) / (1f - fadeOutStartTimeRatio);
            Color currentColor = initialColor;
            currentColor.a = Mathf.Lerp(initialColor.a, 0f, fadeRatio);
            textMesh.color = currentColor;
        }
    }
}