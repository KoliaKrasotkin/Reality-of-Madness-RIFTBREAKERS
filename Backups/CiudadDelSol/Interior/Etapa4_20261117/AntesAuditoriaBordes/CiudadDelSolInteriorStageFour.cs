using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ROM.EditorTools
{
    // Stage selection only. All native probes and safety criteria reuse the received engine.
    public static partial class CiudadDelSolInteriorStageTwo
    {
        const string StageFourOutput="Backups/CiudadDelSol/Interior/Etapa4_20261117";

        public static void InspectStageFourBatch()
        {
            if(!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Entrada batch exclusiva de Edit Mode.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("No se sustituye ninguna escena con cambios pendientes.");
            Directory.CreateDirectory(StageFourOutput);
            if(!UnityEngine.SceneManagement.SceneManager.GetSceneByPath(City).isLoaded)
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(City,UnityEditor.SceneManagement.OpenSceneMode.Single);
            File.WriteAllText(StageFourOutput+"/Ejecucion_batch.txt",DateTime.UtcNow.ToString("o")+"\nUnity "+Application.unityVersion+"; PhysX y NavMesh nativos; Edit Mode; sin movimiento simulado ni guardado.");
            InspectStageFour();
        }

        [MenuItem("ROM/Dungeon Ciudad del Sol/Interior - Etapa 4/Recibir VS01 VS02 (sin obras)")]
        public static void InspectStageFour()
        {
            var stageOne=InspectSelected(new[]{P("A03-R21","A03","R21",
                V(156,-16,99),V(174,-16,99),V(174,-16,117),V(120,-16,117),V(117,-16,123))},
                StageFourOutput+"/RegresionEtapa1","Etapa 1 - regresión para Etapa 4");
            var stageTwo=InspectSelected(Plans(),StageFourOutput+"/RegresionEtapa2","Etapa 2 - regresión para Etapa 4");
            var receivedThree=JsonUtility.FromJson<Report>(File.ReadAllText(StageThreeOutput+"/Recepcion.json"));
            if(receivedThree==null||!receivedThree.filesUnchanged||!string.IsNullOrEmpty(receivedThree.error)||
                receivedThree.connections.Count!=3||receivedThree.connections.Any(c=>!c.technicalIndividual))
                throw new InvalidOperationException("No se encontró la recepción 3/3 íntegra de Etapa 3.");
            var stageThree=InspectSelected(receivedThree.connections.Select(c=>c.plan).ToArray(),
                StageFourOutput+"/RegresionEtapa3","Etapa 3 - regresión para Etapa 4");
            var reception=InspectSelected(StageFourPlans(),StageFourOutput+"/EjesOriginales","Etapa 4 - ejes originales");
            var entry=reception.connections.FirstOrDefault(c=>c.plan.id=="VS01");
            if(entry!=null&&!entry.technicalIndividual&&string.IsNullOrEmpty(reception.error)&&reception.filesUnchanged)
            {
                var alternative=InspectSelected(new[]{StageFourNorthEntry()},StageFourOutput+"/RodeoNorteVS01","Etapa 4 - rodeo norte existente VS01");
                var candidate=alternative.connections.FirstOrDefault();
                if(Healthy(alternative,1)&&candidate!=null&&candidate.technicalIndividual)
                {
                    candidate.chosen="Rodeo norte existente A03 y continuación G04/G03 hacia B01; pilastra conservada; eje rechazado archivado";
                    reception.connections[reception.connections.IndexOf(entry)]=candidate;
                    reception.ended=alternative.ended;
                }
            }
            if(!Healthy(stageOne,1)||!Healthy(stageTwo,6)||!Healthy(stageThree,3))
            {
                reception.error="Regresión previa pendiente; revisar RegresionEtapa1, RegresionEtapa2 y RegresionEtapa3. No se cierra Etapa 4.";
                foreach(var c in reception.connections){c.technicalIndividual=false;c.status="PENDIENTE_REGRESION_PREVIA";}
            }
            Write(reception,"Recepcion.json",StageFourOutput);
            File.WriteAllLines(StageFourOutput+"/Resultado_ultima_recepcion.md",new[]{
                "# Etapa 4 - recepción consolidada",reception.started+" → "+reception.ended,
                "Aprobadas técnicamente: "+reception.connections.Count(c=>c.technicalIndividual)+"/2. Cuatro jugadores reales: pendiente.",
                "Error: "+reception.error}.Concat(reception.connections.Select(c=>c.plan.id+": "+c.status+"; "+c.chosen)));
        }

        static bool Healthy(Report report,int count)=>report!=null&&report.filesUnchanged&&string.IsNullOrEmpty(report.error)&&
            report.negativeControlsRejectObstaclesAndOtherLevels&&report.connections.Count==count&&report.connections.All(c=>c.technicalIndividual);

        static Plan[] StageFourPlans()=>new[]{
            P("VS01","A03","B01",
                V(156,-16,99),V(180,-16,99),V(180,-16,63),V(135,-16,63),V(135,-16,105),
                V(120,-16,105),V(120,-16,12),V(63,-16,12),V(63,-16,-15),V(57,-16,-15),
                V(45,-16,-15),V(45,-16,-12),V(45,-18,-6),V(45,-20,0),V(45,-22,6),V(45,-24,12),V(45,-24,15)),
            P("VS02","B01","B02",V(45,-24,15),V(51,-24,15),V(51,-24,45),V(42,-24,45),
                V(36,-26,45),V(30,-28,45),V(24,-30,45))
        };

        static Plan StageFourNorthEntry()
        {
            var plan=StageFourPlans()[0];
            plan.points=new[]{V(156,-16,99),V(174,-16,99),V(174,-16,117),V(120,-16,117),V(120,-16,105)}
                .Concat(plan.points.Skip(6)).ToArray();
            return plan;
        }
    }
}
