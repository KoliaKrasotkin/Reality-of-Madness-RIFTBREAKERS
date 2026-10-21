using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using UnityEngine;
namespace ROM.Horde {
public partial class Horde {
    public Transform TrainingSpot;
    Health _trainingTarget;float _trainingRetry;
    public override bool CanUseClassAbilities(Vector3 point)=>!IsSafe(point)||(TrainingSpot&&Vector3.Distance(point,TrainingSpot.position)<30);
    void TickTrainingTarget(){
        if(!TrainingSpot||Time.time<_trainingRetry)return;
        _trainingRetry=Time.time+1;
        if(_trainingTarget){var ai=_trainingTarget.GetComponent<BotAI>();if(ai)ai.Passive=true;return;}
        if(!GameManager.Players.Any(p=>p&&!p.BOT&&p.MyCharacter))return;
        var before=_aliveEnemies.Keys.ToArray();
        if(!SpawnEnemy(EnemyKind.Melee,TrainingSpot))return;
        _trainingTarget=_aliveEnemies.Keys.First(h=>!before.Contains(h));
        _aliveEnemies.Remove(_trainingTarget);_botByHealth.Remove(_trainingTarget);
        _trainingTarget.ServerMakeTrainingTarget();_trainingTarget.CharacterName="Muñeco de entrenamiento";
        var bot=_trainingTarget.GetComponent<BotAI>();if(bot)bot.Passive=true;
    }
}
}
