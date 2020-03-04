using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;

namespace Aicraft
{

    public class AircraftArea : MonoBehaviour
    {
        [Tooltip("The path that race will take place")]
        public CinemachineSmoothPath racePath;

        [Tooltip("The prefab to the checkpoint")]
        public GameObject checkpointPrefab;

        [Tooltip("The prefab to use for the start/end checkpoint")]
        public GameObject finishCheckpointPrefab;

        [Tooltip("If true enable training Mode")]
        public bool trainingMode;

        public List<AircraftAgent> AircraftAgents { get; private set; }
        public List<GameObject> Checkpoints { get; private set; }
        public AircraftAcademy AircraftAcademy { get; private set; }

        //Actions to perform when the script wakes up

        private void Awake()
        {
            //Find all aircraft agents in the area

            AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();
            Debug.Assert(AircraftAgents.Count > 0, "No aircraft agent found");

            AircraftAcademy = FindObjectOfType<AircraftAcademy>();
        }

        //Set up the area

        private void Start()
        {
            //Create checkpoints along the race path

            Debug.Assert(racePath != null, "Race path does not exist");
            Checkpoints = new List<GameObject>();
            int numCheckPoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);
            for (int i=0; i<numCheckPoints; i++)
            {
                //instantiate either a checkpoint or a finish line checkpoint
                GameObject Checkpoint;
                if (i == numCheckPoints - 1) Checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                else Checkpoint = Instantiate<GameObject>(checkpointPrefab);

                //Set the parent, position, and rotation
                Checkpoint.transform.SetParent(racePath.transform);
                Checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                Checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                //add checkpoint to list
                Checkpoints.Add(Checkpoint);
            }
        }

        //Resets the agent

        public void ResetAgentPosition(AircraftAgent agent, bool randomize = false)
        {
            if (randomize)
            {
                agent.NextCheckpointIndex = Random.Range(0, Checkpoints.Count);
            }

            int PreviousCheckpointIndex = agent.NextCheckpointIndex - 1;
            if (PreviousCheckpointIndex == -1) PreviousCheckpointIndex = Checkpoints.Count - 1;

            float startPosition = racePath.FromPathNativeUnits(PreviousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);

            //convert position in a race path to a position in 3d space
            Vector3 basePosition = racePath.EvaluatePosition(startPosition);

            //get the orientation at that position on the race path
            Quaternion orientation = racePath.EvaluateOrientation(startPosition);

            //calculate horizontal offset
            Vector3 positionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2f) * 10f;

            //set the aircraft position and rotation
            agent.transform.position = basePosition + orientation * positionOffset;
            agent.transform.rotation = orientation;

        }


    }
}
