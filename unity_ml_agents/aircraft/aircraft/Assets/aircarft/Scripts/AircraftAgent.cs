using MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aicraft
{
    public class AircraftAgent : Agent
    {
        [Header("Movement Parameters")]
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;

        [Header("Explosion Stuff")]
        [Tooltip("The aircraft mesh that will dissappear from the explosion")]
        public GameObject meshobject;
        public GameObject explosionEffect;

        [Header("Training Parameters")]
        public int stepTimeout = 300;

        public int NextCheckpointIndex { get; set; }

        //Components to keep track of
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;
        private RayPerception3D rayPerception;

        //when the next step timeout will be during training
        private float nextStepTimeout;

        private bool frozen = false;

        //Controls
        private float pitchChange = 0f;
        private float smoothPitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();
            rayPerception = GetComponent<RayPerception3D>();

            //override the max step set in the inspector
            //max 5000 steps in training .. infinity when not
            agentParameters.maxStep = area.trainingMode ? 5000 : 0;
            
        }

        /// <summary>
        /// Read action inputs from vectorActions
        /// </summary>
        /// <param name="vectorAction">The chosen action</param>
        /// <param name="textAction">The chosen text action(unused)</param>
        public override void AgentAction(float[] vectorAction, string textAction)
        {
            //Read values for pitch and yaw
            pitchChange = vectorAction[0]; //up or none
            if (pitchChange == 2) pitchChange = -1f; //down
            yawChange = vectorAction[1]; //right or none
            if (yawChange == 2) yawChange = -1f; //left

            //Read value of boost and enable/disable trail renderer
            boost = vectorAction[2] == 1;
            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;

            if (frozen) return;

            ProcessMovement();

            if(area.trainingMode)
            {
                //small negetive reward every step
                AddReward(-1f / agentParameters.maxStep);

                //make sure we don't run out of time
                if (GetStepCount() > nextStepTimeout)
                {
                    AddReward(-0.5f);
                    Done();
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                if(localCheckpointDir.magnitude < area.AircraftAcademy.resetParameters["checkpoint_radius"])
                {
                    GotCheckpoint();
                }
            }

        }

        /// <summary>
        /// observations made by agent to make decisions
        /// </summary>
        public override void CollectObservations()
        {
            //observe aircraft velocity (1vector = 3values)
            AddVectorObs(transform.InverseTransformDirection(rigidbody.velocity));

            //where the next checkpoints is (1vector = 3values)
            AddVectorObs(VectorToNextCheckpoint());

            //orientation of the next checkpoint
            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
            AddVectorObs(transform.InverseTransformDirection(nextCheckpointForward));

            //observe ray perception results
            string[] detectableObjects = { "Untagged", "checkpoint" };

            //look ahead and upward
            //12 values
            AddVectorObs(rayPerception.Perceive(
                    rayDistance: 250f,
                    rayAngles: new float[] { 60f, 90f, 120f },
                    detectableObjects: detectableObjects,
                    startOffset: 0f,
                    endOffset: 75f));

            //look center and at several angles
            //28 values
            AddVectorObs(rayPerception.Perceive(
                    rayDistance: 250f,
                    rayAngles: new float[] { 60f, 70f, 80f, 90f, 100f, 110f, 120f },
                    detectableObjects: detectableObjects,
                    startOffset: 0f,
                    endOffset: 0f));

            //look ahead and downward
            //12 values
            AddVectorObs(rayPerception.Perceive(
                    rayDistance: 250f,
                    rayAngles: new float[] { 60f, 90f, 120f },
                    detectableObjects: detectableObjects,
                    startOffset: 0f,
                    endOffset: -75f));

            //total observations 3+3+3+12+28+12 = 61
        }

        public override void AgentReset()
        {
            //Resets the velocity, position and orientation
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent: this, randomize: area.trainingMode);

            //update the timeout if training
            if (area.trainingMode) nextStepTimeout = GetStepCount() + stepTimeout;
        }

        /// <summary>
        /// prevent the agent from taking actions
        /// </summary>
        public void freezeAgent()
        {
            Debug.Assert(area.trainingMode == false, "not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        /// <summary>
        /// resume agent actions
        /// </summary>
        public void thawAgent()
        {
            Debug.Assert(area.trainingMode == false, "not supported in training");
            frozen = false;
            rigidbody.WakeUp();
        }

        /// <summary>
        /// called when agent flies through the correct checkpoint
        /// </summary>
        private void GotCheckpoint()
        {
            // Next checpoint count update
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

            if (area.trainingMode)
            {
                AddReward(0.5f);
                nextStepTimeout = GetStepCount() + stepTimeout;
            }
        }

        /// <summary>
        /// gets a vector to the next checkpoint the agent needs to fly through
        /// </summary>
        /// <returns>A local space vector</returns>
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        //Calculate and apply movement
        private void ProcessMovement()
        {
            //calculate boost
            float boostModifier = boost ? boostMultiplier : 1f;

            //apply forward thrust
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);

            //get the current rotation
            Vector3 curRot = transform.rotation.eulerAngles;

            //calculate the roll angle(-180 and 180)
            float rollAngle = curRot.z > 180f ? curRot.z - 360 : curRot.z;
            if (yawChange == 0f)
            {
                // not turning; smoothly roll back towards the center
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                //turning;roll in opposite direction of turn
                rollChange = -yawChange;
            }

            //calculate smooth deltas
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            //calculate new pitch, yaw, and roll. clamp pitch and roll
            float pitch = Clampangle(curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed, -maxPitchAngle, maxPitchAngle);
            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
            float roll = Clampangle(curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed, -maxRollAngle, maxRollAngle);

            //set the new rotation
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);

        }

        private static float Clampangle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360f + angle;
            if (angle > 180f) return Mathf.Max(angle, 360f + from);
            return Mathf.Min(angle, to);
        }

        /// <summary>
        /// react to entering a trigger
        /// </summary>
        /// <param name="other">the collider entered</param>
        private void OnTriggerEnter(Collider other)
        {
            if(other.transform.CompareTag("checkpoint") && other.gameObject == area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }

        /// <summary>
        /// react to collision
        /// </summary>
        /// <param name="collision">collision info</param>
        private void OnCollisionEnter(Collision collision)
        {
            if(!collision.transform.CompareTag("agent"))
            {
                if (area.trainingMode)
                {
                    AddReward(-1f);
                    Done();
                    return;
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }

        /// <summary>
        /// resets the aircraft to most recent completed checkpoint
        /// </summary>
        /// <returns>yield return</returns>
        private IEnumerator ExplosionReset()
        {
            freezeAgent();

            //disable aircraft mesh object, enable explosion
            meshobject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            //disable explosion, reenable aircraft mesh
            meshobject.SetActive(true);
            explosionEffect.SetActive(false);
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            thawAgent();
        }
    }
}
