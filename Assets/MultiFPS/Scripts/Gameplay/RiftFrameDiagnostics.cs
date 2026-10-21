using System;
using System.Diagnostics;
namespace MultiFPS.Gameplay {
// Opt-in measurements; no allocations or logging in normal gameplay.
public static class RiftFrameDiagnostics {
 public static bool Enabled;public static readonly long[] Ticks=new long[9],Maximum=new long[9];public static readonly int[] Calls=new int[9];
 public readonly struct Sample:IDisposable {readonly int index;readonly long start;public Sample(int i){index=i;start=Begin();}public void Dispose()=>End(index,start);}
 public static long Begin()=>Enabled?Stopwatch.GetTimestamp():0;
 public static void End(int index,long start){if(start==0)return;long elapsed=Stopwatch.GetTimestamp()-start;Ticks[index]+=elapsed;Maximum[index]=Math.Max(Maximum[index],elapsed);Calls[index]++;}
 public static void Reset(){Array.Clear(Ticks,0,9);Array.Clear(Maximum,0,9);Array.Clear(Calls,0,9);}
 public static string Report(){string[] names={"Minimap.Render","HUD.HideLegacy","Journal.Persist","Journal.Snapshot","Village.Update","Exploration.Tick","Enemy.Spawn","IDS.Initialize","Corpse.Clone"};string text="";for(int i=0;i<9;i++)text+=names[i]+": calls="+Calls[i]+" totalMs="+(Ticks[i]*1000.0/Stopwatch.Frequency).ToString("F2")+" maxMs="+(Maximum[i]*1000.0/Stopwatch.Frequency).ToString("F2")+"\n";return text;}
}}
