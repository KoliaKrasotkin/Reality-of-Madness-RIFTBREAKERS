using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
 [CreateAssetMenu(menuName="ROM/Classes/Paladin prototype")]
 public sealed class PaladinPrototypeSettings:ScriptableObject {
  public const int Skin=2;public const string ClassKey="paladin-prototype";
  [Tooltip("Visible skin name; never used as a character save ID.")]public string CharacterName="Aldren";
  [TextArea]public string Description="Prototipo jugable: locomoci\u00f3n y un ataque cuerpo a cuerpo. Kit inicial de Fe: Golpe Sagrado, Luz Sanadora, Defensa Radiante y Rezar. Balance provisional; talentos pendientes.";
  public GameObject PlayerPrefab,VisualPrefab;public SpeciesDefinition Species;
  public AutoAttackProfile AutoAttack=new AutoAttackProfile{ClassKey=ClassKey,DisplayName="Golpe de espada (prototipo)",Damage=18,Interval=1.8f,Range=2.8f,Arc=100,Windup=.32f,ResourceGain=0,CriticalChance=.05f};
  [Header("Faith: provisional balance, server-authoritative")]
  [Min(0),Tooltip("Faith on first entry in a server session. Reconnect restores server memory instead.")]public float InitialFaith=100;
  [Min(0),Tooltip("Faith regenerated per alive second; no offline regeneration or death refill.")]public float FaithRegenerationPerSecond=5;
  static PaladinPrototypeSettings cached;public static PaladinPrototypeSettings Load{get{if(!cached)cached=Resources.Load<PaladinPrototypeSettings>("PaladinPrototypeSettings");return cached;}}
  public static bool Available=>Load&&Load.PlayerPrefab&&Load.Species;
 }
}
