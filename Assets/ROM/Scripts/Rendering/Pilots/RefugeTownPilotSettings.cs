using System;
using UnityEngine;
namespace ROM.Rendering {
 [CreateAssetMenu(menuName="Rift/World/Refuge town pilot")]
 public sealed class RefugeTownPilotSettings:ScriptableObject {
  public float PlazaRadius=12,RingRadius=22,RingWidth=5,RoadWidth=5.5f,OuterRoad=63;
  public Color PavingTint=new Color(.58f,.55f,.49f),CanopyTint=new Color(.43f,.22f,.12f);
  [Serializable]public sealed class Piece {public string kind="Asset",asset,label;public Vector3 position;public float height=1,maxWidth=8,yaw,width=5,depth=4;}
  public Piece[] Pieces=Array.Empty<Piece>();
 }
}
