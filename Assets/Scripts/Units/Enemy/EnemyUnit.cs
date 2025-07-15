using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using MODZ.RTS.Player;
using System;
using TMPro;

namespace MODZ.RTS.Units.Enemy
{
    [RequireComponent(typeof(NavMeshAgent))]

    public class EnemyUnit : MonoBehaviour
    {

        private NavMeshAgent navAgent;

        public UnitStatType.Base baseStats;

        public int roll;
        public int d6Roll;
        public int d4Roll;
        public int d12Roll;
        private int callCount = 0;

        public bool isActive = false;

        public bool InputReceived { get; set; } // Add this property

        private Collider[] rangeColliders;

        private Transform aggroTarger;

        private Player.PlayerUnit aggroUnit;
        public Player.PlayerUnit target;

        //private bool hasAggro = false;

        // The Canvas and TextMeshPro components
        public GameObject statsPanel;

        public TMP_Text RollText;
        public TMP_Text attributesText;
        public TMP_Text featuresText;

        private float distance;
        public float aggroRange = 10;

        public GameObject unitStatDisplay;
        public GameObject HealthpackParent;

        public Image healthBarAmount;

        public float currentHealth;
        private bool isAOEAttackActive = false;
        private Vector3 lastPosition;
        public bool isMinion = false;

        public int dexterity;
        public int vitality;
        public int charisma;
        public int strenght;
        public int consitution;
        
        public float movingRange;
        public float atkRange;
        public float attack;
        public float health;
        public float armor;
        public float sumOfAttributes;

        private bool keyQPressed;
        public GameObject projectilePrefab;
        public GameObject cleavePrefab;

        public static event Action<EnemyUnit> OnEnemyUnitDestroyed;

        public EnemyUnit protector { get; set; }

        //public bool isActionCancelled { get; set; }
        public int canceledRounds = 0;


        private void OnDestroy()
        {
            OnEnemyUnitDestroyed?.Invoke(this);
        }

        private void Start()
        {
            movingRange = baseStats.movingRange + charisma;
            atkRange = baseStats.atkRange + dexterity;
            attack = baseStats.attack + strenght;
            health = baseStats.health + vitality;
            armor = baseStats.armor + consitution;

            navAgent = gameObject.GetComponent<NavMeshAgent>();
            currentHealth = health;
            RollText.enabled = false;
            statsPanel.SetActive(false);
            HealthpackParent = GameObject.Find("HealthPacks");
        }

        private void LateUpdate()
        {
            HandleHealth();
            // Make the stats panel always face the camera, but stay upright
            Vector3 direction = Camera.main.transform.position - statsPanel.transform.position;
            direction.y = 0; // This will keep the panel upright
            statsPanel.transform.rotation = Quaternion.LookRotation(-direction);
            RollText.text = $"{roll}";
        }
        private void OnMouseOver()
        {
            // When the mouse is over the unit, show the stats
            statsPanel.SetActive(true);

            // Update the text with the unit's stats
            attributesText.text = $"DEXTERITY: {dexterity}\nVITALITY: {vitality}\nCHARISMA: {charisma}\nSTRENGTH: {strenght}\nCONSTITUTION: {consitution}";

            featuresText.text = $"ATTACK RANGE: {atkRange}\nATTACK DAMAGE: {attack}\nCURRENT HEALTH: {currentHealth}\nARMOR: {armor}";
        }

        private void OnMouseExit()
        {
            // When the mouse is no longer over the unit, hide the stats
            statsPanel.SetActive(false);
        }

        public bool CheckForEnemyTargets()
        {
            rangeColliders = Physics.OverlapSphere(transform.position, aggroRange);

            for (int i = 0; i < rangeColliders.Length; i++)
            {
                if (rangeColliders[i].gameObject.layer == UnitHandler.instance.pUnitLayer)
                {
                    aggroTarger = rangeColliders[i].gameObject.transform;
                    aggroUnit = aggroTarger.gameObject.GetComponent<Player.PlayerUnit>();

                    if (aggroUnit != null)
                    {
                        //hasAggro = true;
                        //Debug.Log("Found aggroUnit: " + aggroUnit.gameObject.name);
                        return true; // Return true when hasAggro is set to true
                    }
                    else
                    {
                       // Debug.LogWarning("aggroUnit is null");
                    }

                    break;
                }
            }

            aggroUnit = null; // Reset aggroUnit to null if no valid target is found
            return false; // Return false if no aggroUnit is found
        }

