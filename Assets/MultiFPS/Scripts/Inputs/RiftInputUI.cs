using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace MultiFPS
{
    public static class RiftInputUI
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode)=>Ensure();
        static float nextCheck;
        public static void Tick(){if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.5f;Ensure();}
        // The template used StandaloneInputModule (legacy input) for menus.
        // Use the same Input System for pointers, buttons and rebinding instead.
        public static void Ensure()
        {
            var events=EventSystem.current;if(!events)events=Object.FindFirstObjectByType<EventSystem>();if(!events)return;events.enabled=true;
            // Keep the selected scene UI owner; disable duplicate dispatchers, never their objects or UI.
            foreach(var other in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))if(other!=events)other.enabled=false;
            var module=events.GetComponent<InputSystemUIInputModule>();
            foreach(var old in events.GetComponents<BaseInputModule>())if(old!=module)old.enabled=false;
            if(!module)module=events.gameObject.AddComponent<InputSystemUIInputModule>();
            module.enabled=true;if(!module.actionsAsset)module.AssignDefaultActions();
        }
    }
}
