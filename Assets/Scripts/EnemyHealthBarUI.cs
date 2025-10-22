using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Slider i�in

public class EnemyHealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0); // Ana objeye g�re can bar�n�n pozisyon offset'i
    [Tooltip("Can bar� dolu de�ilken ne kadar s�re sonra otomatik gizlensin (saniye). 0 ise hep g�r�n�r.")]
    [SerializeField] private float autoHideDelay = 0f; // 0 ise hep g�r�n�r, >0 ise hasar sonras� gizlenir.

    private Transform cameraTransform;
    private Transform parentTransform; // Can bar�n�n takip edece�i d��man transformu
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
            Debug.LogError("EnemyHealthBarUI: Slider component'i bulunamad�!", this);
            enabled = false;
        }
    }

    public void Initialize(Camera cam, float initialHealth, float maxHealth)
    {
        cameraTransform = cam.transform;
        parentTransform = transform.parent; // EnemyAI bu prefab� kendi �ocu�u olarak instantiate etmeli

        if (parentTransform == null)
        {
            Debug.LogError("EnemyHealthBarUI: Can bar� bir parent'a (d��mana) atanmam��!", this);
            // enabled = false; // E�er parent yoksa �al��mas�n� engellemek mant�kl� olabilir.
        }

        UpdateHealth(initialHealth, maxHealth);

        if (autoHideDelay > 0 && initialHealth >= maxHealth)
        {
            gameObject.SetActive(false); // Ba�lang��ta tam can ise gizle
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
            gameObject.SetActive(true); // Hasar ald���nda/iyile�ti�inde g�ster
            if (hideCoroutine != null) StopCoroutine(hideCoroutine);

            if (currentHealth < maxHealth && currentHealth > 0) // Tam can de�ilse veya �lmemi�se
            {
                hideCoroutine = StartCoroutine(AutoHideTimer());
            }
            else if (currentHealth >= maxHealth || currentHealth <= 0) // Tam can veya �l� ise
            {
                gameObject.SetActive(currentHealth > 0); // �l� de�ilse g�ster, �l� ise gizle
            }
        }
        else // autoHideDelay <= 0 ise her zaman aktif (�l�m durumu EnemyAI.HideHealthBarOnDeath ile y�netilir)
        {
            gameObject.SetActive(currentHealth > 0);
        }
    }

    private IEnumerator AutoHideTimer()
    {
        yield return new WaitForSeconds(autoHideDelay);
        // E�er bu s�re zarf�nda tekrar hasar almad�ysa (lastUpdateTime de�i�mediyse) gizle
        if (Time.time >= lastUpdateTime + autoHideDelay)
        {
            gameObject.SetActive(false);
        }
    }


    void LateUpdate()
    {
        // Billboarding: Can bar�n� her zaman kameraya do�ru d�nd�r
        if (cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                             cameraTransform.rotation * Vector3.up);
        }

        // Pozisyonu parent'a g�re g�ncelle (EnemyAI bunu parent olarak ayarlamal�)
        // EnemyAI instantiate ederken parent'a atad��� i�in bu k�s�m gerekmeyebilir,
        // ancak can bar� prefab�n�n kendi offset'ini y�netmesi i�in burada b�rak�labilir.
        // E�er EnemyAI'da Instantiate ederken parent'a at�yorsan�z, offset'i orada ayarlamak daha iyi olabilir
        // veya prefab'�n kendi localPosition'�n� ayarlayabilirsiniz.
        // �imdilik EnemyAI'daki offset kullan�m�na g�veniyoruz.
        // if (parentTransform != null)
        // {
        //    transform.position = parentTransform.position + offset + (Vector3.up * healthBarOffsetY_from_EnemyAI);
        // }
    }
}