        public void MoveUnit(Vector3 _destitation)
        {
            //Debug.Log("Moving unit to: " + _destitation);
            navAgent.SetDestination(_destitation);
        }

        public void TakeDamage(float damage)
        {
            if (protector != null)
            {
                protector.TakeDamage(damage);
                return;
            }
            currentHealth -= damage;
        }

        public void MoveToAggroTarget()
        {
            if (aggroTarger == null)
            {
                navAgent.SetDestination(transform.position);
                //hasAggro = false;
            }
            else
            {
                distance = Vector3.Distance(aggroTarger.position, transform.position);
                navAgent.stoppingDistance = (atkRange + 1);

                if (distance <= aggroRange)
                {
                    navAgent.SetDestination(aggroTarger.position);
                }
            }
        }

        private void HandleHealth()
        {
            Camera camera = Camera.main;
            unitStatDisplay.transform.LookAt(unitStatDisplay.transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);

            healthBarAmount.fillAmount = currentHealth / health;


            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            InputManager.InputHandler.instance.selectedUnits.Remove(gameObject.transform);
            Destroy(gameObject);
        }

        public void MoveUnit(Vector3 _destination, float _movingRange)
        {
            Vector3 direction = _destination - transform.position;
            float distance = direction.magnitude;

            //Debug.Log("Desired Destination: " + _destination);
            //Debug.Log("Current Position: " + transform.position);

            // Clamp the distance within the moving range
            if (distance > _movingRange)
            {
                direction = direction.normalized * _movingRange;
            }

            Vector3 targetPosition = transform.position + direction;
            //Debug.Log("Target Position: " + targetPosition);
            navAgent.SetDestination(targetPosition);
        }


        public IEnumerator ThreeStepAttack()
        {
            Debug.Log("ThreeStepAttack");
            keyQPressed = false;
            yield return StartCoroutine(Move());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Attack());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
            transform.Find("AttackRange").gameObject.SetActive(false);
        }


