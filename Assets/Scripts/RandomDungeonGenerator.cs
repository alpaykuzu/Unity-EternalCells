// RandomDungeonGenerator.cs
// Defines and generates the dungeon layout, rooms, corridors, decorations, enemies, and chests.
// Handles room separation, enemy spawning logic, and initial setup for room/corridor activation.
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI; // NavMesh.SamplePosition için
using Unity.AI.Navigation; // NavMeshSurface için

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Unity sahnesinde bir baþlangýç odasýndan baþlayýp, dallanarak bir boss odasýna ulaþan
/// rastgele odalar ve koridorlardan oluþan bir zindan haritasý oluþturur.
/// Odalar arasý aktivasyon ve her odaya düþman spawn etme özelliði eklendi.
/// </summary>
public class RandomDungeonGenerator : MonoBehaviour
{
    [System.Serializable]
    public class DungeonRoomContainer
    {
        public RectInt Rect;
        public GameObject ContainerGO;
        public RoomController Controller;
        public List<DungeonCorridorContainer> ConnectedCorridors = new List<DungeonCorridorContainer>();
        public bool IsStartRoom = false;
        public bool IsBossRoom = false;
        public bool IsGatewayRoom = false; // Geçit odasý mý?

        public DungeonRoomContainer(RectInt rect, GameObject go, RoomController controller, bool isStart = false, bool isBoss = false, bool isGateway = false)
        {
            Rect = rect;
            ContainerGO = go;
            Controller = controller;
            IsStartRoom = isStart;
            IsBossRoom = isBoss;
            IsGatewayRoom = isGateway;
        }
    }

    [System.Serializable]
    public class DungeonCorridorContainer
    {
        public GameObject ContainerGO;
        public DungeonRoomContainer RoomA;
        public DungeonRoomContainer RoomB;
        public List<Vector2Int> Tiles = new List<Vector2Int>(); // Koridoru oluþturan tile koordinatlarý

        public DungeonCorridorContainer(GameObject go, DungeonRoomContainer roomA, DungeonRoomContainer roomB)
        {
            ContainerGO = go;
            RoomA = roomA;
            RoomB = roomB;
        }
    }


    [Header("Harita Genel Ayarlarý")]
    [Tooltip("Haritanýn X eksenindeki KARE sayýsý (geniþlik).")]
    public int mapWidth = 70;
    [Tooltip("Haritanýn Z eksenindeki KARE sayýsý (yükseklik).")]
    public int mapHeight = 70;
    [Tooltip("Bir karenin (tile asset'inin) dünya birimi cinsinden kenar uzunluðu.")]
    public float tileSize = 4.0f;
    [Tooltip("Koridorlarýn kare cinsinden geniþliði.")]
    public int corridorThickness = 2;
    [Tooltip("Odalar arasýnda býrakýlacak minimum boþluk (kare cinsinden, koridor hariç).")]
    public int minRoomSeparationBuffer = 2;


    [Header("Oda Ayarlarý")]
    [Tooltip("Oluþturulacak maksimum oda sayýsý (baþlangýç ve boss odasý dahil).")]
    public int maxRooms = 20;
    [Tooltip("Bir odanýn minimum geniþlik/yüksekliði (kare sayýsý). Balkonlar için en az 5x5 önerilir.")]
    public int minRoomSize = 6;
    [Tooltip("Bir odanýn maksimum geniþlik/yüksekliði (kare sayýsý).")]
    public int maxRoomSize = 12;

    [Header("Baþlangýç Odasý Ayarlarý")]
    [Tooltip("Baþlangýç odasýnýn haritadaki X, Z konumu (sol alt köþe).")]
    public Vector2Int startRoomPosition = new Vector2Int(5, 30);
    [Tooltip("Baþlangýç odasýnýn geniþliði ve yüksekliði (kare sayýsý).")]
    public Vector2Int startRoomSize = new Vector2Int(8, 8);

    [Header("Boss Odasý Ayarlarý")]
    [Tooltip("Boss odasýnýn tercih edilen geniþliði ve yüksekliði (kare sayýsý).")]
    public Vector2Int bossRoomPreferredSize = new Vector2Int(12, 12);
    [Header("Boss Room Teleport Settings")] // NEW SECTION
    [Tooltip("Boss odasýnýn ortasýna yerleþtirilecek teleport prefabý.")]
    public GameObject bossRoomTeleportPrefab;
    [Tooltip("Teleportun Y eksenindeki konumu (zemin seviyesine göre offset).")]
    public float bossRoomTeleportYOffset = 0.5f;

    [Header("Yol Oluþturma ve Ýzolasyon Ayarlarý")]
    [Tooltip("Baþlangýç ve Boss odasý arasýnda oluþturulacak ana yol üzerindeki ek oda sayýsý.")]
    public int numRoomsOnMainPath = 3;
    [Tooltip("Oluþturulacak maksimum yan dal sayýsý.")]
    public int numSideBranches = 8;
    [Tooltip("Bir yan dalýn maksimum derinliði (oda sayýsý).")]
    public int maxSideBranchDepth = 3;
    [Tooltip("Her bir oda yerleþtirme adýmýnda yapýlacak maksimum deneme sayýsý.")]
    public int maxPlacementAttemptsPerStep = 30;
    [Tooltip("Baþlangýç/Boss odalarý ile ana zindan arasýndaki geçit odalarýnýn minimum uzaklýðý (kare sayýsý).")]
    public int minIsolationCorridorRoomDistance = 8;
    [Tooltip("Baþlangýç/Boss odalarý ile ana zindan arasýndaki geçit odalarýnýn maksimum uzaklýðý (kare sayýsý).")]
    public int maxIsolationCorridorRoomDistance = 12;


    [Header("Asset Prefablarý")]
    [Tooltip("Prefablarý þu sýrayla atayýn:\n[0]: Köþe (Varsayýlan yönü: yerel -X ve -Z eksenlerinde duvarlarý var)\n[1]: Kenar/Duvar (Varsayýlan yönü: yerel -X ekseninde duvarý var)\n[2]: Zemin (Duvarsýz)")]
    public GameObject[] tilePrefabs;

    [Header("Özel Zemin Ayarlarý")]
    [Tooltip("Bazý zeminlerin yerine yerleþtirilecek içi boþluk asset'i (Opsiyonel). Baþlangýç ve Boss odalarýnda kullanýlmaz.")]
    public GameObject emptySpacePrefab;
    [Tooltip("Bir zemin karosunun 'içi boþluk asset'i' ile deðiþtirilme olasýlýðý (0.0 ile 1.0 arasý). 0 ise hiç kullanýlmaz.")]
    [Range(0f, 1f)]
    public float emptySpaceProbability = 0.0f;

    [Header("Dekoratif Kaya Ayarlarý")]
    public GameObject[] rockPrefabs;
    [Range(0f, 1f)]
    public float rockPlacementProbability = 0.0f;

    [Header("Dekoratif Balkon Ayarlarý")]
    public GameObject balconyPrefab;
    [Range(0f, 1f)]
    public float balconyPlacementProbability = 0.0f;
    public float balconyYOffset = 0.0f;
    public int balconyFootprintSize = 3;

    [Header("Ýkincil Dekoratif Balkon Ayarlarý")]
    public GameObject secondaryBalconyPrefab;
    [Range(0f, 1f)]
    public float secondaryBalconyPlacementProbability = 0.0f;
    public float secondaryBalconyYOffset = 0.0f;
    public int secondaryBalconyFootprintSize = 3;


    [Header("Dekoratif Heykel Ayarlarý")]
    public GameObject[] statuePrefabs;
    [Range(0f, 1f)]
    public float statuePlacementProbability = 0.0f;
    public float statueYOffset = 0.0f;

    [Header("Dekoratif Mum Ayarlarý")]
    public GameObject[] candlePrefabs;
    [Range(0f, 1f)]
    public float candlePlacementProbability = 0.0f;
    public float candleYOffset = 0.0f;

    [Header("Dekoratif Ivýr Zývýr Ayarlarý")]
    public GameObject[] clutterPrefabs;
    [Range(0f, 1f)]
    public float clutterPlacementProbability = 0.0f;
    public float clutterYOffset = 0.0f;

    [Header("Köþe Rotasyon Ayarlarý (Inspector'dan Düzenlenebilir)")]
    public Vector3 rotationBottomLeftCorner = new Vector3(0, 0, 0);
    public Vector3 rotationBottomRightCorner = new Vector3(0, 90, 0);
    public Vector3 rotationTopLeftCorner = new Vector3(0, 270, 0);
    public Vector3 rotationTopRightCorner = new Vector3(0, 180, 0);

    [Header("Lav Ayarlarý")]
    [Tooltip("Haritanýn altýna yerleþtirilecek lav düzlemi prefabý. Varsayýlan Unity Plane (10x10 birim) veya bu boyutlarda bir prefab olmalý.")]
    public GameObject lavaPlanePrefab;
    [Tooltip("Lav düzleminin Y eksenindeki konumu (zemin seviyesine göre offset). Örn: -4.")]
    public float lavaYOffset = -4.0f;
    [Tooltip("Lav düzleminin harita boyutlarýna göre ekstra ölçeklendirme çarpaný (örn: 1.5 = %50 daha büyük). Kameranýn boþluk görmemesi için 1.0'den büyük olmalý.")]
    public float lavaExtraScale = 1.5f;

    [Header("Hierarchy Düzeni ve NavMesh")]
    [Tooltip("Oluþturulan tüm harita objelerinin altýna yerleþtirileceði ana Transform. NavMeshSurface component'i bu objede olmalý.")]
    public Transform mapParent;
    [Tooltip("Runtime'da NavMesh oluþturmak için kullanýlacak NavMeshSurface component'i. Genellikle 'mapParent' objesine eklenir.")]
    public NavMeshSurface navMeshSurface;

    [Header("Düþman Spawn Ayarlarý")]
    [Tooltip("Varsayýlan düþman prefabý (örneðin Ýskelet).")]
    public GameObject skeletonPrefab;
    [Tooltip("Örümcek düþman prefabý.")]
    public GameObject spiderPrefab;
    [Tooltip("Spawn noktalarýný sahnede göstermek için opsiyonel prefab (Düþman prefablarýndan ayrý).")]
    public GameObject enemySpawnMarkerPrefab;

    [Header("Kristal Düþman Ayarlarý (Opsiyonel)")]
    [Tooltip("Kristal düþman prefabý - Variant A.")]
    public GameObject crystalEnemyPrefabA;
    [Tooltip("Kristal düþman prefabý - Variant B.")]
    public GameObject crystalEnemyPrefabB;
    [Tooltip("Kristal düþman prefabý - Variant C.")]
    public GameObject crystalEnemyPrefabC;
    [Tooltip("Kristal düþmanlarýn spawn olurken yerden yükseklik offset'i.")]
    public float crystalEnemyYOffset = 0.5f;


    [Tooltip("Harita baþýna toplam maksimum düþman spawn noktasý.")]
    public int maxTotalEnemySpawns = 30;
    [Tooltip("Oda baþýna minimum düþman spawn noktasý (Baþlangýç/Boss hariç).")]
    public int minSpawnsPerRoom = 1;
    [Tooltip("Oda baþýna maksimum düþman spawn noktasý.")]
    public int maxSpawnsPerRoom = 3;
    [Tooltip("Ýki spawn noktasý arasýndaki minimum dünya birimi cinsinden mesafe.")]
    public float minDistanceBetweenSpawns = 3.0f;
    [Tooltip("Spawn noktalarýnýn dekorasyon objelerine minimum karo cinsinden mesafesi.")]
    public int minTileDistanceFromDecor = 1;
    [Tooltip("Bir spawn noktasýnýn NavMesh üzerinde geçerli bir yere konulmasý için arama yarýçapý.")]
    public float spawnPointNavMeshSampleRadius = 1.0f;

