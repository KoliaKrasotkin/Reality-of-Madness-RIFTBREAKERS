using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace MultiFPS
{
    public static class RiftInputUI
    {
        // The template used StandaloneInputModule (legacy input) for menus.
        // Use the same Input System for pointers, buttons and rebinding instead.
        public static void Ensure()
        {
            var events=EventSystem.current;if(!events)return;
            var module=events.GetComponent<InputSystemUIInputModule>();
            foreach(var old in events.GetComponents<BaseInputModule>())if(old!=module)old.enabled=false;
            if(!module)module=events.gameObject.AddComponent<InputSystemUIInputModule>();
            module.enabled=true;if(!module.actionsAsset)module.AssignDefaultActions();
        }
    }
}
