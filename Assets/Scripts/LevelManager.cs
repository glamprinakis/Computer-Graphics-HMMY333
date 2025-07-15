using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using MODZ.RTS.Units;
using System.Collections.Generic;
using TMPro;

namespace MODZ.RTS.Player
{

    public class LevelManager : MonoBehaviour
    {
        public NavMeshSurface forest; 
        public NavMeshSurface snowForest;
        public NavMeshSurface savana;
        public NavMeshSurface tropical;

        private GameObject cameraParent;
        public GameObject playerUnitPrefab; 
        public GameObject enemyUnitPrefab; 
        public GameObject healthPackPrefab;
        public Vector3 playerUnitSpawnPosition; 
        public Vector3 enemyUnitSpawnPosition; 
        public Transform playerFighterTransform; 
        public Transform playerRangerTransform; 
        public Transform playerMageTransform; 
        public Transform playerPriestTransform; 
        public Transform enemyFighterTransform; 
        public Transform enemyRangerTransform; 
        public Transform enemyMageTransform; 
        public Transform enemyPriestTransform; 
        public Transform healthPackTransform;
        public bool alldead = true;
        public Transform playerUnits;
        public Transform enemyUnits;
        public string mapName;
        public Portal portal;
        public GameObject portalPrefab;
        private Vector3 startPosition;
        private Vector3 randomMapPosition;
        private Grid gridComponent;
        CameraScript cameraScript;
        public TextMeshProUGUI levelUp;
        
        // List to store the map numbers of the visited maps
        private List<string> nonVisitedMaps = new List<string>() { "Forest", "SnowForest", "Savana", "Tropical" };
        private List<string> VisitedMaps = new List<string>();


        private void Awake()
        {
            StartCoroutine(BakeNavMeshThenSpawnPlayer());
            gridComponent = FindObjectOfType<Grid>();
            cameraParent = GameObject.Find("CameraParent");
            cameraScript = GameObject.Find("CameraParent").GetComponent<CameraScript>();
            alldead = true;
            if(GlobalVariables.enemyType == "Autonomous")
            {
               PlayerManager.instance.isAutonomous = true;

            }
            else if(GlobalVariables.enemyType == "Manual")
            {
                PlayerManager.instance.isAutonomous = false;
            }
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                Vector3 newPosition = gridComponent.GetRandomNonWaterCellPosition(mapName);
                foreach (Transform unitType in playerUnits)
                {
                    foreach (Transform unit in unitType)
                    {
                        unit.transform.gameObject.SetActive(false);
                        unit.position = newPosition;
                        unit.transform.gameObject.SetActive(true);
                    }
                }
            }
        }

