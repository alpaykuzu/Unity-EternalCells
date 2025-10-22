using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [Header("Takip Ayarlar�")]
    [Tooltip("Mini haritan�n takip edece�i oyuncu karakterinin Transform'u.")]
    public Transform playerTarget;
    [Tooltip("Mini harita kameras�n�n oyuncunun Y eksenindeki sabit y�ksekli�i.")]
    public float heightAbovePlayer = 50f;
    [Tooltip("Mini haritan�n oyuncuyla birlikte d�nmesini istiyorsan�z i�aretleyin.")]
    public bool rotateWithPlayer = false;

    private Camera minimapCamera;

    void Start()
    {
        minimapCamera = GetComponent<Camera>();
        if (minimapCamera == null)
        {
            Debug.LogError("MinimapController: Bu GameObject �zerinde bir Camera component'i bulunamad�!", this);
            enabled = false; // Script'i devre d��� b�rak
            return;
        }

        if (playerTarget == null)
        {
            // Oyuncuyu "Player" tag'i ile bulmaya �al��
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                Debug.Log("MinimapController: 'Player' tag'li oyuncu bulundu ve target olarak ayarland�.");
            }
            else
            {
                Debug.LogError("MinimapController: PlayerTarget atanmam�� ve 'Player' tag'ine sahip bir obje sahnede bulunamad�! Mini harita �al��mayacak.", this);
                enabled = false; // Script'i devre d��� b�rak
                return;
            }
        }
        // Ba�lang��ta kameran�n pozisyonunu ve rotasyonunu ayarla
        UpdateCameraPositionAndRotation();
    }

    // LateUpdate, t�m Update i�lemleri bittikten sonra �a�r�l�r.
    // Bu, karakter hareket ettikten sonra kameran�n pozisyonunu ayarlamak i�in idealdir.
    void LateUpdate()
    {
        if (playerTarget == null || minimapCamera == null)
        {
            return; // E�er hedef veya kamera yoksa bir �ey yapma
        }
        UpdateCameraPositionAndRotation();
    }

    void UpdateCameraPositionAndRotation()
    {
        // Kameran�n yeni pozisyonunu hesapla: oyuncunun x ve z pozisyonlar�, sabit y�kseklik
        Vector3 newPosition = playerTarget.position;
        newPosition.y = heightAbovePlayer; // Kameray� oyuncunun Y pozisyonundan ba��ms�z olarak sabit y�kseklikte tut

        transform.position = newPosition;

        // Mini harita kameras�n�n her zaman a�a�� bakmas�n� sa�la (X: 90).
        // E�er oyuncuyla birlikte d�nmesi isteniyorsa, Y rotasyonunu oyuncununkiyle e�itle.
        if (rotateWithPlayer)
        {
            transform.rotation = Quaternion.Euler(90f, playerTarget.eulerAngles.y, 0f);
        }
        else
        {
            // Sabit "Kuzey yukar�" g�r�n�m� i�in kameran�n Y rotasyonunu 0'da tut.
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
