// GrantAttackAbilityUpgradeData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewGrantAttackAbilityUpgrade", menuName = "Upgrades/Grant Attack Ability")]
public class GrantAttackAbilityUpgradeData : UpgradeData
{
    [Header("Sald�r� Yetene�i Verme Ayarlar�")]
    public WeaponType weaponToGrant = WeaponType.Sword; // Hangi silah t�r� verilecek

    [Tooltip("Birincil prefab (Ok, B�y� Mermisi, D��a K�l�� Vuru�u)")]
    public GameObject abilityPrefab1;
    [Tooltip("�kincil prefab (��e K�l�� Vuru�u i�in, di�erleri i�in bo� olabilir)")]
    public GameObject abilityPrefab2; // K�l�� i�in inward slash

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

    // Bu upgrade sadece oyuncunun hi� silah� yoksa m� sunulsun,
    // yoksa mevcut silah�n� de�i�tirmek i�in de mi sunulsun?
    public override bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        if (playerController == null) return false;

        // Se�enek 1: Sadece oyuncunun H�� silah� yoksa bu upgrade'i sun.
        // return playerController.GetCurrentWeaponType() == WeaponType.None;

        // Se�enek 2: Oyuncunun mevcut silah�ndan FARKLI bir silah t�r� veriyorsa sun.
        // Bu, oyuncunun temel silah t�r�n� de�i�tirmesine olanak tan�r.
        // E�er oyuncunun hi� silah� yoksa (WeaponType.None), bu da farkl� say�laca�� i�in yine true d�ner.
        return playerController.GetCurrentWeaponType() != weaponToGrant;
    }
}