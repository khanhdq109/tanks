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
        public GameObject m_EnemyTankPrefab;        // Reference to the prefab the enemies will control.
        public Transform[] m_EnemySpawnPoints;      // Spawn points for enemy tanks.

        private GameObject m_PlayerTank;            // Instance of the player's tank.
        private TankHealth m_PlayerHealth;          // Reference to the player's TankHealth component.
        private GameObject[] m_EnemyTanks;          // Instances of the enemy tanks.
        private WaitForSeconds m_StartWait;         // Delay before starting a round.
        private WaitForSeconds m_EndWait;           // Delay before ending a round.

        private void Start()
        {
            // Initialize delays.
            m_StartWait = new WaitForSeconds(m_StartDelay);
            m_EndWait = new WaitForSeconds(m_EndDelay);

            // Spawn the player and enemy tanks.
            SpawnPlayerTank();
            SpawnEnemyTanks();

            // Set the camera targets.
            SetCameraTargets();

            // Start the game loop.
            StartCoroutine(GameLoop());
        }

        private void SpawnPlayerTank()
        {
            m_PlayerTank = Instantiate(m_PlayerTankPrefab, m_PlayerSpawnPoint.position, m_PlayerSpawnPoint.rotation);
            m_PlayerHealth = m_PlayerTank.GetComponent<TankHealth>();
        }

        private void SpawnEnemyTanks()
        {
            m_EnemyTanks = new GameObject[m_EnemySpawnPoints.Length];

            for (int i = 0; i < m_EnemySpawnPoints.Length; i++)
            {
                m_EnemyTanks[i] = Instantiate(m_EnemyTankPrefab, m_EnemySpawnPoints[i].position, m_EnemySpawnPoints[i].rotation);
            }
        }

        private void SetCameraTargets()
        {
            Transform[] targets = new Transform[m_EnemyTanks.Length + 1];

            // Add the player tank as a target.
            targets[0] = m_PlayerTank.transform;

            // Add all enemy tanks as targets.
            for (int i = 0; i < m_EnemyTanks.Length; i++)
            {
                targets[i + 1] = m_EnemyTanks[i].transform;
            }

            m_CameraControl.m_Targets = targets;
        }

        private IEnumerator GameLoop()
        {
            yield return StartCoroutine(RoundStarting());
            yield return StartCoroutine(RoundPlaying());
            yield return StartCoroutine(RoundEnding());

            if (!m_PlayerHealth.gameObject.activeSelf)
            {
                // Player lost; reload the scene.
                SceneManager.LoadScene(0);
            }
            else if (AllEnemiesDefeated())
            {
                // Player won; reload the scene.
                SceneManager.LoadScene(0);
            }
            else
            {
                // Restart the game loop.
                StartCoroutine(GameLoop());
            }
        }

        private IEnumerator RoundStarting()
        {
            ResetPlayerTank();
            ResetEnemyTanks();
            m_CameraControl.SetStartPositionAndSize();

            m_MessageText.text = "GET READY!";
            yield return m_StartWait;
        }

        private IEnumerator RoundPlaying()
        {
            m_MessageText.text = string.Empty;

            while (m_PlayerHealth.gameObject.activeSelf && !AllEnemiesDefeated())
            {
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
                m_MessageText.text = "YOU WIN!";
            }
            else
            {
                m_MessageText.text = "ROUND OVER!";
            }

            yield return m_EndWait;
        }

        private bool AllEnemiesDefeated()
        {
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
            m_PlayerTank.SetActive(true);
        }

        private void ResetEnemyTanks()
        {
            for (int i = 0; i < m_EnemyTanks.Length; i++)
            {
                m_EnemyTanks[i].transform.position = m_EnemySpawnPoints[i].position;
                m_EnemyTanks[i].transform.rotation = m_EnemySpawnPoints[i].rotation;
                m_EnemyTanks[i].SetActive(true);
            }
        }

        private void DisableTankControl()
        {
            m_PlayerTank.GetComponent<TankMovement>().enabled = false;
            m_PlayerTank.GetComponent<TankShooting>().enabled = false;

            foreach (var enemy in m_EnemyTanks)
            {
                enemy.GetComponent<TankMovement>().enabled = false;
                enemy.GetComponent<TankShooting>().enabled = false;
            }
        }
    }
}