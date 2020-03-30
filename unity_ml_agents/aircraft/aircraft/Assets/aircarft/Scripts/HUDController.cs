using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aicraft
{
    public class HUDController : MonoBehaviour
    {
        public TextMeshProUGUI PlaceText;
        public TextMeshProUGUI TimeText;
        public TextMeshProUGUI LapText;

        public Image checkPointIcon;
        public Image checkPointArrow;

        public float IndicatorLimit = .7f;

        /// <summary>
        /// The agent this hud shows info for
        /// </summary>
        public AircraftAgent FollowAgent { get; set; }

        private RaceManager raceManager;

        private void Awake()
        {
            raceManager = FindObjectOfType<RaceManager>();
        }

        private void Update()
        {
            if (FollowAgent != null)
            {
                UpdatePlaceText();
                UpdateTimeText();
                UpdateLapText();
                UpdateArrow();
            }
        }

        private void UpdatePlaceText()
        {
            string place = raceManager.GetAgentPlace(FollowAgent);
            PlaceText.text = place;
        }

        private void UpdateTimeText()
        {
            float time = raceManager.GetAgentTime(FollowAgent);
            TimeText.text = "Time" + time.ToString("0.0");
        }

        private void UpdateLapText()
        {
            int lap = raceManager.GetAgentLap(FollowAgent);
            LapText.text = "Lap" + lap + "/" + raceManager.numLaps;
        }

        private void UpdateArrow()
        {
            //find the checkpoint within the viewport
            Transform nextCheckpoint = raceManager.GetAgentNextCheckpoint(FollowAgent);
            Vector3 viewportPoint = raceManager.ActiveCamera.WorldToViewportPoint(nextCheckpoint.position);
            bool behindCamera = viewportPoint.z < 0f;
            viewportPoint.z = 0f;

            //do calculations
            Vector3 viewportCenter = new Vector3(.5f, .5f, 0f);
            Vector3 fromCenter = viewportPoint - viewportCenter;
            float halfLimit = IndicatorLimit / 2f;
            bool showArrow = false;

            if(behindCamera)
            {
                //limit distance from center
                //(Viewport point is flipped when object is behind camera)
                fromCenter = -fromCenter.normalized * halfLimit;
                showArrow = true;
            }

            else
            {
                if (fromCenter.magnitude > halfLimit)
                {
                    fromCenter = fromCenter.normalized * halfLimit;
                    showArrow = true;
                }
            }

            //update the checkpoint arrow
            checkPointArrow.gameObject.SetActive(showArrow);
            checkPointArrow.rectTransform.rotation = Quaternion.FromToRotation(Vector3.up, fromCenter);
            checkPointIcon.rectTransform.position = raceManager.ActiveCamera.ViewportToScreenPoint(fromCenter + viewportCenter); 
        }
    }
}
