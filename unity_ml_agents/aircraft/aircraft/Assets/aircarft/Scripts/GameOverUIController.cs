using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Aicraft
{
    public class GameOverUIController : MonoBehaviour
    {
        public TextMeshProUGUI PlaceText;

        private RaceManager raceManager;

        private void Awake()
        {
            raceManager = FindObjectOfType<RaceManager>();
        }

        private void OnEnable()
        {
            if(GameManager.Instance != null && GameManager.Instance.GameState == GameState.GameOver)
            {
                //gets the place and updates the text
                string place = raceManager.GetAgentPlace(raceManager.FollowAgent);
                this.PlaceText.text = place + "place";
            }
        }

        public void MainMenuButtonClicked()
        {
            GameManager.Instance.LoadLevel("MainMenu", GameState.MainMenu);
        }
    }
}
