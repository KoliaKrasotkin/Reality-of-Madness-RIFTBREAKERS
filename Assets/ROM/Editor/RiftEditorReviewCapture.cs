using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace ROM.EditorTools {
// Shared Editor-only capture dependency. No automatic startup or runtime hooks.
public sealed class RiftEditorReviewCapture {
 public bool Done;public Exception Error;
 public void Save(string path,MonoBehaviour host){Done=false;Error=null;host.StartCoroutine(Run(path));}
 IEnumerator Run(string path){yield return new WaitForEndOfFrame();Texture2D image=null;try{image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());}catch(Exception e){Error=e;}finally{if(image)UnityEngine.Object.Destroy(image);Done=true;}}
}
}
