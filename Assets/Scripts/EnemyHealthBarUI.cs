using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Slider için

public class EnemyHealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0); // Ana objeye göre can barýnýn pozisyon offset'i
    [Tooltip("Can barý dolu deðilken ne kadar süre sonra otomatik gizlensin (saniye). 0 ise hep görünür.")]
    [SerializeField] private float autoHideDelay = 0f; // 0 ise hep görünür, >0 ise hasar sonrasý gizlenir.

    private Transform cameraTransform;
    private Transform parentTransform; // Can barýnýn takip edeceði düþman transformu
    private float lastUpdateTime;
    private Coroutine hideCoroutine;

    void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }
        if (healthSlider == null)
        {
            Debug.LogError("EnemyHealthBarUI: Slider component'i bulunamadý!", this);
            enabled = false;
        }
    }

    public void Initialize(Camera cam, float initialHealth, float maxHealth)
    {
        cameraTransform = cam.transform;
        parentTransform = transform.parent; // EnemyAI bu prefabý kendi çocuðu olarak instantiate etmeli

        if (parentTransform == null)
        {
            Debug.LogError("EnemyHealthBarUI: Can barý bir parent'a (düþmana) atanmamýþ!", this);
            // enabled = false; // Eðer parent yoksa çalýþmasýný engellemek mantýklý olabilir.
        }

        UpdateHealth(initialHealth, maxHealth);

        if (autoHideDelay > 0 && initialHealth >= maxHealth)
        {
            gameObject.SetActive(false); // Baþlangýçta tam can ise gizle
        }
        lastUpdateTime = Time.time;
    }

    public Slider GetSlider()
    {
        return healthSlider;
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider == null) return;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        lastUpdateTime = Time.time;

        if (autoHideDelay > 0)
        {
            gameObject.SetActive(true); // Hasar aldýðýnda/iyileþtiðinde göster
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);

            if (currentHealth < maxHealth && currentHealth > 0) // Tam can deðilse veya ölmemiþse
            {
                hideCoroutine = StartCoroutine(AutoHideTimer());
            }
            else if (currentHealth >= maxHealth || currentHealth <= 0) // Tam can veya ölü ise
            {
                gameObject.SetActive(currentHealth > 0); // Ölü deðilse göster, ölü ise gizle
            }
        }
        else // autoHideDelay <= 0 ise her zaman aktif (ölüm durumu EnemyAI.HideHealthBarOnDeath ile yönetilir)
        {
            gameObject.SetActive(currentHealth > 0);
        }
    }

    private IEnumerator AutoHideTimer()
    {
        yield return new WaitForSeconds(autoHideDelay);
        // Eðer bu süre zarfýnda tekrar hasar almadýysa (lastUpdateTime deðiþmediyse) gizle
        if (Time.time >= lastUpdateTime + autoHideDelay)
        {
            gameObject.SetActive(false);
        }
    }


    void LateUpdate()
    {
        // Billboarding: Can barýný her zaman kameraya doðru döndür
        if (cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }

        // Pozisyonu parent'a göre güncelle (EnemyAI bunu parent olarak ayarlamalý)
        // EnemyAI instantiate ederken parent'a atadýðý için bu kýsým gerekmeyebilir,
        // ancak can barý prefabýnýn kendi offset'ini yönetmesi için burada býrakýlabilir.
        // Eðer EnemyAI'da Instantiate ederken parent'a atýyorsanýz, offset'i orada ayarlamak daha iyi olabilir
        // veya prefab'ýn kendi localPosition'ýný ayarlayabilirsiniz.
        // Þimdilik EnemyAI'daki offset kullanýmýna güveniyoruz.
        // if (parentTransform != null)
        // {
        //    transform.position = parentTransform.position + offset + (Vector3.up * healthBarOffsetY_from_EnemyAI);
        // }
    }
}