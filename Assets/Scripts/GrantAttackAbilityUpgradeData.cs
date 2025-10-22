// GrantAttackAbilityUpgradeData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewGrantAttackAbilityUpgrade", menuName = "Upgrades/Grant Attack Ability")]
public class GrantAttackAbilityUpgradeData : UpgradeData
{
    [Header("Saldýrý Yeteneði Verme Ayarlarý")]
    public WeaponType weaponToGrant = WeaponType.Sword; // Hangi silah türü verilecek

    [Tooltip("Birincil prefab (Ok, Büyü Mermisi, Dýþa Kýlýç Vuruþu)")]
    public GameObject abilityPrefab1;
    [Tooltip("Ýkincil prefab (Ýçe Kýlýç Vuruþu için, diðerleri için boþ olabilir)")]
    public GameObject abilityPrefab2; // Kýlýç için inward slash

    public float initialDamage = 10f;
    public float initialAttackCooldown = 0.75f;

    public override void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController != null)
        {
            playerController.EquipNewAttackAbility(weaponToGrant, abilityPrefab1, abilityPrefab2, initialDamage, initialAttackCooldown);
            Debug.Log($"Applied Grant Attack Ability: {upgradeName} - Granted: {weaponToGrant}");
        }
    }

    // Bu upgrade sadece oyuncunun hiç silahý yoksa mý sunulsun,
    // yoksa mevcut silahýný deðiþtirmek için de mi sunulsun?
    public override bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController == null) return false;

        // Seçenek 1: Sadece oyuncunun HÝÇ silahý yoksa bu upgrade'i sun.
        // return playerController.GetCurrentWeaponType() == WeaponType.None;

        // Seçenek 2: Oyuncunun mevcut silahýndan FARKLI bir silah türü veriyorsa sun.
        // Bu, oyuncunun temel silah türünü deðiþtirmesine olanak tanýr.
        // Eðer oyuncunun hiç silahý yoksa (WeaponType.None), bu da farklý sayýlacaðý için yine true döner.
        return playerController.GetCurrentWeaponType() != weaponToGrant;
    }
}