$ErrorActionPreference='Stop'
$dir='Backups/CiudadDelSol/Poblacion/Etapa2_20261116'
$plan=Get-Content "$dir/ExteriorPlan.json" -Raw | ConvertFrom-Json
$culture=[Globalization.CultureInfo]::InvariantCulture
function N($n){ ([double]$n).ToString('0.###',$culture) }
function V($p){ '{x: '+(N $p[0])+', y: '+(N $p[1])+', z: '+(N $p[2])+'}' }
$catalog=@{}
Get-ChildItem Assets/ROM/Enemies -Recurse -Filter '*.asset' | ForEach-Object {
 $id=[regex]::Match([IO.File]::ReadAllText($_.FullName),'(?m)^  Id: (.+)$').Groups[1].Value.Trim()
 if($id){$catalog[$id]=(Select-String -LiteralPath ($_.FullName+'.meta') -Pattern '^guid:').Line.Split(' ')[1]}
}
$guid=(Select-String -LiteralPath Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPopulationDefinition.cs.meta -Pattern '^guid:').Line.Split(' ')[1]
$lines=@('%YAML 1.1','%TAG !u! tag:unity3d.com,2011:','--- !u!114 &11400000','MonoBehaviour:',
'  m_ObjectHideFlags: 0','  m_CorrespondingSourceObject: {fileID: 0}','  m_PrefabInstance: {fileID: 0}',
'  m_PrefabAsset: {fileID: 0}','  m_GameObject: {fileID: 0}','  m_Enabled: 1','  m_EditorHideFlags: 0',
('  m_Script: {fileID: 11500000, guid: '+$guid+', type: 3}'),'  m_Name: PopulationExterior',
'  m_EditorClassIdentifier: Assembly-CSharp::ROM.CiudadDelSol.CiudadDelSolPopulationDefinition',
'  Stage: Etapa 2 - exterior - siete sectores','  CompatibleEnemies:')
foreach($id in ($plan.posts.enemy | Sort-Object -Unique)){
 if(-not $catalog.ContainsKey($id)){throw "Missing native enemy: $id"}
 $lines+='  - {fileID: 11400000, guid: '+$catalog[$id]+', type: 2}'
}
$lines+='  Posts:'
foreach($p in $plan.posts){
 $sector=$plan.sectors | Where-Object id -eq $p.sector
 $rank=0;if($null -ne $p.rank){$rank=$p.rank}
 $hp=1;if($null -ne $p.hp){$hp=$p.hp}
 $damage=1;if($null -ne $p.damage){$damage=$p.damage}
 $seconds=180;if($p.enemy -eq 'little-demon'){$seconds=90};if($p.enemy -eq 'broken-ember-oracle'){$seconds=120}
 if($rank -eq 1){$seconds=240};$respawn=1;if($rank -ge 2){$respawn=0;$seconds=900}
 $root='Dungeon_Ciudad_del_Sol_Generated';if($p.root){$root=$p.root}
 $ancestor=$sector.name;if($p.ancestor){$ancestor=$p.ancestor}
 $activation=110;if($p.id -in 'entrada-diablillo-01','jardines-oraculo-01'){$activation=150}
 $lines+=@(('  - Id: '+$p.id),('    SectorId: '+$p.sector),('    EncounterId: '+$p.group),
 ('    Enemy: {fileID: 11400000, guid: '+$catalog[$p.enemy]+', type: 2}'),('    Rank: '+$rank),('    Position: '+(V $p.p)),
 ('    AnchorPath: "'+$p.anchor+'"'),('    PlayableRoot: '+$root),('    PlayableAncestor: '+$ancestor),
 '    Yaw: 180','    Level: 1',('    Respawn: '+$respawn),('    RespawnSeconds: '+$seconds),
 ('    ActivationRadius: '+$activation),('    HealthMultiplier: '+(N $hp)),('    DamageMultiplier: '+(N $damage)))
 if($p.patrol){$lines+='    PatrolPoints:';foreach($point in $p.patrol){$lines+='    - '+(V $point)}}else{$lines+='    PatrolPoints: []'}
 $lines+='    PatrolWaitSeconds: 1.5'
}
$lines+='  Sectors:'
foreach($s in $plan.sectors){$label=$s.id.Substring(3);$lines+=@(('  - Id: '+$s.id),('    PlayableAncestor: '+$s.name),
 ('    RecoveryAreaId: '+$label+'-descanso'),('    Maximum: '+$s.maximum))}
$lines+='  RecoveryAreas:'
foreach($a in $plan.recovery){$lines+=@(('  - Id: '+$a.id),('    Position: '+(V $a.p)),('    Radius: '+(N $a.radius)))}
$lines+=@('  GlobalMaximum: 36','  TickSeconds: 0.5','  RetrySeconds: 5','  PlayerExclusionRadius: 25',
'  CombatExclusionRadius: 40','  CombatQuietSeconds: 10','  ScaleForHumanPlayers: 1','  ValidateExteriorEncounters: 1',
'  AccessibilityOrigin: {x: 30, y: -2, z: -206}','  MaximumInitialGroupSize: 3')
$output='Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset'
if(Test-Path -LiteralPath $output){Copy-Item -LiteralPath $output -Destination "$dir/PopulationExterior.before-regeneration.asset"}
[IO.File]::WriteAllText($output,($lines -join "`n")+"`n")
if(-not(Test-Path -LiteralPath "$output.meta")){
 [IO.File]::WriteAllText("$output.meta","fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n")
}
Write-Output "Created $output; $($plan.posts.Count) posts; no scene or NavMesh writes."
