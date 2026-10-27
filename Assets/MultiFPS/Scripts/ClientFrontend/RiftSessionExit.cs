using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MultiFPS.UI {
 // A single exit route, including recovery after a connection has already ended.
 public sealed class RiftSessionExit:MonoBehaviour {
  static RiftSessionExit running;
  const string Menu="Assets/ROM/Scenes/MainMenu.unity";
  public static void ReturnToMenu(){if(running)return;var go=new GameObject("Return to main menu");DontDestroyOnLoad(go);running=go.AddComponent<RiftSessionExit>();running.StartCoroutine(running.Exit());}
  IEnumerator Exit(){
   var pause=UIPauseMenu._instance;if(pause){if(pause.InterfaceShowing)pause.ShowInterface(false);if(pause.pauseMenuShowing)pause.ShowPauseMenu(false);}
   var manager=DNNetworkManager.Instance;
   if(manager){if(NetworkServer.active&&NetworkClient.active)manager.StopHost();else if(NetworkClient.active)manager.StopClient();else if(NetworkServer.active)manager.StopServer();}
   float began=Time.realtimeSinceStartup;
   while((NetworkServer.active||NetworkClient.active||NetworkManager.loadingSceneAsync!=null&&!NetworkManager.loadingSceneAsync.isDone)&&Time.realtimeSinceStartup-began<15)yield return null;
   if(NetworkServer.active||NetworkClient.active){Debug.LogWarning("Return to menu: disconnection is still pending; retry is available.");Destroy(gameObject);yield break;}
   if(NetworkManager.loadingSceneAsync!=null&&!NetworkManager.loadingSceneAsync.isDone){Debug.LogWarning("Return to menu: scene change is still pending; retry is available.");Destroy(gameObject);yield break;}
   if(SceneManager.GetActiveScene().path!=Menu){var op=SceneManager.LoadSceneAsync(Menu,LoadSceneMode.Single);if(op!=null)yield return op;}
   ClientFrontend.SetPause(false);Destroy(gameObject);
  }
  void OnDestroy(){if(running==this)running=null;}
 }
}
