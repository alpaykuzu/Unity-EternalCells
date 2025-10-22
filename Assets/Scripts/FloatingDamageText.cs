using UnityEngine;
using TMPro; // TextMeshPro için
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.2f; // Metnin ekranda kalma süresi
    [SerializeField] private float moveSpeedY = 1.5f; // Metnin yukarý doðru hareket hýzý
    [SerializeField] private float fadeOutStartTimeRatio = 0.6f; // Ömür süresinin % kaçýnda solmaya baþlayacaðý (0-1 arasý)
    [SerializeField] private AnimationCurve scaleCurve; // Opsiyonel: Metnin büyüklüðünü animasyonla deðiþtirmek için Curve

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
            Debug.LogError("FloatingDamageText: TextMeshProUGUI component'i bulunamadý!", this);
            enabled = false;
        }
        initialScale = transform.localScale;
    }

    public void Initialize(string text, Color color)
    {
        Debug.Log($"FloatingDamageText Initialize çaðrýldý. Text: {text}, Color: {color}");
        if (textMesh == null) Debug.LogError("Initialize içinde textMesh NULL!");
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
            Debug.LogWarning("FloatingDamageText: Ana kamera bulunamadý. Billboard düzgün çalýþmayabilir.");
        }

        // Baþlangýçta rastgele küçük bir X ekseni hareketi (daha dinamik görünüm için)
        float randomXJitter = Random.Range(-0.3f, 0.3f);
        transform.position += new Vector3(randomXJitter, 0, 0);

        // Yok olma süresini baþlat
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (textMesh == null) return;

        // Yukarý hareket
        transform.position += Vector3.up * moveSpeedY * Time.deltaTime;

        // Billboard (her zaman kameraya dönük olma)
        if (cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }

        float timePassed = Time.time - startTime;
        float lifeRatio = timePassed / lifetime;

        // Ölçek Animasyonu (opsiyonel)
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