    [Header("Düþman Türü Olasýlýklarý")]
    [Tooltip("Standart bir odada örümcek spawn olma olasýlýðý.")]
    [Range(0f, 1f)] public float spiderChanceInStandardRoom = 0.3f;
    [Tooltip("Standart bir odada Kristal Düþman spawn olma olasýlýðý.")]
    [Range(0f, 1f)] public float crystalEnemyChanceInStandardRoom = 0.2f;
    [Tooltip("Bir koridorda örümcek spawn olma olasýlýðý (Þu an için koridorlarda spawn yok).")]
    [Range(0f, 1f)] public float spiderChanceInCorridor = 0.5f;


    [Header("Ateþ Efekti Ayarlarý")]
    [Tooltip("Yerleþtirilecek ateþ efekti prefabý.")]
    public GameObject fireEffectPrefab;
    [Tooltip("Bir zemin karosuna ateþ yerleþtirilme olasýlýðý (0.0 ile 1.0 arasý).")]
    [Range(0f, 1f)]
    public float firePlacementProbability = 0.05f;
    [Tooltip("Ateþ efektinin Y eksenindeki konumu (zemin seviyesine göre offset).")]
    public float fireYOffset = 0.1f;
    [Tooltip("Bir ateþ yerleþtirildiðinde, küme oluþturmaya çalýþma olasýlýðý.")]
    [Range(0f, 1f)]
    public float tryToFormClusterProbability = 0.5f;
    [Tooltip("Bir kümede olabilecek minimum ateþ sayýsý (ilk ateþ dahil).")]
    public int minFiresInCluster = 2;
    [Tooltip("Bir kümede olabilecek maksimum ateþ sayýsý (ilk ateþ dahil).")]
    public int maxFiresInCluster = 3;
    [Tooltip("Baþlangýç odasýnda ateþ spawn edilsin mi?")]
    public bool canFireInStartRoom = false;
    [Tooltip("Boss odasýnda ateþ spawn edilsin mi?")]
    public bool canFireInBossRoom = false;

    [Header("Zehirli Zemin Ayarlarý")]
    [Tooltip("Yerleþtirilecek zehirli zemin efekti prefabý.")]
    public GameObject toxicGroundPrefab;
    [Tooltip("Bir zemin karosuna zehirli zemin yerleþtirilme olasýlýðý (0.0 ile 1.0 arasý).")]
    [Range(0f, 1f)]
    public float toxicGroundPlacementProbability = 0.05f;
    [Tooltip("Zehirli zemin efektinin Y eksenindeki konumu (zemin seviyesine göre offset).")]
    public float toxicGroundYOffset = 0.05f; // Ateþten biraz farklý bir offset olabilir
    [Tooltip("Zehirli zemin yerleþtirildiðinde, küme oluþturmaya çalýþma olasýlýðý.")]
    [Range(0f, 1f)]
    public float tryToFormToxicClusterProbability = 0.4f; // Ateþten farklý deðerler olabilir
    [Tooltip("Bir zehirli zemin kümesinde olabilecek minimum eleman sayýsý (ilk dahil).")]
    public int minToxicGroundsInCluster = 2;
    [Tooltip("Bir zehirli zemin kümesinde olabilecek maksimum eleman sayýsý (ilk dahil).")]
    public int maxToxicGroundsInCluster = 4;
    [Tooltip("Baþlangýç odasýnda zehirli zemin spawn edilsin mi?")]
    public bool canToxicGroundInStartRoom = false;
    [Tooltip("Boss odasýnda zehirli zemin spawn edilsin mi?")]
    public bool canToxicGroundInBossRoom = false;

    [Header("Upgrade Sandýðý Ayarlarý")]
    [Tooltip("Yerleþtirilecek upgrade sandýðý prefabý.")]
    public GameObject upgradeChestPrefab;
    [Tooltip("Bir odaya upgrade sandýðý yerleþtirilme olasýlýðý (0.0 ile 1.0 arasý). Baþlangýç ve Boss odalarýnda kullanýlmaz.")]
    [Range(0f, 1f)]
    public float upgradeChestProbabilityPerRoom = 0.1f;
    [Tooltip("Upgrade sandýðýnýn Y eksenindeki konumu (zemin seviyesine göre offset).")]
    public float upgradeChestYOffset = 0.5f;


    private int[,] _mapData;
    private List<DungeonRoomContainer> _dungeonRooms = new List<DungeonRoomContainer>();
    private List<DungeonCorridorContainer> _dungeonCorridors = new List<DungeonCorridorContainer>();
    private HashSet<Vector2Int> _decorOccupiedTiles = new HashSet<Vector2Int>();

    private DungeonRoomContainer _generatedStartRoomContainer;
    private DungeonRoomContainer _generatedBossRoomContainer;
    private List<Vector3> _enemySpawnPositionsGizmo = new List<Vector3>();

    private Dictionary<Vector2Int, DungeonRoomContainer> _tileToRoomLookup = new Dictionary<Vector2Int, DungeonRoomContainer>();
    private Dictionary<Vector2Int, DungeonCorridorContainer> _tileToCorridorLookup = new Dictionary<Vector2Int, DungeonCorridorContainer>();


    void Start()
    {
        if (Application.isPlaying)
        {
            if (!ValidateSettings()) { enabled = false; return; }
            GenerateDungeon();
        }
    }

    public void GenerateDungeon()
    {
        SetupParentAndNavMeshSurface();
        ClearMap();
        InitializeMapData();

        GenerateRoomsAndCorridors();

        if (_dungeonRooms.Count < 2)
        {
            Debug.LogError("Yeterli oda oluþturulamadý (en az baþlangýç ve boss odasý gerekir). Zindan oluþturma iptal edildi.", gameObject);
            return;
        }

        PopulateMapDataFromContainers();
        ExpandFloorAtEdges();
        RefinedPostProcessMapData();
        InstantiateTilesFromMapData();
        PlaceAllDecorations();
        PlaceUpgradeChests();
        PlaceFireEffects();
        PlaceToxicGroundEffects();
        PlaceLavaPlane();
        PlaceBossRoomTeleport();
        BakeDungeonNavMesh();
        PlaceEnemySpawnPoints();
        SetInitialActivationStates(); // Düþmanlar yerleþtirildikten ve NavMesh bake edildikten sonra çaðýr.

        Debug.Log($"Zindan oluþturma tamamlandý. Toplam Oda: {_dungeonRooms.Count}, Toplam Koridor: {_dungeonCorridors.Count}", gameObject);
    }