        private IEnumerator BakeNavMeshThenSpawnPlayer()
        {
            startPosition = new Vector3(0, 299, 0);
            // Bake the NavMesh
            forest.BuildNavMesh();
            snowForest.BuildNavMesh();
            savana.BuildNavMesh();
            tropical.BuildNavMesh();

            // Wait until the end of the frame to ensure that the NavMesh has finished baking
            yield return new WaitForEndOfFrame();

            // Generate random positions
            if(GlobalVariables.map == 1)
            {
                randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Forest");
                yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(-20, 20, 0), 4f));
                ForestCameraLimitation();
                nonVisitedMaps.Remove("Forest");
                VisitedMaps.Add("Forest");
                ActivateForestEnemy();
                mapName = "Forest";
            }
            else if (GlobalVariables.map == 2)
            {
                randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("SnowForest");
                yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(-20, 20, 0), 4f));
                SnowForestCameraLimitation();
                nonVisitedMaps.Remove("SnowForest");
                VisitedMaps.Add("SnowForest");
                ActivateSnowForestEnemy();
                mapName = "SnowForest";
            }
            else if (GlobalVariables.map == 3)
            {
                randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Savana");
                yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(-20, 20, 0), 4f));
                SavvanaCameraLimitation();
                nonVisitedMaps.Remove("Savana");
                VisitedMaps.Add("Savana");
                ActivateSavanaEnemy();
                mapName = "Savana";
            }
            else if (GlobalVariables.map == 4)
            {
                randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Tropical");
                yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(-20, 20, 0), 4f));
                TropicalCameraLimitation();
                nonVisitedMaps.Remove("Tropical");
                VisitedMaps.Add("Tropical");
                ActivateTropicaltEnemy();
                mapName = "Tropical";
            }
            else
            {
                yield return StartCoroutine(RandomMapInitialization());
            }

        }

        public IEnumerator RandomMapInitialization()
        {
            // Generate a random index between 0 and 3
            int randomIndex = Random.Range(0, 4);
            // Start the corresponding coroutine
            switch (randomIndex)
            {
                case 0:
                    randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Forest");
                    yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                    yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(0, 20, 0), 4f));
                    ForestCameraLimitation();
                    nonVisitedMaps.Remove("Forest");
                    VisitedMaps.Add("Forest");
                    ActivateForestEnemy();
                    mapName = "Forest";
                    break;
                case 1:
                    randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("SnowForest");
                    yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                    yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(0, 20, 0), 4f));
                    SnowForestCameraLimitation();
                    nonVisitedMaps.Remove("SnowForest");
                    VisitedMaps.Add("SnowForest");
                    ActivateSnowForestEnemy();
                    mapName = "SnowForest";
                    break;
                case 2:
                    randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Savana");
                    yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                    yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(0, 20, 0), 4f));
                    SavvanaCameraLimitation();
                    nonVisitedMaps.Remove("Savana");
                    VisitedMaps.Add("Savana");
                    ActivateSavanaEnemy();
                    mapName = "Savana";
                    break;
                case 3:
                    randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Tropical");
                    yield return StartCoroutine(PlayerInitialization(randomMapPosition));
                    yield return StartCoroutine(SmoothMove(startPosition, randomMapPosition + new Vector3(0, 20, 0), 4f));
                    TropicalCameraLimitation();
                    nonVisitedMaps.Remove("Tropical");
                    VisitedMaps.Add("Tropical");
                    ActivateTropicaltEnemy();
                    mapName = "Tropical";
                    break;
            }
        }

        public IEnumerator PlayerInitialization(Vector3 position)
        {
            for (int i = 0; i < GlobalVariables.characters.Count; i++)
            {
                if (GlobalVariables.characters[i].cClass == "Mage")
                {
                    GameObject Mage = Instantiate(playerUnitPrefab, position, Quaternion.identity);
                    Mage.transform.SetParent(playerMageTransform);
                    GameObject bow = Mage.transform.Find("Scepter").gameObject;
                    bow.SetActive(true);
                    Units.Player.PlayerUnit pU = Mage.GetComponent<Units.Player.PlayerUnit>();
                    pU.dexterity = GlobalVariables.characters[i].dexteriry;
                    pU.charisma = GlobalVariables.characters[i].charisma;
                    pU.consitution = GlobalVariables.characters[i].constitution;
                    pU.strenght = GlobalVariables.characters[i].stength;
                    pU.vitality = GlobalVariables.characters[i].vitality;
                    UnitHandler.instance.SetBasicUnitStats(playerUnits);
                }
                else if (GlobalVariables.characters[i].cClass == "Fighter")
                {
                    GameObject Figher = Instantiate(playerUnitPrefab, position, Quaternion.identity);
                    Figher.transform.SetParent(playerFighterTransform);
                    GameObject bow = Figher.transform.Find("Axe").gameObject;
                    bow.SetActive(true);
                    Units.Player.PlayerUnit pU = Figher.GetComponent<Units.Player.PlayerUnit>();
                    pU.dexterity = GlobalVariables.characters[i].dexteriry;
                    pU.charisma = GlobalVariables.characters[i].charisma;
                    pU.consitution = GlobalVariables.characters[i].constitution;
                    pU.strenght = GlobalVariables.characters[i].stength;
                    pU.vitality = GlobalVariables.characters[i].vitality;
                    UnitHandler.instance.SetBasicUnitStats(playerUnits);
                }
                else if (GlobalVariables.characters[i].cClass == "Ranger")
                {
                    GameObject Ranger = Instantiate(playerUnitPrefab, position, Quaternion.identity);
                    Ranger.transform.SetParent(playerRangerTransform);
                    GameObject bow = Ranger.transform.Find("Bow").gameObject;
                    bow.SetActive(true);
                    Units.Player.PlayerUnit pU = Ranger.GetComponent<Units.Player.PlayerUnit>();
                    pU.dexterity = GlobalVariables.characters[i].dexteriry;
                    pU.charisma = GlobalVariables.characters[i].charisma;
                    pU.consitution = GlobalVariables.characters[i].constitution;
                    pU.strenght = GlobalVariables.characters[i].stength;
                    pU.vitality = GlobalVariables.characters[i].vitality;
                    UnitHandler.instance.SetBasicUnitStats(playerUnits);
                }
                else if (GlobalVariables.characters[i].cClass == "Priest")
                {
                    GameObject Priest = Instantiate(playerUnitPrefab, position, Quaternion.identity);
                    Priest.transform.SetParent(playerPriestTransform);
                    GameObject bow = Priest.transform.Find("Cross").gameObject;
                    bow.SetActive(true);
                    Units.Player.PlayerUnit pU = Priest.GetComponent<Units.Player.PlayerUnit>();
                    pU.dexterity = GlobalVariables.characters[i].dexteriry;
                    pU.charisma = GlobalVariables.characters[i].charisma;
                    pU.consitution = GlobalVariables.characters[i].constitution;
                    pU.strenght = GlobalVariables.characters[i].stength;
                    pU.vitality = GlobalVariables.characters[i].vitality;
                    UnitHandler.instance.SetBasicUnitStats(playerUnits);
                }
                GameObject ForesthealthPack = Instantiate(healthPackPrefab, gridComponent.GetRandomNonWaterCellPosition("Forest"), Quaternion.identity);
                ForesthealthPack.transform.SetParent(healthPackTransform);
                GameObject TropicalhealthPack = Instantiate(healthPackPrefab, gridComponent.GetRandomNonWaterCellPosition("Tropical"), Quaternion.identity);
                TropicalhealthPack.transform.SetParent(healthPackTransform);
                GameObject SnowForesthealthPack = Instantiate(healthPackPrefab, gridComponent.GetRandomNonWaterCellPosition("SnowForest"), Quaternion.identity);
                SnowForesthealthPack.transform.SetParent(healthPackTransform);
                GameObject SavannahealthPack = Instantiate(healthPackPrefab, gridComponent.GetRandomNonWaterCellPosition("Savana"), Quaternion.identity);
                SavannahealthPack.transform.SetParent(healthPackTransform);
                yield return null;
            }

        }

        IEnumerator SmoothMove(Vector3 startpos, Vector3 endpos, float seconds)
        {
            float t = 0f;
            float startRotation = 90f; // initial camera rotation
            float endRotation = 30f; // final camera rotation
            float startYRotation = 0f; // initial parent rotation
            float endYRotation = 90f; // final parent rotation

            while (t <= 1.0)
            {
                t += Time.deltaTime / seconds;

                // Move position smoothly
                cameraParent.transform.position = Vector3.Lerp(startpos, endpos, Mathf.SmoothStep(0f, 1f, t));

                // Smoothly rotate the camera from 90 degrees to 30 degrees
                float rotationX = Mathf.Lerp(startRotation, endRotation, Mathf.SmoothStep(0f, 1f, t));
                cameraParent.transform.GetChild(0).eulerAngles = new Vector3(rotationX, cameraParent.transform.GetChild(0).eulerAngles.y, cameraParent.transform.GetChild(0).eulerAngles.z);

                // Smoothly rotate the parent's Y rotation from 0 degrees to 90 degrees, while keeping X and Z rotation at 0
                float rotationY = Mathf.Lerp(startYRotation, endYRotation, Mathf.SmoothStep(0f, 1f, t));
                cameraParent.transform.eulerAngles = new Vector3(0, rotationY, 0);

                yield return null;
            }
        }

        public void PortalGeneration()
        {
            Debug.Log("GAMO TO THEO MOU ");
            Vector3 portalPosition = gridComponent.GetRandomNonWaterCellPosition(mapName); 
            Instantiate(portalPrefab, portalPosition, Quaternion.identity);
            StartCoroutine(ActivateLevelUpText());
        }

        public IEnumerator TeleportUnits()
        {


            // Check if all maps have been visited
            
            mapName = nonVisitedMaps[Random.Range(0, nonVisitedMaps.Count)];
            Vector3 newPosition = gridComponent.GetRandomNonWaterCellPosition(mapName);
            Debug.Log($"map that is aboud to be visited {mapName}, no of unvisited maps: {nonVisitedMaps.Count},  no of visited maps: {VisitedMaps.Count}, position{newPosition}");
            // Wait for the end of the frame to ensure that all updates from the previous frame have finished
            yield return new WaitForEndOfFrame();

            // Iterate over each child of the playerUnits parent and set their position to the new position
            foreach (Transform unitType in playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    //Debug.Log($"Moving unit {unit.name} to position {newPosition}");
                    unit.transform.gameObject.SetActive(false);
                    unit.position = newPosition;
                    unit.transform.gameObject.SetActive(true);
                    //Debug.Log($"Unit {unit.name} is now at position {unit.position}");
                }
            }

            if(mapName == "Forest")
            {
                ForestCameraLimitation();
                ActivateForestEnemy();


            }
            else if (mapName == "SnowForest")
            {
                SnowForestCameraLimitation();
                ActivateSnowForestEnemy();
            }
            else if (mapName == "Savana")
            {
                SavvanaCameraLimitation();
                ActivateSavanaEnemy();
            }
            else if (mapName == "Tropical")
            {
                TropicalCameraLimitation();
                ActivateTropicaltEnemy();
            }


            // Destroy the portal at the end
            if (portal != null)
            {
                Destroy(portal.gameObject);
            }
             alldead = true;
        }

        public void ForestCameraLimitation()
        {
            cameraScript.uLim = new Vector2(170, 170);
            cameraScript.lLim = new Vector2(0, 0);
            cameraScript.maxHeight = 70;
        }

        public void SnowForestCameraLimitation()
        {
            cameraScript.uLim = new Vector2(170, 0);
            cameraScript.lLim = new Vector2(0, 170);
            cameraScript.maxHeight = 70;
        }
        public void SavvanaCameraLimitation()
        {
            cameraScript.uLim = new Vector2(0, 0);
            cameraScript.lLim = new Vector2(170, 170);
            cameraScript.maxHeight = 70;
        }
        public void TropicalCameraLimitation()
        {
            cameraScript.uLim = new Vector2(0, 170);
            cameraScript.lLim = new Vector2(170, 0);
            cameraScript.maxHeight = 70;
        }

        public void ActivateForestEnemy()
        {
            Debug.Log($"its your boy modz");
            randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Forest");
            GameObject Mage = Instantiate(enemyUnitPrefab, randomMapPosition, Quaternion.identity);
            Mage.transform.SetParent(enemyMageTransform);
            GameObject bow = Mage.transform.Find("Scepter").gameObject;
            bow.SetActive(true);
            if (GlobalVariables.difficulty == "Easy")
            {
                Units.Enemy.EnemyUnit eU = Mage.GetComponent< Units.Enemy.EnemyUnit > ();
                eU.dexterity = 5;
                eU.charisma = 4;
                eU.consitution = 2;
                eU.strenght = 5;
                eU.vitality = 2;
            }
            else if (GlobalVariables.difficulty == "Moderate")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 10;
                eU.charisma = 8;
                eU.consitution = 4;
                eU.strenght = 10;
                eU.vitality = 4;
            }
            else if (GlobalVariables.difficulty == "Hard")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 15;
                eU.charisma = 12;
                eU.consitution = 6;
                eU.strenght = 15;
                eU.vitality = 6;
            }
            UnitHandler.instance.SetBasicUnitStats(enemyUnits);
            
        }
        public void ActivateSnowForestEnemy()
        {
            randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("SnowForest");
            GameObject Mage = Instantiate(enemyUnitPrefab, randomMapPosition, Quaternion.identity);
            Mage.transform.SetParent(enemyFighterTransform);
            GameObject bow = Mage.transform.Find("Axe").gameObject;
            bow.SetActive(true);
            if (GlobalVariables.difficulty == "Easy")
            {
                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 3;
                eU.charisma = 2;
                eU.consitution = 4;
                eU.strenght = 5;
                eU.vitality = 4;
            }
            else if (GlobalVariables.difficulty == "Moderate")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 6;
                eU.charisma = 4;
                eU.consitution = 8;
                eU.strenght = 10;
                eU.vitality = 8;
            }
            else if (GlobalVariables.difficulty == "Hard")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 9;
                eU.charisma = 6;
                eU.consitution = 12;
                eU.strenght = 15;
                eU.vitality = 12;
            }
            UnitHandler.instance.SetBasicUnitStats(enemyUnits);
        }
        public void ActivateSavanaEnemy()
        {
            randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Savana");
            GameObject Mage = Instantiate(enemyUnitPrefab, randomMapPosition, Quaternion.identity);
            Mage.transform.SetParent(enemyRangerTransform);
            GameObject bow = Mage.transform.Find("Bow").gameObject;
            bow.SetActive(true);
            if (GlobalVariables.difficulty == "Easy")
            {
                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 5;
                eU.charisma = 4;
                eU.consitution = 2;
                eU.strenght = 5;
                eU.vitality = 2;
            }
            else if (GlobalVariables.difficulty == "Moderate")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 10;
                eU.charisma = 8;
                eU.consitution = 4;
                eU.strenght = 10;
                eU.vitality = 4;
            }
            else if (GlobalVariables.difficulty == "Hard")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 15;
                eU.charisma = 12;
                eU.consitution = 6;
                eU.strenght = 15;
                eU.vitality = 6;
            }
            UnitHandler.instance.SetBasicUnitStats(enemyUnits);
        }
        public void ActivateTropicaltEnemy()
        {
            randomMapPosition = gridComponent.GetRandomNonWaterCellPosition("Tropical");
            GameObject Mage = Instantiate(enemyUnitPrefab, randomMapPosition, Quaternion.identity);
            Mage.transform.SetParent(enemyPriestTransform);
            GameObject bow = Mage.transform.Find("Cross").gameObject;
            bow.SetActive(true);
            if (GlobalVariables.difficulty == "Easy")
            {
                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 3;
                eU.charisma = 2;
                eU.consitution = 4;
                eU.strenght = 5;
                eU.vitality = 4;
            }
            else if (GlobalVariables.difficulty == "Moderate")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 6;
                eU.charisma = 4;
                eU.consitution = 8;
                eU.strenght = 10;
                eU.vitality = 8;
            }
            else if (GlobalVariables.difficulty == "Hard")
            {

                Units.Enemy.EnemyUnit eU = Mage.GetComponent<Units.Enemy.EnemyUnit>();
                eU.dexterity = 9;
                eU.charisma = 6;
                eU.consitution = 12;
                eU.strenght = 15;
                eU.vitality = 12;
            }
            UnitHandler.instance.SetBasicUnitStats(enemyUnits);
        }

        private IEnumerator ActivateLevelUpText()
        {
            levelUp.gameObject.SetActive(true);
            yield return new WaitForSeconds(2);
            foreach (Transform unitType in playerUnits)
            {
                foreach (Transform unit in unitType)
                {
                    Units.Player.PlayerUnit pU = unit.GetComponent<Units.Player.PlayerUnit>();
                    pU.dexterity += 1;
                    pU.charisma += 1;
                    pU.consitution += 1;
                    pU.strenght += 1;
                    pU.vitality += 1;
                }
            }
            levelUp.gameObject.SetActive(false);

        }

        public IEnumerator SpawnMinions(Vector3 position)
        {
            GameObject Figher = Instantiate(enemyUnitPrefab, position, Quaternion.identity);
            Figher.transform.SetParent(enemyFighterTransform);
            GameObject bow = Figher.transform.Find("Axe").gameObject;
            bow.SetActive(true);
            Units.Enemy.EnemyUnit eU = Figher.GetComponent<Units.Enemy.EnemyUnit>();
            UnitHandler.instance.SetBasicUnitStats(enemyUnits);
            yield return new WaitForSeconds(1);
            eU.health = 1;
            eU.attack = 5;
            eU.armor = 0;
            eU.atkRange = 5;
            eU.movingRange = 15;
            eU.currentHealth = 1;
            eU.isMinion = true;
        }

    }
}
    

