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
            Debug.LogError("AnimationEventRelay: Parent objelerde TopDownController script'i bulunamadý!", this.gameObject);
        }
    }

    // Dýþa doðru kýlýç vuruþu efekti için animasyon olayý bu fonksiyonu çaðýracak
    public void TriggerOutwardSlashEffect()
    {
        if (topDownController != null)
        {
            topDownController.TriggerOutwardSlashEffect();
        }
        else
        {
            Debug.LogWarning("Relay: TriggerOutwardSlashEffect çaðrýlacak TopDownController bulunamadý.", this.gameObject);
        }
    }

    // Ýçe doðru kýlýç vuruþu efekti için animasyon olayý bu fonksiyonu çaðýracak
    public void TriggerInwardSlashEffect()
    {
        if (topDownController != null)
        {
            topDownController.TriggerInwardSlashEffect();
        }
        else
        {
            Debug.LogWarning("Relay: TriggerInwardSlashEffect çaðrýlacak TopDownController bulunamadý.", this.gameObject);
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
            Debug.LogWarning("Relay: AnimationEvent_DealSwordDamage() çaðrýlacak TopDownController bulunamadý.", this.gameObject);
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
            Debug.LogWarning("Relay: AnimationEvent_DealDamage() çaðrýlacak enemyAI bulunamadý.", this.gameObject);
        }
    }
    // Gelecekte baþka animasyon olaylarý için buraya benzer fonksiyonlar ekleyebilirsiniz.
}