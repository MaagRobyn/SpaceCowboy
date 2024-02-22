using Assets;
using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static GameMode gameMode;

    private Camera mainCamera;
    public PlayerScript Player;
    [SerializeField] private float currentDangerLevel;
    private readonly List<ShipScript> existingShips = new();
    public float bounty = 0;
    private EncounterType encounterType;
    
    [Header("Debug")]
    [SerializeField] private bool useDebugSettings;
    [SerializeField] private float debugBounty = 0.0f;
    [SerializeField] private EncounterType debugEncounterType;

    [Header("UI")]
    [SerializeField] Button optionButton;
    [SerializeField] RadarScript radar;
    [SerializeField] Transform radarHolder;
    [SerializeField] RectTransform rewardMenu;
    [SerializeField] TextMeshProUGUI bountyText;
    [SerializeField] GameObject deathscreen;
    [SerializeField] GameObject roundCounterScreen;
    [SerializeField] TextMeshProUGUI roundCounterText;
    [SerializeField] Slot slotPrefab;

    [Header("SpawnableObjects")]
    [SerializeField] AlienShipScript alienObject;
    [SerializeField] List<ScriptableAlien> alienList = new();
    [SerializeField] CrateScript reward;
    [SerializeField] List<Reward> rewardList = new();

    bool roundHasEnded = false;
    bool roundCanEnd = false;
    float roundBounty = 1.0f;
    int roundCount = 1;

    // Timers
    float postRoundTimer = 5.0f;
    float spawnDelay = 1.0f;
    float deathScreenTimer = 3.0f;

    bool playerDeathEventHasBeenSet = false;
    bool playerHasDied = false;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;

    }
    private void StartNextRound()
    {

    }

    void Start()
    {
        if (useDebugSettings)
        {
            AddBounty(debugBounty);
            encounterType = debugEncounterType;
        }
        else
        {
            AddBounty(5.0f);
            switch (gameMode)
            {
                case GameMode.Story:
                    encounterType = EncounterType.Basic;
                    break;
                case GameMode.Endless:
                    encounterType = EncounterType.Endless;
                    break;
                default:
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // If the death event has been created but the player exists, create the death event
        if (!playerDeathEventHasBeenSet && Player != null)
        {
            playerDeathEventHasBeenSet = true;
            Player.OnPlayerDeath += () =>
            {
                playerHasDied = true;
            };
        }

        if(!playerHasDied)
        {
            if (!roundHasEnded)
            {
                // Tick Spawntimer
                if (spawnDelay > 0)
                {
                    spawnDelay -= Time.deltaTime;
                }
                // Spawn enemy if we haven't reached the max amount of enemies for this area
                else if (currentDangerLevel < bounty && spawnDelay <= 0)
                {
                    int dangerLevel = int.MaxValue;
                    ScriptableAlien alien;
                    do
                    {
                        alien = alienList[Random.Range(0, alienList.Count)];
                        dangerLevel = alien.dangerLevel;
                    } while (dangerLevel > bounty - currentDangerLevel);

                    var alienShip = SpawnAlien(alienObject);
                    alienShip.SetShipType(alien);
                    currentDangerLevel += alien.dangerLevel;
                    existingShips.Add(alienShip);
                }

                // End of Round
                if (roundCanEnd && existingShips.Count == 0)
                {
                    roundHasEnded = true;
                    Globals.gravityScale = 0;
                    roundCanEnd = false;
                    currentDangerLevel = 0;
                    switch (encounterType)
                    {
                        case EncounterType.Endless:
                            spawnDelay = 1.0f;
                            postRoundTimer = 5.0f;
                            roundBounty = 1.0f;
                            AddBounty(roundBounty);
                            roundCounterText.text = $"Round {roundCount} completed\nBounty Gained: ${roundBounty * 10}00";
                            roundCounterScreen.SetActive(true);
                            roundCount++;
                            break;
                        case EncounterType.Basic:
                            spawnDelay = 5.0f;
                            AddBounty(1.0f);
                            SpawnRewards(2);
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                if(encounterType == EncounterType.Endless)
                {

                    if (postRoundTimer > 0)
                    {
                        postRoundTimer -= Time.deltaTime;
                    }
                    // Display Score
                    else if (postRoundTimer < 0)
                    {
                        postRoundTimer = 5.0f;
                        roundHasEnded = false;
                        if (roundCounterScreen.activeSelf)
                        {
                            roundCounterScreen.SetActive(false);

                        }
                    }
                }
                // Tick postRoundTimer

            }
        }
        else
        {
            // Player Died, and deathscreen hasn't shown up
            if (deathScreenTimer > 0)
            {
                deathScreenTimer -= Time.deltaTime;
            }
            // Player Died and we see death screen
            else if (deathScreenTimer < 0)
            {
                deathscreen.SetActive(true);
            }

        }

        // If we've reached the maxEnemeies for the area, the round can end
        if (!roundCanEnd && currentDangerLevel >= bounty)
        {
            roundCanEnd = true;
        }

        // Clean up existing ships array
        for (int i = 0; i < existingShips.Count; i++)
        {
            if (existingShips[i] == null)
            {
                existingShips.Remove(existingShips[i]);
                i--;
            }
        }


    }
    private void SpawnRewards(int rewardCount)
    {
        List<GameObject> rewardObjs = new();
        for (int i = 0; i < rewardCount; i++)
        {
            var index = Random.Range(0, rewardList.Count - 1);
            Debug.Log(index);
            var randReward = rewardList[index];
            reward.reward = randReward;
            var rewardContainer = new GameObject();
            rewardContainer.name = $"{randReward.name} - Box";
            rewardContainer.transform.SetPositionAndRotation(new Vector3(i * 100, 0), rewardContainer.transform.rotation);
            var crate = Instantiate(reward, rewardContainer.transform);
            crate.OnBoxDestroyed += () =>
            {
                Debug.Log("Box Destroyed");
                while (rewardObjs.Count > 0)
                {
                    rewardObjs[0].SetActive(false);
                    rewardObjs.RemoveAt(0);
                }
                roundHasEnded = false;
            };
            rewardObjs.Add(rewardContainer);

            var radarObj = Instantiate(radar, radarHolder);
            radarObj.matchingObject = rewardContainer;
        }
    }
    private void AddBounty(float bountyIncrease)
    {
        bounty += bountyIncrease;
        bountyText.text = $"${bounty * 1000}";
    }
    private AlienShipScript SpawnAlien(AlienShipScript shipToSpawn)
    {
        var randomNum = Random.Range(-10, 10);
        var alienShip = Instantiate(shipToSpawn);
        alienShip.transform.SetPositionAndRotation(transform.position + new Vector3(randomNum, 10), transform.rotation);
        
        var radarObj = Instantiate(radar, radarHolder);
        radarObj.matchingObject = alienShip.gameObject;
        spawnDelay = 1.0f;
        return alienShip;
    }
    public void ResetLevel()
    {
        for (int i = 0; i < existingShips.Count;)
        {
            Destroy(existingShips[i]);
            bounty = 0;
            AddBounty(5);
        }
    }
    public enum Team
    {
        Player = 0,
        Enemy = 1,
        Enemy2 = 2
    }
    private enum EncounterType
    {
        Endless,
        Basic,
        Debug
    }
    public enum GameMode
    {
        Endless = 0,
        Story = 1
    }
}
