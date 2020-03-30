using Barracuda;
using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aicraft
{
    public class RaceManager : MonoBehaviour
    {
        [Tooltip("The number of laps in the game")]
        public int numLaps = 2;

        [Tooltip("Bonus seconds to give upon reaching checkpoint")]
        public float CheckpointBonusTime = 15f;

        [Serializable]
        public struct DifficultyModel
        {
            public GameDifficulty difficulty;
            public NNModel model;
        }

        public List<DifficultyModel> difficultyModels;

        /// <summary>
        /// the agent being followed by the camera
        /// </summary>
        public AircraftAgent FollowAgent { get; private set; }

        public Camera ActiveCamera { get; private set; }

        private CinemachineVirtualCamera VirtualCamera;
        private CountdownUIController countdownUI;
        private PauseMenuController pauseMenu;
        private HUDController HUD;
        private GameOverUIController gameOverUI;
        private AircraftArea aircraftArea;
        private AircraftPlayer aircraftPlayer;
        private List<AircraftAgent> sortedAircraftAgents;

        //pause logic
        private float lastResumeTime = 0f;
        private float previouslyelapsedTime = 0f;

        private float lastPlaceupdate = 0f;
        private Dictionary<AircraftAgent, AircraftStatus> aircraftStatuses;

        private class AircraftStatus
        {
            public int checkpointIndex = 0;
            public int lap = 0;
            public int place = 0;
            public float timeRemaining = 0f;
        }

        public float RaceTime
        {
            get
            {
                if(GameManager.Instance.GameState == GameState.Playing)
                {
                    return previouslyelapsedTime + Time.time - lastResumeTime;
                }
                else if(GameManager.Instance.GameState == GameState.Paused)
                {
                    return previouslyelapsedTime;
                }
                else
                {
                    return 0f;
                }
            }
        }

        /// <summary>
        /// gets the agents next checkpoint
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        public Transform GetAgentNextCheckpoint(AircraftAgent agent)
        {
            return aircraftArea.Checkpoints[aircraftStatuses[agent].checkpointIndex].transform;
        }

        public int GetAgentLap(AircraftAgent agent)
        {
            return aircraftStatuses[agent].lap;
        }

        public string GetAgentPlace(AircraftAgent agent)
        {
            int place =  aircraftStatuses[agent].place;
            if (place <= 0)
            {
                return string.Empty;
            }

            if (place >= 11 && place <= 13) return place.ToString() + "th";
            else
            {
                switch (place % 10)
                {
                    case 1:
                        return place.ToString() + "st";
                    case 2:
                        return place.ToString() + "nd";
                    case 3:
                        return place.ToString() + "rd";
                    default:
                        return place.ToString() + "th";
                }
            }
        }

        public float GetAgentTime(AircraftAgent agent)
        {
            return aircraftStatuses[agent].timeRemaining;
        }

        private void Awake()
        {
            HUD = FindObjectOfType<HUDController>();
            countdownUI = FindObjectOfType<CountdownUIController>();
            pauseMenu = FindObjectOfType<PauseMenuController>();
            gameOverUI = FindObjectOfType<GameOverUIController>();
            VirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            aircraftArea = FindObjectOfType<AircraftArea>();
            ActiveCamera = FindObjectOfType<Camera>();
        }

        /// <summary>
        /// Initial setup and start race
        /// </summary>
        private void Start()
        {
            GameManager.Instance.OnStateChange += OnStateChange;

            //Choose a default agent for camera to follow in case we cant find a player
            FollowAgent = aircraftArea.AircraftAgents[0];
            foreach(AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                agent.freezeAgent();
                if (agent.GetType() == typeof(AircraftPlayer))
                {
                    //Found the player , follow it
                    FollowAgent = agent;
                    aircraftPlayer = (AircraftPlayer)agent;
                    aircraftPlayer.pauseInput.performed += pauseInputPerformed;
                }
                else
                {
                    // set the difficulty
                    agent.GiveModel(GameManager.Instance.GameDifficulty.ToString(),
                        difficultyModels.Find(x => x.difficulty == GameManager.Instance.GameDifficulty).model);
                }
            }

            //Tell the camera and HUD what to follow
            Debug.Assert(VirtualCamera != null, "Virtual camera was not specified");
            VirtualCamera.Follow = FollowAgent.transform;
            VirtualCamera.LookAt = FollowAgent.transform;
            HUD.FollowAgent = FollowAgent;

            //hide UI
            HUD.gameObject.SetActive(false);
            pauseMenu.gameObject.SetActive(false);
            countdownUI.gameObject.SetActive(false);
            gameOverUI.gameObject.SetActive(false);

            //start the race
            StartCoroutine(StartRace());
        }

        /// <summary>
        /// Start the countdown at the beginning of the race
        /// </summary>
        /// <returns>yield return</returns>
        private IEnumerator StartRace()
        {
            //Show countdown
            countdownUI.gameObject.SetActive(true);
            yield return countdownUI.StartCountdown();

            //Initialize agent status tracking
            aircraftStatuses = new Dictionary<AircraftAgent, AircraftStatus>();
            foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
            {
                AircraftStatus status = new AircraftStatus();
                status.lap = 1;
                status.timeRemaining = CheckpointBonusTime;
                aircraftStatuses.Add(agent, status);
            }

            //begin playing
            GameManager.Instance.GameState = GameState.Playing;
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        /// <param name="obj">The callback context</param>
        private void pauseInputPerformed(InputAction.CallbackContext obj)
        {
            if(GameManager.Instance.GameState == GameState.Playing)
            {
                GameManager.Instance.GameState = GameState.Paused;
                pauseMenu.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// React to state changes
        /// </summary>
        private void OnStateChange()
        {
            if(GameManager.Instance.GameState == GameState.Playing)
            {
                //Start/resume game time, show the hud, thaw the agents
                lastResumeTime = Time.time;
                HUD.gameObject.SetActive(true);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.thawAgent();
            }
            else if (GameManager.Instance.GameState == GameState.Paused)
            {
                previouslyelapsedTime += Time.time - lastResumeTime;
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.freezeAgent();
            }
            else if (GameManager.Instance.GameState == GameState.GameOver)
            {
                //pause game time, hide hud, freeze the agents
                previouslyelapsedTime += Time.time - lastResumeTime;
                HUD.gameObject.SetActive(false);
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents) agent.freezeAgent();

                //Show game over screen
                gameOverUI.gameObject.SetActive(true);
            }
            else
            {
                //reset time
                lastResumeTime = 0f;
                previouslyelapsedTime = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.GameState == GameState.Playing) 
            {
                //update the place list every half second
                if(lastPlaceupdate + 0.5f < Time.fixedTime)
                {
                    lastPlaceupdate = Time.fixedTime;
                    //Get a copy of the list of agents for sorting
                    if (sortedAircraftAgents == null)
                    {
                        //get a copy of the list of agents for sorting
                        sortedAircraftAgents = new List<AircraftAgent>(aircraftArea.AircraftAgents);
                    }

                    //recalculate race places
                    sortedAircraftAgents.Sort((a, b) => PlaceComparer(a, b));
                    for (int i = 0; i < sortedAircraftAgents.Count; i++)
                    {
                        aircraftStatuses[sortedAircraftAgents[i]].place = i + 1;
                    }
                }

                //update agent statuses
                foreach (AircraftAgent agent in aircraftArea.AircraftAgents)
                {
                    AircraftStatus status = aircraftStatuses[agent];

                    //update  agent lap
                    if (status.checkpointIndex != agent.NextCheckpointIndex)
                    {
                        status.checkpointIndex = agent.NextCheckpointIndex;
                        status.timeRemaining = CheckpointBonusTime;

                        if (status.checkpointIndex == 0)
                        {
                            status.lap++;
                            if (agent == FollowAgent && status.lap > numLaps)
                            {
                                GameManager.Instance.GameState = GameState.GameOver;
                            }
                        }
                    }

                    //update agent time remaining
                    status.timeRemaining = Mathf.Max(0f, status.timeRemaining - Time.fixedDeltaTime);
                    if (status.timeRemaining == 0f)
                    {
                        aircraftArea.ResetAgentPosition(agent);
                        status.timeRemaining = CheckpointBonusTime;
                    }
                }
            }
        }

        /// <summary>
        /// compares the race place
        /// </summary>
        /// <param name="a">an agent</param>
        /// <param name="b">another agent</param>
        /// <returns>-1 if b ahead of a, 0 if equal, 1 if a ahead of b</returns>
        private int PlaceComparer(AircraftAgent a, AircraftAgent b)
        {
            AircraftStatus statusA = aircraftStatuses[a];
            AircraftStatus statusB = aircraftStatuses[b];
            int checkpointA = statusA.checkpointIndex + (statusA.lap - 1) * aircraftArea.Checkpoints.Count;
            int checkpointB = statusB.checkpointIndex + (statusB.lap - 1) * aircraftArea.Checkpoints.Count;
            if (checkpointA == checkpointB)
            {
                //compare distances
                Vector3 nextCheckpointPosition = GetAgentNextCheckpoint(a).position;
                int compare = Vector3.Distance(a.transform.position, nextCheckpointPosition)
                    .CompareTo(Vector3.Distance(b.transform.position, nextCheckpointPosition));
                return compare;
            }
            else
            {
                //compare no. of checkpoints
                int compare = -1 * checkpointA.CompareTo(checkpointB);
                return compare;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStateChange -= OnStateChange;
            if (aircraftPlayer != null) aircraftPlayer.pauseInput.performed -= pauseInputPerformed;
        }
    }
    
}
