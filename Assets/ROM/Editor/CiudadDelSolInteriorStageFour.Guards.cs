using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageTwo
    {
        [Serializable] sealed class GuardProposal
        {
            public string started,ended,sceneBefore,sceneAfter;
            public bool hypotheticalArchitecture=true,geometryPersisted=false,playerSimulation=false;
            public List<Vector3> draftPositions=new List<Vector3>();
            public List<GuardNavConflict> samples=new List<GuardNavConflict>();
        }
        [Serializable] sealed class GuardNavConflict
        {
            public int flight,side;
            public bool navFound,originalCapsuleClear,temporaryGuardCollision;
            public Vector3 navPosition;
            public string collisionObject;
        }

        // Hypothetical geometry, native collision evidence. Batch only; never saves.
        static void PreviewStageFourGuards(Report receipt)
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Propuesta solo en batch; nunca ensucia la sesión del usuario.");
            const string railPath="Assets/Standard Assets/Eternal Temple/Prefabs/Railings and Decors/Railing_Staircase_01.prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(railPath);
            if(!prefab||prefab.GetComponentsInChildren<Component>(true).Any(c=>!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)&&!(c is MeshCollider)&&!(c is LODGroup)))
                throw new InvalidOperationException("El prefab de baranda tiene componentes no previstos.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(City);
            var physics=scene.GetPhysicsScene();var agent=NavMesh.GetSettingsByID(0);
            var cfg=new Config{scene=scene,physics=physics,radius=agent.agentRadius,height=Mathf.Max(agent.agentHeight,receipt.warriorHeight),
                step=Mathf.Min(receipt.warriorStep,agent.agentClimb),slope=Mathf.Min(receipt.warriorSlope,agent.agentSlope),warriorSlope=receipt.warriorSlope,mask=receipt.mask};
            var proposal=new GuardProposal{started=DateTime.UtcNow.ToString("o"),sceneBefore=Hash(City)};
            var bases=new List<Station>();var filter=new NavMeshQueryFilter{agentTypeID=0,areaMask=NavMesh.AllAreas};
            for(int i=0;i<4;i++)foreach(int side in new[]{-1,1})
            {
                var q=V(45+side*2.3f,-17-i*2,-9+i*6);
                var sample=new GuardNavConflict{flight=i+1,side=side,navFound=NavMesh.SamplePosition(q,out var nav,.35f,filter)&&Mathf.Abs(nav.position.y-q.y)<=.35f};
                sample.navPosition=sample.navFound?nav.position:Vector3.zero;
                var basis=Probe(q,cfg,true,cfg.step);sample.originalCapsuleClear=basis.Clear;
                bases.Add(basis);proposal.samples.Add(sample);
            }
            GameObject temporary=null;
            try
            {
                temporary=new GameObject("ROM_Stage4_DraftGuards_NotSaved");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(temporary,scene);
                for(int i=0;i<4;i++)foreach(int side in new[]{-1,1})
                {
                    var rail=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                    rail.transform.SetParent(temporary.transform,true);
                    rail.transform.SetPositionAndRotation(Vector3.zero,prefab.transform.localRotation);
                    rail.transform.localScale=Vector3.Scale(prefab.transform.localScale,new Vector3(1,.5f,1));
                    var renderers=rail.GetComponentsInChildren<Renderer>(true);
                    if(renderers.Length==0)throw new InvalidOperationException("Baranda sin geometría visible.");
                    var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
                    rail.transform.position=V(45+side*2.55f,-16-i*2+.75f-bounds.max.y,-12+i*6);
                    proposal.draftPositions.Add(rail.transform.position);
                }
                Physics.SyncTransforms();
                for(int i=0;i<proposal.samples.Count;i++)
                {
                    var sample=proposal.samples[i];var basis=bases[i];
                    if(!sample.navFound||!sample.originalCapsuleClear)continue;
                    int count=physics.OverlapCapsule(basis.foot+Vector3.up*cfg.radius,basis.foot+Vector3.up*(cfg.height-cfg.radius),cfg.radius,Overlaps,cfg.mask,QueryTriggerInteraction.Ignore);
                    if(count==Overlaps.Length)throw new InvalidOperationException("Saturación de consulta de barandas temporales.");
                    for(int j=0;j<count;j++)if(Overlaps[j]&&Overlaps[j].transform.IsChildOf(temporary.transform))
                    {sample.temporaryGuardCollision=true;sample.collisionObject=Hierarchy(Overlaps[j].transform);break;}
                }
            }
            finally
            {
                if(temporary)UnityEngine.Object.DestroyImmediate(temporary);
                Physics.SyncTransforms();proposal.ended=DateTime.UtcNow.ToString("o");proposal.sceneAfter=Hash(City);
                File.WriteAllText(StageFourOutput+"/PropuestaBarandas_NavMesh.json",JsonUtility.ToJson(proposal,true));
                if(proposal.sceneBefore!=proposal.sceneAfter)throw new InvalidOperationException("La propuesta no debe guardar la escena.");
            }
        }
    }
}
