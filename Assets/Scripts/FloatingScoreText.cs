// FloatingScoreText.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingScoreText : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.0f;
    [SerializeField] private float moveSpeedY = 1.8f;
    [SerializeField] private float fadeOutStartTimeRatio = 0.5f;
    [SerializeField] private AnimationCurve scaleCurve; // Opsiyonel

    private TextMeshProUGUI textMesh;
    private Color initialColor;
    private float startTime;
    private Vector3 initialScale;
    private Transform cameraTransform;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh == null) { enabled = false; return; }
        initialScale = transform.localScale;
    }

    public void Initialize(string text, Color color)
    {
        if (textMesh == null) return;

        textMesh.text = text;
        textMesh.color = color;
        initialColor = color;
        startTime = Time.time;

        if (Camera.main != null) cameraTransform = Camera.main.transform;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (textMesh == null) return;

        transform.position += Vector3.up * moveSpeedY * Time.deltaTime;

        if (cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }

        float timePassed = Time.time - startTime;
        float lifeRatio = timePassed / lifetime;

        if (scaleCurve != null && scaleCurve.length > 0)
        {
            transform.localScale = initialScale * scaleCurve.Evaluate(lifeRatio);
        }

        if (lifeRatio >= fadeOutStartTimeRatio)
        {
            float fadeRatio = (lifeRatio - fadeOutStartTimeRatio) / (1f - fadeOutStartTimeRatio);
            Color currentColor = initialColor;
            currentColor.a = Mathf.Lerp(initialColor.a, 0f, fadeRatio);
            textMesh.color = currentColor;
        }
    }
}