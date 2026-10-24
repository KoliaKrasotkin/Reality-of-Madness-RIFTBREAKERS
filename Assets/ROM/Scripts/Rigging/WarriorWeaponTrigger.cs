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
    private void Awake() { ConfigureCollider(); foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled=false; }

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
        if (drawn && RiftInitialCampWeaponZone.Contains(warrior.transform.position)) return false;
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

// Uses the authored safety volume and the existing Gori weapon-state API.
// No new colliders, animator parameters, gameplay safety rules or scene edits.
public sealed class RiftInitialCampWeaponZone : MonoBehaviour
{
    static ROM.Horde.RiftCheckpoint[] camps;
    readonly System.Collections.Generic.Dictionary<WarriorAvatar,bool> states = new System.Collections.Generic.Dictionary<WarriorAvatar,bool>();
    float next;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install() { UnityEngine.SceneManagement.SceneManager.sceneLoaded-=Loaded; UnityEngine.SceneManagement.SceneManager.sceneLoaded+=Loaded; }
    static void Loaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if(scene.name!="map_rom_horde")return;
        camps=System.Array.FindAll(Object.FindObjectsByType<ROM.Horde.RiftCheckpoint>(FindObjectsSortMode.None),c=>c.StartingCamp&&c.gameObject.scene==scene);
        if(camps.Length>0&&!camps[0].GetComponent<RiftInitialCampWeaponZone>())camps[0].gameObject.AddComponent<RiftInitialCampWeaponZone>();
    }
    public static bool Contains(Vector3 position) { if(camps==null)return false;foreach(var c in camps)if(c&&c.isActiveAndEnabled&&c.Contains(position))return true;return false; }
    void Update()
    {
        if(!NetworkServer.active||Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
        foreach(var warrior in Object.FindObjectsByType<WarriorAvatar>(FindObjectsSortMode.None))
        {
            if(!warrior.isServer||warrior.gameObject.scene!=gameObject.scene)continue;
            bool inside=Contains(warrior.transform.position);
            bool known=states.TryGetValue(warrior,out bool previous);
            if(!known||inside!=previous) { states[warrior]=inside; if(inside||known)warrior.SetWeaponDrawn(!inside); }
        }
        var dead=new System.Collections.Generic.List<WarriorAvatar>();foreach(var pair in states)if(!pair.Key)dead.Add(pair.Key);foreach(var value in dead)states.Remove(value);
    }
}