    void PopulateMapDataFromContainers()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                _mapData[x, z] = 0;
            }
        }

        foreach (var roomContainer in _dungeonRooms)
        {
            CarveAreaInMapData(roomContainer.Rect, 1, roomContainer, null);
        }
        foreach (var corridorContainer in _dungeonCorridors)
        {
            foreach (var tilePos in corridorContainer.Tiles)
            {
                if (IsInBounds(tilePos.x, tilePos.y))
                {
                    _mapData[tilePos.x, tilePos.y] = 1;
                    _tileToCorridorLookup[tilePos] = corridorContainer;
                }
            }
        }
    }


    void InitializeMapData()
    {
        _mapData = new int[mapWidth, mapHeight];
        _dungeonRooms.Clear();
        _dungeonCorridors.Clear();
        _decorOccupiedTiles.Clear();
        _enemySpawnPositionsGizmo.Clear();
        _tileToRoomLookup.Clear();
        _tileToCorridorLookup.Clear();
    }

    void GenerateRoomsAndCorridors()
    {
        _generatedStartRoomContainer = PlaceStartRoom();
        if (_generatedStartRoomContainer == null) { Debug.LogError("Baþlangýç odasý oluþturulamadý!"); return; }
        _dungeonRooms.Add(_generatedStartRoomContainer);

        List<RectInt> existingRectsForOverlap = new List<RectInt> { _generatedStartRoomContainer.Rect };

        _generatedBossRoomContainer = PlaceBossRoom(existingRectsForOverlap);
        if (_generatedBossRoomContainer == null) { Debug.LogError("Boss odasý oluþturulamadý!"); return; }
        _dungeonRooms.Add(_generatedBossRoomContainer);
        existingRectsForOverlap.Add(_generatedBossRoomContainer.Rect);


        DungeonRoomContainer mainPathStartNode = _generatedStartRoomContainer;
        DungeonRoomContainer mainPathEndNode = _generatedBossRoomContainer;

        bool dungeonHasIntermediateStructure = numRoomsOnMainPath > 0 || numSideBranches > 0;

        if (dungeonHasIntermediateStructure)
        {
            RectInt gatewayRectFromStart = TryPlaceDistantRoom(_generatedStartRoomContainer.Rect, Vector2Int.RoundToInt(_generatedBossRoomContainer.Rect.center),
                                      minIsolationCorridorRoomDistance, maxIsolationCorridorRoomDistance,
                                      existingRectsForOverlap, true);
            if (gatewayRectFromStart.width > 0)
            {
                DungeonRoomContainer gatewayFromStart = CreateDungeonRoomContainer(gatewayRectFromStart, "GatewayFromStart_", true);
                _dungeonRooms.Add(gatewayFromStart);
                existingRectsForOverlap.Add(gatewayRectFromStart);
                ConnectTwoDungeonRooms(_generatedStartRoomContainer, gatewayFromStart, corridorThickness);
                mainPathStartNode = gatewayFromStart;
            }
            else Debug.LogWarning("Baþlangýç için uzak geçit odasý yerleþtirilemedi.", gameObject);
        }

        if (dungeonHasIntermediateStructure)
        {
            Vector2Int directionTargetForBossGateway = Vector2Int.RoundToInt(mainPathStartNode.Rect.center);
            if (mainPathStartNode == _generatedStartRoomContainer)
            {
                directionTargetForBossGateway = new Vector2Int(mapWidth / 2, mapHeight / 2);
            }

            RectInt gatewayRectToBoss = TryPlaceDistantRoom(_generatedBossRoomContainer.Rect, directionTargetForBossGateway,
                                    minIsolationCorridorRoomDistance, maxIsolationCorridorRoomDistance,
                                    existingRectsForOverlap, true);
            if (gatewayRectToBoss.width > 0)
            {
                DungeonRoomContainer gatewayToBoss = CreateDungeonRoomContainer(gatewayRectToBoss, "GatewayToBoss_", true);
                _dungeonRooms.Add(gatewayToBoss);
                existingRectsForOverlap.Add(gatewayRectToBoss);
                mainPathEndNode = gatewayToBoss;
            }
            else Debug.LogWarning("Boss için uzak geçit odasý yerleþtirilemedi.", gameObject);
        }

        if (mainPathStartNode != mainPathEndNode && numRoomsOnMainPath > 0)
        {
            CreateMainPath(mainPathStartNode, mainPathEndNode, existingRectsForOverlap);
        }
        else if (mainPathStartNode != mainPathEndNode)
        {
            ConnectTwoDungeonRooms(mainPathStartNode, mainPathEndNode, corridorThickness);
        }

        if (mainPathEndNode != _generatedBossRoomContainer && !AreRoomsConnected(mainPathEndNode, _generatedBossRoomContainer)) // Avoid double connection
        {
            ConnectTwoDungeonRooms(mainPathEndNode, _generatedBossRoomContainer, corridorThickness);
        }


        if (numSideBranches > 0 && _dungeonRooms.Count < maxRooms)
        {
            List<DungeonRoomContainer> potentialBranchParents = _dungeonRooms
              .Where(dr => !dr.IsStartRoom && !dr.IsBossRoom)
              .ToList();

            if (potentialBranchParents.Count == 0 && _dungeonRooms.Count > 2)
            {
                potentialBranchParents = _dungeonRooms.Where(dr => dr.IsGatewayRoom && !dr.IsStartRoom && !dr.IsBossRoom).ToList();
            }

            if (potentialBranchParents.Count > 0)
            {
                CreateSideBranches(potentialBranchParents, existingRectsForOverlap);
            }
        }
        LogRoomSummary();
    }

    DungeonRoomContainer CreateDungeonRoomContainer(RectInt rect, string namePrefix, bool isGateway = false, bool isStart = false, bool isBoss = false)
    {
        GameObject roomGO = new GameObject(namePrefix + rect.x + "_" + rect.y);
        roomGO.transform.SetParent(mapParent);
        RoomController rc = roomGO.AddComponent<RoomController>();
        rc.roomRect = rect;
        return new DungeonRoomContainer(rect, roomGO, rc, isStart, isBoss, isGateway);
    }

    void LogRoomSummary()
    {
        if (_dungeonRooms.Count == 0) Debug.LogWarning("Hiç oda yerleþtirilemedi.", gameObject);
        else if (_dungeonRooms.Count == 1) Debug.LogWarning("Sadece 1 oda yerleþtirildi.", gameObject);
        else Debug.Log($"{_dungeonRooms.Count} adet DungeonRoomContainer oluþturuldu.", gameObject);
    }

    DungeonRoomContainer PlaceStartRoom()
    {
        int x = Mathf.Clamp(startRoomPosition.x, 1, mapWidth - startRoomSize.x - 1);
        int z = Mathf.Clamp(startRoomPosition.y, 1, mapHeight - startRoomSize.y - 1);
        RectInt startRect = new RectInt(x, z, startRoomSize.x, startRoomSize.y);
        return CreateDungeonRoomContainer(startRect, "StartRoom_", false, true, false);
    }

    DungeonRoomContainer PlaceBossRoom(List<RectInt> existingRoomsToAvoid)
    {
        int roomWidth = bossRoomPreferredSize.x;
        int roomHeight = bossRoomPreferredSize.y;
        RectInt bossRoomRect = new RectInt();
        int attempts = 0; bool placed = false;
        RectInt startRoomRef = existingRoomsToAvoid.FirstOrDefault(); // Should be the start room

        do
        {
            int roomX, roomZ;
            float midWidth = mapWidth / 2f;
            float midHeight = mapHeight / 2f;

            // Try to place boss room on the opposite side of the map from the start room
            if (startRoomRef != null && startRoomRef.width > 0) // Check if startRoomRef is valid
            {
                if (startRoomRef.center.x < midWidth) // Start room on left, try place boss on right
                    roomX = Random.Range(Mathf.RoundToInt(midWidth + mapWidth * 0.05f), mapWidth - roomWidth - 1);
                else // Start room on right, try place boss on left
                    roomX = Random.Range(1, Mathf.RoundToInt(midWidth - mapWidth * 0.05f) - roomWidth);

                if (startRoomRef.center.y < midHeight) // Start room on bottom, try place boss on top
                    roomZ = Random.Range(Mathf.RoundToInt(midHeight + mapHeight * 0.05f), mapHeight - roomHeight - 1);
                else // Start room on top, try place boss on bottom
                    roomZ = Random.Range(1, Mathf.RoundToInt(midHeight - mapHeight * 0.05f) - roomHeight);
            }
            else // Fallback if startRoomRef is somehow invalid
            {
                roomX = Random.Range(1, mapWidth - roomWidth - 1);
                roomZ = Random.Range(1, mapHeight - roomHeight - 1);
            }


            roomX = Mathf.Clamp(roomX, 1, mapWidth - roomWidth - 1);
            roomZ = Mathf.Clamp(roomZ, 1, mapHeight - roomHeight - 1);
            bossRoomRect = new RectInt(roomX, roomZ, roomWidth, roomHeight);

            if (!DoesOverlapAnyExisting(bossRoomRect, existingRoomsToAvoid, minRoomSeparationBuffer))
            {
                placed = true;
            }
            attempts++;
        } while (attempts < 100 && !placed);

        if (!placed) // Fallback to fully random placement if directional placement fails
        {
            for (int i = 0; i < 200; i++)
            {
                int rX = Random.Range(1, mapWidth - roomWidth - 1);
                int rZ = Random.Range(1, mapHeight - roomHeight - 1);
                bossRoomRect = new RectInt(rX, rZ, roomWidth, roomHeight);
                if (!DoesOverlapAnyExisting(bossRoomRect, existingRoomsToAvoid, minRoomSeparationBuffer)) { placed = true; break; }
            }
            if (!placed) { Debug.LogError("Boss odasý yerleþtirilemedi!", gameObject); return null; }
        }
        return CreateDungeonRoomContainer(bossRoomRect, "BossRoom_", false, false, true);
    }

    RectInt TryPlaceDistantRoom(RectInt originRoom, Vector2Int targetDirectionPoint, int minDist, int maxDist, List<RectInt> existingRooms, bool isGateway = false)
    {
        for (int attempt = 0; attempt < maxPlacementAttemptsPerStep * (isGateway ? 3 : 1); attempt++)
        {
            int roomWidth = Random.Range(minRoomSize, maxRoomSize + 1);
            int roomHeight = Random.Range(minRoomSize, maxRoomSize + 1);

            Vector2 direction = ((Vector2)targetDirectionPoint - originRoom.center).normalized;
            if (direction == Vector2.zero) direction = Random.insideUnitCircle.normalized;
            if (direction == Vector2.zero) direction = Vector2.up; // Should not happen if origin and target are different

            float distance = Random.Range(minDist, maxDist + 1);
            // Add more angular variation for non-gateway rooms to spread them out
            float angleSpread = isGateway ? 60f : 90f;
            float angleOffset = Random.Range(-angleSpread, angleSpread) * Mathf.Deg2Rad;

            Vector2 finalDirection = new Vector2(
              direction.x * Mathf.Cos(angleOffset) - direction.y * Mathf.Sin(angleOffset),
              direction.x * Mathf.Sin(angleOffset) + direction.y * Mathf.Cos(angleOffset)
            ).normalized;

            Vector2 targetCenter = originRoom.center + finalDirection * distance;
            int roomX = Mathf.RoundToInt(targetCenter.x - roomWidth / 2f);
            int roomZ = Mathf.RoundToInt(targetCenter.y - roomHeight / 2f);

            roomX = Mathf.Clamp(roomX, 1, mapWidth - roomWidth - 1);
            roomZ = Mathf.Clamp(roomZ, 1, mapHeight - roomHeight - 1);
            RectInt newRoom = new RectInt(roomX, roomZ, roomWidth, roomHeight);

            // Ensure the new room is actually somewhat distant, especially for non-gateways
            float minActualDistance = isGateway ? minDist * 0.8f : minDist;
            if (Vector2.Distance(newRoom.center, originRoom.center) < minActualDistance - 1f) continue;


            if (!DoesOverlapAnyExisting(newRoom, existingRooms, minRoomSeparationBuffer))
            {
                return newRoom;
            }
        }
        return new RectInt(0, 0, 0, 0);
    }

    void CreateMainPath(DungeonRoomContainer pathStartNode, DungeonRoomContainer pathEndNode, List<RectInt> existingRects)
    {
        DungeonRoomContainer currentRoomNode = pathStartNode;
        for (int i = 0; i < numRoomsOnMainPath; i++)
        {
            if (_dungeonRooms.Count >= maxRooms - 1) break; // Leave space for boss room if not yet connected

            // Aim towards the pathEndNode, but allow some deviation
            Vector2 dirToPathEnd = (pathEndNode.Rect.center - currentRoomNode.Rect.center).normalized;
            Vector2 randomOffset = Random.insideUnitCircle * (maxRoomSize * 0.5f); // Add some randomness to target
            Vector2Int targetPoint = Vector2Int.RoundToInt(currentRoomNode.Rect.center + dirToPathEnd * (maxRoomSize + corridorThickness) + randomOffset);


            RectInt nextRoomRect = TryPlaceDistantRoom(currentRoomNode.Rect, targetPoint, // Vector2Int.RoundToInt(pathEndNode.Rect.center),
                                                       Mathf.Max(1, minRoomSize / 2) + corridorThickness,
                              maxRoomSize + corridorThickness + minRoomSeparationBuffer, // increase max dist slightly
                                                       existingRects);
            if (nextRoomRect.width > 0)
            {
                DungeonRoomContainer nextNode = CreateDungeonRoomContainer(nextRoomRect, "MainPathRoom_");
                _dungeonRooms.Add(nextNode);
                existingRects.Add(nextRoomRect);
                ConnectTwoDungeonRooms(currentRoomNode, nextNode, corridorThickness);
                currentRoomNode = nextNode;
            }
            else
            {
                // Debug.LogWarning($"Could not place main path room {i+1}. Attempting to connect directly to end node.");
                break; // If a main path room can't be placed, connect to the end node
            }
        }

        // Always try to connect the last placed/current main path node to the designated pathEndNode
        if (currentRoomNode != pathEndNode && !AreRoomsConnected(currentRoomNode, pathEndNode))
        {
            ConnectTwoDungeonRooms(currentRoomNode, pathEndNode, corridorThickness);
        }
    }
    // ADD THE NEW METHOD HERE:
    void PlaceBossRoomTeleport()
    {
        if (bossRoomTeleportPrefab == null)
        {
            Debug.LogWarning("Boss Room Teleport Prefab atanmamýþ. Teleport yerleþtirilmeyecek.");
            return;
        }

        if (_generatedBossRoomContainer == null || _generatedBossRoomContainer.ContainerGO == null)
        {
            Debug.LogError("Boss odasý veya container'ý bulunamadý. Teleport yerleþtirilemiyor.");
            return;
        }

        // Calculate the center of the boss room in world coordinates.
        // Rect.center gives the center in tile coordinates (can be float).
        // Tile (tx, tz) has its world center at (tx * tileSize + tileSize / 2, Y, tz * tileSize + tileSize / 2).
        // So, world center of room = (roomRect.center.x * tileSize, Y, roomRect.center.y * tileSize) if tileSize/2 is not pre-added.
        // Given InstantiateTilesFromMapData: Vector3 finalTilePosition = new Vector3(x * tileSize + tileSize / 2f, 0, z * tileSize + tileSize / 2f);
        // The center of a tile (cx,cz) is at (cx*tileSize + tileSize/2, 0, cz*tileSize + tileSize/2).
        // The rect's center (float) should be multiplied by tileSize to get its world equivalent if origin is at 0,0.

        Vector3 worldCenterPosition = new Vector3(
             _generatedBossRoomContainer.Rect.center.x * tileSize,
             bossRoomTeleportYOffset, // Use the specified Y offset
             _generatedBossRoomContainer.Rect.center.y * tileSize
        );

        GameObject teleportGO = Instantiate(bossRoomTeleportPrefab, worldCenterPosition, Quaternion.identity, _generatedBossRoomContainer.ContainerGO.transform);
        teleportGO.name = "BossRoomTeleport";
        // Debug.Log($"Boss Room Teleport placed at {worldCenterPosition} in Boss Room '{_generatedBossRoomContainer.ContainerGO.name}'.");

        // Optional: Mark the tile(s) under the teleport as occupied if it affects enemy/decor placement significantly.
        // This depends on the size and nature of your teleport prefab.
        // For a simple 1x1 footprint at the center:
        Vector2Int centerTileFootprint = new Vector2Int(Mathf.FloorToInt(_generatedBossRoomContainer.Rect.center.x), Mathf.FloorToInt(_generatedBossRoomContainer.Rect.center.y));
        if (IsInBounds(centerTileFootprint.x, centerTileFootprint.y))
        {
            _decorOccupiedTiles.Add(centerTileFootprint);
            // If your teleport is larger, you might need to mark more tiles:
            // e.g., for a 2x2 area around the center:
            // _decorOccupiedTiles.Add(new Vector2Int(centerTileFootprint.x - 1, centerTileFootprint.y -1));
            // _decorOccupiedTiles.Add(new Vector2Int(centerTileFootprint.x, centerTileFootprint.y -1));
            // ... and so on, carefully checking bounds.
        }
    }
    void CreateSideBranches(List<DungeonRoomContainer> potentialParentRooms, List<RectInt> existingRects)
    {
        List<DungeonRoomContainer> validParents = new List<DungeonRoomContainer>(potentialParentRooms);

        for (int i = 0; i < numSideBranches; i++)
        {
            if (_dungeonRooms.Count >= maxRooms) break;
            if (validParents.Count == 0) break;

            DungeonRoomContainer parentRoomNode = validParents[Random.Range(0, validParents.Count)];
            DungeonRoomContainer currentBranchHeadNode = parentRoomNode;
            // Ensure at least 1 room in a branch, up to max depth
            int currentBranchActualDepth = Random.Range(1, Mathf.Max(1, maxSideBranchDepth) + 1);


            for (int d = 0; d < currentBranchActualDepth; d++)
            {
                if (_dungeonRooms.Count >= maxRooms) break;

                // Try to branch outwards from parent
                Vector2 parentCenter = parentRoomNode.Rect.center;
                Vector2 currentCenter = currentBranchHeadNode.Rect.center;
                Vector2 outwardDirection = (currentCenter - parentCenter).normalized;
                if (outwardDirection == Vector2.zero) outwardDirection = Random.insideUnitCircle.normalized; // If current is parent

                Vector2Int randomDirPoint = new Vector2Int(
          Mathf.RoundToInt(currentCenter.x + outwardDirection.x * Random.Range(minRoomSize, mapWidth / 4f) + Random.Range(-mapWidth / 5f, mapWidth / 5f)),
          Mathf.RoundToInt(currentCenter.y + outwardDirection.y * Random.Range(minRoomSize, mapHeight / 4f) + Random.Range(-mapHeight / 5f, mapHeight / 5f))
        );


                RectInt newRoomRect = TryPlaceDistantRoom(currentBranchHeadNode.Rect, randomDirPoint,
                                     Mathf.Max(1, minRoomSize / 2) + corridorThickness, // Min distance for branches
                                                                  maxRoomSize + corridorThickness + minRoomSeparationBuffer, // Max distance for branches
                                                                  existingRects);
                if (newRoomRect.width > 0)
                {
                    DungeonRoomContainer newNode = CreateDungeonRoomContainer(newRoomRect, "SideBranchRoom_");
                    _dungeonRooms.Add(newNode);
                    existingRects.Add(newRoomRect);
                    // Add new non-special rooms as potential parents for further branches IF desired
                    // For now, let's keep branches simpler and not have branches off branches unless from main path/gateways
                    // if (!newNode.IsStartRoom && !newNode.IsBossRoom && !newNode.IsGatewayRoom)
                    //     validParents.Add(newNode);

                    ConnectTwoDungeonRooms(currentBranchHeadNode, newNode, corridorThickness);
                    currentBranchHeadNode = newNode;
                }
                else break; // Stop this branch if a room can't be placed
            }
        }
    }

    bool AreRoomsConnected(DungeonRoomContainer room1, DungeonRoomContainer room2)
    {
        if (room1 == null || room2 == null) return false;
        foreach (var corridor in room1.ConnectedCorridors)
        {
            if ((corridor.RoomA == room1 && corridor.RoomB == room2) || (corridor.RoomA == room2 && corridor.RoomB == room1))
            {
                return true;
            }
        }
        return false;
    }


    bool DoesOverlapAnyExisting(RectInt roomToCheck, List<RectInt> existingRooms, int buffer = 1)
    {
        RectInt checkRect = new RectInt(roomToCheck.x - buffer, roomToCheck.y - buffer, roomToCheck.width + (buffer * 2), roomToCheck.height + (buffer * 2));
        foreach (RectInt existingRoom in existingRooms)
        {
            if (checkRect.Overlaps(existingRoom)) return true;
        }
        return false;
    }

    void ConnectTwoDungeonRooms(DungeonRoomContainer room1, DungeonRoomContainer room2, int thickness)
    {
        if (room1 == null || room2 == null) return;
        if (AreRoomsConnected(room1, room2)) return;

        GameObject corridorGO = new GameObject($"Corridor_R{_dungeonRooms.IndexOf(room1)}_to_R{_dungeonRooms.IndexOf(room2)}");
        corridorGO.transform.SetParent(mapParent);
        DungeonCorridorContainer corridor = new DungeonCorridorContainer(corridorGO, room1, room2);

        Vector2Int center1 = Vector2Int.RoundToInt(room1.Rect.center);
        Vector2Int center2 = Vector2Int.RoundToInt(room2.Rect.center);

        List<Vector2Int> currentCorridorTiles = new List<Vector2Int>();

        if (Random.value < 0.5f) // Horizontal first, then vertical
        {
            CarveHorizontalTunnel(center1.x, center2.x, center1.y, thickness, currentCorridorTiles);
            CarveVerticalTunnel(center1.y, center2.y, center2.x, thickness, currentCorridorTiles);
        }
        else // Vertical first, then horizontal
        {
            CarveVerticalTunnel(center1.y, center2.y, center1.x, thickness, currentCorridorTiles);
            CarveHorizontalTunnel(center1.x, center2.x, center2.y, thickness, currentCorridorTiles);
        }

        corridor.Tiles.AddRange(currentCorridorTiles.Distinct());
        foreach (var tilePos in corridor.Tiles)
        {
            _tileToCorridorLookup[tilePos] = corridor;
        }

        _dungeonCorridors.Add(corridor);
        room1.ConnectedCorridors.Add(corridor);
        room2.ConnectedCorridors.Add(corridor);

        if (room1.Controller != null && room2.Controller != null)
        {
            room1.Controller.AddConnection(room2.Controller, corridorGO);
            room2.Controller.AddConnection(room1.Controller, corridorGO);
        }
        else
        {
            Debug.LogError($"Controller missing for room connection: {room1.ContainerGO.name} or {room2.ContainerGO.name}", gameObject);
        }
    }


    void CarveAreaInMapData(RectInt area, int value, DungeonRoomContainer roomOwner, DungeonCorridorContainer corridorOwner)
    {
        for (int x = area.xMin; x < area.xMax; x++)
        {
            for (int z = area.yMin; z < area.yMax; z++)
            {
                if (IsInBounds(x, z))
                {
                    _mapData[x, z] = value;
                    Vector2Int tilePos = new Vector2Int(x, z);
                    if (roomOwner != null) _tileToRoomLookup[tilePos] = roomOwner;
                    else if (corridorOwner != null) _tileToCorridorLookup[tilePos] = corridorOwner;
                }
            }
        }
    }

    void CarveHorizontalTunnel(int xStart, int xEnd, int zPos, int thickness, List<Vector2Int> corridorTilesList)
    {
        int minX = Mathf.Min(xStart, xEnd);
        int maxX = Mathf.Max(xStart, xEnd);
        int halfThicknessFloor = Mathf.FloorToInt((thickness - 1) / 2f);
        int halfThicknessCeil = Mathf.CeilToInt((thickness - 1) / 2f);

        for (int x = minX; x <= maxX; x++)
        {
            for (int offset = -halfThicknessFloor; offset <= halfThicknessCeil; offset++)
            {
                int currentZ = zPos + offset;
                if (IsInBounds(x, currentZ))
                {
                    // _mapData[x, currentZ] = 1; // Data populated later by PopulateMapDataFromContainers
                    corridorTilesList.Add(new Vector2Int(x, currentZ));
                }
            }
        }
    }

    void CarveVerticalTunnel(int zStart, int zEnd, int xPos, int thickness, List<Vector2Int> corridorTilesList)
    {
        int minZ = Mathf.Min(zStart, zEnd);
        int maxZ = Mathf.Max(zStart, zEnd);
        int halfThicknessFloor = Mathf.FloorToInt((thickness - 1) / 2f);
        int halfThicknessCeil = Mathf.CeilToInt((thickness - 1) / 2f);

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int offset = -halfThicknessFloor; offset <= halfThicknessCeil; offset++)
            {
                int currentX = xPos + offset;
                if (IsInBounds(currentX, z))
                {
                    // _mapData[currentX, z] = 1; // Data populated later by PopulateMapDataFromContainers
                    corridorTilesList.Add(new Vector2Int(currentX, z));
                }
            }
        }
    }

    bool IsInBounds(int x, int z)
    {
        return x >= 0 && x < mapWidth && z >= 0 && z < mapHeight;
    }

    void ExpandFloorAtEdges()
    {
        HashSet<Vector2Int> wallsToBecomeFloor = new HashSet<Vector2Int>();
        int[,] originalMapData = (int[,])_mapData.Clone();

        for (int x = 1; x < mapWidth - 1; x++)
        {
            for (int z = 1; z < mapHeight - 1; z++)
            {
                if (originalMapData[x, z] == 0)
                {
                    int floorNeighbors = 0;
                    if (originalMapData[x + 1, z] == 1) floorNeighbors++;
                    if (originalMapData[x - 1, z] == 1) floorNeighbors++;
                    if (originalMapData[x, z + 1] == 1) floorNeighbors++;
                    if (originalMapData[x, z - 1] == 1) floorNeighbors++;

                    if (floorNeighbors >= 3)
                    {
                        wallsToBecomeFloor.Add(new Vector2Int(x, z));
                    }
                }
            }
        }
        foreach (var wp in wallsToBecomeFloor) _mapData[wp.x, wp.y] = 1;
    }


    void RefinedPostProcessMapData()
    {
        int totalChangedToWall = 0, passes = 0; const int maxPass = 3;
        do
        {
            int changedThisPass = 0;
            int[,] currentMap = (int[,])_mapData.Clone();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    if (currentMap[x, z] == 1)
                    {
                        int floorNeighbors = 0;
                        if (x > 0 && currentMap[x - 1, z] == 1) floorNeighbors++;
                        if (x < mapWidth - 1 && currentMap[x + 1, z] == 1) floorNeighbors++;
                        if (z > 0 && currentMap[x, z - 1] == 1) floorNeighbors++;
                        if (z < mapHeight - 1 && currentMap[x, z + 1] == 1) floorNeighbors++;

                        Vector2Int currentTilePos = new Vector2Int(x, z);
                        bool inRoom = _tileToRoomLookup.ContainsKey(currentTilePos);
                        DungeonRoomContainer roomOwner = null;
                        if (inRoom) roomOwner = _tileToRoomLookup[currentTilePos];

                        // Do not remove tiles that are part of room edges, even if they have few neighbors after corridor carving
                        bool isRoomEdge = false;
                        if (roomOwner != null)
                        {
                            if (x == roomOwner.Rect.xMin || x == roomOwner.Rect.xMax - 1 ||
                             z == roomOwner.Rect.yMin || z == roomOwner.Rect.yMax - 1)
                            {
                                isRoomEdge = true;
                            }
                        }

                        // Keep tiles if they are essential parts of rooms or thick corridors
                        bool inThickCorridorSegment = false;
                        if (_tileToCorridorLookup.ContainsKey(currentTilePos) && corridorThickness > 1)
                        {
                            // Check diagonal neighbors for thickness. This is a simplification.
                            int diagonalFloorNeighbors = 0;
                            if (x > 0 && z > 0 && currentMap[x - 1, z - 1] == 1) diagonalFloorNeighbors++;
                            if (x < mapWidth - 1 && z < mapHeight - 1 && currentMap[x + 1, z + 1] == 1) diagonalFloorNeighbors++;
                            if (x > 0 && z < mapHeight - 1 && currentMap[x - 1, z + 1] == 1) diagonalFloorNeighbors++;
                            if (x < mapWidth - 1 && z > 0 && currentMap[x + 1, z - 1] == 1) diagonalFloorNeighbors++;
                            if (floorNeighbors + diagonalFloorNeighbors >= 3 && floorNeighbors >= 1) // Arbitrary numbers, tune if needed
                            {
                                inThickCorridorSegment = true;
                            }
                        }


                        if (floorNeighbors <= 1 && !inRoom && !_tileToCorridorLookup.ContainsKey(currentTilePos)) // Stricter: only remove truly isolated tiles not in rooms/corridors
                        {
                            if (_mapData[x, z] == 1)
                            {
                                _mapData[x, z] = 0;
                                changedThisPass++;
                            }
                        }
                        else if (floorNeighbors < 2 && _tileToCorridorLookup.ContainsKey(currentTilePos) && !isRoomEdge && !inThickCorridorSegment) // Thin out 1-tile wide corridor protrusions if not essential
                        {
                            if (_mapData[x, z] == 1)
                            {
                                _mapData[x, z] = 0;
                                changedThisPass++;
                            }
                        }
                    }
                }
            }
            totalChangedToWall += changedThisPass;
            passes++;
            if (changedThisPass == 0) break;
        } while (passes < maxPass);
    }

    // *** MODIFIED METHOD ***
    void SetInitialActivationStates()
    {
        // Set initial state for all rooms.
        // RoomController's SetInitialActiveState makes the room's GameObject active.
        // For the start room, it also calls MarkAsCleared(), which attempts to activate connected pathways.
        foreach (var roomC in _dungeonRooms)
        {
            bool isStart = roomC.IsStartRoom;
            if (roomC.Controller != null)
            {
                roomC.Controller.SetInitialActiveState(isStart, isStart);
            }
            else
            {
                Debug.LogError($"Room {roomC.ContainerGO.name} has no RoomController component!", roomC.ContainerGO);
            }
        }

        // Initially, set all corridor GameObjects to inactive.
        // This step is crucial because the StartRoomController might have tried to activate its corridors
        // during its SetInitialActiveState call, but we want a clean slate before specifically activating start paths.
        foreach (var corridorC in _dungeonCorridors)
        {
            if (corridorC.ContainerGO != null)
            {
                corridorC.ContainerGO.SetActive(false);
            }
        }

        // Now, specifically activate pathways for the start room.
        // The StartRoomController's SetInitialActiveState would have already set it to 'isCleared = true'.
        // We call ActivateConnectedPathways() again here to ensure its corridors are active
        // *after* the global deactivation of all corridors above.
        if (_generatedStartRoomContainer != null && _generatedStartRoomContainer.Controller != null)
        {
            if (_generatedStartRoomContainer.Controller.isCleared) // This should be true.
            {
                // Debug.Log($"Ensuring pathways for start room '{_generatedStartRoomContainer.ContainerGO.name}' are active.");
                _generatedStartRoomContainer.Controller.ActivateConnectedPathways();
            }
            else
            {
                // This is a fallback, ideally SetInitialActiveState for the start room should handle this.
                Debug.LogWarning($"Start room '{_generatedStartRoomContainer.ContainerGO.name}' was not cleared initially. Attempting to mark as cleared and activate pathways.");
                _generatedStartRoomContainer.Controller.MarkAsCleared(); // This will call ActivateConnectedPathways
            }
        }
        else
        {
            Debug.LogError("Generated Start Room Container or its Controller is null. Cannot set initial active pathways for start room.");
        }
    }


    void InstantiateTilesFromMapData()
    {
        if (tilePrefabs == null || tilePrefabs.Length < 3 || tilePrefabs.Any(p => p == null)) { Debug.LogError("Temel Tile Prefablarý (Köþe, Kenar, Zemin) atanmamýþ!", gameObject); return; }
        _decorOccupiedTiles.Clear(); // Clear at the beginning of instantiation

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapHeight; z++)
            {
                if (_mapData[x, z] == 1) // Is a floor tile
                {
                    Vector2Int currentTileCoords = new Vector2Int(x, z);
                    Transform parentTransform = mapParent; // Default parent
                    DungeonRoomContainer roomOwner = null;
                    DungeonCorridorContainer corridorOwner = null;

                    _tileToRoomLookup.TryGetValue(currentTileCoords, out roomOwner);
                    _tileToCorridorLookup.TryGetValue(currentTileCoords, out corridorOwner);

                    if (roomOwner != null && roomOwner.ContainerGO != null)
                    {
                        parentTransform = roomOwner.ContainerGO.transform;
                    }
                    else if (corridorOwner != null && corridorOwner.ContainerGO != null)
                    {
                        parentTransform = corridorOwner.ContainerGO.transform;
                    }
                    // else it's a stray tile, parent to mapParent, which should be rare after post-processing

                    bool wallN = IsWallAt(x, z + 1);
                    bool wallS = IsWallAt(x, z - 1);
                    bool wallE = IsWallAt(x + 1, z);
                    bool wallW = IsWallAt(x - 1, z);
                    int surroundingWallCount = (wallN ? 1 : 0) + (wallS ? 1 : 0) + (wallE ? 1 : 0) + (wallW ? 1 : 0);

                    GameObject prefabToInstantiate = null;
                    Quaternion tileRotation = Quaternion.identity;
                    bool isInSpecialRoom = (roomOwner != null && (roomOwner.IsStartRoom || roomOwner.IsBossRoom));
                    bool useEmptySpace = !isInSpecialRoom &&
                              emptySpacePrefab != null &&
                              Random.value < emptySpaceProbability &&
                              !IsTileNearRoomEdge(x, z, 1, roomOwner) && // Don't use empty space near room edges
                                                   surroundingWallCount == 0; // Only for fully open floor tiles

                    if (useEmptySpace)
                    {
                        prefabToInstantiate = emptySpacePrefab;
                        _decorOccupiedTiles.Add(currentTileCoords); // Mark as occupied for other decor
                    }
                    else
                    {
                        switch (surroundingWallCount)
                        {
                            case 0: // Floor
                                prefabToInstantiate = tilePrefabs[2];
                                break;
                            case 1: // Wall
                                prefabToInstantiate = tilePrefabs[1];
                                if (wallW) tileRotation = Quaternion.Euler(0, 0, 0);       // Default wall on -X
                                else if (wallN) tileRotation = Quaternion.Euler(0, 90, 0);  // Wall on +Z
                                else if (wallE) tileRotation = Quaternion.Euler(0, 180, 0); // Wall on +X
                                else if (wallS) tileRotation = Quaternion.Euler(0, 270, 0); // Wall on -Z
                                break;
                            case 2: // Corner or corridor passage
                                if (wallW && wallS) { prefabToInstantiate = tilePrefabs[0]; tileRotation = Quaternion.Euler(rotationBottomLeftCorner); }
                                else if (wallS && wallE) { prefabToInstantiate = tilePrefabs[0]; tileRotation = Quaternion.Euler(rotationBottomRightCorner); }
                                else if (wallE && wallN) { prefabToInstantiate = tilePrefabs[0]; tileRotation = Quaternion.Euler(rotationTopRightCorner); }
                                else if (wallN && wallW) { prefabToInstantiate = tilePrefabs[0]; tileRotation = Quaternion.Euler(rotationTopLeftCorner); }
                                else if ((wallN && wallS) || (wallW && wallE)) // Straight passage with walls on two opposite sides
                                {
                                    prefabToInstantiate = tilePrefabs[2]; // Floor tile for passages
                                }
                                else // Should not happen if logic is correct, fallback to floor
                                {
                                    prefabToInstantiate = tilePrefabs[2];
                                    // Debug.LogWarning($"Tile at ({x},{z}) has 2 non-opposite walls. Defaulting to floor.");
                                }
                                break;
                            case 3: // Inner corner (usually part of a 1-tile thick wall ending, looks like a U-shape wall)
                                prefabToInstantiate = tilePrefabs[0]; // Use corner piece
                                // This creates an "alcove" floor tile surrounded by 3 walls
                                if (!wallW) { tileRotation = Quaternion.Euler(rotationBottomRightCorner); } // Opening to West
                                else if (!wallE) { tileRotation = Quaternion.Euler(rotationBottomLeftCorner); }  // Opening to East
                                else if (!wallS) { tileRotation = Quaternion.Euler(rotationTopLeftCorner); } // Opening to South
                                else if (!wallN) { tileRotation = Quaternion.Euler(rotationBottomLeftCorner); } // Opening to North (using BL as example)
                                else prefabToInstantiate = tilePrefabs[2]; // Fallback, should not occur
                                break;
                            case 4: // Surrounded by walls (should ideally not be a floor tile unless it's a 1x1 room/alcove)
                                prefabToInstantiate = tilePrefabs[2]; // Floor
                                break;
                        }
                    }


                    if (prefabToInstantiate != null)
                    {
                        Vector3 finalTilePosition = new Vector3(x * tileSize + tileSize / 2f, 0, z * tileSize + tileSize / 2f);
                        GameObject tileInstance = Instantiate(prefabToInstantiate, finalTilePosition, tileRotation, parentTransform);
                        tileInstance.name = $"Tile_{x}_{z}_({prefabToInstantiate.name})_P_{parentTransform.name}";
                    }
                }
            }
        }
    }

    bool IsTileNearRoomEdge(int x, int z, int buffer, DungeonRoomContainer specificRoom)
    {
        if (specificRoom == null) return false;

        RectInt roomRect = specificRoom.Rect;
        // Check if the tile (x,z) is within the room's bounds first
        if (x >= roomRect.xMin && x < roomRect.xMax && z >= roomRect.yMin && z < roomRect.yMax)
        {
            // Now check if it's near the edge
            if (x < roomRect.xMin + buffer || x >= roomRect.xMax - buffer ||
        z < roomRect.yMin + buffer || z >= roomRect.yMax - buffer)
                return true;
        }
        return false;
    }


    bool IsWallAt(int x, int z)
    {
        if (!IsInBounds(x, z)) return true; // Treat out-of-bounds as a wall
        return _mapData[x, z] == 0; // 0 represents a wall or empty space
    }

    int GetSurroundingWallCount(int x, int z)
    {
        int count = 0;
        if (IsWallAt(x, z + 1)) count++; // North
        if (IsWallAt(x, z - 1)) count++; // South
        if (IsWallAt(x + 1, z)) count++; // East
        if (IsWallAt(x - 1, z)) count++; // West
        return count;
    }

    void PlaceSingleTileDecor(GameObject prefab, int x, int z, float yOffset, string namePrefix, DungeonRoomContainer roomOwner, Quaternion? rotation = null)
    {
        Vector2Int tilePos = new Vector2Int(x, z);
        // Ensure the tile is a floor, not occupied by other decor, and belongs to the specified roomOwner
        if (prefab != null && _mapData[x, z] == 1 && !_decorOccupiedTiles.Contains(tilePos) && roomOwner != null)
        {
            // Check if this tile belongs to the room we intend to place decor in
            DungeonRoomContainer actualRoomOwner;
            if (_tileToRoomLookup.TryGetValue(tilePos, out actualRoomOwner) && actualRoomOwner == roomOwner)
            {
                Vector3 decorPos = new Vector3(x * tileSize + tileSize / 2f, yOffset, z * tileSize + tileSize / 2f);
                Quaternion decorRot = rotation ?? GetRotationAwayFromWall(x, z); // Use provided or calculate

                Instantiate(prefab, decorPos, decorRot, roomOwner.ContainerGO.transform).name = $"{namePrefix}_({prefab.name})_At_({x},{z})";
                _decorOccupiedTiles.Add(tilePos); // Mark as occupied
            }
        }
    }

    void PlaceAllDecorations()
    {
        // Iterate through rooms first, then tiles within those rooms
        foreach (DungeonRoomContainer roomC in _dungeonRooms)
        {
            // Skip Start and Boss rooms for these general decorations
            if (roomC.IsStartRoom || roomC.IsBossRoom) continue;

            List<Vector2Int> potentialDecorTilesInRoom = new List<Vector2Int>();
            for (int x = roomC.Rect.xMin; x < roomC.Rect.xMax; x++)
            {
                for (int z = roomC.Rect.yMin; z < roomC.Rect.yMax; z++)
                {
                    Vector2Int tilePos = new Vector2Int(x, z);
                    // Ensure it's a floor tile and not already taken by special floor (emptySpacePrefab)
                    if (_mapData[x, z] == 1 && !_decorOccupiedTiles.Contains(tilePos))
                    {
                        potentialDecorTilesInRoom.Add(tilePos);
                    }
                }
            }
            potentialDecorTilesInRoom = potentialDecorTilesInRoom.OrderBy(t => Random.value).ToList(); // Shuffle per room

            foreach (Vector2Int tilePos in potentialDecorTilesInRoom)
            {
                if (_decorOccupiedTiles.Contains(tilePos)) continue; // Double check, might have been taken by another decor in same room pass

                int x = tilePos.x;
                int z = tilePos.y;
                int surroundingWalls = GetSurroundingWallCount(x, z);

                // Try placing different types of decor based on probability and conditions
                bool placedDecorThisTile = false;

                if (!placedDecorThisTile && rockPrefabs != null && rockPrefabs.Length > 0 && Random.value < rockPlacementProbability)
                {
                    // Rocks can be placed more freely, even near walls or center, if the tile is available
                    PlaceSingleTileDecor(rockPrefabs[Random.Range(0, rockPrefabs.Length)], x, z, 0, "Rock", roomC);
                    placedDecorThisTile = true;
                }
                if (!placedDecorThisTile && statuePrefabs != null && statuePrefabs.Length > 0 && Random.value < statuePlacementProbability)
                {
                    // Statues often look good near walls or in corners (1 or 2 non-opposite walls)
                    if (surroundingWalls >= 1 && surroundingWalls <= 2 && !AreWallsOpposite(x, z))
                    {
                        PlaceSingleTileDecor(statuePrefabs[Random.Range(0, statuePrefabs.Length)], x, z, statueYOffset, "Statue", roomC, GetRotationAwayFromWall(x, z));
                        placedDecorThisTile = true;
                    }
                }
                if (!placedDecorThisTile && candlePrefabs != null && candlePrefabs.Length > 0 && Random.value < candlePlacementProbability)
                {
                    // Candles are often near walls
                    if (surroundingWalls > 0)
                    {
                        PlaceSingleTileDecor(candlePrefabs[Random.Range(0, candlePrefabs.Length)], x, z, candleYOffset, "Candle", roomC, GetRotationAwayFromWall(x, z));
                        placedDecorThisTile = true;
                    }
                }
                if (!placedDecorThisTile && clutterPrefabs != null && clutterPrefabs.Length > 0 && Random.value < clutterPlacementProbability)
                {
                    // Clutter usually in more open spaces, not right against the edge
                    if (!IsTileNearRoomEdge(x, z, 1, roomC) && surroundingWalls == 0) // Place clutter if not near edge and open
                    {
                        PlaceSingleTileDecor(clutterPrefabs[Random.Range(0, clutterPrefabs.Length)], x, z, clutterYOffset, "Clutter", roomC, Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0));
                        // placedDecorThisTile = true; // Clutter might be less intrusive, allow other small things?
                    }
                }
            }
        }
    }

    void PlaceUpgradeChests()
    {
        if (upgradeChestPrefab == null)
        {
            // Debug.LogWarning("Upgrade Chest Prefab atanmamýþ. Sandýk yerleþtirilmeyecek."); // Ýsteðe baðlý uyarý
            return;
        }
        if (upgradeChestProbabilityPerRoom <= 0f)
        {
            // Debug.Log("Upgrade Chest olasýlýðý 0 veya daha düþük. Sandýk yerleþtirilmeyecek."); // Ýsteðe baðlý uyarý
            return;
        }

        int chestsPlaced = 0; // Kaç tane sandýk yerleþtirildiðini saymak için (opsiyonel)

        foreach (DungeonRoomContainer roomC in _dungeonRooms)
        {
            // Baþlangýç ve Boss odalarýna sandýk koyma. Geçit odalarýna artýk izin veriliyor.
            if (roomC.IsStartRoom || roomC.IsBossRoom) continue;

            if (Random.value < upgradeChestProbabilityPerRoom)
            {
                List<Vector2Int> possibleChestLocations = new List<Vector2Int>();
                // Sandýk için odanýn iç kýsýmlarýnda (kenarlardan 1 birim içeride) boþ yer ara
                for (int x = roomC.Rect.xMin + 1; x < roomC.Rect.xMax - 1; x++)
                {
                    for (int z = roomC.Rect.yMin + 1; z < roomC.Rect.yMax - 1; z++)
                    {
                        Vector2Int tilePos = new Vector2Int(x, z);
                        // Zemin mi, baþka bir dekorla dolu mu, ve etrafý çok kapalý mý?
                        if (_mapData[x, z] == 1 && !_decorOccupiedTiles.Contains(tilePos))
                        {
                            // Tercihen en fazla 1 duvarý olan yerler (daha açýk alanlar)
                            if (GetSurroundingWallCount(x, z) <= 1)
                            {
                                possibleChestLocations.Add(tilePos);
                            }
                        }
                    }
                }

                // Eðer uygun yer bulunduysa, rastgele birini seç ve sandýðý yerleþtir
                if (possibleChestLocations.Count > 0)
                {
                    Vector2Int chestTile = possibleChestLocations[Random.Range(0, possibleChestLocations.Count)];
                    Vector3 chestPos = new Vector3(chestTile.x * tileSize + tileSize / 2f, upgradeChestYOffset, chestTile.y * tileSize + tileSize / 2f);

                    // Sandýðýn duvara bakmamasýný saðla (eðer duvara yakýnsa)
                    Quaternion chestRotation = GetRotationAwayFromWall(chestTile.x, chestTile.y);
                    if (GetSurroundingWallCount(chestTile.x, chestTile.y) == 0) // Eðer etrafý tamamen açýksa rastgele döndür
                    {
                        chestRotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);
                    }

                    Instantiate(upgradeChestPrefab, chestPos, chestRotation, roomC.ContainerGO.transform).name = $"UpgradeChest_R{_dungeonRooms.IndexOf(roomC)}";
                    _decorOccupiedTiles.Add(chestTile); // Bu alaný dolu olarak iþaretle
                    chestsPlaced++;
                    // break; // Bu break, eðer aktif edilirse, bu odada sandýk bulduktan sonra diðer odalara bakmayý durdurur.
                    // Þu anki haliyle her uygun oda için bir sandýk þansý tanýr.
                    // Eðer oda BAÞINA sadece bir sandýk isteniyorsa bu break doðru yerdedir.
                    // Eðer tüm zindanda belirli sayýda sandýk isteniyorsa farklý bir mantýk gerekir.
                    // Mevcut hali: "Bu oda sandýk almaya hak kazandý, bir tane koy ve bu oda için bitir."
                }
            }
        }
        // Debug.Log($"{chestsPlaced} adet upgrade sandýðý yerleþtirildi."); // Opsiyonel
    }


    void PlaceFireEffects()
    {
        if (fireEffectPrefab == null || firePlacementProbability <= 0f) return;
        foreach (DungeonRoomContainer roomC in _dungeonRooms)
        {
            if (!canFireInStartRoom && roomC.IsStartRoom) continue;
            if (!canFireInBossRoom && roomC.IsBossRoom) continue;
            // if (roomC.IsGatewayRoom) continue; // *** REMOVED: Allow fire in gateway rooms ***

            for (int x = roomC.Rect.xMin; x < roomC.Rect.xMax; x++)
            {
                for (int z = roomC.Rect.yMin; z < roomC.Rect.yMax; z++)
                {
                    Vector2Int tilePos = new Vector2Int(x, z);
                    // Check if tile is valid for fire (floor, not occupied)
                    if (_mapData[x, z] == 1 && !_decorOccupiedTiles.Contains(tilePos) && Random.value < firePlacementProbability)
                    {
                        int fireCountThisCluster = 1;
                        if (Random.value < tryToFormClusterProbability)
                        {
                            fireCountThisCluster = Random.Range(minFiresInCluster, maxFiresInCluster + 1);
                        }

                        // Place first fire at the current tile
                        Vector3 firstFirePos = new Vector3(x * tileSize + tileSize / 2f, fireYOffset, z * tileSize + tileSize / 2f);
                        Instantiate(fireEffectPrefab, firstFirePos, Quaternion.identity, roomC.ContainerGO.transform).name = $"FireEffect_({x},{z})";
                        _decorOccupiedTiles.Add(tilePos);

                        // Attempt to place additional fires for the cluster
                        for (int i = 1; i < fireCountThisCluster; i++) // Start from 1 as first is already placed
                        {
                            // Try to place near the original fire (x,z) or previously placed fires in this cluster
                            int fx = x + Random.Range(-1, 2); // Spread of 1 tile around original
                            int fz = z + Random.Range(-1, 2);
                            Vector2Int fireClusterTilePos = new Vector2Int(fx, fz);

                            // Check bounds, if it's a floor, not occupied, and within the current room's rectangle
                            if (IsInBounds(fx, fz) &&
                roomC.Rect.Contains(fireClusterTilePos) && // Ensure fire stays within its designated room
                                _mapData[fx, fz] == 1 &&
                !_decorOccupiedTiles.Contains(fireClusterTilePos))
                            {
                                Vector3 firePos = new Vector3(fx * tileSize + tileSize / 2f, fireYOffset, fz * tileSize + tileSize / 2f);
                                Instantiate(fireEffectPrefab, firePos, Quaternion.identity, roomC.ContainerGO.transform).name = $"FireEffect_Cluster_({fx},{fz})";
                                _decorOccupiedTiles.Add(fireClusterTilePos);
                            }
                        }
                    }
                }
            }
        }
    }

    void PlaceToxicGroundEffects()
    {
        if (toxicGroundPrefab == null || toxicGroundPlacementProbability <= 0f) return;

        foreach (DungeonRoomContainer roomC in _dungeonRooms)
        {
            if (!canToxicGroundInStartRoom && roomC.IsStartRoom) continue;
            if (!canToxicGroundInBossRoom && roomC.IsBossRoom) continue;
            // Ýsteðe baðlý: Geçit odalarýnda da istemiyorsanýz:
            // if (roomC.IsGatewayRoom) continue; 

            for (int x = roomC.Rect.xMin; x < roomC.Rect.xMax; x++)
            {
                for (int z = roomC.Rect.yMin; z < roomC.Rect.yMax; z++)
                {
                    Vector2Int tilePos = new Vector2Int(x, z);
                    // Karoyu kontrol et: zemin mi, baþka bir dekorla dolu mu?
                    if (_mapData[x, z] == 1 && !_decorOccupiedTiles.Contains(tilePos) && Random.value < toxicGroundPlacementProbability)
                    {
                        int toxicCountThisCluster = 1;
                        if (Random.value < tryToFormToxicClusterProbability)
                        {
                            toxicCountThisCluster = Random.Range(minToxicGroundsInCluster, maxToxicGroundsInCluster + 1);
                        }

                        // Ýlk zehirli zemini yerleþtir
                        Vector3 firstToxicPos = new Vector3(x * tileSize + tileSize / 2f, toxicGroundYOffset, z * tileSize + tileSize / 2f);
                        Instantiate(toxicGroundPrefab, firstToxicPos, Quaternion.identity, roomC.ContainerGO.transform).name = $"ToxicGround_({x},{z})";
                        _decorOccupiedTiles.Add(tilePos);

                        // Küme için ek zehirli zeminleri yerleþtirmeye çalýþ
                        for (int i = 1; i < toxicCountThisCluster; i++)
                        {
                            int tx = x + Random.Range(-1, 2); // 1 karo etrafýnda yayýlým
                            int tz = z + Random.Range(-1, 2);
                            Vector2Int toxicClusterTilePos = new Vector2Int(tx, tz);

                            // Sýnýrlarý, zemini, dolu olup olmadýðýný ve oda içinde kalýp kalmadýðýný kontrol et
                            if (IsInBounds(tx, tz) &&
                                roomC.Rect.Contains(toxicClusterTilePos) &&
                                _mapData[tx, tz] == 1 &&
                                !_decorOccupiedTiles.Contains(toxicClusterTilePos))
                            {
                                Vector3 toxicPos = new Vector3(tx * tileSize + tileSize / 2f, toxicGroundYOffset, tz * tileSize + tileSize / 2f);
                                Instantiate(toxicGroundPrefab, toxicPos, Quaternion.identity, roomC.ContainerGO.transform).name = $"ToxicGround_Cluster_({tx},{tz})";
                                _decorOccupiedTiles.Add(toxicClusterTilePos);
                            }
                        }
                    }
                }
            }
        }
    }
    void PlaceLavaPlane()
    {
        if (lavaPlanePrefab == null) return;

        float worldMapWidth = mapWidth * tileSize;
        float worldMapHeight = mapHeight * tileSize;
        float actualLavaExtraScale = Mathf.Max(1.0f, lavaExtraScale);
        float prefabBaseSize = 10.0f; // Assuming the lavaPlanePrefab is 10x10 Unity units by default

        // Get the prefab's actual scale in case it's not (1,1,1) and its base size is different from 10x10
        MeshFilter mf = lavaPlanePrefab.GetComponent<MeshFilter>();
        Vector3 prefabMeshSize = Vector3.one * prefabBaseSize; // Default if no mesh
        if (mf != null && mf.sharedMesh != null)
        {
            prefabMeshSize = mf.sharedMesh.bounds.size;
        }

        float scaledPrefabWidth = prefabMeshSize.x * lavaPlanePrefab.transform.localScale.x;
        float scaledPrefabDepth = prefabMeshSize.z * lavaPlanePrefab.transform.localScale.z;

        if (scaledPrefabWidth <= 0) scaledPrefabWidth = prefabBaseSize; // Fallback
        if (scaledPrefabDepth <= 0) scaledPrefabDepth = prefabBaseSize; // Fallback


        float totalLavaCoverageWidth = worldMapWidth * actualLavaExtraScale;
        float totalLavaCoverageDepth = worldMapHeight * actualLavaExtraScale;

        if (scaledPrefabWidth <= 0 || scaledPrefabDepth <= 0) { Debug.LogError("Lav prefab boyutu 0 veya negatif olamaz!", gameObject); return; }

        int numPlanesX = Mathf.CeilToInt(totalLavaCoverageWidth / scaledPrefabWidth);
        int numPlanesZ = Mathf.CeilToInt(totalLavaCoverageDepth / scaledPrefabDepth);

        // Center of the entire map in world coordinates
        float mapWorldCenterX = (worldMapWidth / 2.0f) - (tileSize / 2.0f); // Adjusted for tile center
        float mapWorldCenterZ = (worldMapHeight / 2.0f) - (tileSize / 2.0f);


        // Starting position for the bottom-left lava plane, so the grid of planes is centered
        float startX = mapWorldCenterX - (numPlanesX * scaledPrefabWidth) / 2.0f + (scaledPrefabWidth / 2f);
        float startZ = mapWorldCenterZ - (numPlanesZ * scaledPrefabDepth) / 2.0f + (scaledPrefabDepth / 2f);


        for (int i = 0; i < numPlanesZ; i++)
        {
            for (int j = 0; j < numPlanesX; j++)
            {
                float currentPlanePosX = startX + j * scaledPrefabWidth;
                float currentPlanePosZ = startZ + i * scaledPrefabDepth;
                Vector3 lavaPosition = new Vector3(currentPlanePosX, lavaYOffset, currentPlanePosZ);
                GameObject lavaInstance = Instantiate(lavaPlanePrefab, lavaPosition, Quaternion.identity, mapParent);
                lavaInstance.name = $"LavaPlane_{j}_{i}";
                // If the lava prefab had a different scale, apply it, but usually, we tile unscaled prefabs.
                // lavaInstance.transform.localScale = lavaPlanePrefab.transform.localScale; (already handled by Instantiate)
            }
        }
    }

    void PlaceEnemySpawnPoints()
    {
        // Check if any enemy prefab is assigned, considering the new crystal enemies
        bool anyCrystalPrefabAssigned = crystalEnemyPrefabA != null || crystalEnemyPrefabB != null || crystalEnemyPrefabC != null;
        if (maxTotalEnemySpawns <= 0 || (skeletonPrefab == null && spiderPrefab == null && !anyCrystalPrefabAssigned))
        {
            Debug.LogWarning("Düþman spawn ayarlarý yetersiz veya prefablar eksik. Düþman spawn edilmeyecek.");
            foreach (DungeonRoomContainer roomC in _dungeonRooms)
            {
                if (roomC.Controller != null) roomC.Controller.RegisterEnemyCount(0);
            }
            return;
        }
        if (navMeshSurface == null || !navMeshSurface.navMeshData) // Check if NavMesh is baked
        {
            Debug.LogError("NavMeshSurface atanmamýþ veya NavMesh bake edilmemiþ. Düþmanlar doðru yerleþtirilemeyebilir.", gameObject);
            foreach (DungeonRoomContainer roomC in _dungeonRooms)
            {
                if (roomC.Controller != null) roomC.Controller.RegisterEnemyCount(0);
            }
            return;
        }

        _enemySpawnPositionsGizmo.Clear();
        int totalSpawnsPlacedOverall = 0;

        foreach (DungeonRoomContainer roomC in _dungeonRooms)
        {
            if (totalSpawnsPlacedOverall >= maxTotalEnemySpawns) break;
            if (roomC.Controller == null)
            {
                Debug.LogWarning($"Oda {roomC.ContainerGO.name} için RoomController bulunamadý. Bu odaya düþman spawn edilmeyecek.");
                continue;
            }

            bool allowSpawnThisRoom = true;
            int spawnsForThisSpecificRoom = Random.Range(minSpawnsPerRoom, maxSpawnsPerRoom + 1);

            if (roomC.IsStartRoom || roomC.IsBossRoom)
            {
                allowSpawnThisRoom = false;
                spawnsForThisSpecificRoom = 0;
            }

            if (!allowSpawnThisRoom || spawnsForThisSpecificRoom == 0)
            {
                roomC.Controller.RegisterEnemyCount(0);
                continue;
            }

            int spawnsPlacedInThisRoom = 0;
            List<Vector3> tempSpawnPositionsInRoom = new List<Vector3>();

            List<Vector2Int> validSpawnTiles = new List<Vector2Int>();
            for (int rx = roomC.Rect.xMin + minTileDistanceFromDecor; rx < roomC.Rect.xMax - minTileDistanceFromDecor; rx++)
            {
                for (int rz = roomC.Rect.yMin + minTileDistanceFromDecor; rz < roomC.Rect.yMax - minTileDistanceFromDecor; rz++)
                {
                    Vector2Int tile = new Vector2Int(rx, rz);
                    if (IsInBounds(rx, rz) && _mapData[rx, rz] == 1 && !_decorOccupiedTiles.Contains(tile))
                    {
                        validSpawnTiles.Add(tile);
                    }
                }
            }
            validSpawnTiles = validSpawnTiles.OrderBy(t => Random.value).ToList(); // Shuffle


            foreach (Vector2Int spawnTile in validSpawnTiles)
            {
                if (spawnsPlacedInThisRoom >= spawnsForThisSpecificRoom || totalSpawnsPlacedOverall >= maxTotalEnemySpawns) break;

                Vector3 potentialSpawnPos = new Vector3(spawnTile.x * tileSize + tileSize / 2f, 0.1f, spawnTile.y * tileSize + tileSize / 2f);

                bool tooCloseToOtherSpawns = false;
                foreach (Vector3 existingSpawn in _enemySpawnPositionsGizmo)
                    if (Vector3.Distance(potentialSpawnPos, existingSpawn) < minDistanceBetweenSpawns) { tooCloseToOtherSpawns = true; break; }
                if (tooCloseToOtherSpawns) continue;
                foreach (Vector3 existingRoomSpawn in tempSpawnPositionsInRoom)
                    if (Vector3.Distance(potentialSpawnPos, existingRoomSpawn) < minDistanceBetweenSpawns) { tooCloseToOtherSpawns = true; break; }
                if (tooCloseToOtherSpawns) continue;


                NavMeshHit hit;
                if (NavMesh.SamplePosition(potentialSpawnPos, out hit, spawnPointNavMeshSampleRadius, NavMesh.AllAreas))
                {
                    GameObject prefabToSpawn = null;
                    bool canSpawnCrystal = (crystalEnemyPrefabA != null || crystalEnemyPrefabB != null || crystalEnemyPrefabC != null);

                    // Decision logic based on priority: Crystal > Spider > Skeleton
                    if (canSpawnCrystal && Random.value < crystalEnemyChanceInStandardRoom)
                    {
                        List<GameObject> crystalVariants = new List<GameObject>();
                        if (crystalEnemyPrefabA != null) crystalVariants.Add(crystalEnemyPrefabA);
                        if (crystalEnemyPrefabB != null) crystalVariants.Add(crystalEnemyPrefabB);
                        if (crystalEnemyPrefabC != null) crystalVariants.Add(crystalEnemyPrefabC);

                        if (crystalVariants.Count > 0) // Ensure list is not empty
                        {
                            prefabToSpawn = crystalVariants[Random.Range(0, crystalVariants.Count)];
                        }
                    }
                    else if (spiderPrefab != null && Random.value < spiderChanceInStandardRoom)
                    {
                        prefabToSpawn = spiderPrefab;
                    }
                    else if (skeletonPrefab != null)
                    {
                        prefabToSpawn = skeletonPrefab;
                    }
                    else
                    {
                        if (canSpawnCrystal)
                        {
                            List<GameObject> crystalVariants = new List<GameObject>();
                            if (crystalEnemyPrefabA != null) crystalVariants.Add(crystalEnemyPrefabA);
                            if (crystalEnemyPrefabB != null) crystalVariants.Add(crystalEnemyPrefabB);
                            if (crystalEnemyPrefabC != null) crystalVariants.Add(crystalEnemyPrefabC);
                            if (crystalVariants.Count > 0) prefabToSpawn = crystalVariants[Random.Range(0, crystalVariants.Count)];
                        }
                        else if (spiderPrefab != null)
                        {
                            prefabToSpawn = spiderPrefab;
                        }
                    }

                    if (prefabToSpawn != null)
                    {
                        Vector3 finalSpawnPosition = hit.position; // NavMesh'ten gelen temel pozisyon

                        // Seçilen prefab kristal düþman ise Y ofsetini uygula
                        if (prefabToSpawn == crystalEnemyPrefabA ||
                            prefabToSpawn == crystalEnemyPrefabB ||
                            prefabToSpawn == crystalEnemyPrefabC)
                        {
                            finalSpawnPosition.y += crystalEnemyYOffset;
                        }

                        GameObject enemyGO = Instantiate(prefabToSpawn, finalSpawnPosition, Quaternion.identity, roomC.ContainerGO.transform);
                        enemyGO.name = $"{prefabToSpawn.name}_{roomC.ContainerGO.name}_{spawnsPlacedInThisRoom}";

                        EnemyAI ai = enemyGO.GetComponent<EnemyAI>();
                        if (ai != null) ai.AssignRoomController(roomC.Controller);
                        else Debug.LogWarning($"{enemyGO.name} üzerinde EnemyAI scripti bulunamadý.");

                        _enemySpawnPositionsGizmo.Add(finalSpawnPosition); // Gizmo için güncellenmiþ pozisyonu kullan
                        tempSpawnPositionsInRoom.Add(finalSpawnPosition);  // Oda içi pozisyon için güncellenmiþ pozisyonu kullan
                        _decorOccupiedTiles.Add(spawnTile);

                        enemyGO.SetActive(false);

                        spawnsPlacedInThisRoom++;
                        totalSpawnsPlacedOverall++;
                        if (enemySpawnMarkerPrefab != null) Instantiate(enemySpawnMarkerPrefab, finalSpawnPosition, Quaternion.identity, roomC.ContainerGO.transform);
                    }
                }
            }
            roomC.Controller.RegisterEnemyCount(spawnsPlacedInThisRoom);
        }
        Debug.Log($"{totalSpawnsPlacedOverall} adet düþman spawn noktasý yerleþtirildi.");
    }

    bool ValidateSettings()
    {
        bool isValid = true;
        if (tilePrefabs == null || tilePrefabs.Length != 3 || tilePrefabs.Any(p => p == null)) { Debug.LogError("HATA: 'Tile Prefabs' dizisi (Köþe, Kenar, Zemin) düzgün atanmamýþ!", gameObject); isValid = false; }
        if (minSpawnsPerRoom < 0) { Debug.LogWarning("Min Spawns Per Room 0'dan küçük olamaz.", gameObject); minSpawnsPerRoom = 0; }
        if (maxSpawnsPerRoom < minSpawnsPerRoom && maxSpawnsPerRoom > 0) { Debug.LogWarning("Max Spawns Per Room, Min Spawns Per Room'dan küçük olamaz (0 deðilse).", gameObject); maxSpawnsPerRoom = minSpawnsPerRoom; }
        if (mapParent == null) Debug.LogWarning("Map Parent atanmamýþ. Otomatik olarak 'GeneratedMap_Root' oluþturulacak veya bulunacak.", gameObject);
        if (navMeshSurface == null) Debug.LogWarning("NavMeshSurface atanmamýþ. Map Parent üzerinde aranacak veya eklenecek.", gameObject);

        bool anyCrystalPrefabAssigned = crystalEnemyPrefabA != null || crystalEnemyPrefabB != null || crystalEnemyPrefabC != null;
        if (skeletonPrefab == null && spiderPrefab == null && !anyCrystalPrefabAssigned && maxTotalEnemySpawns > 0)
        {
            Debug.LogWarning("Düþman spawn edilecek ama Ýskelet, Örümcek ve Kristal prefablarýndan en az biri atanmamýþ!", gameObject);
        }

        return isValid;
    }

    void SetupParentAndNavMeshSurface()
    {
        if (mapParent == null)
        {
            GameObject existingParent = GameObject.Find("GeneratedMap_Root");
            mapParent = (existingParent != null) ? existingParent.transform : new GameObject("GeneratedMap_Root").transform;
        }
        if (navMeshSurface == null && mapParent != null)
        {
            navMeshSurface = mapParent.GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                Debug.Log("Mevcut mapParent üzerinde NavMeshSurface bulunamadý, yeni bir tane ekleniyor.", gameObject);
                navMeshSurface = mapParent.gameObject.AddComponent<NavMeshSurface>();
            }
        }
        else if (mapParent == null) // Should be caught by ValidateSettings if critical
        {
            Debug.LogError("mapParent null olduðu için NavMeshSurface ayarlanamadý!", gameObject);
        }
    }

    public void ClearMap()
    {
        if (mapParent == null)
        {
            if (!Application.isPlaying) mapParent = GameObject.Find("GeneratedMap_Root")?.transform;
            if (mapParent == null) return; // Nothing to clear if no parent
        }
        // Destroy children
        for (int i = mapParent.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(mapParent.GetChild(i).gameObject);
            else DestroyImmediate(mapParent.GetChild(i).gameObject);
        }
        // Clear lists and data structures
        _dungeonRooms.Clear();
        _dungeonCorridors.Clear();
        if (_mapData != null) System.Array.Clear(_mapData, 0, _mapData.Length); // Clear existing data
        _decorOccupiedTiles.Clear();
        _enemySpawnPositionsGizmo.Clear();
        _tileToRoomLookup.Clear();
        _tileToCorridorLookup.Clear();
        _generatedStartRoomContainer = null;
        _generatedBossRoomContainer = null;

        // If NavMesh was baked on a surface that's part of mapParent, it might need clearing too
        // but Unity's NavMeshSurface handles its data. If you have custom NavMesh data management, add here.
    }

    void BakeDungeonNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("NavMeshSurface bulunamadý. NavMesh bake iþlemi yapýlamayacak. Lütfen mapParent objesine bir NavMeshSurface component'i ekleyin veya atayýn.", gameObject);
            return;
        }
        // Clear existing NavMesh data before baking new one, if any
        navMeshSurface.RemoveData();
        Debug.Log("NavMesh bake iþlemi baþlatýlýyor...", gameObject);
        navMeshSurface.BuildNavMesh();
        Debug.Log("NavMesh bake iþlemi tamamlandý.", gameObject);
    }

    Quaternion GetRotationAwayFromWall(int x, int z)
    {
        bool wallN = IsWallAt(x, z + 1);
        bool wallS = IsWallAt(x, z - 1);
        bool wallE = IsWallAt(x + 1, z);
        bool wallW = IsWallAt(x - 1, z);
        int wallCount = (wallN ? 1 : 0) + (wallS ? 1 : 0) + (wallE ? 1 : 0) + (wallW ? 1 : 0); // GetSurroundingWallCount(x,z);

        if (wallCount == 1) // Against a single wall, face away from it
        {
            if (wallN) return Quaternion.Euler(0, 180, 0); // Wall North, face South
            if (wallS) return Quaternion.Euler(0, 0, 0);   // Wall South, face North
            if (wallE) return Quaternion.Euler(0, 270, 0); // Wall East, face West
            if (wallW) return Quaternion.Euler(0, 90, 0);  // Wall West, face East
        }
        else if (wallCount == 2 && !AreWallsOpposite(x, z)) // In a corner
        {
            if (wallN && wallW) return Quaternion.Euler(0, 135, 0); // NW corner, face SE
            if (wallN && wallE) return Quaternion.Euler(0, 225, 0); // NE corner, face SW
            if (wallS && wallW) return Quaternion.Euler(0, 45, 0);  // SW corner, face NE
            if (wallS && wallE) return Quaternion.Euler(0, 315, 0); // SE corner, face NW
        }
        // Default or if in open space / against opposite walls / 3 walls (alcove)
        return Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0); // Random 90-degree rotation
    }

    bool AreWallsOpposite(int x, int z)
    {
        bool wallN = IsWallAt(x, z + 1);
        bool wallS = IsWallAt(x, z - 1);
        bool wallE = IsWallAt(x + 1, z);
        bool wallW = IsWallAt(x - 1, z);
        return (wallN && wallS && !wallE && !wallW) || (wallE && wallW && !wallN && !wallS);
    }

