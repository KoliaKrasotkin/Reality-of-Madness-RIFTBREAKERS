using Mono.Debugger.Soft;
using System;
using System.Net;
using System.Linq;
class Sample {
 static void Main() {
 var vm=VirtualMachineManager.Connect(new IPEndPoint(IPAddress.Loopback,56456));
 try {vm.Suspend(); foreach(var t in vm.GetThreads()){var fs=t.GetFrames(); Console.WriteLine("thread "+t.Name+" frames "+fs.Length);
 foreach(var f in fs.Take(12)){Console.WriteLine(f.Method.Name+" line "+f.LineNumber);if(f.Method.Name=="TryDependency"||f.Method.Name=="BuildDependencies")foreach(var l in f.GetVisibleVariables().Where(l=>l.Name=="i"||l.Name=="why"||l.Name=="yaw"||l.Name=="site"||l.Name=="anchor")){var v=f.GetValue(l);Console.WriteLine(l.Name+" = "+(v is StringMirror?((StringMirror)v).Value:v is StructMirror?string.Join(",",((StructMirror)v).Fields.Select(q=>q.ToString()).ToArray()):v.ToString()));}}}}
 finally {vm.Detach();}
 }}
