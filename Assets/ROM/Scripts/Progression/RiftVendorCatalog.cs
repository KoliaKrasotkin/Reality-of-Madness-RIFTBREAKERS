using System;using UnityEngine;using MultiFPS.Gameplay.RiftCombat;
namespace ROM.Progression {
public enum RiftVendorKind {LifePotion,MajorPotion,Axe}
[Serializable]public sealed class RiftVendorEntry {public string Id,Name;public RiftVendorKind Kind;[Min(1)]public int Price=25;public RiftLootItemDefinition Item;[Range(1,60)]public int Level=1;public bool Valid=>!string.IsNullOrEmpty(Id)&&Price>0&&(Kind!=RiftVendorKind.Axe||Item&&Item.Supported);public int Potion=>Kind==RiftVendorKind.LifePotion?0:1;}
[CreateAssetMenu(menuName="ROM/Vendor/Catalog")]
public sealed class RiftVendorCatalog:ScriptableObject {public bool Repairs=true;public RiftVendorEntry[] Entries;[Min(0)]public int AxeResaleBase=40;[Range(0,1)]public float PotionResale=.5f;public static RiftVendorCatalog Default=>Resources.Load<RiftVendorCatalog>("RiftVendorCatalog");public int SalePrice(RiftLootAxe item)=>item==null?0:Mathf.Clamp(Mathf.FloorToInt(AxeResaleBase*Mathf.Clamp(item.Multiplier,.01f,10)),0,1000000000);}
}
