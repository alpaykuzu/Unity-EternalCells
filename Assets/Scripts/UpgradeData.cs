// UpgradeData.cs
using UnityEngine;

public abstract class UpgradeData : ScriptableObject
{
    [Header("Genel Upgrade Bilgileri")]
    public string upgradeName = "Upgrade Adý";
    [TextArea] public string upgradeDescription = "Upgrade Açýklamasý...";
    public Sprite upgradeIcon; // UI'da göstermek için
    public int weight = 1; // Rastgele seçilirken aðýrlýðý (daha sýk/nadir çýkmasý için)

    // Bu metod, seçilen upgrade'in oyuncuya nasýl uygulanacaðýný tanýmlar.
    // Her bir özel upgrade türü (can, hýz vb.) bunu kendine göre dolduracak.
    public abstract void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem);

    // Bu upgrade'in oyuncu için uygun olup olmadýðýný kontrol eder (opsiyonel).
    // Örneðin, zaten maksimum hýza ulaþmýþsa hýz upgrade'i sunulmayabilir.
    public virtual bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        return true;
    }
}