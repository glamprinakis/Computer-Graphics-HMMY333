using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MODZ.RTS.InputManager;
using TMPro;

namespace MODZ.RTS.Player
{

    // Define an enumeration for the two states of the game, free roam and combat mode
    public enum GameState
    {
        FreeRoam,
        CombatMode
    }

    // Main class for managing the player, inheriting from Unity's MonoBehaviour
    public class PlayerManager : MonoBehaviour
    {
        // Singleton pattern: only one instance of this class should exist
        public static PlayerManager instance;

        // Collections for player and enemy units
        private List<Units.Player.PlayerUnit> pRolls = new List<Units.Player.PlayerUnit>();
        private List<Units.Enemy.EnemyUnit> eRolls = new List<Units.Enemy.EnemyUnit>();

        // Reference to GameObject used for end credits
        public GameObject credits;

        // References to parent objects containing player and enemy units
        public Transform playerUnits;
        public Transform enemyUnits;

        // UI Text components for displaying text during FreeRoam and CombatMode
        public TextMeshProUGUI freeRoamText;
        public TextMeshProUGUI combatText;

        // Flag for if units are autonomous (AI controlled) or manual
        public bool isAutonomous = true;

        // Counter to keep track of the number of maps
        private int mapCount = 1;

        // Flag for if the alpha1 key has been pressed
        bool alpha1KeyPressed = false;

        // Default game state is FreeRoam
        private GameState currentState = GameState.FreeRoam;

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            // Singleton instance set up
            instance = this;

            // Set basic stats for units
            Units.UnitHandler.instance.SetBasicUnitStats(playerUnits);
            Units.UnitHandler.instance.SetBasicUnitStats(enemyUnits);

            // Subscribe to unit destroyed events
            Units.Player.PlayerUnit.OnPlayerUnitDestroyed += HandlePlayerUnitDestroyed;
            Units.Enemy.EnemyUnit.OnEnemyUnitDestroyed += HandleEnemyUnitDestroyed;

            // Check GlobalVariables for enemy type and set isAutonomous accordingly
            if (GlobalVariables.enemyType == "Manual")
            {
                isAutonomous = false;
            }
            else
            {
                isAutonomous = true;
            }
        }

        // Called when this GameObject is destroyed
        private void OnDestroy()
        {
            // Unsubscribe from unit destroyed events
            Units.Player.PlayerUnit.OnPlayerUnitDestroyed -= HandlePlayerUnitDestroyed;
            Units.Enemy.EnemyUnit.OnEnemyUnitDestroyed -= HandleEnemyUnitDestroyed;
        }

        // Handle the event of a player unit being destroyed
        private void HandlePlayerUnitDestroyed(Units.Player.PlayerUnit unit)
        {
            // Remove the destroyed unit from the player unit list
            pRolls.Remove(unit);
        }

        // Handle the event of an enemy unit being destroyed
        private void HandleEnemyUnitDestroyed(Units.Enemy.EnemyUnit unit)
        {
            // Remove the destroyed unit from the enemy unit list
            eRolls.Remove(unit);
        }

        // Called before the first frame update
        private void Start()
        {
            // Begin the game loop coroutine
            StartCoroutine(GameLoopCoroutine());
        }

        // Called once per frame
        private void Update()
        {
            // Handle game state-specific logic
            switch (currentState)
            {
                case GameState.FreeRoam:
                    // Handle unit movement if in free roam mode
                    InputHandler.instance.HandleUnitMovement();
                    ActivateFreeRoamText();
                    break;
                case GameState.CombatMode:
                    // Handle combat mode text display
                    ActivateCombatText();
                    break;
            }
        }

        // Game loop coroutine, yielding execution until certain conditions are met
        public IEnumerator GameLoopCoroutine()
        {
            // Infinite loop, only ending when the application is closed
            while (true)
            {
                // Pause execution until the next frame
                yield return null;

                // Handle game state-specific logic
                switch (currentState)
                {
                    case GameState.FreeRoam:
                        // Check if any unit has detected an opponent, if so, switch to combat mode
                        if (CheckForCombatTrigger())
                        {
                            currentState = GameState.CombatMode;
                            StartCoroutine(CombatModeCoroutine());
                        }
                        break;
                    case GameState.CombatMode:
                        // Check if all enemy units or all player units are dead, if so, switch back to free roam
                        if (allDead())
                        {
                            currentState = GameState.FreeRoam;
                            Debug.Log("Transitioning back to FreeRoam");
                        }
                        break;
                }
            }
        }

