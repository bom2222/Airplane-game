using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aicraft
{
    public class AircraftPlayer : AircraftAgent
    {
        [Header("Input Bindings")]
        public InputAction pitchInput;
        public InputAction yawInput;
        public InputAction boostInput;
        public InputAction pauseInput;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            pitchInput.Enable();
            yawInput.Enable();
            boostInput.Enable();
            pauseInput.Enable();
        }

        public override float[] Heuristic()
        {
            //pitch: 1=up 0=none -1=down
            float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());

            //yaw: 1=right 0=none -1=down
            float yawValue = Mathf.Round(yawInput.ReadValue<float>());

            //boost: 1=boost 0=noboost
            float boostValue = Mathf.Round(boostInput.ReadValue<float>());

            if (pitchValue == -1f)
            {
                pitchValue = 2f;
            }
            if (yawValue == -1f) yawValue = 2f;

            return new float[] { pitchValue, yawValue, boostValue };
        }

        private void OnDestroy()
        {
            pitchInput.Disable();
            yawInput.Disable();
            boostInput.Disable();
            pauseInput.Disable();
        }
    }
} 
