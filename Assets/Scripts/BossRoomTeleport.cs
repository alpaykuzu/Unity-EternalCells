using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management

public class BossRoomTeleport : MonoBehaviour
{
    [Tooltip("The name of the Main Menu scene to load.")]
    public string mainMenuSceneName = "MainMenu"; // Ensure this matches your scene name exactly

    [Tooltip("The tag assigned to the player GameObject.")]
    public string playerTag = "Player"; // Ensure your player has this tag

    private bool _isTeleporting = false; // Prevents multiple teleport attempts

    // This function is called when another Collider enters the trigger.
    private void OnTriggerEnter(Collider other)
    {
        // Check if already teleporting or if the object that entered is not the player
        if (_isTeleporting || !other.CompareTag(playerTag))
        {
            return;
        }

        // Player has entered the teleport
        Debug.Log($"Player ({other.name}) entered the Boss Room Teleport. Loading scene: {mainMenuSceneName}");
        _isTeleporting = true; // Set flag to prevent multiple calls

        // You could add a fade-out effect or a short delay here if desired.
        // For example, using a coroutine: StartCoroutine(DelayedLoadScene());

        // Directly load the main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Optional: Reset the flag if the teleport GameObject is disabled and re-enabled.
    private void OnDisable()
    {
        _isTeleporting = false;
    }

    // Example of a delayed scene load with a coroutine (optional)
    // System.Collections.IEnumerator DelayedLoadScene()
    // {
    //     // Play fade animation, sound, etc.
    //     yield return new WaitForSeconds(1.0f); // Wait for 1 second
    //     SceneManager.LoadScene(mainMenuSceneName);
    // }
}