#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ROM.UI.Sprint01;
using ROM.UI.Gameplay;
using W=ROM.UI.Sprint01.RiftWidgets;

// Explicit authoring command. Never rebuilds the authored asset on import or Play.
public static class RiftFrontEndAuthoring {
 const string Folder="Assets/ROM/UI/FrontEnd/Prefabs",PathName=Folder+"/RiftFrontEnd.prefab";
 static Color white=new Color(.94f,.92f,1),muted=new Color(.79f,.75f,.85f);
 static RiftMapSurface Surface(Transform parent,string name,float x,float y,float width,float height){
  var s=W.Rect(parent,name,x,y,width,height).gameObject.AddComponent<RiftMapSurface>();s.Fill=new Color(.03f,.027f,.05f,.92f);s.Border=new Color(.43f,.34f,.59f);s.Radius=10;s.raycastTarget=false;return s;
 }
 static Button Button(Transform parent,string text,float x,float y,float width,float height){
  var surface=Surface(parent,text,x,y,width,height);surface.raycastTarget=true;var b=surface.gameObject.AddComponent<Button>();b.targetGraphic=surface;var c=b.colors;c.highlightedColor=new Color(1.2f,1.12f,1.35f);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.72f,.65f,.85f);b.colors=c;
  var label=W.Label(surface.transform,text,12,0,width-24,height,height<60?18:22,white);label.alignment=TextAnchor.MiddleCenter;return b;
 }
 static RawImage Preview(Transform p,string name,float x,float y,float width,float height){var raw=W.Rect(p,name,x,y,width,height).gameObject.AddComponent<RawImage>();raw.color=Color.white;raw.enabled=false;raw.raycastTarget=true;return raw;}
 static void Portrait(Transform p,float x,float y,float size,out Image image,out RiftHUDGraphic placeholder){var surface=Surface(p,"Portrait frame",x,y,size,size);image=W.Rect(surface.transform,"Dynamic portrait",3,3,size-6,size-6).gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;var art=RiftCharacterPresentationAssets.Load;if(art)image.sprite=art.NertorPortrait;placeholder=W.Rect(surface.transform,"Class symbol",size*.2f,size*.2f,size*.6f,size*.6f).gameObject.AddComponent<RiftHUDGraphic>();placeholder.Shape=HUDShape.Helmet;placeholder.color=new Color(.74f,.66f,.9f);placeholder.raycastTarget=false;placeholder.gameObject.SetActive(false);}
 static GameObject Page(Transform p,string name,string title,out Button back,out Button close){var root=W.Rect(p,name,0,0,1920,1080);Surface(root,"Screen glass",40,40,1840,1000).raycastTarget=true;W.Label(root,title,270,64,1350,64,38,white);W.Label(root,"RIFTBREAKERS",270,122,800,28,15,new Color(.68f,.56f,.85f));back=Button(root,"VOLVER",72,74,160,48);close=Button(root,"CERRAR",1690,74,150,48);return root.gameObject;}
 public static void CreateFromDisk(){if(!Application.isBatchMode)throw new InvalidOperationException("Batch entry only.");EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity",OpenSceneMode.Single);Create();}
 [MenuItem("Rift/Authoring/Create editable front end (one time)")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
  var scene=EditorSceneManager.GetActiveScene();if(scene.name!="MainMenu")throw new InvalidOperationException("Open MainMenu first; no scene is opened or discarded by this command.");
  var hub=UnityEngine.Object.FindObjectsByType<RiftHub>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(h=>!h.InGame);
  if(!hub)throw new InvalidOperationException("MainMenu RiftHub missing.");
  if(hub.FrontEnd||AssetDatabase.LoadAssetAtPath<GameObject>(PathName))throw new InvalidOperationException("Authored front end already exists. Edit it directly; do not regenerate it.");
  var snapshot=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Temp","Rift-FrontEnd-before-authoring.unity");
  if(!EditorSceneManager.SaveScene(scene,snapshot,true))throw new IOException("Could not preserve the active scene snapshot.");
  W.Body=hub.InterfaceFont?hub.InterfaceFont:hub.Theme?hub.Theme.Body:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");W.Title=W.Body;W.ButtonFont=hub.ButtonFont?hub.ButtonFont:W.Body;
  var root=new GameObject("RiftFrontEnd",typeof(RectTransform));var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
  var v=root.AddComponent<RiftFrontEndView>();v.Catalog=hub.Catalog;
  var backdrop=W.Rect(root.transform,"Full bleed background",0,0,1920,1080).gameObject.AddComponent<RawImage>();backdrop.texture=Resources.Load<Texture2D>("RiftMenu/RiftBackground");backdrop.raycastTarget=false;var br=backdrop.rectTransform;br.anchorMin=br.anchorMax=br.pivot=new Vector2(.5f,.5f);br.anchoredPosition=Vector2.zero;
  var fit=backdrop.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=backdrop.texture?(float)backdrop.texture.width/backdrop.texture.height:16f/9;
  v.Design=W.Rect(root.transform,"SafeAreaDesign",0,0,1920,1080);
  v.Home=W.Rect(v.Design,"MainMenu",0,0,1920,1080).gameObject;
  v.HomePreview=Preview(v.Home.transform,"Character preview viewport",530,20,1100,1040);v.HomePreview.raycastTarget=false;
  Surface(v.Home.transform,"Main navigation glass",36,48,500,984);
  var logo=W.Rect(v.Home.transform,"Logo",70,115,430,143).gameObject.AddComponent<RawImage>();logo.texture=Resources.Load<Texture2D>("RiftMenu/Logo");logo.raycastTarget=false;
  W.Label(v.Home.transform,"EL RIFT · EXPLORACIÓN",84,268,400,28,14,muted);
  v.Play=Button(v.Home.transform,"Jugar",68,330,436,88);v.Characters=Button(v.Home.transform,"Personaje",68,442,436,88);v.Settings=Button(v.Home.transform,"Ajustes",68,554,436,88);v.Exit=Button(v.Home.transform,"Salir",68,666,436,88);
  v.Crelium=Button(v.Home.transform,"Crelium",68,766,436,48);v.Connect=Button(v.Home.transform,"Unirse por dirección",68,826,436,48);v.More=Button(v.Home.transform,"Más opciones",68,886,436,48);
  W.Label(v.Home.transform,"↑ ↓ para navegar · Enter para elegir",70,968,430,35,16,muted);
  var identity=Surface(v.Home.transform,"Selected character identity",1540,864,344,160).transform;
  v.HomeName=W.Label(identity,"Nertör",24,18,296,52,32,white);v.HomeClass=W.Label(identity,"Warrior",24,74,296,40,23,muted);v.HomeStatus=W.Label(identity,"Personaje seleccionado",24,128,296,24,15,muted);
  v.Version=W.Label(v.Home.transform,"Versión",650,1034,1200,26,15,muted);v.ReleaseStatus=W.Label(v.Version.transform,"Estado",0,24,1200,18,12,muted);
  v.Selection=Page(v.Design,"CharacterSelection","TUS PERSONAJES",out v.SelectionBack,out v.SelectionClose);
  Surface(v.Selection.transform,"Character list panel",70,180,450,684);
  v.Rows=W.ScrollArea(v.Selection.transform,"Character list",82,194,426,650,650);var layout=v.Rows.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=10;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=false;var sizing=v.Rows.gameObject.AddComponent<ContentSizeFitter>();sizing.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
  var row=Surface(v.Rows,"Character row template (dynamic data)",0,0,410,106);var element=row.gameObject.AddComponent<LayoutElement>();element.preferredHeight=106;element.preferredWidth=410;row.raycastTarget=true;
  v.RowTemplate=row.gameObject.AddComponent<RiftCharacterRowView>();v.RowTemplate.Surface=row;v.RowTemplate.Select=row.gameObject.AddComponent<Button>();v.RowTemplate.Select.targetGraphic=row;
  Portrait(row.transform,10,15,74,out v.RowTemplate.Portrait,out v.RowTemplate.Placeholder);v.RowTemplate.Name=W.Label(row.transform,"Nertör · 1",98,12,294,30,22,white);v.RowTemplate.Details=W.Label(row.transform,"Nivel 1 · Warrior",98,46,294,24,16,muted);v.RowTemplate.Status=W.Label(row.transform,"SELECCIONADO",98,75,294,22,13,new Color(.74f,.56f,.97f));
  v.Empty=W.Rect(v.Selection.transform,"Empty roster message",90,260,430,170).gameObject;W.Label(v.Empty.transform,"No tenés personajes",0,0,430,70,30,white);W.Label(v.Empty.transform,"Creá un personaje para comenzar.",0,90,430,80,23,muted);v.Empty.SetActive(false);
  v.SelectionPreview=Preview(v.Selection.transform,"Selected character viewport",530,154,850,760);
  W.Label(v.Selection.transform,"Arrastrá para girar · Rueda para acercar",590,900,730,36,16,muted).alignment=TextAnchor.MiddleCenter;
  v.SelectedDetails=Surface(v.Selection.transform,"Selected character details",1400,180,440,640).gameObject;
  Portrait(v.SelectedDetails.transform,30,30,92,out v.SelectedPortrait,out v.SelectedPlaceholder);v.SelectedName=W.Label(v.SelectedDetails.transform,"Nertör",140,35,275,45,29,white);v.SelectedClass=W.Label(v.SelectedDetails.transform,"Warrior",140,83,275,32,19,muted);v.SelectedLevel=W.Label(v.SelectedDetails.transform,"Nivel 1",30,150,380,42,27,white);v.SelectedTime=W.Label(v.SelectedDetails.transform,"Tiempo jugado",30,202,380,34,17,muted);v.SelectedDescription=W.Label(v.SelectedDetails.transform,"Datos del personaje seleccionado",30,265,380,300,21,muted);
  v.Delete=Button(v.Selection.transform,"BORRAR PERSONAJE",1424,830,392,44);v.Undo=Button(v.Selection.transform,"DESHACER BORRADO",70,850,450,40);v.Create=Button(v.Selection.transform,"+ CREAR PERSONAJE",70,900,450,68);v.Continue=Button(v.Selection.transform,"CONTINUAR EN CRELIUM",1400,900,440,68);
  v.Creation=Page(v.Design,"CharacterCreation","CREAR PERSONAJE",out v.CreationBack,out v.CreationClose);
  var lore=Surface(v.Creation.transform,"Class lore panel",70,180,450,650).transform;v.CreationClass=W.Label(lore,"WARRIOR",30,28,390,54,32,white);v.CreationName=W.Label(lore,"Nertör",30,95,390,48,28,white);v.Lore=W.Label(lore,"Class lore (bound to presentation data)",30,180,390,300,22,muted);v.Availability=W.Label(lore,"CLASE DISPONIBLE",30,560,390,34,15,new Color(.68f,.51f,.91f));
  v.CreationPreview=Preview(v.Creation.transform,"Class preview viewport",530,244,850,650);
  v.Warrior=Button(v.Creation.transform,"WARRIOR",600,180,270,44);v.Paladin=Button(v.Creation.transform,"PALADÍN (PRUEBA)",890,180,320,44);
  v.WarriorSelected=W.Image(v.Warrior.transform,"Selected underline",10,42,250,2,new Color(.7f,.4f,1)).gameObject;v.PaladinSelected=W.Image(v.Paladin.transform,"Selected underline",10,42,300,2,new Color(.7f,.4f,1)).gameObject;v.PaladinSelected.SetActive(false);
  var style=Surface(v.Creation.transform,"Class playstyle panel",1400,180,440,650).transform;W.Label(style,"ESTILO DE JUEGO",30,28,380,46,26,white);v.Style=W.Label(style,"Class style (bound to presentation data)",30,112,380,375,21,muted);Portrait(style,30,534,76,out v.CreationPortrait,out v.CreationPlaceholder);v.CreationPortraitName=W.Label(style,"Nertör",130,550,275,40,22,white);
  W.Label(v.Creation.transform,"Arrastrá para girar · Rueda para acercar",590,900,730,36,16,muted).alignment=TextAnchor.MiddleCenter;
  v.Cancel=Button(v.Creation.transform,"CANCELAR",70,900,450,68);v.Confirm=Button(v.Creation.transform,"CREAR Y SELECCIONAR",1400,900,440,68);
  v.LegacyContent=W.Rect(v.Design,"Other screens (runtime, not migrated)",36,168,1848,810);v.Toast=W.Label(v.Design,"",590,1008,1200,40,18,new Color(.86f,.75f,1));
  v.Selection.SetActive(false);v.Creation.SetActive(false);
  Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var prefab=PrefabUtility.SaveAsPrefabAsset(root,PathName);UnityEngine.Object.DestroyImmediate(root);
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,hub.transform);hub.FrontEnd=instance.GetComponent<RiftFrontEndView>();if(hub.Design)hub.Design.gameObject.SetActive(false);
  hub.FrontEnd.ClearEditorPreviews();EditorUtility.SetDirty(hub);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Debug.Log("Editable front end authored. Original loaded scene snapshot: "+snapshot+". Edit "+PathName+" or its instance overrides; runtime does not regenerate it.");
 }
}
#endif