        // Coroutine for handling combat mode
        public IEnumerator CombatModeCoroutine()
        {
            // Log entering combat mode
            Debug.Log("Entering Combat mode");

            // Wait for all player units to stop
            yield return StartCoroutine(StopAllPlayerUnitsImmediately());

            // Reset all units' canceled rounds counter
            foreach (Units.Enemy.EnemyUnit eUnit in eRolls)
            {
                eUnit.canceledRounds = 0;
            }
            foreach (Units.Player.PlayerUnit pUnit in pRolls)
            {
                pUnit.canceledRounds = 0;
            }

            // Loop until all units are dead
            while (!allDead())
            {
                // If no units are in play, roll for new actions
                if (pRolls.Count == 0 && eRolls.Count == 0)
                {
                    Diseroll();
                }
                // Wait for next action execution
                yield return StartCoroutine(ExecuteNextActionCoroutine());
            }

            // Log combat mode end
            Debug.Log("Combat mode finished");
        }

        // Check if all player or enemy units are dead
        private bool allDead()
        {
            // Transition back to FreeRoam if all enemy units or all player units are dead
            bool allEnemiesDead = enemyUnits.GetComponentsInChildren<Units.Enemy.EnemyUnit>(true).Length == 0;
            bool allPlayersDead = playerUnits.GetComponentsInChildren<Units.Player.PlayerUnit>(true).Length == 0;

            if (allEnemiesDead || allPlayersDead)
            {
                // If all enemy or player units are dead, do the following:
                // Find the LevelManager and trigger appropriate actions
                // If all maps are cleared, show the credits and pause the game
                // Otherwise, generate a new map
                LevelManager levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null && levelManager.alldead)
                {
                    if (mapCount < 4)
                    {
                        levelManager.PortalGeneration();
                        
                    }
                    else if (mapCount >= 4)
                    {
                        credits.SetActive(true);
                        Time.timeScale = 0f;
                    }
                    mapCount++;
                    levelManager.alldead = false;
                    DisableRollText();

                }
                return true;
            }
            return false;
        }

        // Check if any player or enemy unit has detected an opponent
        private bool CheckForCombatTrigger()
        {
            // Check if any player unit or enemy unit has detected an opponent
            foreach (Units.Player.PlayerUnit pUnit in playerUnits.GetComponentsInChildren<Units.Player.PlayerUnit>())
            {
                if (pUnit.CheckForEnemyTargets())
                {
                    return true; // Enter combat mode
                }
            }

            foreach (Units.Enemy.EnemyUnit eUnit in enemyUnits.GetComponentsInChildren<Units.Enemy.EnemyUnit>())
            {
                if (eUnit.CheckForEnemyTargets())
                {
                    return true; // Enter combat mode
                }
            }

            return false; // No combat trigger detected
        }

        public IEnumerator ExecuteNextActionCoroutine()
        {
            while (pRolls.Count > 0 || eRolls.Count > 0)
            {
                if (allDead())
                {
                    break;
                }

                // Get the highest-rolled unit from both sides
                Units.Player.PlayerUnit pUnit = pRolls.Count > 0 ? pRolls.OrderByDescending(p => p.roll).First() : null;
                Units.Enemy.EnemyUnit eUnit = eRolls.Count > 0 ? eRolls.OrderByDescending(e => e.roll).First() : null;

                // If both types of units exist and the player unit has a higher or equal roll, or if there are no enemy units
                if ((pUnit != null && eUnit != null && pUnit.roll >= eUnit.roll) || eUnit == null)
                {
                    // Player unit acts
                    pUnit.isActive = true;
                    Debug.Log($"It's Player unit: {pUnit.name} turn with roll: {pUnit.roll}");

                    // Wait for player input/action
                    while (pUnit.isActive && !pUnit.InputReceived)
                    {
                        diselectAllPlayers();
                        enablePlayerAttackRangeHightLight(pUnit);

                        // Handle player input here
                        if (Input.GetKeyDown(KeyCode.Alpha1) && !alpha1KeyPressed)
                        {
                            Debug.Log("Aplha1 key pressed");
                            yield return pUnit.StartCoroutine(pUnit.ThreeStepAttack());
                            yield return pUnit.StartCoroutine(pUnit.IsUnitStationary(pUnit, isStationary => {
                                if (isStationary)
                                {
                                    pUnit.InputReceived = true;
                                }
                            }));
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha2))
                        {
                            Debug.Log("Dash");
                            yield return pUnit.StartCoroutine(pUnit.Dash());
                            yield return pUnit.StartCoroutine(pUnit.IsUnitStationary(pUnit, isStationary => {
                                if (isStationary)
                                {
                                    pUnit.InputReceived = true;
                                }
                            }));
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha3))
                        {
                            Debug.Log("Aplha3 key pressed");
                            GameObject parentObject = pUnit.transform.parent.gameObject;
                            if (parentObject.name == "Fighters")
                            {
                                Debug.Log("Cleave attack");
                                yield return StartCoroutine(pUnit.TwoStepCleve());
                            }
                            else if (parentObject.name == "Priests")
                            {
                                Debug.Log("Bless");
                                yield return StartCoroutine(pUnit.ThreeStepBless());
                            }
                            else if (parentObject.name == "Rangers")
                            {
                                Debug.Log("Projectile attack");
                                yield return StartCoroutine(pUnit.ThreeStepAreaAttack());

                            }
                            else if (parentObject.name == "Mages")
                            {
                                Debug.Log("Projectile attack");
                                yield return StartCoroutine(pUnit.AreaAttack());
                            }
                            disablePlayerAttackRangeHightLight(pUnit);
                            yield return pUnit.StartCoroutine(pUnit.IsUnitStationary(pUnit, isStationary => {
                                if (isStationary)
                                {
                                    pUnit.InputReceived = true;
                                }
                            }));
                        }
                        else if (Input.GetKeyDown(KeyCode.Alpha4))
                        {
                            Debug.Log("Aplha4 key pressed");
                            GameObject parentObject = pUnit.transform.parent.gameObject;
                            if (parentObject.name == "Fighters")
                            {
                                Debug.Log("I will be your protector");
                                yield return StartCoroutine(pUnit.TwoStepProtect());
                            }
                            else if (parentObject.name == "Rangers")
                            {
                                Debug.Log("Aiming at all targets...");
                                yield return StartCoroutine(pUnit.TwoStepMultiHit());
                            }
                            else if (parentObject.name == "Priests")
                            {
                                Debug.Log("Healing the allies...");
                                yield return StartCoroutine(pUnit.TwoStepHeal());
                            }
                            else if (parentObject.name == "Mages")
                            {
                                Debug.Log("Preparing to cancel enemy's round...");
                                yield return StartCoroutine(pUnit.ThreeStepCancelRound());
                            }
                            disablePlayerAttackRangeHightLight(pUnit);
                            pUnit.InputReceived = true;
                        }else if (Input.GetKeyDown(KeyCode.Alpha5))
                        {
                            Debug.Log("Skipping round");
                            disablePlayerAttackRangeHightLight(pUnit);
                            yield return pUnit.StartCoroutine(pUnit.IsUnitStationary(pUnit, isStationary => {
                                if (isStationary)
                                {
                                    pUnit.InputReceived = true;
                                }
                            }));
                            break;
                        }

                        yield return null; // Wait for the next frame
                    }

                    // Remove the unit from the list and set its flags
                    pRolls.Remove(pUnit);
                    pUnit.isActive = false;
                    pUnit.InputReceived = false;
                    Debug.Log($"Player unit: {pUnit.name} completed its turn");
                }
                else
                {
                    // Player unit acts
                    eUnit.isActive = true;
                    Debug.Log($"It's Enemy unit: {eUnit.name} turn with roll: {eUnit.roll}");

                    // Wait for player input/action
                    while (eUnit.isActive && !eUnit.InputReceived)
                    {
                        diselectAllEnemys();
                        enableEnemyAttackRangeHightLight(eUnit);
                        if (isAutonomous )
                        {
                            yield return eUnit.StartCoroutine(eUnit.SelectAction());
                        }
                        else
                        {
                            // Handle player input here
                            if (Input.GetKeyDown(KeyCode.Alpha1) && !alpha1KeyPressed)
                            {
                                Debug.Log("Aplha1 key pressed");
                                yield return eUnit.StartCoroutine(eUnit.ThreeStepAttack());
                                eUnit.InputReceived = true;
                            }
                            else if (Input.GetKeyDown(KeyCode.Alpha2))
                            {
                                Debug.Log("Dash");
                                yield return eUnit.StartCoroutine(eUnit.Dash());
                                eUnit.InputReceived = true;
                            }
                            else if (Input.GetKeyDown(KeyCode.Alpha3))
                            {
                                Debug.Log("Aplha3 key pressed");
                                GameObject parentObject = eUnit.transform.parent.gameObject;
                                if (parentObject.name == "Fighters")
                                {
                                    Debug.Log("Cleave attack");
                                    yield return StartCoroutine(eUnit.TwoStepCleve());
                                }
                                else if (parentObject.name == "Priests")
                                {
                                    Debug.Log("Bless");
                                    yield return StartCoroutine(eUnit.ThreeStepBless());
                                }
                                else if (parentObject.name == "Rangers")
                                {
                                    Debug.Log("Projectile attack");
                                    yield return StartCoroutine(eUnit.ThreeStepAreaAttack());

                                }
                                else if (parentObject.name == "Mages")
                                {
                                    Debug.Log("Projectile attack");
                                    yield return StartCoroutine(eUnit.AreaAttack());
                                }
                                disableEnemyAttackRangeHightLight(eUnit);
                                eUnit.InputReceived = true;
                            }
                            else if (Input.GetKeyDown(KeyCode.Alpha4))
                            {
                                Debug.Log("Aplha4 key pressed");
                                GameObject parentObject = eUnit.transform.parent.gameObject;
                                if (parentObject.name == "Fighters")
                                {
                                    Debug.Log("I will be your protector");
                                    yield return StartCoroutine(eUnit.TwoStepProtect());
                                }
                                else if (parentObject.name == "Rangers")
                                {
                                    Debug.Log("Aiming at all targets...");
                                    yield return StartCoroutine(eUnit.TwoStepMultiHit());
                                }
                                else if (parentObject.name == "Priests")
                                {
                                    Debug.Log("Healing the allies...");
                                    yield return StartCoroutine(eUnit.TwoStepHeal());
                                }
                                else if (parentObject.name == "Mages")
                                {
                                    Debug.Log("Preparing to cancel enemy's round...");
                                    yield return StartCoroutine(eUnit.ThreeStepCancelRound());
                                }
                                disableEnemyAttackRangeHightLight(eUnit);
                                eUnit.InputReceived = true;
                            }
                            else if (Input.GetKeyDown(KeyCode.Alpha5))
                            {
                                Debug.Log("Skipping round");
                                disableEnemyAttackRangeHightLight(eUnit);
                                eUnit.InputReceived = true;
                                break;
                            }
                        }
                        disableEnemyAttackRangeHightLight(eUnit);
                        yield return null; // Wait for the next frame
                    }

                    // Move to the next unit
                    eRolls.Remove(eUnit);
                    eUnit.isActive = false;
                    eUnit.InputReceived = false;
                    Debug.Log($"Enemy unit: {eUnit.name} completed its turn");
                }
            }

            Debug.Log("All units have completed their turns.");
        }



        private void Diseroll()
        {
            // Clear player and enemy rolls list
            pRolls.Clear();
            eRolls.Clear();

            // Iterate over all player units
            foreach (Transform unitType in playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    // Try to get a PlayerUnit component
                    Units.Player.PlayerUnit pU = unit.GetComponent<Units.Player.PlayerUnit>();

                    // If component exists and it has no cancelled rounds
                    if (pU != null && pU.canceledRounds == 0)
                    {
                        // Roll a dice for this unit, adding its dexterity to the result
                        pU.roll = Random.Range(1, 21) + pU.dexterity;

                        // Enable the RollText UI component
                        pU.RollText.enabled = true;

                        // Add this unit to the player rolls list
                        pRolls.Add(pU);

                        // Log player roll
                        Debug.Log($"PLAYER {pU.name} has been added to the list with roll: {pU.roll} ");
                    }

                    // If the player unit has more than 0 canceled rounds, decrease it by 1
                    if (pU != null && pU.canceledRounds > 0)
                        pU.canceledRounds--;
                }
            }

            // Repeat the same process for enemy units
            foreach (Transform unitType in enemyUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Units.Enemy.EnemyUnit eU = unit.GetComponent<Units.Enemy.EnemyUnit>();
                    if (eU != null && eU.canceledRounds == 0)
                    {
                        eU.roll = Random.Range(1, 21) + eU.dexterity;
                        eU.RollText.enabled = true;
                        eRolls.Add(eU);
                        Debug.Log($"ENEMY {eU.name} has been added to the Queue with roll: {eU.roll} ");
                    }

                    if (eU != null && eU.canceledRounds > 0)
                        eU.canceledRounds--;
                }
            }

            // Sort player and enemy rolls lists in descending order based on their roll value
            pRolls.Sort((a, b) => b.roll.CompareTo(a.roll));
            eRolls.Sort((a, b) => b.roll.CompareTo(a.roll));

            // Call function to reset protectors
            resetProtectors();
        }


        private void resetProtectors()
        {
            foreach (Transform unitType in playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Units.Player.PlayerUnit pU = unit.GetComponent<Units.Player.PlayerUnit>();
                    if (pU != null)
                    {
                        pU.protector = null;
                    }
                }
            }
            foreach (Transform unitType in enemyUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Units.Enemy.EnemyUnit eU = unit.GetComponent<Units.Enemy.EnemyUnit>();
                    if (eU != null)
                    {
                        eU.protector = null;
                    }
                }
            }
        }
        private void enablePlayerAttackRangeHightLight(Units.Player.PlayerUnit unit)
        {
            Transform highlight = unit.transform.Find("AttackRange");
            if (highlight != null)
            {
                float scale = unit.atkRange * 2f; // Multiply by 2 to account for diameter instead of radius
                highlight.localScale = new Vector3(scale, 0.1f, scale);
                highlight.gameObject.SetActive(true);
            }
        }

        private void disablePlayerAttackRangeHightLight(Units.Player.PlayerUnit unit)
        {
            Transform highlight = unit.transform.Find("AttackRange");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(false);
            }
        }

        private void enableEnemyAttackRangeHightLight(Units.Enemy.EnemyUnit unit)
        {
            Transform highlight = unit.transform.Find("AttackRange");
            if (highlight != null)
            {
                float scale = unit.atkRange * 2f; // Multiply by 2 to account for diameter instead of radius
                highlight.localScale = new Vector3(scale, 0.1f, scale);
                highlight.gameObject.SetActive(true);
            }
        }

        private void disableEnemyAttackRangeHightLight(Units.Enemy.EnemyUnit unit)
        {
            Transform highlight = unit.transform.Find("AttackRange");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(false);
            }
        }

        public void diselectAllEnemys()
        {
            foreach (Transform unitType in enemyUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Transform highlight = unit.Find("Highlight");
                    if (highlight != null)
                    {
                        highlight.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void diselectAllPlayers()
        {
            foreach (Transform unitType in playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Transform highlight = unit.Find("Highlight");
                    if (highlight != null)
                    {
                        highlight.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void diselectEnemyHighlight(Units.Player.PlayerUnit unit)
        {
         
            Transform highlight = unit.aggroUnit.transform;
            if (highlight != null)
            {
                 highlight.Find("Highlight").gameObject.SetActive(false);
                 unit.aggroUnit = null;

            }
            
        }


        public void ActivateFreeRoamText()
        {
            freeRoamText.gameObject.SetActive(true);
            combatText.gameObject.SetActive(false);
        }

        public void ActivateCombatText()
        {
            freeRoamText.gameObject.SetActive(false);
            combatText.gameObject.SetActive(true);
        }

        public IEnumerator StopAllPlayerUnitsImmediately()
        {
            foreach (Transform unitType in playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    if (unit != null)
                    {
                        Vector3 currentPosition = unit.transform.position;
                        unit.GetComponent<Units.Player.PlayerUnit>().MoveUnit(currentPosition);
                        yield return null;
                    }
                }
            }

        }


        private void DisableRollText()
        {
            foreach (Transform unitType in playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Units.Player.PlayerUnit pU = unit.GetComponent<Units.Player.PlayerUnit>();
                    if (pU != null)
                    {
                        pU.RollText.enabled = false;
                    }
                }
            }
            foreach (Transform unitType in enemyUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Units.Enemy.EnemyUnit eU = unit.GetComponent<Units.Enemy.EnemyUnit>();
                    if (eU != null)
                    {
                        eU.RollText.enabled = false;
                    }
                }
            }
        }





    }
}
