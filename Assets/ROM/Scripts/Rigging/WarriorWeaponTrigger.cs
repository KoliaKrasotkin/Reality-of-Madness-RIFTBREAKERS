using Mirror;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class WarriorWeaponTrigger : MonoBehaviour
{
    public enum WeaponAction { Holster, Draw }

    [Tooltip("Holster guarda el arma. Draw la saca al entrar en este trigger.")]
    public WeaponAction Action = WeaponAction.Holster;

    private void Reset() => ConfigureCollider();
    private void Awake() => ConfigureCollider();

    private void ConfigureCollider()
    {
        // This project's collision matrix excludes characters from Ignore Raycast.
        gameObject.layer = (int)MultiFPS.GameLayers.trigger;
        GetComponent<BoxCollider>().isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other) => TryApply(other);

    // Also exposed for scripted interactions and editor validation.
    public bool TryApply(Collider other)
    {
        if (!other) return false;
        var warrior = other.GetComponentInParent<WarriorAvatar>();
        if (!warrior) return false;
        // Movement runs on the owning client. Forward its local trigger entry
        // through WarriorAvatar's Command; remote client replicas do nothing.
        if (NetworkClient.active && !NetworkServer.active && !warrior.isOwned) return false;
        bool drawn = Action == WeaponAction.Draw;
        // A character has several hitbox colliders. Repeated enters must not
        // restart drawing or cancel the same cast multiple times.
        if (warrior.RequestedWeaponDrawn == drawn) return false;
        warrior.SetWeaponDrawn(drawn);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider>();
        if (!box) return;
        Gizmos.color = Action == WeaponAction.Draw ? new Color(.2f, .9f, .5f) : new Color(1f, .65f, .15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
