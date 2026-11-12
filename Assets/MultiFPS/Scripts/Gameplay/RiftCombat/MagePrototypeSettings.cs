using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
 [CreateAssetMenu(menuName="ROM/Classes/Mage prototype")]
 public sealed class MagePrototypeSettings:ScriptableObject {
  public const int Skin=3; public const string ClassKey="mage-prototype";
  public string CharacterName="Mago";
  [TextArea] public string Description="Mago de batalla. Movimiento, selección de objetivos y descarga arcana a distancia. Kit inicial de prueba.";
  public GameObject PlayerPrefab; public SpeciesDefinition Species;
  public AutoAttackProfile AutoAttack=new AutoAttackProfile{ClassKey=ClassKey,DisplayName="Descarga arcana",Damage=18,Interval=1.8f,Range=25,Arc=120,Windup=.32f,ResourceGain=0};
  static MagePrototypeSettings cached;
  public static MagePrototypeSettings Load {get{if(!cached)cached=Resources.Load<MagePrototypeSettings>("MagePrototypeSettings");return cached;}}
  public static bool Available=>Load&&Load.PlayerPrefab&&Load.Species;
 }
}
