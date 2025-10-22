using UnityEngine;
using System.Collections;

// Bu script, bir trigger alanýna giren "Player" tag'ine sahip
// karakterlere saniye baþýna hasar uygular.
// Bu script'in eklendiði GameObject'te bir Collider component'i olmalý
// ve bu Collider'ýn "Is Trigger" özelliði iþaretli olmalýdýr.
public class DamageOverTimeArea : MonoBehaviour
{
    [Header("Hasar Ayarlarý")]
    [Tooltip("Saniye baþýna uygulanacak hasar miktarý.")]
    public float damagePerSecond = 10f;

    [Tooltip("Hasar uygulandýktan sonra bir sonraki hasar için beklenecek süre (saniye).")]
    public float damageInterval = 1.0f; // Varsayýlan olarak saniyede bir hasar

    [Tooltip("Hasar verirken 'Hit Stop' efekti tetiklensin mi? Genellikle sürekli hasar için false olur.")]
    public bool causeHitStopOnDamage = false;

    // Hasar verilen oyuncunun HealthSystem component'ini ve coroutine'i saklamak için
    private HealthSystem currentTargetHealthSystem;
    private Coroutine damageCoroutine;

    // Trigger alanýna bir obje girdiðinde çaðrýlýr
    private void OnTriggerEnter(Collider other)
    {
        // Giren objenin "Player" tag'ine sahip olup olmadýðýný kontrol et
        if (other.CompareTag("Player"))
        {
            // Eðer zaten bu oyuncuya veya baþka bir oyuncuya hasar veriyorsak,
            // yeni bir coroutine baþlatma (bu senaryo genelde tek oyuncu için tasarlanýr).
            if (damageCoroutine != null)
            {
                // Ýsteðe baðlý: Eðer farklý bir oyuncu girerse ne yapýlacaðýna karar verilebilir.
                // Þimdilik, sadece ilk giren oyuncuya odaklanýyoruz.
                Debug.LogWarning("DamageOverTimeArea: Alan zaten bir oyuncuya hasar veriyor. Yeni giren '" + other.name + "' için iþlem yapýlmadý.");
                return;
            }

            // Oyuncudan HealthSystem component'ini al
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();

            if (playerHealth != null)
            {
                Debug.Log("Oyuncu '" + other.name + "' hasar alanýna girdi.");
                currentTargetHealthSystem = playerHealth;
                // Hasar verme coroutine'ini baþlat
                damageCoroutine = StartCoroutine(ApplyDamageRepeatedly());
            }
            else
            {
                Debug.LogWarning("Oyuncu '" + other.name + "' HealthSystem component'ine sahip deðil.");
            }
        }
    }

    // Trigger alanýndan bir obje çýktýðýnda çaðrýlýr
    private void OnTriggerExit(Collider other)
    {
        // Çýkan objenin "Player" tag'ine sahip olup olmadýðýný ve
        // þu anda hasar verdiðimiz hedef olup olmadýðýný kontrol et
        if (other.CompareTag("Player") && currentTargetHealthSystem != null && other.GetComponent<HealthSystem>() == currentTargetHealthSystem)
        {
            Debug.Log("Oyuncu '" + other.name + "' hasar alanýndan çýktý.");
            // Eðer aktif bir hasar coroutine'i varsa durdur
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null; // Coroutine referansýný temizle
            }
            currentTargetHealthSystem = null; // Hedef referansýný temizle
        }
    }

    // Belirli aralýklarla hedefe hasar uygulayan Coroutine
    private IEnumerator ApplyDamageRepeatedly()
    {
        Debug.Log("ApplyDamageRepeatedly Coroutine baþlatýldý. Hedef: " + currentTargetHealthSystem.gameObject.name);
        // Hedef geçerli olduðu ve caný olduðu sürece döngüye devam et
        while (currentTargetHealthSystem != null && currentTargetHealthSystem.CurrentHealth > 0)
        {
            // Hasar bilgilerini oluþtur
            // Hasarýn yönü ve vuruþ noktasý bu tür bir alan hasarý için daha az önemli olabilir,
            // bu yüzden basit deðerler kullanýyoruz. Hasar veren obje bu script'in eklendiði objedir.
            Vector3 damageDirection = (currentTargetHealthSystem.transform.position - transform.position).normalized;
            if (damageDirection == Vector3.zero) damageDirection = currentTargetHealthSystem.transform.forward; // Eðer ayný pozisyondaysalar

            DamageInfo damageInfo = new DamageInfo(
                damagePerSecond,                         // Hasar miktarý
                damageDirection,                         // Hasarýn yönü (isteðe baðlý)
                gameObject,                              // Hasarý veren (bu obje)
                currentTargetHealthSystem.transform.position, // Vuruþ noktasý (oyuncunun merkezi)
                causeHitStopOnDamage                     // Hit stop tetiklesin mi?
            );

            // Hedefe hasar uygula
            Debug.Log(currentTargetHealthSystem.gameObject.name + " hedefine " + damagePerSecond + " hasar uygulanýyor. Mevcut Can: " + currentTargetHealthSystem.CurrentHealth);
            currentTargetHealthSystem.TakeDamage(damageInfo);

            // Hedefin caný bittiyse coroutine'i sonlandýr
            if (currentTargetHealthSystem.CurrentHealth <= 0)
            {
                Debug.Log(currentTargetHealthSystem.gameObject.name + " öldü. Hasar coroutine'i durduruluyor.");
                break; // Döngüden çýk
            }

            // Belirtilen aralýk kadar bekle
            yield return new WaitForSeconds(damageInterval);
        }

        Debug.Log("ApplyDamageRepeatedly Coroutine sonlandý. Hedef: " + (currentTargetHealthSystem != null ? currentTargetHealthSystem.gameObject.name : "NULL"));
        // Coroutine bittiðinde referanslarý temizle (OnTriggerExit zaten yapar ama güvenlik için)
        damageCoroutine = null;
        // currentTargetHealthSystem = null; // Bunu burada null yapmak, oyuncu ölse bile hala trigger içindeyse OnTriggerExit'in düzgün çalýþmasýný engelleyebilir.
        // OnTriggerExit bu temizliði yapmalý.
    }

    // Script devre dýþý býrakýldýðýnda veya obje yok edildiðinde coroutine'i durdur
    private void OnDisable()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
            Debug.Log("DamageOverTimeArea devre dýþý býrakýldý, hasar coroutine'i durduruldu.");
        }
        currentTargetHealthSystem = null;
    }
}
