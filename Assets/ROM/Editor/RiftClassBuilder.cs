using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MultiFPS.Gameplay;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class RiftClassBuilder {
[MenuItem("ROM/Clases/Instalar cuatro clases")]
public static void Build(){
Directory.CreateDirectory("Assets/ROM/Gameplay/Classes");Directory.CreateDirectory("Assets/ROM/Resources");AssetDatabase.Refresh();
var rules=AssetDatabase.LoadAssetAtPath<RiftClassRules>("Assets/ROM/Resources/RiftClassRules.asset");if(!rules){rules=ScriptableObject.CreateInstance<RiftClassRules>();AssetDatabase.CreateAsset(rules,"Assets/ROM/Resources/RiftClassRules.asset");}
string[] names={"Paladín del Alba","Bastión","Tejedor de Guerra","Acechador del Velo"},roles={"APOYO OFENSIVO","GUERRERO TANQUE","MAGO DE BATALLA","CAZADOR FURTIVO"};
string[] descriptions={"Sostiene al grupo con luz sagrada y combate con magia y armas de fuego.","Protege a sus aliados, atrae enemigos y resiste en primera línea.","Combina ráfagas arcanas con armas convencionales potenciadas.","Marca presas para el grupo y rompe el contacto con el velo."};
string[] first={"Luz radiante","Guardia desafiante","Estallido arcano","Marca de presa"},second={"Juicio del Alba","Golpe sísmico","Infusión de guerra","Velo de sombras"};
string[] firstDesc={"Cura 35 % en 16 m y daña enemigos cercanos.","Guardia 6 s y mayor amenaza. Sin apoyo: cura 25 %.","Explosión de 7 m sobre un objetivo visible a 45 m.","Disparo a 65 m. La presa recibe +20 % de daño, 10 s."};
string[] secondDesc={"Golpea y debilita al objetivo; cura aliados cercanos.","Golpe en 10 m; debilita el daño enemigo durante 4 s.","8 s de daño +35 %, protección adicional y cura 15 %.","Oculto 6 s y cura 20 %. Próximo impacto: +60 %."};
string[] passive={"Recuperación fuera de combate. Curas +25 % al Bastión.","Con paladín cercano, Guardia absorbe 25 % del daño aliado.","Recuperación fuera de combate. Magia y armas sin bloqueo.","Recuperación fuera de combate. El grupo aprovecha tu marca."};
int[] health={130,180,110,120};float[] weapon={1.15f,1.05f,1.2f,1.15f},armor={.10f,.22f,.04f,.08f},power1={65,0,110,80},power2={100,95,0,0};Color[] colors={new Color(1,.8f,.3f),new Color(.5f,.75f,1),new Color(.65f,.4f,1),new Color(.3f,.95f,.7f)};
rules.Classes=new RiftClassDefinition[4];
for(int i=0;i<4;i++){
string path="Assets/ROM/Gameplay/Classes/"+((RiftClassId)i)+".asset";var def=AssetDatabase.LoadAssetAtPath<RiftClassDefinition>(path);
if(!def){def=ScriptableObject.CreateInstance<RiftClassDefinition>();def.Id=(RiftClassId)i;def.Name=names[i];def.Role=roles[i];def.Description=descriptions[i];def.Passive=passive[i];def.FirstAbility=first[i];def.SecondAbility=second[i];def.FirstDescription=firstDesc[i];def.SecondDescription=secondDesc[i];def.Health=health[i];def.WeaponDamage=weapon[i];def.Armor=armor[i];def.FirstPower=power1[i];def.SecondPower=power2[i];def.FirstCooldown=i==3?14:12;def.SecondCooldown=i==1?10:18;def.Color=colors[i];AssetDatabase.CreateAsset(def,path);}
rules.Classes[i]=def;}
if(!rules.EffectMaterial){var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,"Assets/ROM/Gameplay/Classes/AbilityGlow.mat");rules.EffectMaterial=mat;}
EditorUtility.SetDirty(rules);AssetDatabase.SaveAssets();
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();mode.ClassConfiguration=rules;EditorUtility.SetDirty(mode);EditorSceneManager.SaveScene(scene);
scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();hub.Build();EditorUtility.SetDirty(hub);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
}
public static void Run(){try{Build();RiftClassProbe.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}}
