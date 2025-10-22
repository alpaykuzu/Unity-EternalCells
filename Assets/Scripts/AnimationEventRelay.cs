using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private TopDownController topDownController;
    private EnemyAI enemyAI;
    private BossAI bossAI;

    void Awake()
    {
        // Parent objelerdeki TopDownController script'ini bul
        topDownController = GetComponentInParent<TopDownController>();
        enemyAI = GetComponentInParent<EnemyAI>();
        bossAI = GetComponentInParent<BossAI>();

        if (topDownController == null)
        {
            Debug.LogError("AnimationEventRelay: Parent objelerde TopDownController script'i bulunamad�!", this.gameObject);
        }
    }

    // D��a do�ru k�l�� vuru�u efekti i�in animasyon olay� bu fonksiyonu �a��racak
    public void TriggerOutwardSlashEffect()
    {
        if (topDownController != null)
        {
            topDownController.TriggerOutwardSlashEffect();
        }
        else
        {
            Debug.LogWarning("Relay: TriggerOutwardSlashEffect �a�r�lacak TopDownController bulunamad�.", this.gameObject);
        }
    }

    // ��e do�ru k�l�� vuru�u efekti i�in animasyon olay� bu fonksiyonu �a��racak
    public void TriggerInwardSlashEffect()
    {
        if (topDownController != null)
        {
            topDownController.TriggerInwardSlashEffect();
        }
        else
        {
            Debug.LogWarning("Relay: TriggerInwardSlashEffect �a�r�lacak TopDownController bulunamad�.", this.gameObject);
        }
    }
  

    public void AnimationEvent_DealSwordDamage()
    {
        if (topDownController != null)
        {
            topDownController.AnimationEvent_DealSwordDamage();
        }
        else
        {
            Debug.LogWarning("Relay: AnimationEvent_DealSwordDamage() �a�r�lacak TopDownController bulunamad�.", this.gameObject);
        }
    }

    public void AnimationEvent_DealDamage()
    {
        if (enemyAI != null)
        {
            enemyAI.AnimationEvent_DealDamage();
        }
        else
        {
            Debug.LogWarning("Relay: AnimationEvent_DealDamage() �a�r�lacak enemyAI bulunamad�.", this.gameObject);
        }
    }
    // Gelecekte ba�ka animasyon olaylar� i�in buraya benzer fonksiyonlar ekleyebilirsiniz.
}