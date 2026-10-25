using UnityEngine;
namespace ROM.Progression {
[CreateAssetMenu(menuName="Rift/Quests/Party kill credit")]
public sealed class RiftPartyQuestTuning:ScriptableObject {
 [Tooltip("Provisional metres from the corpse for party members without combat participation.")][Min(1)]public float NearbyRadius=40;
 [Tooltip("Maximum distance for participants in this enemy's current threat table.")][Min(1)]public float ParticipantRadius=120;
 public float Nearby=>Mathf.Clamp(float.IsFinite(NearbyRadius)?NearbyRadius:40,1,80);
 public float Participant=>Mathf.Clamp(float.IsFinite(ParticipantRadius)?ParticipantRadius:120,Nearby,120);
 static RiftPartyQuestTuning cached;public static RiftPartyQuestTuning Load{get{if(!cached)cached=Resources.Load<RiftPartyQuestTuning>("RiftPartyQuestTuning");if(!cached)cached=CreateInstance<RiftPartyQuestTuning>();return cached;}}
}}
