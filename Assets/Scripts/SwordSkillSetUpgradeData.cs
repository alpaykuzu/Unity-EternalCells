// SwordSkillSetUpgradeData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordSkillSetUpgrade", menuName = "Upgrades/Sword Skill Set Upgrade")]
public class SwordSkillSetUpgradeData : UpgradeData
{
    [Header("K�l�� Yetenek Seti Ayarlar�")]
    [Tooltip("Yeni d��a do�ru k�l�� vuru�u efekti prefab�.")]
    public GameObject newOutwardSlashPrefab;

    [Tooltip("Yeni i�e do�ru k�l�� vuru�u efekti prefab�.")]
    public GameObject newInwardSlashPrefab;

    public override void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController != null)
        {
            // Hem outward hem de inward null de�ilse veya en az biri null de�ilse loglama yapabiliriz.
            // Kullan�c� birini de�i�tirmek istemeyip null b�rakabilir.
            bool applied = false;
            if (newOutwardSlashPrefab != null || newInwardSlashPrefab != null)
            {
                playerController.SetSwordSkillSetPrefabs(newOutwardSlashPrefab, newInwardSlashPrefab);
                Debug.Log($"Applying Sword Skill Set Upgrade: {upgradeName}");
                applied = true;
            }

            if (!applied)
            {
                Debug.LogWarning($"Sword Skill Set Upgrade '{upgradeName}' uygulanamad�, prefablar atanmam�� olabilir.");
            }
        }
    }

    // Opsiyonel: Bu upgrade'in mevcut olup olmad���n� kontrol etme
    // �rne�in, oyuncu zaten bu k�l�� setine sahipse tekrar sunulmayabilir.
    public override bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController == null) return false;
        // Bu k�l�� seti upgrade'i sadece oyuncunun mevcut silah� K�l�� ise uygun olmal�.
        bool isSwordEquipped = playerController.GetCurrentWeaponType() == WeaponType.Sword;
        if (!isSwordEquipped) return false;

        // Opsiyonel: Zaten bu set aktifse tekrar sunma
        // bool isDifferentSet = playerController.GetCurrentOutwardSlashPrefab() != newOutwardSlashPrefab ||
        //                       playerController.GetCurrentInwardSlashPrefab() != newInwardSlashPrefab;
        // return isDifferentSet;
        return true; // �imdilik, k�l�� varsa her zaman uygun.
    }
}