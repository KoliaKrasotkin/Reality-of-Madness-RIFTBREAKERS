using UnityEngine;
namespace ROM.Horde {
public sealed class FracturedHeart:MonoBehaviour {
 public Light WorldLight;public Transform Crystal,Fragments;public Color Calm=new Color(.63f,.67f,1),Storm=new Color(.72f,.3f,1);[Range(0,1)]public float Intensity;public float BaseLight=.85f,PulseStrength=.04f,PulsePeriod=18;float visual;
 public void Transition(float energy){Intensity=Mathf.Clamp01(energy);}
 void Update(){if(!Application.isPlaying)return;visual=Mathf.MoveTowards(visual,Intensity,Time.deltaTime*.1f);float wave=Mathf.Sin(Time.time*Mathf.PI*2/PulsePeriod);if(Crystal)Crystal.Rotate(0,Time.deltaTime*1.5f,0,Space.Self);if(Fragments)Fragments.Rotate(Vector3.up,Time.deltaTime*-2,Space.Self);if(WorldLight){WorldLight.color=Color.Lerp(Calm,Storm,visual);WorldLight.intensity=BaseLight*(1+wave*PulseStrength+visual*.15f);} }
}
}