#if UNITY_EDITOR
    [ContextMenu("Zindaný Editörde Oluþtur (NavMesh ile)")]
    void GenerateDungeonInEditor()
    {
        if (!ValidateSettings()) { Debug.LogError("Ayarlar geçersiz, Editör'de zindan oluþturma iptal edildi.", gameObject); return; }
        SetupParentAndNavMeshSurface(); // Ensure mapParent and navMeshSurface are assigned or created.

        if (navMeshSurface == null && mapParent != null) // Double check after Setup
        {
            if (!Application.isPlaying) // Only prompt in Editor mode
            {
                if (EditorUtility.DisplayDialog("NavMeshSurface Eksik",
                  $"'{mapParent.name}' objesinde NavMeshSurface component'i bulunamadý. Otomatik olarak eklensin mi?", "Evet, Ekle", "Hayýr"))
                {
                    navMeshSurface = mapParent.gameObject.AddComponent<NavMeshSurface>();
                    EditorUtility.SetDirty(mapParent.gameObject); // Mark for save
                }
                else
                {
                    Debug.LogError("NavMeshSurface eklenmedi. Zindan oluþturma ve NavMesh bake iþlemi düzgün çalýþmayabilir.", gameObject);
                    // Optionally, could prevent generation if NavMesh is critical: return;
                }
            }
        }
        else if (mapParent == null) // Should be caught by ValidateSettings
        {
            Debug.LogError("mapParent bulunamadý! Zindan oluþturulamaz.", gameObject); return;
        }

        Undo.SetCurrentGroupName("Generate Dungeon");
        int group = Undo.GetCurrentGroup();
        Undo.RegisterCompleteObjectUndo(this, "Dungeon Generator State");
        if (mapParent != null) Undo.RegisterFullObjectHierarchyUndo(mapParent.gameObject, "Dungeon Map Objects");


        GenerateDungeon(); // This clears and generates

        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this); // Mark this component as dirty
            if (mapParent != null) EditorUtility.SetDirty(mapParent.gameObject); // Mark parent as dirty
            // Mark all created GameObjects under mapParent as dirty
            for (int i = 0; i < mapParent.childCount; i++)
            {
                EditorUtility.SetDirty(mapParent.GetChild(i).gameObject);
                // Also mark RoomController components on children if they exist
                RoomController rc = mapParent.GetChild(i).GetComponent<RoomController>();
                if (rc != null) EditorUtility.SetDirty(rc);

                // Mark child's children too (tiles, enemies etc.)
                Transform roomOrCorridorTransform = mapParent.GetChild(i);
                for (int j = 0; j < roomOrCorridorTransform.childCount; j++)
                {
                    EditorUtility.SetDirty(roomOrCorridorTransform.GetChild(j).gameObject);
                }
            }

            if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        Undo.CollapseUndoOperations(group);
    }

    [ContextMenu("Haritayý Editörde Temizle")]
    void ClearMapInEditor()
    {
        SetupParentAndNavMeshSurface(); // Ensure mapParent is found/created if null
        if (mapParent != null) // Only proceed if mapParent exists
        {
            Undo.SetCurrentGroupName("Clear Dungeon Map");
            Undo.RegisterFullObjectHierarchyUndo(mapParent.gameObject, "Clear Dungeon Map Objects");
            Undo.RegisterCompleteObjectUndo(this, "Dungeon Generator State Update");
        }


        ClearMap(); // Perform the map clearing operations

        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            if (mapParent != null) EditorUtility.SetDirty(mapParent.gameObject);
            if (gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif

    private void OnDrawGizmos()
    {
        if (_enemySpawnPositionsGizmo != null && _enemySpawnPositionsGizmo.Count > 0)
        {
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.7f); // Reddish for enemies
            foreach (Vector3 spawnPos in _enemySpawnPositionsGizmo)
            {
                Gizmos.DrawSphere(spawnPos, 0.4f);
                Gizmos.DrawRay(spawnPos, Vector3.up * 1.5f);
            }
        }

        // Optional: Gizmos for room rectangles if needed for debugging
        // if (_dungeonRooms != null && _dungeonRooms.Count > 0)
        // {
        // Gizmos.color = Color.yellow;
        // foreach (var roomC in _dungeonRooms)
        // {
        // Vector3 roomCenterWorld = new Vector3(roomC.Rect.center.x * tileSize, 0, roomC.Rect.center.y * tileSize);
        // Vector3 roomSizeWorld = new Vector3(roomC.Rect.width * tileSize, 1, roomC.Rect.height * tileSize);
        // Gizmos.DrawWireCube(roomCenterWorld, roomSizeWorld);
        // if (roomC.IsStartRoom) Gizmos.color = Color.green;
        // else if (roomC.IsBossRoom) Gizmos.color = Color.red;
        // else if (roomC.IsGatewayRoom) Gizmos.color = Color.blue;
        // else Gizmos.color = Color.yellow;
        // Gizmos.DrawCube(roomCenterWorld, Vector3.one * tileSize * 0.5f);
        // }
        // }
    }
}