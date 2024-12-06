using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_PlayerTankPrefab;       // Reference to the prefab the player will control.
        public Transform m_PlayerSpawnPoint;        // Spawn point for the player tank.
        public GameObject m_SmallEnemyPrefab;       // Reference to the prefab the small enemies will control.
        public GameObject m_MediumEnemyPrefab;      // Reference to the prefab the medium enemies will control.
        public GameObject m_BossPrefab;             // Reference to the prefab the boss will control.
        public Transform[] m_SpawnPointsSmall;      // Spawn points for small enemy tanks.
        public Transform[] m_SpawnPointsMedium;     // Spawn points for medium enemy tanks.
        public Transform[] m_SpawnPointsBoss;       // Spawn points for boss.
        public GameObject m_HealthItemPrefab;       // Item that recovers and increases max health

        private string m_Phase = "small";           // Current phase of the game - small, medium, boss
        private GameObject m_PlayerTank;            // Instance of the player's tank.
        private TankHealth m_PlayerHealth;          // Reference to the player's TankHealth component.
        private GameObject[] m_SmallEnemies;        // Instances of the small enemy tanks.
        private GameObject[] m_MediumEnemies;       // Instances of the medium enemy tanks.
        private GameObject[] m_Boss;                // Instances of the boss.
        private float m_HealthIncreaseAmount = 50f; // Amount to increase max health
        private float m_HealthRestoreAmount = 30f;  // Amount to restore current health
        private WaitForSeconds m_StartWait;         // Delay before starting a round.
        private WaitForSeconds m_EndWait;           // Delay before ending a round.

        private void Start()
        {
            // Initialize delays.
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            // Spawn the player and small enemy tanks.
            SpawnPlayerTank();
            SpawnSmallEnemies();

            // Set the camera targets.
            SetCameraTargets();

            // Start the game loop.
            StartCoroutine(GameLoop());
        }

        private void SpawnPlayerTank()
        {
            m_PlayerTank = Instantiate(m_PlayerTankPrefab, m_PlayerSpawnPoint.position, m_PlayerSpawnPoint.rotation);
            m_PlayerHealth = m_PlayerTank.GetComponent<TankHealth>();

            SetTankColor(m_PlayerTank, new Color(0f, 0.6f, 0f));
        }

        private void SpawnSmallEnemies()
        {
            m_SmallEnemies = new GameObject[m_SpawnPointsSmall.Length];

            for (int i = 0; i < m_SpawnPointsSmall.Length; i++)
            {
                m_SmallEnemies[i] = Instantiate(m_SmallEnemyPrefab, m_SpawnPointsSmall[i].position, m_SpawnPointsSmall[i].rotation);
                SetTankColor(m_SmallEnemies[i], new Color(28.0f / 255.0f, 102.0f / 255.0f, 207.0f / 255.0f));
            }
        }

        private void SpawnMediumEnemies()
        {
            m_MediumEnemies = new GameObject[m_SpawnPointsMedium.Length];

            for (int i = 0; i < m_SpawnPointsMedium.Length; i++)
            {
                m_MediumEnemies[i] = Instantiate(m_MediumEnemyPrefab, m_SpawnPointsMedium[i].position, m_SpawnPointsMedium[i].rotation);
                SetTankColor(m_MediumEnemies[i], new Color(176.0f / 255.0f, 173.0f / 255.0f, 8.0f / 255.0f));
            }
        }

        private void SpawnBoss()
        {
            m_Boss = new GameObject[m_SpawnPointsBoss.Length];

            for (int i = 0; i < m_SpawnPointsBoss.Length; i++)
            {
                m_Boss[i] = Instantiate(m_BossPrefab, m_SpawnPointsBoss[i].position, m_SpawnPointsBoss[i].rotation);
                SetTankColor(m_Boss[i], new Color(213.0f / 255.0f, 15.0f / 255.0f, 15.0f / 255.0f));
            }
        }

        private void SetTankColor(GameObject tank, Color color)
        {
            Renderer[] renderers = tank.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.CompareTag("Trail"))
                    continue;

                renderer.material.color = color;
            }
        }

        private void SetCameraTargets()
        {
            // Get the current set of enemy tanks based on the current phase.
            GameObject[] m_EnemyTanks = GetCurrentEnemies();

            // Create an array of targets that includes the player tank.
            Transform[] targets = new Transform[m_EnemyTanks.Length + 1];

            // Set the player tank as the first target.
            targets[0] = m_PlayerTank.transform;

            // Set each enemy tank as a target.
            for (int i = 0; i < m_EnemyTanks.Length; i++)
            {
                targets[i + 1] = m_EnemyTanks[i].transform;
            }

            // Assign the updated targets to the camera.
            m_CameraControl.m_Targets = targets;
        }

        private GameObject[] GetCurrentEnemies()
        {
            if (m_Phase == "small")
            {
                return m_SmallEnemies;
            }
            else if (m_Phase == "medium")
            {
                return m_MediumEnemies;
            }
            else if (m_Phase == "boss")
            {
                return m_Boss;
            }
            return new GameObject[0];
        }

        private IEnumerator GameLoop()
        {
            while (true)
            {
                yield return StartCoroutine(RoundStarting());
                yield return StartCoroutine(RoundPlaying());
                yield return StartCoroutine(RoundEnding());

                if (!m_PlayerHealth.gameObject.activeSelf)
                {
                    m_MessageText.text = "YOU LOSE!";
                    yield return m_EndWait;
                    SceneManager.LoadScene(0); // Reload the scene
                    yield break;
                }
                else if (AllEnemiesDefeated())
                {
                    if (m_Phase == "small")
                    {
                        m_Phase = "medium";
                        SpawnMediumEnemies();
                        SetCameraTargets();
                        
                    }
                    else if (m_Phase == "medium")
                    {
                        m_Phase = "boss";
                        SpawnBoss();
                        SetCameraTargets();
                    }
                    else if (m_Phase == "boss")
                    {
                        m_MessageText.text = "YOU WIN!";
                        yield return m_EndWait;
                        SceneManager.LoadScene(0); // Reload the scene or exit
                        yield break;
                    }
                }
            }
        }

        private IEnumerator RoundStarting()
        {
            ResetPlayerTank();
            ResetEnemyTanks();
            m_CameraControl.SetStartPositionAndSize();

            m_MessageText.text = "GET READY!";
            yield return m_StartWait;

            // Spawn health item randomly in the current phase
            SpawnHealthItem();
        }

        private IEnumerator RoundPlaying()
        {
            m_MessageText.text = string.Empty;

            while (m_PlayerHealth.gameObject.activeSelf && !AllEnemiesDefeated())
            {
                // Check for item pickups during gameplay
                CheckForItemPickup();
                yield return null;
            }
        }

        private IEnumerator RoundEnding()
        {
            DisableTankControl();

            if (!m_PlayerHealth.gameObject.activeSelf)
            {
                m_MessageText.text = "YOU LOSE!";
            }
            else if (AllEnemiesDefeated())
            {
                if (m_Phase == "small")
                {
                    m_MessageText.text = "PHASE 1 COMPLETED!";
                }
                else if (m_Phase == "medium")
                {
                    m_MessageText.text = "PHASE 2 COMPLETED";
                }
                else if (m_Phase == "boss")
                {
                    m_MessageText.text = "YOU WIN!";
                }
            }
            else
            {
                m_MessageText.text = "ROUND OVER!";
            }

            yield return m_EndWait;
        }

        private bool AllEnemiesDefeated()
        {
            GameObject[] m_EnemyTanks = GetCurrentEnemies();

            foreach (var enemy in m_EnemyTanks)
            {
                if (enemy.activeSelf)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResetPlayerTank()
        {
            m_PlayerTank.transform.position = m_PlayerSpawnPoint.position;
            m_PlayerTank.transform.rotation = m_PlayerSpawnPoint.rotation;

            TankMovement playerTankMovement = m_PlayerTank.GetComponent<TankMovement>();
            if (playerTankMovement != null) playerTankMovement.enabled = true;

            TankShooting playerTankShooting = m_PlayerTank.GetComponent<TankShooting>();
            if (playerTankShooting != null) playerTankShooting.enabled = true;
        }

        private void ResetEnemyTanks()
        {
            GameObject[] m_EnemyTanks = GetCurrentEnemies();

            foreach (var enemy in m_EnemyTanks)
            {
                enemy.transform.position = GetEnemySpawnPoint(enemy).position;
                enemy.transform.rotation = GetEnemySpawnPoint(enemy).rotation;

                TankMovement enemyTankMovement = enemy.GetComponent<TankMovement>();
                if (enemyTankMovement != null) enemyTankMovement.enabled = true;

                TankShooting enemyTankShooting = enemy.GetComponent<TankShooting>();
                if (enemyTankShooting != null) enemyTankShooting.enabled = true;
            }
        }

        private Transform GetEnemySpawnPoint(GameObject enemy)
        {
            if (m_Phase == "small")
            {
                return m_SpawnPointsSmall[System.Array.IndexOf(m_SmallEnemies, enemy)];
            }
            else if (m_Phase == "medium")
            {
                return m_SpawnPointsMedium[System.Array.IndexOf(m_MediumEnemies, enemy)];
            }
            else if (m_Phase == "boss")
            {
                return m_SpawnPointsBoss[System.Array.IndexOf(m_Boss, enemy)];
            }
            return null;
        }

        private void DisableTankControl()
        {
            // Disable control for the player tank and all enemy tanks.
            TankMovement playerTankMovement = m_PlayerTank.GetComponent<TankMovement>();
            if (playerTankMovement != null) playerTankMovement.enabled = false;

            TankShooting playerTankShooting = m_PlayerTank.GetComponent<TankShooting>();
            if (playerTankShooting != null) playerTankShooting.enabled = false;

            GameObject[] m_EnemyTanks = GetCurrentEnemies();
            foreach (var enemy in m_EnemyTanks)
            {
                TankMovement enemyTankMovement = enemy.GetComponent<TankMovement>();
                if (enemyTankMovement != null) enemyTankMovement.enabled = false;

                TankShooting enemyTankShooting = enemy.GetComponent<TankShooting>();
                if (enemyTankShooting != null) enemyTankShooting.enabled = false;
            }
        }

        private void SpawnHealthItem()
        {
            Vector3 spawnPosition = m_PlayerTank.transform.position + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            Instantiate(m_HealthItemPrefab, spawnPosition, Quaternion.identity);
        }

        private void CheckForItemPickup()
        {
            Collider[] hitColliders = Physics.OverlapSphere(m_PlayerTank.transform.position, 2f); // Check within range of 2 units

            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Item"))
                {
                    OnItemPickedUp(hitCollider.gameObject);
                }
            }
        }

        private void OnItemPickedUp(GameObject item)
        {
            if (item.CompareTag("Item"))
            {
                m_PlayerHealth.IncreaseMaxHealth(m_HealthIncreaseAmount);
                m_PlayerHealth.RestoreHealth(m_HealthRestoreAmount);
            }

            Destroy(item);
        }
    }
}