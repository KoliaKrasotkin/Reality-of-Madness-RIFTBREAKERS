using MultiFPS.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.UI.HUD
{
    /// <summary>
    /// Base class for every hud script that will display information about player
    /// </summary>
    public class CharacterHud : MonoBehaviour
    {
        protected CharacterInstance _myObservedCharacter { private set; get; }
        protected CharacterInstance _myOwnedCharacter { private set; get; }
        protected virtual void Awake()
        {
            ClientFrontend.Event_OnObservedCharacterSet += OnNewCharacterObserverd;
            ClientFrontend.Event_OnOwnedCharacterSet += OnOwnedCharacterSet;
        }

        protected virtual void OnDestroy()
        {
            ClientFrontend.Event_OnObservedCharacterSet -= OnNewCharacterObserverd;
            ClientFrontend.Event_OnOwnedCharacterSet -= OnOwnedCharacterSet;
        }

        #region OWNED character managament
        private void OnOwnedCharacterSet(CharacterInstance characterInstance)
        {
            //desub from previously observed character
            if (_myOwnedCharacter)
                DeassignCurrentlyOwnedCharacterFromUI(_myOwnedCharacter);

            if (characterInstance)
            {
                _myOwnedCharacter = characterInstance;
                AssignOwnedCharacterForUI(_myOwnedCharacter);
            }
        }
        protected virtual void AssignOwnedCharacterForUI(CharacterInstance characterInstanceToAssignForUI) { }

        protected virtual void DeassignCurrentlyOwnedCharacterFromUI(CharacterInstance characterToDeassign) { }
        #endregion

        #region OBSERVED character managament
        private void OnNewCharacterObserverd(CharacterInstance charInstance)
        {
            
            if (_myObservedCharacter)
                DeassignCurrentObservedCharacterFromUI(_myObservedCharacter);

            if (charInstance)
            {
                _myObservedCharacter = charInstance;
                AssignObservedCharacterForUI(_myObservedCharacter);
            }
        }
        protected virtual void AssignObservedCharacterForUI(CharacterInstance characterInstanceToAssignForUI) { }

        protected virtual void DeassignCurrentObservedCharacterFromUI(CharacterInstance characterToDeassign) { }
        #endregion
    }
}