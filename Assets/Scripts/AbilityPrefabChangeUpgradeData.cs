// AbilityPrefabChangeUpgradeData.cs
using UnityEngine;

public enum AbilityToModify
{
    SwordOutwardSlash,
    SwordInwardSlash,
    BowArrow,
    MagicProjectile
}

[CreateAssetMenu(fileName = "NewAbilityPrefabChange", menuName = "Upgrades/Ability Prefab Change")]
public class AbilityPrefabChangeUpgradeData : UpgradeData
{
    [Header("Yetenek Prefab Deðiþtirme Ayarlarý")]
    public AbilityToModify abilityType;
    public GameObject newPrefab; // Deðiþtirilecek yeni prefab

    public override void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController != null && newPrefab != null)
        {
            Debug.Log($"Applying Ability Prefab Change: {upgradeName} - Ability: {abilityType}");
            switch (abilityType)
            {
                case AbilityToModify.SwordOutwardSlash:
                    playerController.SetSwordOutwardSlashPrefab(newPrefab);
                    break;
                case AbilityToModify.SwordInwardSlash:
                    playerController.SetSwordInwardSlashPrefab(newPrefab);
                    break;
                case AbilityToModify.BowArrow:
                    playerController.SetArrowPrefab(newPrefab);
                    break;
                case AbilityToModify.MagicProjectile:
                    playerController.SetMagicProjectilePrefab(newPrefab);
                    break;
            }
        }
    }
    public override bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController == null) return false;
        switch (abilityType)
        {
            case AbilityToModify.BowArrow:
                return playerController.GetCurrentWeaponType() == WeaponType.Bow;
            case AbilityToModify.MagicProjectile:
                return playerController.GetCurrentWeaponType() == WeaponType.Magic;
            default:
                return false;
        }
    }
}