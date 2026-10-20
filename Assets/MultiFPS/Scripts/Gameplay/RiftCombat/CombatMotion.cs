using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
namespace MultiFPS.Gameplay.RiftCombat {
// Presentation only. Hit timing lives in server-authored AbilityDefinition windows.
[DefaultExecutionOrder(12000)]
public sealed class CombatMotion:MonoBehaviour {
 PlayableGraph graph;AnimationClipPlayable clip;Animator animator;float start,duration,length;
 public void Play(Animator target,AnimationClip animation,float seconds){Stop();if(!target||!animation)return;animator=target;duration=seconds;length=animation.length;start=Time.time;graph=PlayableGraph.Create("Nertor attack overlay");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);clip=AnimationClipPlayable.Create(graph,animation);clip.SetApplyFootIK(true);var output=AnimationPlayableOutput.Create(graph,"Attack",target);output.SetSourcePlayable(clip);graph.Play();}
 void LateUpdate(){if(!graph.IsValid())return;float t=Time.time-start;if(t>=duration){Stop();return;}clip.SetTime(t/duration*length);graph.Evaluate(0);}
 void Stop(){if(graph.IsValid())graph.Destroy();}
 void OnDisable()=>Stop();void OnDestroy()=>Stop();
}}
