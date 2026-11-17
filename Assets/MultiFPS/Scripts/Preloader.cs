using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiFPS.Preloader
{
    public static class Preloader
    {
#if UNITY_EDITOR
        public const string CityDevelopmentEnabledKey = "ROM.CityDevelopment.Enabled";
        public static System.Action EditorCityDevelopmentBootstrap;
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Preload()
        {
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(CityDevelopmentEnabledKey, false) &&
                SceneManager.GetActiveScene().path == "Assets/Scenes/Dungeon_Isla_del_Sol.unity")
            {
                if (EditorCityDevelopmentBootstrap != null) EditorCityDevelopmentBootstrap();
                else
                {
                    Debug.LogError("CIUDAD_DESARROLLO: falta el coordinador del Editor; se canceló el ensayo.");
                    UnityEditor.EditorApplication.delayCall += () => UnityEditor.EditorApplication.isPlaying = false;
                }
                return;
            }
#endif
            var index = SceneManager.GetActiveScene().buildIndex;

            if (index != 0) SceneManager.LoadScene(0);
        }
    }
}
