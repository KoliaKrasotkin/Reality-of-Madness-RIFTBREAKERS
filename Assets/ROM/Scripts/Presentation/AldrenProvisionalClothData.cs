using UnityEngine;
namespace ROM.Presentation
{
    [CreateAssetMenu(menuName="ROM/Aldren/Provisional V7 Cloth data")]
    public sealed class AldrenProvisionalClothData:ScriptableObject
    {
        public string Revision="V7 + seam-pinned Cloth, 2026-10-30; provisional, visual defects accepted";
        public Mesh FullMesh,BodyMesh,GarmentMesh;
        public string[] BoneNames;
        public Vector3[] ParticleRestPositions;
        public float[] ParticleMaxDistances;
        public int SeamVertices;
        [Range(0,1)]public float Damping=.3f,BendingStiffness=.35f,StretchingStiffness=1,Friction=.5f;
        [Min(1)]public float SolverFrequency=120;
        public float WorldVelocityScale=.1f,WorldAccelerationScale=.1f;
        [Min(0)]public float ThighRadius=.10f,CalfRadius=.075f;
    }
}
