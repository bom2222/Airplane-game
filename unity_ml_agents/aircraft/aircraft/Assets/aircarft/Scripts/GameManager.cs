using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aicraft
{
    public enum GameState
    {
        Default,
        MainMenu,
        Preparing,
        Playing,
        Paused,
        GameOver
    }

    public enum GameDifficulty
    {
        Normal,
        Hard
    }

    public delegate void OnStateChangeHandler();

    public class GameManager : MonoBehaviour
    {
        public event OnStateChangeHandler OnStateChange;

        private GameState gameState;

        public GameState GameState
        {
            get
            {
                return gameState;
            }

            set
            {
                gameState = value;
                if (OnStateChange != null) OnStateChange();
            }
        }

        /// <summary>
        /// the singleton game manager instance
        /// </summary>
        public GameDifficulty GameDifficulty { get; set; }

        public static GameManager Instance
        {
            get;private set;
        }


        /// <summary>
        /// Manage the singleton and set fullscreen resolution
        /// </summary>
        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void OnApplicationQuit()
        {
            Instance = null;
        }


        /// <summary>
        /// Loads a level and sets the Game State
        /// </summary>
        /// <param name="LevelName">The level to load</param>
        /// <param name="newState">The new game state</param>
        public void LoadLevel(string LevelName, GameState newState)
        {
            StartCoroutine(LoadLevelAsync(LevelName, newState));
        }

        private IEnumerator LoadLevelAsync(string LevelName, GameState newState)
        {
            //Load the new level
            AsyncOperation operation = SceneManager.LoadSceneAsync(LevelName);
            while(operation.isDone == false)
            {
                yield return null;
            }

            // Set the resolution
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);

            //Update the Game State
            GameState = newState;

        }
    }
}
