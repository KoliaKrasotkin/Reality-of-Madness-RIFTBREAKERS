using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ROM.UI.Gameplay;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01 {
public sealed partial class RiftHub {
 string storeCategory="Personaje";Text storeName,storeType,storePrice;Button storePurchase;PreviewModel storeModel;
 readonly Dictionary<string,RiftMapSurface> storeCards=new Dictionary<string,RiftMapSurface>();
 static readonly Color StoreInk=new Color(.021f,.023f,.038f,.94f),StoreSilver=new Color(.43f,.44f,.53f,.85f),StoreViolet=new Color(.67f,.44f,.98f),StoreWhite=new Color(.95f,.94f,.98f);
 RiftMapSurface StoreSurface(Transform parent,string name,float x,float y,float width,float height,Color fill,Color border){var rect=W.Rect(parent,name,x,y,width,height);var surface=rect.gameObject.AddComponent<RiftMapSurface>();surface.Fill=fill;surface.Border=border;surface.Radius=10;surface.raycastTarget=false;return surface;}
 Button StoreButton(Transform parent,string title,float x,float y,float width,float height,Action action,bool primary=false){var surface=StoreSurface(parent,title,x,y,width,height,primary?new Color(.19f,.095f,.3f,.96f):new Color(.045f,.043f,.07f,.9f),primary?StoreViolet:StoreSilver);surface.raycastTarget=true;surface.Glow=primary?4:0;var button=surface.gameObject.AddComponent<Button>();button.targetGraphic=surface;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.13f,1.06f,1.2f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.65f,.57f,.78f);button.colors=colors;button.onClick.AddListener(()=>action());var text=W.Label(surface.transform,title,12,0,width-24,height,18,StoreWhite);text.alignment=TextAnchor.MiddleCenter;return button;}
 string StoreProductName(ProductRecord product){var model=Catalog?Catalog.Find(product.ModelKey):null;return model!=null&&model.SkinIndex==1?MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Name(1):product.Name;}
 void ModernStore(RectTransform panel){
  Clear(panel);panel.name="Modern store";panel.anchoredPosition=new Vector2(0,104);panel.sizeDelta=new Vector2(1848,952);RiftModernWidgets.Surface(panel,StoreInk);storeCards.Clear();
  W.Label(panel,"RIFTBREAKERS",40,16,500,28,16,new Color(.74f,.73f,.82f));W.Label(panel,"TIENDA",40,48,360,52,36,StoreWhite);W.Label(panel,"Cosméticos oficiales",310,63,600,32,20,new Color(.69f,.68f,.77f));
  StoreButton(panel,"CASA DE SUBASTAS",1300,36,380,48,()=>{shopTab="Subastas";selectedModel="";Navigate(HubPage.Auction);});var exit=StoreButton(panel,"×",1736,32,64,52,()=>Navigate(HubPage.Home));exit.name="Close modern store";
  var banner=StoreSurface(panel,"Store demonstration banner",40,117,1768,46,new Color(.105f,.067f,.16f,.88f),new Color(.45f,.31f,.63f,.8f));W.Label(banner.transform,"DEMOSTRACIÓN · Compras de prueba · Sin transacciones reales",18,0,1728,46,18,new Color(.84f,.74f,.98f));
  var categories=Services.Marketplace.Products.Select(p=>p.Category).Distinct().ToArray();if(!categories.Contains(storeCategory))storeCategory=categories.FirstOrDefault()??"";
  var products=Services.Marketplace.Products.Where(p=>p.Category==storeCategory).ToArray();float tabX=40;foreach(var category in categories){var captured=category;string title=category=="Personaje"?"SKINS":category=="Arma"?"ARMAS":category.ToUpperInvariant();var tab=StoreButton(panel,title,tabX,184,200,44,()=>{storeCategory=captured;selectedModel="";Navigate(HubPage.Marketplace);},category==storeCategory);tab.name="Store category "+category;tabX+=214;}
  StoreSurface(panel,"Catalog separator",778,184,1,646,new Color(.4f,.36f,.51f,.55f),Color.clear);
  var list=W.ScrollArea(panel,"Store card catalog",40,245,720,576,Mathf.Max(576,Mathf.Ceil(products.Length/2f)*296));list.name="Store product cards";
  for(int i=0;i<products.Length;i++){
   var product=products[i];var model=Catalog?Catalog.Find(product.ModelKey):null;float x=(i%2)*368,y=(i/2)*296;var card=StoreSurface(list,"Store card "+product.Id,x,y,350,280,new Color(.045f,.043f,.063f,.94f),StoreSilver);card.raycastTarget=true;var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card;button.onClick.AddListener(()=>SelectStoreProduct(product));storeCards[product.Id]=card;
   if(model!=null&&model.Prefab)RiftStoreThumbnail.Place(card.transform,model,12,10,326,190);
   var sample=StoreSurface(card.transform,"Demo sample badge",252,16,82,24,new Color(.025f,.022f,.04f,.9f),new Color(.43f,.34f,.56f,.8f));W.Label(sample.transform,"Muestra",8,0,66,24,12,new Color(.8f,.75f,.9f)).alignment=TextAnchor.MiddleCenter;
   W.Label(card.transform,StoreProductName(product),16,211,318,30,20,StoreWhite);W.Label(card.transform,product.GoldPrice.ToString("N0")+" AG",16,245,318,27,18,new Color(.79f,.7f,.93f));
   var events=card.gameObject.AddComponent<EventTrigger>();var entry=new EventTrigger.Entry{eventID=EventTriggerType.Select};entry.callback.AddListener(_=>SelectStoreProduct(product));events.triggers.Add(entry);var tip=card.gameObject.AddComponent<RiftTooltipTrigger>();tip.Title=StoreProductName(product);tip.Description=product.GoldPrice.ToString("N0")+" AG · Cosmético de muestra. Compra de prueba, sin saldo real.";
  }
  var hero=StoreSurface(panel,"Selected cosmetic presentation",800,184,1008,570,new Color(.025f,.026f,.043f,.28f),new Color(.34f,.33f,.44f,.5f));
  productView=RiftModelView.Create(hero.transform,8,8,992,554,Catalog,false);productView.UseInspectionRendering();
  W.Label(panel,"Arrastrá para girar · Rueda: zoom",800,756,780,28,17,new Color(.68f,.66f,.77f));StoreButton(panel,"GIRAR",1678,753,130,34,()=>{if(productView)productView.Turn(45);});
  storeName=W.Label(panel,"",800,797,1008,38,29,StoreWhite);storeType=W.Label(panel,"",800,839,1008,28,18,new Color(.71f,.69f,.8f));storePrice=W.Label(panel,"",800,885,360,36,25,new Color(.87f,.77f,1));storePurchase=StoreButton(panel,"PROBAR COMPRA · DEMO",1200,875,608,52,ConfirmStorePurchase,true);storePurchase.name="Store purchase demo";
  StoreButton(panel,"VOLVER",40,878,220,42,()=>Navigate(HubPage.Home));W.Label(panel,"Cosméticos de muestra",280,884,470,30,16,new Color(.65f,.64f,.73f));
  var selected=products.FirstOrDefault(p=>p.ModelKey==selectedModel)??products.FirstOrDefault(p=>Catalog&&Catalog.Find(p.ModelKey)?.SkinIndex==1)??products.FirstOrDefault();
  if(selected!=null)SelectStoreProduct(selected);else{storeName.text="Sin cosméticos en esta categoría";storePurchase.interactable=false;}
 }
 void SelectStoreProduct(ProductRecord product){
  if(product==null||!storeName)return;selectedModel=product.ModelKey;storeModel=Catalog?Catalog.Find(product.ModelKey):null;
  foreach(var card in storeCards){bool selected=card.Key==product.Id;card.Value.Border=selected?StoreViolet:StoreSilver;card.Value.Glow=selected?3:0;card.Value.SetVerticesDirty();}
  if(productView&&storeModel!=null){productView.Show(storeModel);productView.UseInspectionRendering();productView.SetInspectionZoom(.8f);var framing=productView.GetComponent<RiftStoreHeroFraming>();if(!framing)framing=productView.gameObject.AddComponent<RiftStoreHeroFraming>();framing.ResetZoom(.8f);}
  storeName.text=StoreProductName(product);storeType.text=product.Category=="Personaje"?"Aspecto de personaje · Muestra":product.Category+" · Cosmético de muestra";storePrice.text=product.GoldPrice.ToString("N0")+" AG";storePurchase.interactable=!product.Owned;storePurchase.GetComponentInChildren<Text>().text=product.Owned?"ADQUIRIDO EN LA DEMO":"PROBAR COMPRA · DEMO";
 }
 void ConfirmStorePurchase(){var product=Services.Marketplace.Products.FirstOrDefault(p=>p.ModelKey==selectedModel);if(product==null||product.Owned)return;Dialog("Compra de prueba",StoreProductName(product)+" · "+product.GoldPrice.ToString("N0")+" AG\nNo se modifica el saldo ni el inventario reales.",()=>{var result=Services.Marketplace.PreviewPurchase(product.Id);Navigate(HubPage.Marketplace);Notify(result.Message);});if(modal)RiftPanelChrome.Apply(modal);}
}
}
