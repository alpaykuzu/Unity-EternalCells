using UnityEngine;
using System.Collections;

// Bu script, bir trigger alan�na giren "Player" tag'ine sahip
// karakterlere saniye ba��na hasar uygular.
// Bu script'in eklendi�i GameObject'te bir Collider component'i olmal�
// ve bu Collider'�n "Is Trigger" �zelli�i i�aretli olmal�d�r.
public class DamageOverTimeArea : MonoBehaviour
{
    [Header("Hasar Ayarlar�")]
    [Tooltip("Saniye ba��na uygulanacak hasar miktar�.")]
    public float damagePerSecond = 10f;

    [Tooltip("Hasar uyguland�ktan sonra bir sonraki hasar i�in beklenecek s�re (saniye).")]
    public float damageInterval = 1.0f; // Varsay�lan olarak saniyede bir hasar

    [Tooltip("Hasar verirken 'Hit Stop' efekti tetiklensin mi? Genellikle s�rekli hasar i�in false olur.")]
    public bool causeHitStopOnDamage = false;

    // Hasar verilen oyuncunun HealthSystem component'ini ve coroutine'i saklamak i�in
    private HealthSystem currentTargetHealthSystem;
    private Coroutine damageCoroutine;

    // Trigger alan�na bir obje girdi�inde �a�r�l�r
    private void OnTriggerEnter(Collider other)
    {
        // Giren objenin "Player" tag'ine sahip olup olmad���n� kontrol et
        if (other.CompareTag("Player"))
        {
            // E�er zaten bu oyuncuya veya ba�ka bir oyuncuya hasar veriyorsak,
            // yeni bir coroutine ba�latma (bu senaryo genelde tek oyuncu i�in tasarlan�r).
            if (damageCoroutine != null)
            {
                // �ste�e ba�l�: E�er farkl� bir oyuncu girerse ne yap�laca��na karar verilebilir.
                // �imdilik, sadece ilk giren oyuncuya odaklan�yoruz.
                Debug.LogWarning("DamageOverTimeArea: Alan zaten bir oyuncuya hasar veriyor. Yeni giren '" + other.name + "' i�in i�lem yap�lmad�.");
                return;
            }

            // Oyuncudan HealthSystem component'ini al
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();

            if (playerHealth != null)
            {
                Debug.Log("Oyuncu '" + other.name + "' hasar alan�na girdi.");
                currentTargetHealthSystem = playerHealth;
                // Hasar verme coroutine'ini ba�lat
                damageCoroutine = StartCoroutine(ApplyDamageRepeatedly());
            }
            else
            {
                Debug.LogWarning("Oyuncu '" + other.name + "' HealthSystem component'ine sahip de�il.");
            }
        }
    }

    // Trigger alan�ndan bir obje ��kt���nda �a�r�l�r
    private void OnTriggerExit(Collider other)
    {
        // ��kan objenin "Player" tag'ine sahip olup olmad���n� ve
        // �u anda hasar verdi�imiz hedef olup olmad���n� kontrol et
        if (other.CompareTag("Player") && currentTargetHealthSystem != null && other.GetComponent<HealthSystem>() == currentTargetHealthSystem)
        {
            Debug.Log("Oyuncu '" + other.name + "' hasar alan�ndan ��kt�.");
            // E�er aktif bir hasar coroutine'i varsa durdur
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null; // Coroutine referans�n� temizle
            }
            currentTargetHealthSystem = null; // Hedef referans�n� temizle
        }
    }

    // Belirli aral�klarla hedefe hasar uygulayan Coroutine
    private IEnumerator ApplyDamageRepeatedly()
    {
        Debug.Log("ApplyDamageRepeatedly Coroutine ba�lat�ld�. Hedef: " + currentTargetHealthSystem.gameObject.name);
        // Hedef ge�erli oldu�u ve can� oldu�u s�rece d�ng�ye devam et
        while (currentTargetHealthSystem != null && currentTargetHealthSystem.CurrentHealth > 0)
        {
            // Hasar bilgilerini olu�tur
            // Hasar�n y�n� ve vuru� noktas� bu t�r bir alan hasar� i�in daha az �nemli olabilir,
            // bu y�zden basit de�erler kullan�yoruz. Hasar veren obje bu script'in eklendi�i objedir.
            Vector3 damageDirection = (currentTargetHealthSystem.transform.position - transform.position).normalized;
            if (damageDirection == Vector3.zero) damageDirection = currentTargetHealthSystem.transform.forward; // E�er ayn� pozisyondaysalar

            DamageInfo damageInfo = new DamageInfo(
                damagePerSecond,                         // Hasar miktar�
                damageDirection,                         // Hasar�n y�n� (iste�e ba�l�)
                gameObject,                              // Hasar� veren (bu obje)
                currentTargetHealthSystem.transform.position, // Vuru� noktas� (oyuncunun merkezi)
                causeHitStopOnDamage                     // Hit stop tetiklesin mi?
            );

            // Hedefe hasar uygula
            Debug.Log(currentTargetHealthSystem.gameObject.name + " hedefine " + damagePerSecond + " hasar uygulan�yor. Mevcut Can: " + currentTargetHealthSystem.CurrentHealth);
            currentTargetHealthSystem.TakeDamage(damageInfo);

            // Hedefin can� bittiyse coroutine'i sonland�r
            if (currentTargetHealthSystem.CurrentHealth <= 0)
            {
                Debug.Log(currentTargetHealthSystem.gameObject.name + " �ld�. Hasar coroutine'i durduruluyor.");
                break; // D�ng�den ��k
            }

            // Belirtilen aral�k kadar bekle
            yield return new WaitForSeconds(damageInterval);
        }

        Debug.Log("ApplyDamageRepeatedly Coroutine sonland�. Hedef: " + (currentTargetHealthSystem != null ? currentTargetHealthSystem.gameObject.name : "NULL"));
        // Coroutine bitti�inde referanslar� temizle (OnTriggerExit zaten yapar ama g�venlik i�in)
        damageCoroutine = null;
        // currentTargetHealthSystem = null; // Bunu burada null yapmak, oyuncu �lse bile hala trigger i�indeyse OnTriggerExit'in d�zg�n �al��mas�n� engelleyebilir.
        // OnTriggerExit bu temizli�i yapmal�.
    }

    // Script devre d��� b�rak�ld���nda veya obje yok edildi�inde coroutine'i durdur
    private void OnDisable()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
            Debug.Log("DamageOverTimeArea devre d��� b�rak�ld�, hasar coroutine'i durduruldu.");
        }
        currentTargetHealthSystem = null;
    }
}
