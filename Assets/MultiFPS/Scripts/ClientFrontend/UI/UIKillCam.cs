using MultiFPS.Gameplay;
using MultiFPS;
using UnityEngine;
using MultiFPS.Gameplay.Gamemodes;
using MultiFPS.UI.HUD;

[DisallowMultipleComponent]
public class UIKillCam : CharacterHud
{
    CharacterInstance _characterInstance;
    Health _killer;
    Vector3 _deathCameraDirection;

    protected override void Awake()
    {
        base.Awake();
        enabled = false;
    }

    protected override void AssignObservedCharacterForUI(CharacterInstance characterInstanceToAssignForUI)
    {
        characterInstanceToAssignForUI.Health.Client_OnHealthDepleted += OnCharacterDied;

        _characterInstance = characterInstanceToAssignForUI;

        enabled = true;
    }

    protected override void DeassignCurrentObservedCharacterFromUI(CharacterInstance characterToDeassign)
    {
        characterToDeassign.Health.Client_OnHealthDepleted -= OnCharacterDied;

        enabled = false;
    }

    private void OnCharacterDied(CharacterPart damagedPart, byte attackerID)
    {
        GameplayCamera._instance.SetFovToDefault();

        _killer = GameSync.Singleton.Healths.GetObj(attackerID);

        if (_killer)
        {
            _deathCameraDirection = _killer.netId != _characterInstance.Health.netId ?
            (_characterInstance.FPPLook.transform.position - _killer.GetPositionToAttack()).normalized :
            -_characterInstance.FPPLook.forward;

            if (_killer && (attackerID != _characterInstance.Health.DNID)) //dont show this message in case of suicide
                GameManager.GameEvent_GamemodeEvent_Message?.Invoke("You were killed by " + _killer.CharacterName, RoomSetup.Properties.P_RespawnCooldown);
        }
    }

    // Update is called once per frame
    void Update()
    {
        #region killcam
        if (_characterInstance.Health.CurrentHealth <= 0)
        {
            if (_killer)
                _characterInstance.FPPLook.rotation = Quaternion.Lerp(_characterInstance.FPPLook.rotation, Quaternion.LookRotation(_killer.GetPositionToAttack() - _characterInstance.FPPLook.position), 10f * Time.deltaTime);

            if (!_characterInstance.ObjectForDeathCameraToFollow) 
                return;

            Vector3 castPosition = _characterInstance.ObjectForDeathCameraToFollow.position + Vector3.up * 0.1f;

            float length;
            if (Physics.Raycast(castPosition, _deathCameraDirection, out RaycastHit hit, 5f, GameManager.environmentLayer))
            {
                length = Mathf.Max(0, Vector3.Distance(hit.point, castPosition) - 0.5f);
            }
            else
                length = 5f;

            _characterInstance.FPPLook.transform.position = castPosition + _deathCameraDirection * length + transform.up * 0.2f;
            return;
        }
        #endregion
    }
}
