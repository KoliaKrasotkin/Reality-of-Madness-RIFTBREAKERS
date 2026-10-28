using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MultiFPS.Gameplay.RiftCombat;
namespace ROM.UI.Sprint01 {
// Persistent presentation. Runtime binds only explicitly identified data fields.
[ExecuteAlways]
public sealed class RiftFrontEndView : MonoBehaviour {
 [Header("Persistent screens: edit their hierarchy, RectTransforms, text and graphics")]
 public RectTransform Design,LegacyContent;public GameObject Home,Selection,Creation;public Text Toast;
 public FrontEndCatalog Catalog;
 [Header("Home actions")]
 public Button Play,Characters,Settings,Exit,Crelium,Connect,More;
 [Header("Dynamic identity and release data (runtime text)")]
 public Text HomeName,HomeClass,HomeStatus,Version,ReleaseStatus;
 [Header("Character actions")]
 public Button SelectionBack,SelectionClose,Create,Delete,Undo,Continue;
 public Button CreationBack,CreationClose,Cancel,Warrior,Paladin,Confirm;
 public GameObject WarriorSelected,PaladinSelected;
 [Header("Dynamic roster: edit the row template for all records")]
 public RectTransform Rows;public RiftCharacterRowView RowTemplate;public GameObject Empty;
 public GameObject SelectedDetails;public Text SelectedName,SelectedClass,SelectedLevel,SelectedTime,SelectedDescription;
 public Image SelectedPortrait;public ROM.UI.Gameplay.RiftHUDGraphic SelectedPlaceholder;
 [Header("Dynamic class data")]
 public Text CreationClass,CreationName,Lore,Style,Availability,CreationPortraitName;
 public Image CreationPortrait;public ROM.UI.Gameplay.RiftHUDGraphic CreationPlaceholder;
 [Header("Preview viewports (their positions and sizes are authoritative)")]
 public RawImage HomePreview,SelectionPreview,CreationPreview;
 readonly List<RiftCharacterRowView> rows=new List<RiftCharacterRowView>();
 RiftModelView homeModel,selectionModel,creationModel;RiftHub owner;int homeSkin=-1,selectedSkin=-1,creationSkin=-1;
 bool bound;string selectedRecord;
 public void Bind(RiftHub hub){
  owner=hub;if(bound)return;bound=true;RowTemplate.gameObject.SetActive(false);
  Hook(Play,()=>owner.FrontEndAction("Play"));Hook(Characters,()=>owner.FrontEndAction("Characters"));Hook(Settings,()=>owner.FrontEndAction("Settings"));Hook(Exit,()=>owner.FrontEndAction("Exit"));
  Hook(Crelium,()=>owner.FrontEndAction("Crelium"));Hook(Connect,()=>owner.FrontEndAction("Connect"));Hook(More,()=>owner.FrontEndAction("More"));
  Hook(SelectionBack,()=>owner.FrontEndAction("Home"));Hook(SelectionClose,()=>owner.FrontEndAction("Home"));Hook(Create,()=>owner.FrontEndAction("Create"));Hook(Delete,()=>owner.FrontEndAction("Delete"));Hook(Undo,()=>owner.FrontEndAction("Undo"));Hook(Continue,()=>owner.FrontEndAction("Continue"));
  Hook(CreationBack,()=>owner.FrontEndAction("Characters"));Hook(CreationClose,()=>owner.FrontEndAction("Home"));Hook(Cancel,()=>owner.FrontEndAction("Characters"));Hook(Warrior,()=>owner.FrontEndAction("Warrior"));Hook(Paladin,()=>owner.FrontEndAction("Paladin"));Hook(Confirm,()=>owner.FrontEndAction("Confirm"));
 }
 static void Hook(Button button,UnityEngine.Events.UnityAction action){if(button)button.onClick.AddListener(action);}
 void Pages(bool home,bool selection,bool creation){Home.SetActive(home);Selection.SetActive(selection);Creation.SetActive(creation);}
 public void HideScreens(){Pages(false,false,false);}
 public void ShowHome(RiftPlayerCharacter record){
  Pages(true,false,false);bool valid=record?.Available==true;Play.interactable=valid;Crelium.interactable=valid&&record.ClassKey=="warrior"&&!record.CreliumCompleted;
  HomeName.text=valid?record.DisplayName:"Sin personaje";HomeClass.text=valid?(record.ClassKey==PaladinPrototypeSettings.ClassKey?"Paladín (prueba)":"Warrior"):"";
  HomeStatus.text=valid?"Personaje seleccionado":"Elegí un personaje para jugar";Version.text=RiftReleaseInfo.Load.DisplayLabel;ReleaseStatus.text=RiftReleaseInfo.Load.StatusLabel;
  HomePreview.gameObject.SetActive(valid);if(valid)Preview(HomePreview,record.Skin,ref homeModel,ref homeSkin,true);
 }
 public void ShowSelection(IReadOnlyList<RiftPlayerCharacter> records,RiftPlayerCharacter active,bool undo,System.Func<RiftPlayerCharacter,string> className){
  Pages(false,true,false);bool valid=active!=null;Empty.SetActive(!valid);SelectedDetails.SetActive(valid);SelectionPreview.gameObject.SetActive(valid&&active.Available);
  Delete.gameObject.SetActive(valid);Continue.gameObject.SetActive(valid);Undo.gameObject.SetActive(undo);
  while(rows.Count>records.Count){var row=rows[rows.Count-1];rows.RemoveAt(rows.Count-1);Destroy(row.gameObject);}
  for(int i=0;i<records.Count;i++){
   if(i>=rows.Count){var row=Instantiate(RowTemplate,Rows);rows.Add(row);row.gameObject.SetActive(true);}
   var record=records[i];string id=record.Id;rows[i].Bind(record,i+1,active?.Id==id,RiftCharacterMetadata.LevelLabel(id)+" · "+className(record),()=>owner.FrontEndSelect(id));
  }
  if(!valid)return;
  SelectedName.text=active.DisplayName;SelectedClass.text=className(active);SelectedLevel.text=RiftCharacterMetadata.LevelLabel(active.Id);SelectedTime.text=RiftCharacterMetadata.PlayedLabel(active.Id);
  SelectedDescription.text=active.ClassKey==PaladinPrototypeSettings.ClassKey?PaladinPrototypeSettings.Load.Description:active.Available?"Guerrero · Furia\n\nHacha a dos manos\no espada y escudo\n\n"+(active.CreliumCompleted?"El Rift te espera.":"Antes del Rift: Crelium."):"Esta clase todavía no está disponible.";
  Portrait(active.Skin,SelectedPortrait,SelectedPlaceholder);Continue.interactable=active.Available;
  Continue.GetComponentInChildren<Text>().text=active.ClassKey==PaladinPrototypeSettings.ClassKey?"PROBAR EN EL RIFT":active.CreliumCompleted?"ENTRAR A EL RIFT":"CONTINUAR EN CRELIUM";
  if(active.Available){bool changed=selectedRecord!=active.Id;selectedRecord=active.Id;if(changed)selectedSkin=-1;Preview(SelectionPreview,active.Skin,ref selectionModel,ref selectedSkin,false);if(changed)owner.FrontEndEquipment(selectionModel,active);}
 }
 public void ShowCreation(bool paladin){
  Pages(false,false,true);paladin=paladin&&PaladinPrototypeSettings.Available;int skin=paladin?PaladinPrototypeSettings.Skin:1;
  WarriorSelected.SetActive(!paladin);PaladinSelected.SetActive(paladin);Paladin.gameObject.SetActive(PaladinPrototypeSettings.Available);
  var data=RiftCharacterPresentationAssets.Load;CreationClass.text=paladin?"PALADÍN":"WARRIOR";CreationName.text=RiftSkinIdentity.Name(skin);CreationPortraitName.text=CreationName.text;
  Lore.text=paladin?PaladinPrototypeSettings.Load.Description:data?data.Lore:"El guerrero de Crelium.";Style.text=paladin?"Cuerpo a cuerpo\n\nGastá Fe en Golpe Sagrado.\n\nLuz Sanadora cura y Defensa Radiante absorbe daño: vos o un aliado.\n\nRegeneración pasiva y Rezar.\n\nBalance de prueba; sin talentos todavía.":data?data.Style:"Cuerpo a cuerpo · Furia";Availability.text=paladin?"PROTOTIPO JUGABLE":"CLASE DISPONIBLE";
  Portrait(skin,CreationPortrait,CreationPlaceholder);Preview(CreationPreview,skin,ref creationModel,ref creationSkin,false);
 }
 static void Portrait(int skin,Image image,ROM.UI.Gameplay.RiftHUDGraphic placeholder){var data=RiftCharacterPresentationAssets.Load;bool use=skin==1&&data&&data.NertorPortrait;image.gameObject.SetActive(use);placeholder.gameObject.SetActive(!use);if(use)image.sprite=data.NertorPortrait;}
 void Preview(RawImage image,int skin,ref RiftModelView model,ref int previous,bool home){
  if(!Catalog)return;var entry=Catalog.Characters.FirstOrDefault(c=>c.SkinIndex==skin);if(entry==null||!entry.Prefab)return;
  if(!model)model=RiftModelView.Attach(image,Catalog,home);
  if(previous==skin)return;model.Show(entry);model.UseInspectionRendering();if(home)model.ConfigureMainMenuPresentation();else{model.SetInspectionZoom(.9f);model.Turn(-15);RiftLivingMenuAssets.Attach(model);}previous=skin;
 }
 [ContextMenu("Vista previa / Menú principal")]
 public void PreviewHome(){if(Application.isPlaying)return;Pages(true,false,false);HomePreview.gameObject.SetActive(true);Preview(HomePreview,1,ref homeModel,ref homeSkin,true);}
 [ContextMenu("Vista previa / Selección")]
 public void PreviewSelection(){if(Application.isPlaying)return;Pages(false,true,false);SelectionPreview.gameObject.SetActive(true);Preview(SelectionPreview,1,ref selectionModel,ref selectedSkin,false);}
 [ContextMenu("Vista previa / Creación Warrior")]
 public void PreviewWarrior(){if(!Application.isPlaying)ShowCreation(false);}
 [ContextMenu("Vista previa / Creación Paladín")]
 public void PreviewPaladin(){if(!Application.isPlaying)ShowCreation(true);}
 [ContextMenu("Vista previa / Limpiar modelos transitorios")]
 public void ClearEditorPreviews(){
  if(Application.isPlaying)return;
  foreach(var raw in new[]{HomePreview,SelectionPreview,CreationPreview})if(raw){var model=raw.GetComponent<RiftModelView>();if(model)DestroyImmediate(model);raw.texture=null;raw.enabled=false;}
  homeModel=selectionModel=creationModel=null;homeSkin=selectedSkin=creationSkin=-1;
 }
 void Update(){
  if(Application.isPlaying||!Design)return;var parent=Design.parent as RectTransform;if(!parent||parent.rect.width<=0||parent.rect.height<=0)return;
  Design.anchorMin=Design.anchorMax=Design.pivot=new Vector2(.5f,.5f);Design.anchoredPosition=Vector2.zero;Design.localScale=Vector3.one*Mathf.Min(parent.rect.width/1920f,parent.rect.height/1080f);
 }
 void OnDisable(){if(!Application.isPlaying)ClearEditorPreviews();}
}
}
