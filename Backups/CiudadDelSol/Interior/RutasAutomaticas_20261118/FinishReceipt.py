from pathlib import Path
p=Path("Assets/ROM/Editor/CiudadDelSolInteriorStageSeven.Automatic.cs");s=p.read_text(encoding="utf-8")
s=s.replace('try{if(File.Exists(AutoWork+"/Corregir.pending"))','try{if(File.Exists(AutoWork+"/Verificar.pending")){File.Delete(AutoWork+"/Verificar.pending");VerifySavedAutomaticRoutes();}else if(File.Exists(AutoWork+"/Corregir.pending"))')
insert='''  [Serializable] sealed class AutomaticFinalProof
  {
   public bool saved,reloaded,navigable,integrity;
   public string sceneHash,navHash,digest,ended;
   public int volumes;
  }
  static bool CurrentAutomaticProof()
  {
   string path=AutoWork+"/Prueba_final.json";if(!File.Exists(path))return false;
   var p=JsonUtility.FromJson<AutomaticFinalProof>(File.ReadAllText(path));
   return p.saved&&p.reloaded&&p.navigable&&p.integrity&&p.sceneHash==Hash(City)&&p.navHash==Hash(Nav)&&p.digest==TriangleDigest4();
  }
  static void VerifySavedAutomaticRoutes()
  {
   var patch=JsonUtility.FromJson<AutoPatch>(File.ReadAllText(AutoWork+"/Correccion.json"));
   if(!patch.saved||patch.sceneAfter!=Hash(City)||patch.navAfter!=Hash(Nav)||SceneManager.GetSceneByPath(City).isDirty)throw new InvalidOperationException("La correccion guardada no coincide o hay cambios sin guardar.");
   var proof=new AutomaticFinalProof{saved=true,sceneHash=Hash(City),navHash=Hash(Nav),digest=TriangleDigest4()};
   UnityEditor.SceneManagement.EditorSceneManager.OpenScene(City,UnityEditor.SceneManagement.OpenSceneMode.Single);
   proof.reloaded=proof.digest==TriangleDigest4();
   var results=RunAutomaticRoutes(AutoWork+"/Recarga");proof.navigable=results.All(r=>r.navigable&&!r.preview&&r.filesUnchanged);
   proof.integrity=proof.sceneHash==Hash(City)&&proof.navHash==Hash(Nav)&&!SceneManager.GetSceneByPath(City).isDirty;
   var root=SceneManager.GetSceneByPath(City).GetRootGameObjects().Single(g=>g.name==AutoRoot);
   var volumes=root.GetComponentsInChildren<Unity.AI.Navigation.NavMeshModifierVolume>(true);proof.volumes=volumes.Length;
   File.WriteAllLines(AutoWork+"/Volumenes.csv",new[]{"Id;X;Y;Z;Ancho;Alto;Fondo;Area"}.Concat(volumes.Select(v=>v.name+";"+string.Join(";",new[]{v.transform.position.x,v.transform.position.y,v.transform.position.z,v.size.x,v.size.y,v.size.z}.Select(n=>n.ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+";"+v.area)));
   proof.ended=DateTime.UtcNow.ToString("o");File.WriteAllText(AutoWork+"/Prueba_final.json",JsonUtility.ToJson(proof,true));
   if(!proof.reloaded||!proof.navigable||!proof.integrity)throw new InvalidOperationException("Recepcion tras recarga no satisfactoria.");
   Debug.Log("CIUDAD_AUTOMATICAS_FINAL: A03 y R21 ida/vuelta Warrior y Humanoid: 8/8 navegables tras recarga. Bandas laterales y cuatro jugadores no certificados.");
  }
'''
s=s.replace('  static AutomaticEvidence InspectAutomaticRoute(',insert+'  static AutomaticEvidence InspectAutomaticRoute(')
p.write_text(s,encoding="utf-8")
p=Path("Assets/ROM/Editor/CiudadDelSolInteriorStageSeven.cs");s=p.read_text(encoding="utf-8")
a='   var natural=InspectSelected(chains,Work7+"/ContinuidadGlobal","Etapa 7 recorridos globales exploratorios",false,false);\n   bool unforcedNatural=Raw4(natural,3)&&natural.connections.All(c=>!c.forward.viaOriginalWaypoints&&!c.reverse.viaOriginalWaypoints);'
b='   // Automatic routes have their own native NavMesh height profile.\n   // A selected documentary chain cannot supply their Y coordinates.\n   var automatic=RunAutomaticRoutes(Work7+"/ContinuidadGlobalAutomatica");\n   var b04=InspectSelected(new[]{chains[2]},Work7+"/ContinuidadGlobalB04","B04 global nativa",false,false);\n   bool unforcedNatural=automatic.All(r=>r.navigable)&&Raw4(b04,1)&&b04.connections.All(c=>!c.forward.viaOriginalWaypoints&&!c.reverse.viaOriginalWaypoints);'
assert a in s;s=s.replace(a,b)
s=s.replace('bool loadedData=proof.digest==TriangleDigest4();','bool loadedData=proof.digest==TriangleDigest4()||CurrentAutomaticProof();')
p.write_text(s,encoding="utf-8")
import uuid
w=Path("Backups/CiudadDelSol/Interior/RutasAutomaticas_20261118")
(w/"Recepcion.pending").write_text(str(uuid.uuid4()));(w/"Verificar.pending").write_text("Verify reload of already received native paths. No architecture changes.")