        public IEnumerator Move()
        {
            Debug.Log("Waiting position to Move");
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    keyQPressed = true;
                    break;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        MoveUnit(hit.point, movingRange);
                        break;
                    }
                }
                yield return null;
                
            }
            float stationaryTime = 0f;
            Vector3 oldPosition = transform.position;

            while (stationaryTime < 1f)
            {
                yield return new WaitForSeconds(0.1f); // update every 0.1 second
                if (Vector3.Distance(oldPosition, transform.position) < 0.01f)
                {
                    // the agent is stationary
                    stationaryTime += 0.1f;
                }
                else
                {
                    // the agent is moving, reset the timer
                    stationaryTime = 0f;
                    oldPosition = transform.position;
                }
                yield return null;
            }
        }

        public IEnumerator Attack()
        {
            Debug.Log("Waiting for target to Attack");
            bool attackSuccessful = false;
            while (!attackSuccessful)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    keyQPressed = true;
                    break;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit))
                    {
                        LayerMask layerHit = hit.transform.gameObject.layer;

                        switch (layerHit.value)
                        {
                            case 8: // unit layer

                                target = hit.transform.gameObject.GetComponent<Player.PlayerUnit>();
                                if (target != null)
                                {
                                    PlayerManager.instance.diselectAllPlayers();
                                    Transform highlight = target.transform.Find("Highlight");
                                    highlight.gameObject.SetActive(true);

                                    if (Vector3.Distance(target.transform.position, transform.position) <= atkRange + 1)
                                    {
                                        int d12Roll = UnityEngine.Random.Range(1, 12) + dexterity;
                                        if (d12Roll > target.armor)
                                        {
                                            target.TakeDamage(attack);
                                            Debug.Log($"Unit: {target.name} took damage {target.attack}");
                                        }
                                        else
                                        {
                                            Debug.Log($"d12 roll was smoller then players armor");
                                        }
                                        target.transform.Find("Highlight").gameObject.SetActive(false);
                                        target = null;
                                        InputReceived = true;
                                        attackSuccessful = true; // attack was successful
                                    }
                                    else
                                    {
                                        Debug.Log($"Player target was out of range");
                                    }
                                    break;
                                }
                                break;
                            default:
                                PlayerManager.instance.diselectAllPlayers();
                                target = null;// If the clicked object is not an enemy unit, do nothing
                                break;
                        }
                    }
                }

                yield return null;
            }
        }

        public IEnumerator ThreeStepAreaAttack()
        {
            Debug.Log("ThreeStepAreaAttack");
            keyQPressed = false;
            yield return StartCoroutine(Move());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(AreaAttack());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
            transform.Find("AttackRange").gameObject.SetActive(false);
        }

        public IEnumerator AreaAttack()
        {
            Debug.Log("Waiting for target to Projectile attack");
            float AreaAttackRange = atkRange;
            transform.Find("AttackRange").localScale = new Vector3(AreaAttackRange * 2, 0.1f, 2 * AreaAttackRange);
            bool attackSuccessful = false;
            int radius = 3;
            while (!attackSuccessful)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    keyQPressed = true;
                    break;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        // Check if the unit is within 40 units of the hit
                        float distance = Vector3.Distance(transform.position, hit.point);
                        if (distance <= atkRange)
                        {
                            Vector3 instantiatePosition = new Vector3(hit.point.x, 1.03f, hit.point.z);
                            GameObject projectileInstance = Instantiate(projectilePrefab, instantiatePosition, Quaternion.identity);
                            projectileInstance.transform.localScale = new Vector3(radius, 1.03f, radius);
                            // Get all colliders within the attack radius on the enemy layer
                            LayerMask enemyLayer = 1 << 9;
                            Collider[] hitColliders = Physics.OverlapSphere(hit.point, radius, enemyLayer);

                            foreach (Collider hitCollider in hitColliders)
                            {
                                // Get enemy unit from collider
                                Player.PlayerUnit playerUnit = hitCollider.transform.gameObject.GetComponent<Player.PlayerUnit>();

                                if (playerUnit != null) // If the object has an EnemyUnit component
                                {
                                    d6Roll = UnityEngine.Random.Range(1, 6) + dexterity;
                                    playerUnit.TakeDamage((attack * 6) / d6Roll);
                                }
                            }
                            attackSuccessful = true;  // Attack was successful

                            // Destroy the instantiated projectile after a delay
                            StartCoroutine(DestroyAfterDelay(projectileInstance, 2.0f));
                        }
                        else
                        {
                            Debug.Log("Unit is too far away from the target point. Please click somewhere closer.");
                        }
                    }
                }

                yield return null;
            }
        }

        IEnumerator DestroyAfterDelay(GameObject gameObject, float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        public IEnumerator TwoStepCleve()
        {
            Debug.Log("TwoStepCleave");
            keyQPressed = false;
            yield return StartCoroutine(Cleave());  // Wait for Cleave() to finish
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
            transform.Find("AttackRange").gameObject.SetActive(false);
        }

        public IEnumerator Cleave()
        {
            isAOEAttackActive = true;
            lastPosition = transform.position;

            Debug.Log("Cleave is activated");
            float totalDistanceTraveled = 0f;
            float maxTravelDistance = 10f;
            transform.Find("AttackRange").localScale = new Vector3(maxTravelDistance * 2, 0.1f, 2 * maxTravelDistance);
            float timeNearTarget = 0f;  // How long the unit has been near the target
            float timeThreshold = 0.5f;  // The amount of time the unit needs to be near the target to finish
            float distanceThreshold = 2.5f;  // The distance within which the unit is considered "near" the target


            // Activate the cleave indicator prefab
            transform.Find("Cleave").gameObject.SetActive(true);
            transform.Find("Cleave").localScale = new Vector3(atkRange * 2, 1f, atkRange * 2);
            // Wait for the player to set a destination by clicking
            Vector3 destination = Vector3.zero;
            bool destinationSet = false;
            while (!destinationSet)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        destination = hit.point;
                        destinationSet = true;

                        // Once the destination is set, start moving the unit
                        MoveUnit(destination);

                    }
                }

                yield return null;
            }

            while (isAOEAttackActive)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    keyQPressed = true;
                    transform.Find("Cleave").gameObject.SetActive(false);
                    break;
                }

                float distanceTraveled = Vector3.Distance(transform.position, lastPosition);
                totalDistanceTraveled += distanceTraveled;

                if (Vector3.Distance(transform.position, destination) <= distanceThreshold)
                {
                    // If the unit is near the target, increase the timeNearTarget
                    timeNearTarget += Time.deltaTime;
                }
                else if (GetComponent<NavMeshAgent>().velocity.magnitude == 0f)
                {
                    // If the unit is not moving, increase the timeNearTarget
                    timeNearTarget += Time.deltaTime;
                }
                else
                {
                    // If the unit is not near the target, reset the timeNearTarget
                    timeNearTarget = 0f;
                }

                if (timeNearTarget >= timeThreshold)
                {
                    isAOEAttackActive = false;
                    Debug.Log("Area attack deactivated");

                    // Disable the cleave indicator prefab
                    transform.Find("Cleave").gameObject.SetActive(false);
                    keyQPressed = true;
                    yield break;
                }

                if (distanceTraveled > 0)
                {
                    // Wait for 0.5 seconds before applying damage
                    yield return new WaitForSeconds(0.5f);

                    LayerMask playerLayer = 1 << 9;
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, atkRange, playerLayer);

                    foreach (Collider hitCollider in hitColliders)
                    {
                        Player.PlayerUnit playerUnit = hitCollider.transform.gameObject.GetComponent<Player.PlayerUnit>();

                        if (playerUnit != null)
                        {
                            d4Roll = UnityEngine.Random.Range(1, 4) + dexterity;
                            playerUnit.TakeDamage(attack / d4Roll);
                        }
                        if (GetComponent<NavMeshAgent>().velocity.magnitude < 1f)
                        {
                            isAOEAttackActive = false;
                            Debug.Log("Area attack deactivated");

                            // Disable the cleave indicator prefab
                            transform.Find("Cleave").gameObject.SetActive(false);
                            keyQPressed = true;
                            break; // stops the foreach loop early
                        }

                    }
                }

                lastPosition = transform.position;

                if (totalDistanceTraveled >= maxTravelDistance)
                {
                    isAOEAttackActive = false;
                    Debug.Log("Area attack deactivated");

                    // Disable the cleave indicator prefab
                    transform.Find("Cleave").gameObject.SetActive(false);
                    yield break;
                }

                yield return null;
            }

        }
        public IEnumerator ThreeStepBless()
        {
            Debug.Log("ThreeStepBless");
            keyQPressed = false;
            yield return StartCoroutine(Move());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Bless());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
        }

        public IEnumerator Bless()
        {
            Debug.Log("Bless activated");
            bool blessSuccessful = false;
            float blessRange = atkRange;  
            transform.Find("AttackRange").localScale = new Vector3(blessRange * 2, 0.1f, blessRange * 2);
            while (!blessSuccessful)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    keyQPressed = true;
                    break;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (distance <= atkRange)
                        {
                            LayerMask layerHit = hit.transform.gameObject.layer;

                            switch (layerHit.value)
                            {
                                case 9: // player layer
                                    EnemyUnit enemyUnit = hit.transform.gameObject.GetComponent<EnemyUnit>();
                                    if (enemyUnit != null)
                                    {
                                        d4Roll = UnityEngine.Random.Range(1, 4) + dexterity;
                                        enemyUnit.attack += d4Roll;
                                        blessSuccessful = true;
                                        Debug.Log("Unit has been blessed");
                                    }
                                    else
                                    {
                                        Debug.Log("No target was selected");
                                    }
                                    break;

                                default:
                                    target = null;// If the clicked object is not an enemy unit, do nothing
                                    break;
                            }
                        }
                        else
                        {
                            Debug.Log("Unit is too far away from the target point. Please click somewhere closer.");
                        }
                    }
                }
                yield return null;
            }
        }


        public IEnumerator TwoStepProtect()
        {
            Debug.Log("TwoStepProtect");
            keyQPressed = false;
            yield return StartCoroutine(Protect());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
        }

        public IEnumerator Protect()
        {
            foreach (Transform unitType in PlayerManager.instance.enemyUnits)
            {
                foreach (Transform unit in unitType)
                {
                    EnemyUnit eU = unit.GetComponent<EnemyUnit>();
                    if (eU != transform.GetComponent<EnemyUnit>()) // Don't set protector for itself
                    {
                        eU.protector = transform.GetComponent<EnemyUnit>();
                    }
                }
            }
            yield return null;
        }

        public IEnumerator TwoStepMultiHit()
        {
            Debug.Log("TwoStepProtect");
            keyQPressed = false;
            yield return StartCoroutine(MultiHit());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
        }

        public IEnumerator MultiHit()
        {
            // Get all colliders within the attack range and on the "Enemy Layer"
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, atkRange, 1 << 8);
            // Iterate through the hit colliders
            foreach (Collider hitCollider in hitColliders)
            {
                // Get enemy unit from collider
                Player.PlayerUnit playerUnit = hitCollider.transform.gameObject.GetComponent<Player.PlayerUnit>();

                if (playerUnit != null) // If the object has an EnemyUnit component
                {
                    d12Roll = UnityEngine.Random.Range(1, 12) + dexterity;
                    playerUnit.TakeDamage((attack * 12) / d12Roll);
                }
            }
            yield return null;
        }

        public IEnumerator TwoStepHeal()
        {
            Debug.Log("TwoStepProtect");
            keyQPressed = false;
            yield return StartCoroutine(Healing());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
        }

        public IEnumerator Healing()
        {
            int healAmount = 5;
            foreach (Transform unitType in PlayerManager.instance.enemyUnits)
            {
                foreach (Transform unit in unitType)
                {
                    EnemyUnit eU = unit.GetComponent<EnemyUnit>();
                    if (eU.currentHealth + healAmount < eU.health)
                    {
                        eU.currentHealth += healAmount;
                    }
                    else
                    {
                        eU.currentHealth = eU.health;
                    }
                }
            }
            yield return null;
        }

        public IEnumerator ThreeStepCancelRound()
        {
            Debug.Log("TwoStepCanselRound");
            keyQPressed = false;
            yield return StartCoroutine(Move());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(CancelRound());
            if (keyQPressed) yield return new WaitForSeconds(0.1f);
            keyQPressed = false;
            yield return StartCoroutine(Move());
        }

        public IEnumerator CancelRound()
        {
            bool CanselSuccessful = false;
            while (!CanselSuccessful)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    keyQPressed = true;
                    break;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        LayerMask layerHit = hit.transform.gameObject.layer;

                        switch (layerHit.value)
                        {
                            case 8: // enemy layer
                                target = hit.transform.gameObject.GetComponent<Player.PlayerUnit>();
                                if (target != null)
                                {
                                    if (Vector3.Distance(target.transform.position, transform.position) <= atkRange + 1)
                                    {
                                        target.canceledRounds++;
                                        Debug.Log($"Player {target} has lost its next round");
                                        CanselSuccessful = true;
                                    }
                                    else
                                    {
                                        Debug.Log($"Player target was out of range");
                                    }
                                    break;
                                }
                                break;
                            default:
                                target = null;// If the clicked object is not an enemy unit, do nothing
                                break;
                        }

                    }
                }

                yield return null;
            }
        }

        public IEnumerator Dash()
        {
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    keyQPressed = true;
                    break;
                }
                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        MoveUnit(hit.point, movingRange * 3);
                        transform.Find("AttackRange").gameObject.SetActive(false);
                        break;
                    }
                }
                yield return null;

            }

            float stationaryTime = 0f;
            Vector3 oldPosition = transform.position;

            while (stationaryTime < 1f)
            {
                yield return new WaitForSeconds(0.1f); // update every 0.1 second
                if (Vector3.Distance(oldPosition, transform.position) < 0.01f)
                {
                    // the agent is stationary
                    stationaryTime += 0.1f;
                }
                else
                {
                    // the agent is moving, reset the timer
                    stationaryTime = 0f;
                    oldPosition = transform.position;
                }
                yield return null;
            }

        }








        //----------------------------Autonomus Section------------------------------------

        GameObject nearestPlayerUnit;
        GameObject closestHealthPack;
        public IEnumerator SelectAction()
        {
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            callCount++;
            bool trirdRound = (callCount == 1 || callCount % 3 == 0);
            yield return StartCoroutine(FindNearestPlayerUnit());
            yield return StartCoroutine(FindClosestHealthPack());
            float distance = Vector3.Distance(nearestPlayerUnit.transform.position, transform.position);
            if (trirdRound && !isMinion)
            {
                yield return StartCoroutine(levelManager.SpawnMinions(transform.position));

            }
            if(currentHealth ==  health * 0.3f && Vector3.Distance(closestHealthPack.transform.position, transform.position) < movingRange * 3)
            {
                yield return StartCoroutine(AutonomousMove(closestHealthPack.transform.position));
            }
            else
            {
                if (distance <= movingRange + atkRange - 2)
                {
                    yield return StartCoroutine(ThreeStepAutonomousAttack(nearestPlayerUnit));

                }
                else if (distance >= movingRange + atkRange - 2 && distance <= 3 * movingRange + atkRange - 2)
                {
                    yield return StartCoroutine(AutonomousMove(nearestPlayerUnit.GetComponent<Player.PlayerUnit>().transform.position));
                }
            }
            InputReceived = true;
        }

        public IEnumerator FindNearestPlayerUnit()
        {
            // Get the Player Units game object
            GameObject playerUnits = GameObject.Find("Player Units");

            // Get all child game objects (Fighters, Priests, Mages, Rangers)
            Transform[] unitTypes = playerUnits.GetComponentsInChildren<Transform>();

            GameObject nearestUnit = null;
            while (nearestUnit == null)
            {
                float minDistance = Mathf.Infinity;
                Vector3 currentPos = transform.position;

                List<GameObject> potentialTargets = new List<GameObject>();

                foreach (Transform unitType in unitTypes)
                {
                    // Get all PlayerUnit objects under each unit type
                    Player.PlayerUnit[] pUnits = unitType.GetComponentsInChildren<Player.PlayerUnit>();

                    foreach (Player.PlayerUnit playerUnit in pUnits)
                    {
                        NavMeshAgent agent = playerUnit.GetComponent<NavMeshAgent>();
                        // If the unit's velocity is near zero, wait for a short delay
                        if (agent.velocity.sqrMagnitude <= 0.01f)
                        {
                            yield return new WaitForSeconds(0.1f);
                            // If the unit's velocity is still near zero, add it to the potential targets list
                            if (agent.velocity.sqrMagnitude <= 0.01f)
                            {
                                potentialTargets.Add(playerUnit.gameObject);
                            }
                        }
                    }
                }

                // If there are any potential targets, find the nearest one
                if (potentialTargets.Count > 0)
                {
                    foreach (GameObject potentialTarget in potentialTargets)
                    {
                        float dist = Vector3.Distance(potentialTarget.transform.position, currentPos);
                        if (dist < minDistance)
                        {
                            nearestUnit = potentialTarget;
                            minDistance = dist;
                        }
                    }
                }

                yield return null; // Wait for the next frame
            }

            // Set the nearestPlayerUnit variable
            nearestPlayerUnit = nearestUnit;

            yield return null;
        }


        public IEnumerator ThreeStepAutonomousAttack(GameObject target)
        {
            //yield return StartCoroutine(FindNearestPlayerUnit());
            //GameObject target = nearestPlayerUnit;
            if (target != null)
            {
                Debug.Log("Target Acquired");
                Player.PlayerUnit targetPlayerUnit = target.GetComponent<Player.PlayerUnit>();
                if (targetPlayerUnit != null)
                {
                    Vector3 targetPosition = targetPlayerUnit.transform.position;

                    // Ensure the agent is not stopped before starting a move
                    NavMeshAgent agent = GetComponent<NavMeshAgent>();
                    agent.isStopped = false;

                    yield return StartCoroutine(AutonomousMove(targetPosition));
                    yield return StartCoroutine(AutonomusAttack(target.GetComponent<Player.PlayerUnit>()));

                    // Ensure the agent is not stopped before starting a retreat
                    agent.isStopped = false;

                    // Assuming retreatDistance is defined somewhere
                    yield return StartCoroutine(AutonomousRetreatMove(targetPlayerUnit, movingRange));
                }
            }
        }

        public IEnumerator AutonomousRetreatMove(Player.PlayerUnit target, float retreatDistance)
        {
            if (target != null)
            {
                Debug.Log("Retreating");

                // Calculate the retreat direction
                Vector3 retreatDirection = (transform.position - target.transform.position).normalized;

                // Calculate the retreat position which is `movingDistance - 2` away from current position
                Vector3 retreatPosition = transform.position + retreatDirection * (retreatDistance - 2);

                // Debugging: Draw a line from the agent to the retreat position
                Debug.DrawLine(transform.position, retreatPosition, Color.blue, 5f);

                // Make sure the retreat position is valid
                NavMeshHit hit;
                if (NavMesh.SamplePosition(retreatPosition, out hit, retreatDistance, NavMesh.AllAreas))
                {
                    retreatPosition = hit.position;
                }

                // Use the AutonomousMove method to move to the retreat position
                yield return StartCoroutine(AutonomousMove(retreatPosition, false));
            }
        }

        public IEnumerator AutonomousMove(Vector3 targetPosition, bool isAttackMove = true, bool isDashing = false)
        {
            Debug.Log("Moving");

            Vector3 stoppingPosition = targetPosition;

            if (isAttackMove)
            {
                // Calculate the stopping position which is `attackRange - 2` away from target position
                Vector3 directionToTarget = (targetPosition - transform.position).normalized;
                stoppingPosition = targetPosition - directionToTarget * (atkRange - 2);
            }
            if (isDashing)
            {
                MoveUnit(stoppingPosition, 3*movingRange);
            }
            else
            {
                MoveUnit(stoppingPosition, movingRange);
            }
            float stationaryTime = 0f;
            Vector3 oldPosition = transform.position;

            while (stationaryTime < 1f)
            {
                yield return new WaitForSeconds(0.1f); // update every 0.1 second
                if (Vector3.Distance(oldPosition, transform.position) < 0.01f)
                {
                    // the agent is stationary
                    stationaryTime += 0.1f;
                }
                else
                {
                    // the agent is moving, reset the timer
                    stationaryTime = 0f;
                    oldPosition = transform.position;
                }
            }

            Debug.Log("Movement finished");
        }

        public IEnumerator AutonomusAttack(Player.PlayerUnit target)
        {
            if (target != null)
            {
                Debug.Log("Attack");
                if (Vector3.Distance(target.transform.position, transform.position) <= atkRange + 1)
                {
                    int d12Roll = UnityEngine.Random.Range(1, 12) + dexterity;
                    if (d12Roll > target.armor)
                    {
                        target.TakeDamage(attack);
                        Debug.Log($"Unit: {target.name} took damage {target.attack}");
                    }
                    else
                    {
                        Debug.Log($"d12 roll was smaller than player's armor");
                    }

                    InputReceived = true;
                }
                else
                {
                    Debug.Log($"Player target was out of range");
             }
            }

                yield return null;
        }

        public IEnumerator AutonomusTwoStepCleave()
        {
            yield return StartCoroutine(FindNearestPlayerUnit());
            if (nearestPlayerUnit != null)
            {
                Debug.Log("TwoStepCleave");

                yield return StartCoroutine(Cleave(nearestPlayerUnit.transform.position));
                yield return new WaitForSeconds(0.1f);

                yield return StartCoroutine(AutonomousMove(nearestPlayerUnit.transform.position));
                transform.Find("AttackRange").gameObject.SetActive(false);
            }
        }

        public IEnumerator Cleave(Vector3 targetPosition)
        {
            isAOEAttackActive = true;
            lastPosition = transform.position;

            Debug.Log("Cleave is activated");
            float totalDistanceTraveled = 0f;
            float maxTravelDistance = 10f;
            transform.Find("AttackRange").localScale = new Vector3(maxTravelDistance * 2, 0.1f, 2 * maxTravelDistance);
            float timeNearTarget = 0f;
            float timeThreshold = 0.5f;
            float distanceThreshold = 2.5f;

            transform.Find("Cleave").gameObject.SetActive(true);
            transform.Find("Cleave").localScale = new Vector3(atkRange * 2, 1f, atkRange * 2);

            MoveUnit(targetPosition);

            while (isAOEAttackActive)
            {
                float distanceTraveled = Vector3.Distance(transform.position, lastPosition);
                totalDistanceTraveled += distanceTraveled;

                if (Vector3.Distance(transform.position, targetPosition) <= distanceThreshold)
                {
                    timeNearTarget += Time.deltaTime;
                }
                else if (GetComponent<NavMeshAgent>().velocity.magnitude == 0f)
                {
                    timeNearTarget += Time.deltaTime;
                }
                else
                {
                    timeNearTarget = 0f;
                }

                if (timeNearTarget >= timeThreshold)
                {
                    isAOEAttackActive = false;
                    Debug.Log("Area attack deactivated");
                    transform.Find("Cleave").gameObject.SetActive(false);
                    yield break;
                }

                if (distanceTraveled > 0)
                {
                    yield return new WaitForSeconds(0.5f);

                    LayerMask playerLayer = 1 << 8;
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, atkRange, playerLayer);

                    foreach (Collider hitCollider in hitColliders)
                    {
                        Player.PlayerUnit playerUnit = hitCollider.transform.gameObject.GetComponent<Player.PlayerUnit>();

                        if (playerUnit != null)
                        {
                            d4Roll = UnityEngine.Random.Range(1, 4) + dexterity;
                            playerUnit.TakeDamage(attack / d4Roll);
                        }
                        if (GetComponent<NavMeshAgent>().velocity.magnitude < 1f)
                        {
                            isAOEAttackActive = false;
                            Debug.Log("Area attack deactivated");
                            transform.Find("Cleave").gameObject.SetActive(false);
                            break;
                        }
                    }
                }

                lastPosition = transform.position;

                if (totalDistanceTraveled >= maxTravelDistance)
                {
                    isAOEAttackActive = false;
                    Debug.Log("Area attack deactivated");
                    transform.Find("Cleave").gameObject.SetActive(false);
                    yield break;
                }

                yield return null;
            }
        }

        public IEnumerator FindClosestHealthPack()
        {
            Transform[] healthPacks = HealthpackParent.GetComponentsInChildren<Transform>();

            float minDistance = Mathf.Infinity;
            Vector3 currentPos = transform.position;

            foreach (Transform healthPack in healthPacks)
            {
                float dist = Vector3.Distance(healthPack.position, currentPos);
                if (dist < minDistance)
                {
                    closestHealthPack = healthPack.gameObject;
                    minDistance = dist;
                }
                yield return null; 
            }
        }

    }

}
