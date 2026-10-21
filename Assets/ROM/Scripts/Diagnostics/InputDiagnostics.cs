using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using MultiFPS;

namespace ROM.Diagnostics
{
    /// <summary>
    /// Throwaway diagnostic for the "keyboard works, mouse doesn't" bug under the new Input System.
    /// Self-installing: it spawns itself after the first scene load, so there is nothing to attach
    /// and no scene to dirty. Press F9 in play mode and read the console.
    /// Delete once the bug is understood.
    /// </summary>
    public class InputDiagnostics : MonoBehaviour
    {
        MultiFPSInputs _input;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var go = new GameObject("[ROM] InputDiagnostics");
            go.AddComponent<InputDiagnostics>();
            Object.DontDestroyOnLoad(go);
        }

        void OnEnable()
        {
            // Our own asset instance, enabled exactly like PlayerGameplayInput does it.
            _input = new MultiFPSInputs();
            _input.Enable();

            Debug.Log("[ROM] InputDiagnostics activo. Apretá F9 mientras movés el mouse y mantenés el click izquierdo.");
        }

        void OnDisable()
        {
            _input?.Disable();
        }

        void Update()
        {
            // Read the trigger key through BOTH backends: if one of them is dead, the other
            // still fires the dump instead of leaving us with a silent console.
            bool legacyF9 = Input.GetKeyDown(KeyCode.F9);
            bool newF9 = Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
            if (!legacyF9 && !newF9) return;

            var sb = new StringBuilder();
            sb.AppendLine("===== INPUT DIAGNOSTICS =====");
            sb.AppendLine($"trigger: legacy={legacyF9} newInputSystem={newF9}");

            // ── LEVEL 1: does the Input System see a mouse at all? ───────────────
            var mouse = Mouse.current;
            sb.AppendLine($"[1] Mouse.current        : {(mouse == null ? "NULL  <-- the Input System has no mouse" : "present")}");
            if (mouse != null)
            {
                sb.AppendLine($"    mouse.enabled        : {mouse.enabled}");
                sb.AppendLine($"    mouse.added          : {mouse.added}");
                sb.AppendLine($"    delta (raw device)   : {mouse.delta.ReadValue()}");
                sb.AppendLine($"    leftButton.isPressed : {mouse.leftButton.isPressed}");
                sb.AppendLine($"    position             : {mouse.position.ReadValue()}");
            }

            sb.AppendLine($"[1b] devices seen by the Input System ({InputSystem.devices.Count}):");
            foreach (var d in InputSystem.devices)
                sb.AppendLine($"     - {d.GetType().Name,-14} \"{d.displayName}\"  enabled={d.enabled} added={d.added}");

            var supported = InputSystem.settings.supportedDevices;
            sb.AppendLine($"[1c] settings.supportedDevices : {(supported.Count == 0 ? "empty (= all devices allowed, OK)" : string.Join(", ", supported))}");
            sb.AppendLine($"[1d] settings.updateMode       : {InputSystem.settings.updateMode}");

            // ── LEVEL 2: do the mouse-bound actions resolve to controls? ────────
            DumpAction(sb, "Look", _input.Player.Look);
            DumpAction(sb, "Fire1Hold", _input.Player.Fire1Hold);
            DumpAction(sb, "Fire2Hold", _input.Player.Fire2Hold);
            DumpAction(sb, "Movement (keyboard control)", _input.Player.Movement);

            // ── LEVEL 3: is the game's own gate letting input through? ──────────
            sb.AppendLine($"[3] UICursorManager.GamePlayInput() : {UICursorManager.GamePlayInput()}");
            sb.AppendLine($"    Cursor.lockState                : {Cursor.lockState}");
            sb.AppendLine($"    Cursor.visible                  : {Cursor.visible}");
            sb.AppendLine($"    Application.isFocused           : {Application.isFocused}");
            sb.AppendLine($"    UserSettings.MouseSensitivity   : {UserSettings.MouseSensitivity}");

            // ── LEVEL 4: legacy backend, same frame, for comparison ────────────
            sb.AppendLine($"[4] legacy Input.GetAxis(\"Mouse X\") : {Input.GetAxis("Mouse X")}");
            sb.AppendLine($"    legacy Input.GetMouseButton(0)   : {Input.GetMouseButton(0)}");

            sb.AppendLine("=============================");
            Debug.Log(sb.ToString());
        }

        static void DumpAction(StringBuilder sb, string label, InputAction action)
        {
            sb.AppendLine($"[2] action \"{label}\"");
            sb.AppendLine($"    enabled        : {action.enabled}");
            sb.AppendLine($"    controls bound : {action.controls.Count}{(action.controls.Count == 0 ? "   <-- resolved to NOTHING" : "")}");
            foreach (var c in action.controls)
                sb.AppendLine($"      -> {c.path}  (device enabled={c.device.enabled})");
            sb.AppendLine($"    activeControl  : {(action.activeControl == null ? "none" : action.activeControl.path)}");
            sb.AppendLine($"    ReadValueAsObject : {action.ReadValueAsObject()}");
        }
    }
}
