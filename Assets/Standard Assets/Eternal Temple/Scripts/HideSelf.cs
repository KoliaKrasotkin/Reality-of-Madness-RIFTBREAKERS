using UnityEngine;
using System.Collections;

namespace commanastationwww.eternaltemple{
public class HideSelf : MonoBehaviour {

        //This script is to be assigned to an individual object that needs to be hidden.
        //If you need to hide several objects as one piece, please use "HideGroup" and "HideablePart" scripts.
	
	private int collisionEntriesCounter = 0;	//counter of entries and exits to the trigger. Allows to have multiple triggers (colliders)

	private Material mat;
	private float currentAlpha;	
	
	public bool hideable = true;
	private float hidingSpeed = 0.3f;
	private float minAlpha = 0.03f;
	private float maxAlpha = 0.2f;

		
        bool EnsureMaterial(){
            if(mat)return true;
            if(!TryGetComponent<Renderer>(out var renderer)||!renderer.sharedMaterial||!renderer.sharedMaterial.HasProperty("_Cutoff"))return false;
            mat=renderer.material;currentAlpha=mat.GetFloat("_Cutoff");return true;
        }
        void Start(){EnsureMaterial();}
        IEnumerator unideInterpolation(){while(mat&&currentAlpha>minAlpha){currentAlpha=Mathf.Max(minAlpha,currentAlpha-hidingSpeed*Time.deltaTime);mat.SetFloat("_Cutoff",currentAlpha);yield return null;}}
        IEnumerator hideInterpolation(){while(mat&&currentAlpha<maxAlpha){currentAlpha=Mathf.Min(maxAlpha,currentAlpha+hidingSpeed*Time.deltaTime);mat.SetFloat("_Cutoff",currentAlpha);yield return null;}}
        void OnTriggerEnter(Collider other){if(!hideable||!other||!other.CompareTag("MainCamera")||!EnsureMaterial())return;collisionEntriesCounter++;StopAllCoroutines();StartCoroutine(hideInterpolation());}
        void OnTriggerExit(Collider other){if(!hideable||!other||!other.CompareTag("MainCamera"))return;collisionEntriesCounter=Mathf.Max(0,collisionEntriesCounter-1);if(collisionEntriesCounter>0||!EnsureMaterial())return;StopAllCoroutines();StartCoroutine(unideInterpolation());}
        void OnDisable(){StopAllCoroutines();collisionEntriesCounter=0;}
        void OnDestroy(){if(mat)Destroy(mat);}
    }
}
