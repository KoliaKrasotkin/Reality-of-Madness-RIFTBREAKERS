using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ROM.UI.Sprint01;
namespace ROM.EditorTools {
public static class MenuCharacterPreviewValidation {
 public static void Run(){int result=0;try{
 EditorSceneManager.OpenScene("Assets/ROM/Scenes/MainMenu.unity");
 var front=UnityEngine.Object.FindFirstObjectByType<RiftFrontEndView>();
 if(!front)throw new Exception("No authored front end");
 front.PreviewHome();var view=front.HomePreview.GetComponent<RiftModelView>();
 foreach(int skin in new[]{1,2,3}){
 view.Show(front.Catalog.Characters.Single(x=>x.SkinIndex==skin));
 var animator=view.PreviewAnimator;if(!animator||!animator.runtimeAnimatorController)throw new Exception("No animator "+skin);
 animator.Rebind();animator.Update(0);float before=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;animator.Update(.5f);
 if(before==animator.GetCurrentAnimatorStateInfo(0).normalizedTime)throw new Exception("Animation does not advance "+skin);
 view.PreviewCamera.Render();var old=RenderTexture.active;RenderTexture.active=view.PreviewTexture;
 var image=new Texture2D(RenderTexture.active.width,RenderTexture.active.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();RenderTexture.active=old;
 string folder=Path.GetFullPath("Logs/MenuCharacterPreview");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"skin-"+skin+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
 Debug.Log("MENU_PREVIEW_PASS skin "+skin+": actual MainMenu HomePreview, animation sample advances, render captured");
 }
 }catch(Exception e){Debug.LogException(e);result=1;}finally{EditorApplication.Exit(result);}}
}}
