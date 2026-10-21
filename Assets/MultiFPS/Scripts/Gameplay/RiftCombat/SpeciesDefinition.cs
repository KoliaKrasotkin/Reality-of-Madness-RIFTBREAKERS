using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
[CreateAssetMenu(menuName="ROM/Combat/Species")]
public sealed class SpeciesDefinition:ScriptableObject {public string Id,DisplayName; [TextArea] public string Description; public int[] SkinIndices; public CombatClassDefinition[] Classes; public bool AllowsSkin(int id)=>SkinIndices!=null&&System.Array.IndexOf(SkinIndices,id)>=0;}
}
