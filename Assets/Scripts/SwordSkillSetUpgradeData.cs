// SwordSkillSetUpgradeData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordSkillSetUpgrade", menuName = "Upgrades/Sword Skill Set Upgrade")]
public class SwordSkillSetUpgradeData : UpgradeData
{
    [Header("Kýlýç Yetenek Seti Ayarlarý")]
    [Tooltip("Yeni dýþa doðru kýlýç vuruþu efekti prefabý.")]
    public GameObject newOutwardSlashPrefab;

    [Tooltip("Yeni içe doðru kýlýç vuruþu efekti prefabý.")]
    public GameObject newInwardSlashPrefab;

    public override void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController != null)
        {
            // Hem outward hem de inward null deðilse veya en az biri null deðilse loglama yapabiliriz.
            // Kullanýcý birini deðiþtirmek istemeyip null býrakabilir.
            bool applied = false;
            if (newOutwardSlashPrefab != null || newInwardSlashPrefab != null)
            {
                playerController.SetSwordSkillSetPrefabs(newOutwardSlashPrefab, newInwardSlashPrefab);
                Debug.Log($"Applying Sword Skill Set Upgrade: {upgradeName}");
                applied = true;
            }

            if (!applied)
            {
                Debug.LogWarning($"Sword Skill Set Upgrade '{upgradeName}' uygulanamadý, prefablar atanmamýþ olabilir.");
            }
        }
    }

    // Opsiyonel: Bu upgrade'in mevcut olup olmadýðýný kontrol etme
    // Örneðin, oyuncu zaten bu kýlýç setine sahipse tekrar sunulmayabilir.
    public override bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController == null) return false;
        // Bu kýlýç seti upgrade'i sadece oyuncunun mevcut silahý Kýlýç ise uygun olmalý.
        bool isSwordEquipped = playerController.GetCurrentWeaponType() == WeaponType.Sword;
        if (!isSwordEquipped) return false;

        // Opsiyonel: Zaten bu set aktifse tekrar sunma
        // bool isDifferentSet = playerController.GetCurrentOutwardSlashPrefab() != newOutwardSlashPrefab ||
        //                       playerController.GetCurrentInwardSlashPrefab() != newInwardSlashPrefab;
        // return isDifferentSet;
        return true; // Þimdilik, kýlýç varsa her zaman uygun.
    }
}