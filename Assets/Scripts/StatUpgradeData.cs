// StatUpgradeData.cs
using UnityEngine;

public enum StatTypeToUpgrade
{
    MaxHealth,
    MoveSpeed,
    DodgeCooldown, // Saniye cinsinden azaltma (pozitif de�er girilir)
    SwordDamage,
    BowDamage,
    MagicDamage
    // �leride eklenebilir: AttackSpeed, CritChance, etc.
}

[CreateAssetMenu(fileName = "NewStatUpgrade", menuName = "Upgrades/Stat Upgrade")]
public class StatUpgradeData : UpgradeData
{
    [Header("Stat Upgrade Ayarlar�")]
    public StatTypeToUpgrade statType;
    public float S_valueToAdd; // Can, Hasar, H�z i�in eklenecek miktar
    public float S_valueToSet; // Direkt ayarlanacak de�er (opsiyonel, baz� statlar i�in)
    public bool S_isPercentage; // De�er y�zde olarak m� uygulanacak? (H�z i�in %10 art�� gibi)

    public override void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem)
    {
        Debug.Log($"Applying Stat Upgrade: {upgradeName} - Stat: {statType}, Value: {S_valueToAdd}");
        switch (statType)
        {
            case StatTypeToUpgrade.MaxHealth:
                if (healthSystem != null)
                {
                    healthSystem.IncreaseMaxHealth(S_valueToAdd);
                }
                break;
            case StatTypeToUpgrade.MoveSpeed:
                if (playerController != null)
                {
                    playerController.ModifyMoveSpeed(S_valueToAdd, S_isPercentage);
                }
                break;
            case StatTypeToUpgrade.DodgeCooldown:
                if (playerController != null)
                {
                    // Cooldown azaltma genellikle pozitif bir de�erle ifade edilir
                    playerController.ModifyDodgeCooldown(-S_valueToAdd); // De�eri negatif yaparak azalt
                }
                break;
            case StatTypeToUpgrade.SwordDamage:
                if (playerController != null)
                {
                    playerController.ModifyWeaponDamage(WeaponType.Sword, S_valueToAdd, S_isPercentage);
                }
                break;
            case StatTypeToUpgrade.BowDamage:
                if (playerController != null)
                {
                    playerController.ModifyWeaponDamage(WeaponType.Bow, S_valueToAdd, S_isPercentage);
                }
                break;
            case StatTypeToUpgrade.MagicDamage:
                if (playerController != null)
                {
                    playerController.ModifyWeaponDamage(WeaponType.Magic, S_valueToAdd, S_isPercentage);
                }
                break;
        }
    }
    public override bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController == null) return true; // Genel statlar i�in belki her zaman true
        switch (statType)
        {
            case StatTypeToUpgrade.SwordDamage:
                return playerController.GetCurrentWeaponType() == WeaponType.Sword;
            case StatTypeToUpgrade.BowDamage:
                return playerController.GetCurrentWeaponType() == WeaponType.Bow;
            case StatTypeToUpgrade.MagicDamage:
                return playerController.GetCurrentWeaponType() == WeaponType.Magic;
            // MaxHealth, MoveSpeed, DodgeCooldown her zaman uygun olabilir.
            default:
                return true;
        }
    }
}