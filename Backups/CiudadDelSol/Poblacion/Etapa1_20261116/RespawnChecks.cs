using System;
using ROM.CiudadDelSol;
class RespawnChecks {
 static int count;
 static void Check(bool yes,string name){if(!yes)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
 static void Throws(Action action,string name){try{action();}catch(InvalidOperationException){Check(true,name);return;}throw new Exception(name);}
 static void Main(){
  var normal=new CiudadDelSolRespawnSchedule();
  Check(normal.Due(0),"initial post is eligible");
  normal.Spawned();Check(!normal.Due(9999),"occupied post never schedules a duplicate");
  Throws(()=>normal.Spawned(),"double reservation is rejected");
  normal.Lost(100,90,true);Check(normal.ReadyAt==190,"death starts exact configured cooldown");
  Check(!normal.Due(189.999),"no early respawn");
  Check(normal.Due(190),"deadline is inclusive");
  normal.Lost(180,90,true);Check(normal.ReadyAt==190,"duplicate death callback does not move deadline");
  normal.Spawned();Check(!normal.Due(1000),"respawn reclaims the reservation");
  normal.Lost(250,90,true);Check(normal.ReadyAt==340,"next lifecycle gets a new cooldown");
  var boss=new CiudadDelSolRespawnSchedule();boss.Spawned();boss.Lost(0,90,false);
  Check(!boss.Due(1000000),"boss cannot auto-respawn");
  Throws(()=>boss.Spawned(),"defeated boss cannot silently reset");
  var missing=new CiudadDelSolRespawnSchedule();missing.Spawned();missing.Lost(10,120,true);
  Check(!missing.Due(10)&&missing.ReadyAt==130,"unexpected removal still requires full cooldown");
  var invalid=new CiudadDelSolRespawnSchedule();invalid.Spawned();
  try{invalid.Lost(0,double.NaN,true);throw new Exception("accepted NaN");}catch(ArgumentOutOfRangeException){Check(true,"invalid timer rejected");}
  Check(invalid.Phase==CiudadDelSolRespawnSchedule.State.Occupied,"invalid timer leaves reservation intact");
  Console.WriteLine("TOTAL "+count+" PASS; pure scheduling only, no Unity/physics/Mirror simulation");
 }
}