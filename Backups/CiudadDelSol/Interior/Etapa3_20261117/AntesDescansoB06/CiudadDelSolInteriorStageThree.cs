using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace ROM.EditorTools
{
    // Stage selection only: physics, capsule and NavMesh checks reuse StageTwo's engine.
    // No scene writes, objects, links, baking, gameplay or player simulation.
    public static partial class CiudadDelSolInteriorStageTwo
    {
        const string StageThreeOutput="Backups/CiudadDelSol/Interior/Etapa3_20261117";
        const string StageThreeRequest=StageThreeOutput+"/Recepcion.pending";
        static double stageThreeDeadline;

        [InitializeOnLoadMethod]
        static void InstallStageThreeRequest()
        {
            if(!File.Exists(StageThreeRequest))return;
            stageThreeDeadline=EditorApplication.timeSinceStartup+120;
            EditorApplication.update-=RunStageThreeRequest;
            EditorApplication.update+=RunStageThreeRequest;
        }
        static void RunStageThreeRequest()
        {
            if(!File.Exists(StageThreeRequest)){EditorApplication.update-=RunStageThreeRequest;return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=RunStageThreeRequest;
            string nonce=File.ReadAllText(StageThreeRequest).Trim();
            if(!Guid.TryParse(nonce,out _))return;
            File.Move(StageThreeRequest,StageThreeRequest+".consumed."+nonce);
            try
            {
                if(EditorApplication.timeSinceStartup>stageThreeDeadline)throw new TimeoutException("Recepción Etapa 3 no iniciada antes de 120 s.");
                InspectStageThree();
            }
            catch(Exception e){File.WriteAllText(StageThreeOutput+"/Error_solicitud.txt",e.ToString());UnityEngine.Debug.LogException(e);}
        }

        [MenuItem("ROM/Dungeon Ciudad del Sol/Interior - Etapa 3/Recibir VE01 VE02 VD01 (sin obras)")]
        public static void InspectStageThree()
        {
            // Fresh native regression, in a separate folder: preserves Stage 2's evidence.
            var regression=InspectSelected(Plans(),StageThreeOutput+"/RegresionEtapa2","Etapa 2 - regresión para Etapa 3");
            var reception=InspectSelected(StageThreePlans(),StageThreeOutput+"/EjesOriginales","Etapa 3 - ejes originales");
            var entry=reception.connections.FirstOrDefault(c=>c.plan.id=="VE01");
            // Existing north detour, documented/received in Stage 1. Do not remove the
            // Nave_08_2 pilaster at (132,-16,105). Test its full continuation anew.
            if(entry!=null&&!entry.technicalIndividual&&string.IsNullOrEmpty(reception.error)&&reception.filesUnchanged)
            {
                var alternative=InspectSelected(new[]{StageThreeNorthEntry()},StageThreeOutput+"/AlternativaNorteVE01","Etapa 3 - rodeo norte existente VE01");
                var candidate=alternative.connections.FirstOrDefault();
                if(string.IsNullOrEmpty(alternative.error)&&alternative.filesUnchanged&&candidate!=null&&candidate.technicalIndividual)
                {
                    candidate.chosen="Rodeo norte existente de Etapa 1 y continuidad sureste; eje original conservado en EjesOriginales/Recepcion.json";
                    reception.connections[reception.connections.IndexOf(entry)]=candidate;
                    reception.ended=alternative.ended;
                }
            }
            if(!string.IsNullOrEmpty(regression.error)||!regression.filesUnchanged||regression.connections.Count!=6||regression.connections.Any(c=>!c.technicalIndividual))
            {
                reception.error="La regresión de Etapa 2 no supera seis conexiones. Revisar RegresionEtapa2/Recepcion.json; no se cierra Etapa 3.";
                foreach(var c in reception.connections){c.technicalIndividual=false;c.status="PENDIENTE_REGRESION_ETAPA2";}
            }
            Write(reception,"Recepcion.json",StageThreeOutput);
            File.WriteAllLines(StageThreeOutput+"/Resultado_ultima_recepcion.md",new[]{
                "# Etapa 3 - recepción consolidada",reception.started+" → "+reception.ended,
                "Aprobadas técnicamente: "+reception.connections.Count(c=>c.technicalIndividual)+"/3. Cuatro jugadores reales: pendiente.",
                "Error: "+reception.error}.Concat(reception.connections.Select(c=>c.plan.id+": "+c.status+"; "+c.chosen)));
        }

        static Plan[] StageThreePlans()=>new[]{
            P("VE01","A03","B06",
                V(156,-16,99),V(180,-16,99),V(180,-16,63),V(135,-16,63),
                V(135,-16,105),V(120,-16,105),V(120,-16,12),V(123,-16,12),V(123,-16,6),
                V(123,-18,0),V(123,-20,-6),V(123,-22,-12),V(123,-24,-18),V(123,-24,-24),
                V(123,-26,-30),V(123,-28,-36),V(123,-30,-42),V(120,-30,-42)),
            P("VE02","B06","D02",
                V(120,-30,-42),V(123,-30,-42),V(123,-30,-45),V(111,-30,-45),
                V(111,-30,-39),V(108,-30,-39),V(102,-32,-39),V(96,-34,-39),
                V(93,-34,-39),V(93,-34,-9),V(99,-34,-9)),
            P("VD01","D02","D01",
                V(99,-34,-9),V(93,-34,-9),V(93,-34,-3),V(96,-34,-3),V(96,-34,27),
                V(96,-34,33),V(99,-34,39),V(99,-34,45),V(102,-34,45),V(102,-34,63),V(105,-34,63))
        };

        static Plan StageThreeNorthEntry()=>P("VE01","A03","B06",
            V(156,-16,99),V(174,-16,99),V(174,-16,117),V(120,-16,117),
            V(120,-16,105),V(120,-16,12),V(123,-16,12),V(123,-16,6),
            V(123,-18,0),V(123,-20,-6),V(123,-22,-12),V(123,-24,-18),V(123,-24,-24),
            V(123,-26,-30),V(123,-28,-36),V(123,-30,-42),V(120,-30,-42));
    }
}
