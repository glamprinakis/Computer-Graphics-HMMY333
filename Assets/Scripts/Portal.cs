using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public float detectionRadius = 3.0f; // Set the radius for detection
    private Collider[] detectedObjects; // Array to hold all detected objects
    private int playerMask; // LayerMask to filter for player units

    void Start()
    {
        // Set the LayerMask to the Player layer.
        // Assumes your player units are on a layer named "Player"
        playerMask = LayerMask.GetMask("Player Units");
    }

    void Update()
    {
        // Detect all player units within the portal's detection radius
        detectedObjects = Physics.OverlapSphere(transform.position, detectionRadius, playerMask);

        // If any player units were detected...
        foreach (Collider collider in detectedObjects)
        {
            // Cast a ray towards each detected player unit
            Vector3 directionToPlayer = collider.transform.position - transform.position;
            Ray ray = new Ray(transform.position, directionToPlayer);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, detectionRadius))
            {
                Debug.Log("GAMO TIN PANAGIA MOU ");
                MODZ.RTS.Player.LevelManager levelManager = FindObjectOfType<MODZ.RTS.Player.LevelManager>();
                if (levelManager != null)
                {
                    // Set the portal reference in the LevelManager
                    levelManager.portal = this;

                    // Start the teleportation coroutine
                    StartCoroutine(levelManager.TeleportUnits());
                }
                
            }
        }
    }
}

