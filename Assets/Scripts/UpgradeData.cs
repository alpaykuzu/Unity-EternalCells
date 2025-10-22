// UpgradeData.cs
using UnityEngine;

public abstract class UpgradeData : ScriptableObject
{
    [Header("Genel Upgrade Bilgileri")]
    public string upgradeName = "Upgrade Ad�";
    [TextArea] public string upgradeDescription = "Upgrade A��klamas�...";
    public Sprite upgradeIcon; // UI'da g�stermek i�in
    public int weight = 1; // Rastgele se�ilirken a��rl��� (daha s�k/nadir ��kmas� i�in)

    // Bu metod, se�ilen upgrade'in oyuncuya nas�l uygulanaca��n� tan�mlar.
    // Her bir �zel upgrade t�r� (can, h�z vb.) bunu kendine g�re dolduracak.
    public abstract void ApplyUpgrade(TopDownController playerController, HealthSystem healthSystem);

    // Bu upgrade'in oyuncu i�in uygun olup olmad���n� kontrol eder (opsiyonel).
    // �rne�in, zaten maksimum h�za ula�m��sa h�z upgrade'i sunulmayabilir.
    public virtual bool IsAvailable(TopDownController playerController, HealthSystem healthSystem)
    {
        return true;
    }
}