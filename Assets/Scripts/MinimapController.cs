using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [Header("Takip Ayarlarý")]
    [Tooltip("Mini haritanýn takip edeceði oyuncu karakterinin Transform'u.")]
    public Transform playerTarget;
    [Tooltip("Mini harita kamerasýnýn oyuncunun Y eksenindeki sabit yüksekliði.")]
    public float heightAbovePlayer = 50f;
    [Tooltip("Mini haritanýn oyuncuyla birlikte dönmesini istiyorsanýz iþaretleyin.")]
    public bool rotateWithPlayer = false;

    private Camera minimapCamera;

    void Start()
    {
        minimapCamera = GetComponent<Camera>();
        if (minimapCamera == null)
        {
            Debug.LogError("MinimapController: Bu GameObject üzerinde bir Camera component'i bulunamadý!", this);
            enabled = false; // Script'i devre dýþý býrak
            return;
        }

        if (playerTarget == null)
        {
            // Oyuncuyu "Player" tag'i ile bulmaya çalýþ
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                Debug.Log("MinimapController: 'Player' tag'li oyuncu bulundu ve target olarak ayarlandý.");
            }
            else
            {
                Debug.LogError("MinimapController: PlayerTarget atanmamýþ ve 'Player' tag'ine sahip bir obje sahnede bulunamadý! Mini harita çalýþmayacak.", this);
                enabled = false; // Script'i devre dýþý býrak
                return;
            }
        }
        // Baþlangýçta kameranýn pozisyonunu ve rotasyonunu ayarla
        UpdateCameraPositionAndRotation();
    }

    // LateUpdate, tüm Update iþlemleri bittikten sonra çaðrýlýr.
    // Bu, karakter hareket ettikten sonra kameranýn pozisyonunu ayarlamak için idealdir.
    void LateUpdate()
    {
        if (playerTarget == null || minimapCamera == null)
        {
            return; // Eðer hedef veya kamera yoksa bir þey yapma
        }
        UpdateCameraPositionAndRotation();
    }

    void UpdateCameraPositionAndRotation()
    {
        // Kameranýn yeni pozisyonunu hesapla: oyuncunun x ve z pozisyonlarý, sabit yükseklik
        Vector3 newPosition = playerTarget.position;
        newPosition.y = heightAbovePlayer; // Kamerayý oyuncunun Y pozisyonundan baðýmsýz olarak sabit yükseklikte tut

        transform.position = newPosition;

        // Mini harita kamerasýnýn her zaman aþaðý bakmasýný saðla (X: 90).
        // Eðer oyuncuyla birlikte dönmesi isteniyorsa, Y rotasyonunu oyuncununkiyle eþitle.
        if (rotateWithPlayer)
        {
            transform.rotation = Quaternion.Euler(90f, playerTarget.eulerAngles.y, 0f);
        }
        else
        {
            // Sabit "Kuzey yukarý" görünümü için kameranýn Y rotasyonunu 0'da tut.
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
