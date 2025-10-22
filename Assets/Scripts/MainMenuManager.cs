using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için bu satýrý eklemeyi unutmayýn!

public class MainMenuManager : MonoBehaviour
{
    // Oyunu baþlatacak sahnenin adýný buraya yazýn (build settings'e eklenmiþ olmalý)
    // Þimdilik "GameScene" gibi bir isim verebiliriz, daha sonra oluþturunca güncelleriz.
    public string gameSceneName = "GameScene"; // Bu ismi kendi oyun sahnenizin adýna göre deðiþtirin

    public void StartGame()
    {
        // Oyun sahnesini yükle
        // ÖNEMLÝ: "gameSceneName" adýndaki sahnenin File > Build Settings... altýnda
        // "Scenes In Build" listesine eklenmiþ olmasý gerekir.
        Debug.Log("Oyunu Baþlat butonuna týklandý! " + gameSceneName + " sahnesi yüklenecek.");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        // Þimdilik sadece bir mesaj yazdýralým.
        // Ýleride buraya ayarlar menüsünü açacak kodu ekleyebilirsiniz.
        Debug.Log("Ayarlar butonuna týklandý!");
        // Örneðin: optionsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan Çýk butonuna týklandý!");
        // Uygulamadan çýk
        // Not: Bu komut Unity Editör'de çalýþmayabilir, ancak build alýndýðýnda çalýþacaktýr.
        // Editörde test etmek için UnityEditor.EditorApplication.isPlaying = false; kullanabilirsiniz
        // ama bu sadece editöre özeldir ve build'e dahil edilmemelidir.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}