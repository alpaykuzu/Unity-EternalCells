using UnityEngine;
using UnityEngine.SceneManagement; // Sahne y�netimi i�in bu sat�r� eklemeyi unutmay�n!

public class MainMenuManager : MonoBehaviour
{
    // Oyunu ba�latacak sahnenin ad�n� buraya yaz�n (build settings'e eklenmi� olmal�)
    // �imdilik "GameScene" gibi bir isim verebiliriz, daha sonra olu�turunca g�ncelleriz.
    public string gameSceneName = "GameScene"; // Bu ismi kendi oyun sahnenizin ad�na g�re de�i�tirin

    public void StartGame()
    {
        // Oyun sahnesini y�kle
        // �NEML�: "gameSceneName" ad�ndaki sahnenin File > Build Settings... alt�nda
        // "Scenes In Build" listesine eklenmi� olmas� gerekir.
        Debug.Log("Oyunu Ba�lat butonuna t�kland�! " + gameSceneName + " sahnesi y�klenecek.");
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        // �imdilik sadece bir mesaj yazd�ral�m.
        // �leride buraya ayarlar men�s�n� a�acak kodu ekleyebilirsiniz.
        Debug.Log("Ayarlar butonuna t�kland�!");
        // �rne�in: optionsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan ��k butonuna t�kland�!");
        // Uygulamadan ��k
        // Not: Bu komut Unity Edit�r'de �al��mayabilir, ancak build al�nd���nda �al��acakt�r.
        // Edit�rde test etmek i�in UnityEditor.EditorApplication.isPlaying = false; kullanabilirsiniz
        // ama bu sadece edit�re �zeldir ve build'e dahil edilmemelidir.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}