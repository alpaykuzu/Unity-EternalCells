// RoomController.cs
// Her bir odanýn durumunu (aktiflik, düþman sayýsý, temizlenme durumu) yönetir.
// Baðlý olduðu koridorlarý ve sonraki odalarý aktive eder.
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoomController : MonoBehaviour
{
    public RectInt roomRect;
    public bool isActive = false; // Odanýn mantýksal durumu (içindeki olaylar aktif mi?)
    public bool isCleared = false;
    public bool isStartRoomNode = false;

    private List<EnemyAI> enemiesInRoom = new List<EnemyAI>();
    private int totalEnemiesToClear = 0; 
    private int enemiesDefeatedCount = 0;

    private List<RoomController> connectedRooms = new List<RoomController>();
    private List<GameObject> connectingCorridors = new List<GameObject>();

    public void SetInitialActiveState(bool isRoomLogicallyActive, bool isStart)
    {
        this.isStartRoomNode = isStart;
        this.isActive = isRoomLogicallyActive; // True if start room, false otherwise initially
        this.gameObject.SetActive(true);    // Room GameObject is always visible

        FindAndRegisterEnemies(); // Populate enemiesInRoom list, even if it's empty

        if (this.isStartRoomNode)
        {
            // Start room specific logic
            // Debug.Log($"{name} (Start Room) initializing. Total enemies to clear set to 0.");
            this.totalEnemiesToClear = 0; // Start room has no enemies to clear by definition now
            this.enemiesDefeatedCount = 0;
            // Enemies in start room (if any were accidentally placed by old logic or manually)
            // will remain inactive as generator now sets all enemies inactive initially.
            // This controller won't activate them if it's a start room.
            MarkAsCleared(); // Open pathways immediately for start room
        }
        else // For non-start rooms (including Boss room, which also has no enemies now)
        {
            // If this non-start room's logic is active from the start (e.g. future pre-cleared room)
            // AND it has no enemies (which will be true for Boss room as per generator changes)
            if (this.isActive) 
            {
                // Debug.Log($"{name}: Logic is active. Activating enemies if any (should be none for Boss).");
                ActivateEnemiesInRoom(); // Will activate enemies if any were spawned and made active by generator (now none for boss)
                if (totalEnemiesToClear == 0 && !isCleared) // If active and no enemies (true for Boss room)
                {
                    MarkAsCleared();
                }
            }
            // else: Debug.Log($"{name}: Logic is not initially active. Enemies will be activated on entry.");
        }
    }
    
    public void ActivateRoomOnEntry()
    {
        if (this.isActive) // If room logic is already active, or GameObject is inactive (shouldn't be)
        {
            // Debug.Log($"{name}: ActivateRoomOnEntry called, but room logic is already active.");
            return;
        }

        // Debug.Log($"{name}: ActivateRoomOnEntry called. Activating room logic and enemies.");
        this.isActive = true;          // This room's logic is now active.
        this.gameObject.SetActive(true); // Ensure GameObject is active (should be already).

        // enemiesInRoom list is populated by SetInitialActiveState.
        ActivateEnemiesInRoom(); // Activate enemies for this room.

        // If, upon entry, there are no enemies to clear (e.g., a decorative room or a bug)
        // and it hasn't been cleared yet, mark it as cleared.
        if (totalEnemiesToClear == 0 && !isCleared)
        {
            // Debug.Log($"{name}: Activated on entry with no enemies to clear. Marking as cleared.");
            MarkAsCleared();
        }
    }

    void FindAndRegisterEnemies()
    {
        enemiesInRoom.Clear();
        // GetComponentsInChildren<EnemyAI>(true) finds all EnemyAI, even if their GameObject is inactive.
        // This is important because RandomDungeonGenerator now sets all spawned enemies to inactive initially.
        EnemyAI[] foundEnemies = GetComponentsInChildren<EnemyAI>(true); 

        foreach (EnemyAI enemy in foundEnemies)
        {
            enemiesInRoom.Add(enemy);
            if (enemy.roomController == null) // Assign this room to the enemy if not already set
            {
                enemy.AssignRoomController(this);
            }
        }
        // Debug.Log($"{name}: FindAndRegisterEnemies found {enemiesInRoom.Count} enemy components. totalEnemiesToClear is currently {totalEnemiesToClear}.");
    }
    
    public void RegisterEnemyCount(int count)
    {
        if (this.isStartRoomNode) // Start room never has enemies to clear
        {
            this.totalEnemiesToClear = 0;
        }
        else
        {
            this.totalEnemiesToClear = count;
        }
        this.enemiesDefeatedCount = 0; // Reset defeated count
        // Debug.Log($"{name} registered with {this.totalEnemiesToClear} enemies. IsStart: {this.isStartRoomNode}");

        // If this room is already active (e.g. a non-start room that became active for some reason, or Boss room)
        // and it's registered with 0 enemies, and not already cleared, then clear it.
        // For the Start Room, MarkAsCleared() is handled in SetInitialActiveState.
        if (this.isActive && !this.isStartRoomNode && this.totalEnemiesToClear == 0 && !isCleared)
        {
            // Debug.Log($"{name} is active, not start, has 0 enemies registered. Marking as cleared.");
            MarkAsCleared();
        }
    }

    void ActivateEnemiesInRoom()
    {
        // Debug.Log($"{name}: Attempting to activate {enemiesInRoom.Count} enemies from list.");
        foreach (EnemyAI enemy in enemiesInRoom)
        {
            if (enemy != null && !enemy.gameObject.activeSelf)
            {
                // Debug.Log($"Activating enemy: {enemy.name} in room {gameObject.name}");
                enemy.gameObject.SetActive(true); // Make the enemy GameObject active
            }
            // else if (enemy != null) Debug.Log($"Enemy {enemy.name} in {gameObject.name} was already active or is null.");
        }
    }

    public void AddConnection(RoomController otherRoom, GameObject corridorGO)
    {
        if (!connectedRooms.Contains(otherRoom))
        {
            connectedRooms.Add(otherRoom);
        }
        if (corridorGO != null && !connectingCorridors.Contains(corridorGO))
        {
            connectingCorridors.Add(corridorGO);
        }
    }

    public void OnEnemyDefeated(EnemyAI defeatedEnemy)
    {
        if (!isActive || isCleared) return; // Only process if room is active and not yet cleared

        enemiesDefeatedCount++;
        // Debug.Log($"{name}: Enemy defeated. {enemiesDefeatedCount}/{totalEnemiesToClear} defeated.");

        if (enemiesDefeatedCount >= totalEnemiesToClear)
        {
            MarkAsCleared();
        }
    }

    public void MarkAsCleared()
    {
        if (isCleared) return; // Already marked
        isCleared = true;
        // Debug.Log($"{gameObject.name} (Room) is cleared! Activating connected pathways.");
        ActivateConnectedPathways();
    }

    public void ActivateConnectedPathways()
    {
        // Debug.Log($"{gameObject.name} is activating connected pathways. Corridors: {connectingCorridors.Count}, Rooms: {connectedRooms.Count}");
        foreach (GameObject corridorGO in connectingCorridors)
        {
            if (corridorGO != null)
            {
                // Debug.Log($"Activating corridor: {corridorGO.name}");
                corridorGO.SetActive(true); // Make the corridor GameObject visible/active
            }
        }

        // For connected rooms, their GameObjects should already be active (visible).
        // We call ActivateRoomOnEntry for them if their *logic* isn't active yet.
        // This will, in turn, activate their enemies if they have any.
        foreach (RoomController room in connectedRooms)
        {
            if (room != null && !room.isActive) 
            {
                // Debug.Log($"Preparing to activate connected room: {room.gameObject.name} via ActivateRoomOnEntry.");
                room.ActivateRoomOnEntry();
            }
            // else if (room != null && room.isActive) Debug.Log($"Connected room {room.gameObject.name} is already logically active.");
        }
    }
}
