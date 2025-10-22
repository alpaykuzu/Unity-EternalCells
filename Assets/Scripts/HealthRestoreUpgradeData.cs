// HealthRestoreUpgradeData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewHealthRestoreUpgrade", menuName = "Upgrades/Health Restore Upgrade")]
public class HealthRestoreUpgradeData : UpgradeData
{
    [Header("Can Yenileme Ayarlarý")]
    public float healAmount;
    public bool healToFull = false;

    public override void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem)
    {
        if (healthSystem != null)
        {
            if (healToFull)
            {
                healthSystem.Heal(healthSystem.MaxHealth); // Tam cana iyileþtir
                Debug.Log($"Applying Health Restore: {upgradeName} - Healed to full.");
            }
            else
            {
                healthSystem.Heal(healAmount);
                Debug.Log($"Applying Health Restore: {upgradeName} - Healed for {healAmount}.");
            }
        }
    }
}