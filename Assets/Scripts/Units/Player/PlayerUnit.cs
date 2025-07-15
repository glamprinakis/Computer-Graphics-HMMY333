using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using MODZ.RTS.Player;
using System;
using TMPro;


namespace MODZ.RTS.Units.Player
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerUnit : MonoBehaviour
    {
        private NavMeshAgent navAgent;

        public UnitStatType.Base baseStats;

        public int roll;
        public int d6Roll;
        public int d4Roll;
        public int d12Roll;

        public bool isActive = false;
        public bool InputReceived { get; set; } // Add this property

        public GameObject unitStatDisplay;

        // The Canvas and TextMeshPro components
        public GameObject statsPanel;

        public TMP_Text attributesText;
        public TMP_Text featuresText;
        public TMP_Text RollText;

        private Vector3 mousePos;
        private RaycastHit hit;
        public Transform selectedUnit;

        public Image healthBarAmount;

        public float currentHealth;
        private Collider[] rangeColliders;
        private Transform aggroTarger;
        public Enemy.EnemyUnit aggroUnit;
        public Enemy.EnemyUnit target;
        private float distance;
        public bool destinationReached;
        public float aggroRange = 10;
        private bool isAOEAttackActive = false;
        private Vector3 lastPosition;

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

        public GameObject projectilePrefab;
        public GameObject cleavePrefab;
        private bool keyQPressed;
        public int canceledRounds = 0;
        public float stopDistance = 3f;


        public static event Action<PlayerUnit> OnPlayerUnitDestroyed;

        public PlayerUnit protector { get; set; }

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


        private void OnDestroy()
        {
            OnPlayerUnitDestroyed?.Invoke(this);
        }

        private void OnEnable()
        {
            navAgent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            movingRange = baseStats.movingRange + charisma;
            atkRange = baseStats.atkRange + dexterity;
            attack = baseStats.attack + strenght;
            health = baseStats.health + vitality;
            armor = baseStats.armor + consitution;

            currentHealth = health;
            //Debug.Log("NavMeshAgent enabled: " + navAgent.enabled);
            RollText.enabled = false;
            statsPanel.SetActive(false);
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

        public void MoveUnit(Vector3 _destitation)
        {
            //Debug.Log("Moving unit to: " + _destitation);
            navAgent.SetDestination(_destitation);
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

        public void TakeDamage(float damage)
        {
            if (protector != null)
            {
                protector.TakeDamage(damage);
                return;
            }

            if (damage > armor)
            {
                float totalDamage = damage - armor;
                currentHealth -= totalDamage;
            }
        }


        private void HandleHealth()
        {
            Camera camera = Camera.main;
            unitStatDisplay.transform.LookAt(unitStatDisplay.transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);

            healthBarAmount.fillAmount = currentHealth / health;


            if (currentHealth <= 0)
            {
                Debug.LogWarning("Died");
                Die();
            }
        }

        public void Die()
        {
            InputManager.InputHandler.instance.selectedUnits.Remove(gameObject.transform);
            Destroy(gameObject);
        }




        public bool CheckForEnemyTargets()
        {
            rangeColliders = Physics.OverlapSphere(transform.position, aggroRange);

            for (int i = 0; i < rangeColliders.Length; i++)
            {
                if (rangeColliders[i].gameObject.layer == UnitHandler.instance.pUnitLayer)
                {
                    aggroTarger = rangeColliders[i].gameObject.transform;

                    if (aggroTarger.gameObject.GetComponent<Enemy.EnemyUnit>() != null)
                    {
                        //Debug.Log("Found aggroUnit: " + aggroTarger.gameObject.name);
                        return true; // Return true when hasAggro is set to true
                    }
                    else
                    {
                       // Debug.LogWarning("aggroUnit is null");
                    }

                    break;
                }
            }
            return false; // Return false if no aggroUnit is found
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

                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
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
                            case 9: // unit layer

                                target = hit.transform.gameObject.GetComponent<Enemy.EnemyUnit>();
                                if (target != null)
                                {
                                    PlayerManager.instance.diselectAllEnemys();
                                    Transform highlight = target.transform.Find("Highlight");
                                    highlight.gameObject.SetActive(true);

                                    if (Vector3.Distance(target.transform.position, transform.position) <= atkRange + 1)
                                    {
                                        int d12Roll = UnityEngine.Random.Range(1, 12) + dexterity;
                                        if (d6Roll > target.armor)
                                        {
                                            target.TakeDamage(attack);
                                            Debug.Log($"Unit: {target.name} took damage {target.attack}");
                                        }
                                        else
                                        {
                                            Debug.Log($"d12 roll was smoller then enemys armor");
                                        }
                                        target.transform.Find("Highlight").gameObject.SetActive(false);
                                        target = null;
                                        InputReceived = true;
                                        attackSuccessful = true; // attack was successful
                                    }
                                    else
                                    {
                                        Debug.Log($"Enemy target was out of range");
                                    }
                                    break;
                                }
                                break;
                            default:
                                PlayerManager.instance.diselectAllEnemys();
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
                    break;

                if (Input.GetMouseButtonDown(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        // Check if the unit is within 40 units of the hit
                        float distance = Vector3.Distance(transform.position, hit.point);
                        if (distance <=atkRange)
                        {
                            Vector3 instantiatePosition = new Vector3(hit.point.x, 1.03f, hit.point.z);
                            GameObject projectileInstance = Instantiate(projectilePrefab, instantiatePosition, Quaternion.identity);
                            projectileInstance.transform.localScale = new Vector3(radius, 1.03f,radius);
                            // Get all colliders within the attack radius on the enemy layer
                            LayerMask enemyLayer = 1 << 9;
                            Collider[] hitColliders = Physics.OverlapSphere(hit.point, radius, enemyLayer);

                            foreach (Collider hitCollider in hitColliders)
                            {
                                // Get enemy unit from collider
                                Enemy.EnemyUnit enemyUnit = hitCollider.transform.gameObject.GetComponent<Enemy.EnemyUnit>();

                                if (enemyUnit != null) // If the object has an EnemyUnit component
                                {
                                    d6Roll = UnityEngine.Random.Range(1, 6) + dexterity;
                                    enemyUnit.TakeDamage((attack * 6)/ d6Roll);
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
            Debug.Log("TwoStepAction");
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
            transform.Find("AttackRange").localScale = new Vector3(maxTravelDistance * 2, 0.1f,2* maxTravelDistance);
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

                    LayerMask enemyLayer = 1 << 9;
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, atkRange, enemyLayer);

                    foreach (Collider hitCollider in hitColliders)
                    {
                        Enemy.EnemyUnit enemyUnit = hitCollider.transform.gameObject.GetComponent<Enemy.EnemyUnit>();

                        if (enemyUnit != null)
                        {
                            d4Roll = UnityEngine.Random.Range(1, 4) + dexterity;
                            enemyUnit.TakeDamage(attack/ d4Roll);
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
            float blessRange = atkRange;  // Set this to the maximum range for blessing
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
                                case 8: // player layer
                                    PlayerUnit playerUnit = hit.transform.gameObject.GetComponent<PlayerUnit>();
                                    if (playerUnit != null)
                                    {
                                        d4Roll = UnityEngine.Random.Range(1, 4) + dexterity;
                                        playerUnit.attack += d4Roll;
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
            foreach (Transform unitType in PlayerManager.instance.playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    PlayerUnit pU = unit.GetComponent<PlayerUnit>();
                    if (pU != transform.GetComponent<PlayerUnit>()) // Don't set protector for itself
                    {
                        pU.protector = transform.GetComponent<PlayerUnit>();
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
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, atkRange, 1 << 9);
            // Iterate through the hit colliders
            foreach (Collider hitCollider in hitColliders)
            {
                // Get enemy unit from collider
                Enemy.EnemyUnit enemyUnit = hitCollider.transform.gameObject.GetComponent<Enemy.EnemyUnit>();

                if (enemyUnit != null) // If the object has an EnemyUnit component
                {
                    d12Roll = UnityEngine.Random.Range(1, 12) + dexterity;
                    enemyUnit.TakeDamage((attack * 12) / d12Roll);
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
            foreach (Transform unitType in PlayerManager.instance.playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    PlayerUnit pU = unit.GetComponent<PlayerUnit>();
                    if (pU.currentHealth + healAmount < pU.health)
                    {
                        pU.currentHealth += healAmount;
                    }
                    else
                    {
                        pU.currentHealth = pU.health;
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
                            case 9: // enemy layer
                                target = hit.transform.gameObject.GetComponent<Enemy.EnemyUnit>();
                                if (target != null)
                                {
                                    if (Vector3.Distance(target.transform.position, transform.position) <= atkRange + 1)
                                    {
                                        target.canceledRounds++;
                                        Debug.Log($"Enemy {target} has lost its next round");
                                        CanselSuccessful = true;
                                    }
                                    else
                                    {
                                        Debug.Log($"Enemy target was out of range");
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
                        MoveUnit(hit.point, movingRange*3);
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

        public IEnumerator IsUnitStationary(PlayerUnit playerUnit, Action<bool> result)
        {
            NavMeshAgent agent = playerUnit.GetComponent<NavMeshAgent>();
            float notMovingTime = 0;
            const float MAX_NOT_MOVING_TIME = 1; // Maximum allowed time not moving

            while (notMovingTime < MAX_NOT_MOVING_TIME)
            {
                if (agent.velocity.sqrMagnitude <= 0.01f)
                {
                    notMovingTime += Time.deltaTime; // Increase notMovingTime if the unit is not moving
                }
                else
                {
                    notMovingTime = 0; // Reset notMovingTime if the unit started moving
                }

                yield return null; // Wait for the next frame
            }

            // If the code reaches here, it means the unit was not moving for more than MAX_NOT_MOVING_TIME
            result(true);
        }



    }


}






