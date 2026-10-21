using UnityEngine;
namespace MultiFPS.Gameplay {
    [CreateAssetMenu(menuName="ROM/Reglas de clases")]
    public sealed class RiftClassRules : ScriptableObject {
        public RiftClassDefinition[] Classes;
        public Material EffectMaterial;
        public float ExtraPlayerHealth=.65f,ExtraPlayerDamage=.12f,ParticipantRadius=175;
        public float RecoveryDelay=7,RecoveryPerSecond=.08f,AllyRadius=24;
        public RiftClassDefinition Find(int id)=>Classes!=null&&id>=0&&id<Classes.Length?Classes[id]:null;
        public float HealthScale(int players)=>1+Mathf.Clamp(players-1,0,3)*ExtraPlayerHealth;
        public float DamageScale(int players)=>1+Mathf.Clamp(players-1,0,3)*ExtraPlayerDamage;
        public static RiftClassRules Load()=>Resources.Load<RiftClassRules>("RiftClassRules");
    }
}
