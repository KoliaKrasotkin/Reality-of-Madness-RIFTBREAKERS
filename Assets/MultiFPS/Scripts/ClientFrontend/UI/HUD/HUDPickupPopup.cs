using MultiFPS.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.UI.HUD {
    public class HUDPickupPopup : CharacterHud
    {
        [SerializeField] GameObject _popupPrefab;
        [SerializeField] int _maxPopupsAtOnce = 8;
        [SerializeField] Transform _gridParent;

        HUDPickupPopupElement[] _popups;

        int nextPopupIDtoUse = 0;

        protected override void AssignObservedCharacterForUI(CharacterInstance _characterInstanceToAssignForUI)
        {
            _characterInstanceToAssignForUI.Client_OnPickedupObject += PickedupObject;
            _characterInstanceToAssignForUI.Health.Client_OnHealthDepleted += ClearOnDeath;
        }
        protected override void DeassignCurrentObservedCharacterFromUI(CharacterInstance _characterToDeassign)
        {
            _characterToDeassign.Client_OnPickedupObject -= PickedupObject;
            _characterToDeassign.Health.Client_OnHealthDepleted -= ClearOnDeath;Clear();
        }


        protected override void Awake()
        {
            base.Awake();

            _popups = new HUDPickupPopupElement[_maxPopupsAtOnce];
            for (int i = 0; i < _maxPopupsAtOnce-1; i++)
            {
                GameObject popup = Instantiate(_popupPrefab, _gridParent);
                _popups.SetValue(popup.GetComponent<HUDPickupPopupElement>(), i);
            }

            _popups.SetValue(_popupPrefab.GetComponent<HUDPickupPopupElement>(), _maxPopupsAtOnce - 1);
        }

        void ClearOnDeath(CharacterPart part,byte attacker)=>Clear();
        void Clear(){if(_popups!=null)foreach(var popup in _popups)if(popup)popup.gameObject.SetActive(false);}
        void LateUpdate(){if(!_myObservedCharacter||!_myObservedCharacter.Health||_myObservedCharacter.Health.CurrentHealth<=0)Clear();}
        void PickedupObject(string msg)
        {
            if(!_myObservedCharacter||!_myObservedCharacter.Health||_myObservedCharacter.Health.CurrentHealth<=0)return;
            _popups[nextPopupIDtoUse].Set(msg);

            nextPopupIDtoUse++;
            if (nextPopupIDtoUse >= _popups.Length)
                nextPopupIDtoUse = 0;
        }
    }
}