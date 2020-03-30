using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Aicraft
{
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("The list of levels (scene names) that can be loaded")]
        public List<string> levels;

        public TMP_Dropdown levelDropdown;

        public TMP_Dropdown difficultyDropdown;

        private string selectedLevel;
        private GameDifficulty selectedDifficulty;

        /// <summary>
        /// Automatically fill dropdown lists
        /// </summary>
        private void Start()
        {
            Debug.Assert(levels.Count > 0, "No Levels Available");
            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(levels);
            selectedLevel = levels[0];

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(Enum.GetNames(typeof(GameDifficulty)).ToList());
            selectedDifficulty = GameDifficulty.Normal;
        }

        public void SetLevel(int levelIndex)
        {
            selectedLevel = levels[levelIndex];
        }

        public void SetDifficulty(int difficultyIndex)
        {
            selectedDifficulty = (GameDifficulty)difficultyIndex;
        }

        /// <summary>
        /// Start the chosen level
        /// </summary>
        public void StartBttonClicked()
        {
            GameManager.Instance.GameDifficulty = selectedDifficulty;

            GameManager.Instance.LoadLevel(selectedLevel, GameState.Preparing);
        }


        /// <summary>
        /// quit the game
        /// </summary>
        public void QuitButtonClicked()
        {
            UnityEditor.EditorApplication.isPlaying = false;
            //Application.Quit(); 
        }

    }
}
