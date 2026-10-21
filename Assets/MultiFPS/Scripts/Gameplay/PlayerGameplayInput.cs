using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiFPS.UI;
using UnityEngine.InputSystem;

namespace MultiFPS.Gameplay
{
    [DisallowMultipleComponent]
    /// <summary>
    /// This class is always instantiated on scene once, and will manage player input, so we dont
    /// have to check on every player instance separately if it belongs to us, we can just apply input
    /// read from this script to one spawned player prefab that is ours
    /// </summary>
    public class PlayerGameplayInput : MonoBehaviour
    {
        public static PlayerGameplayInput Instance { private set; get; }
        private CharacterInstance _myCharIntance;
        private CharacterMotor _motor;

        [SerializeField] InputSystem _inputSystem = InputSystem.NewInputSystem;

        MultiFPSInputs _input;

        Vector2 _lookInput;
        Vector2 _movementInput;

        public enum InputSystem
        {
            Legacy,
            NewInputSystem
        }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                RiftMMOController.Ensure(gameObject);
            }
        }

        void Update()
        {
            if(_myCharIntance)RiftMMOController.Ensure(gameObject).ReadCharacter(_myCharIntance);
        }

        public void AssignCharacterToBeControlledByPlayer(CharacterInstance character)
        {
            _myCharIntance = character;
            _motor = character.GetComponent<CharacterMotor>();
        }

        #region new input system implementation
        private void OnEnable()
        {
            

            if (_inputSystem == InputSystem.NewInputSystem)
            {
                if (_input == null) 
                    _input = new MultiFPSInputs();

                _input.Enable();
            }
        }
        private void OnDisable()
        {
            if (_inputSystem == InputSystem.NewInputSystem)
            {
                _input.Disable();
            }
        }
        #endregion
    }
}