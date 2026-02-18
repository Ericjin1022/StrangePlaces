using UnityEngine;
using UnityEngine.SceneManagement;

namespace StrangePlaces.DemoQuantumCollapse
{
    public sealed class ReturnToLevelSelect : MonoBehaviour
    {
        [SerializeField] private string levelSelectSceneName = "LevelSelect";
        [SerializeField] private KeyCode key = KeyCode.Escape;

        private void Update()
        {
            if (!Input.GetKeyDown(key))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(levelSelectSceneName))
            {
                return;
            }

            SceneManager.LoadScene(levelSelectSceneName);
        }
    }
}

