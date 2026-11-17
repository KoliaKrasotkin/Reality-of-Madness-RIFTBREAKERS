$ErrorActionPreference='Stop'
$backup=$PSScriptRoot
$script:checks=0
function Check($condition,$message){if(-not $condition){throw "FAIL $message"};$script:checks++;Write-Output "PASS $message"}
function Posts($text){
 $part=[regex]::Match($text,'(?s)  Posts:\r?\n(.*?)  Sectors:').Groups[1].Value
 @([regex]::Matches($part,'(?ms)^  - Id: (?<id>[^\r\n]+).*?(?=^  - Id: |\z)') | ForEach-Object {
  [pscustomobject]@{Id=$_.Groups['id'].Value.Trim();Raw=$_.Value.Trim();
   Sector=[regex]::Match($_.Value,'(?m)^    SectorId: (.+)$').Groups[1].Value.Trim();
   Group=[regex]::Match($_.Value,'(?m)^    EncounterId: (.+)$').Groups[1].Value.Trim();
   Enemy=[regex]::Match($_.Value,'Enemy: \{fileID: \d+, guid: ([a-f0-9]+),').Groups[1].Value}
 })
}
$fauna=Get-Content "$backup/FaunaOriginal.json" -Raw | ConvertFrom-Json
$before=Posts (Get-Content "$backup/PopulationExterior.asset" -Raw)
$currentText=Get-Content Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset -Raw
$after=Posts $currentText
Check ($before.Count -eq 34 -and $after.Count -eq 29) '34 -> 29 posts'
Check (($after.Id | Sort-Object -Unique).Count -eq 29) 'post identities remain unique'
Check (($after.Group | Sort-Object -Unique).Count -eq 18) '18 encounter identities'
$expectedRemoved=@('jardines-jabali','jardines-oso','jardines-ciervo','jardines-alce','santuario-lobo')
$removed=@($before | Where-Object Id -notin $after.Id)
Check (-not (Compare-Object ($removed.Id | Sort-Object) ($expectedRemoved | Sort-Object))) 'only five requested animal posts removed'
foreach($post in $after){
 $old=$before | Where-Object Id -eq $post.Id
 Check ($old.Raw -ceq $post.Raw) ("combat post unchanged: "+$post.Id)
 Check ($post.Enemy -notin $fauna.Guid) ("no wildlife: "+$post.Id)
}
$runtimeAssets=@(Get-ChildItem Assets -Recurse -Filter '*.asset' | Select-String 'guid: 23cc826b2f4840d190786bc60b18e52c' | Select-Object -ExpandProperty Path -Unique)
Check ($runtimeAssets.Count -eq 2) 'only Stage1 and Exterior population assets exist; no interior population configured'
foreach($path in $runtimeAssets){
 $text=Get-Content $path -Raw
 $catalog=[regex]::Match($text,'(?s)  CompatibleEnemies:\r?\n(.*?)  Posts:').Groups[1].Value
 Check (([regex]::Matches($catalog,'guid:')).Count -eq 6) ('six combat definitions in '+[IO.Path]::GetFileName($path))
 foreach($animal in $fauna){Check (-not $text.Contains($animal.Guid)) ('no '+$animal.Id+' reference in '+[IO.Path]::GetFileName($path))}
 foreach($post in (Posts $text)){Check ($catalog.Contains($post.Enemy)) ('native definition remains linked: '+$post.Id)}
}
$stage1Before=Posts (Get-Content "$backup/PopulationStage1.asset" -Raw)
$stage1After=Posts (Get-Content Assets/ROM/Resources/CiudadDelSol/PopulationStage1.asset -Raw)
Check ($stage1After.Count -eq 2) 'Stage1 still has exactly two combat posts'
for($i=0;$i -lt 2;$i++){Check ($stage1Before[$i].Raw -ceq $stage1After[$i].Raw) ('Stage1 post unchanged: '+$stage1After[$i].Id)}
$patrolBefore=[regex]::Matches((Get-Content "$backup/PopulationExterior.asset" -Raw),'(?m)^    PatrolPoints:\r?$').Count
$patrolAfter=[regex]::Matches($currentText,'(?m)^    PatrolPoints:\r?$').Count
Check ($patrolBefore -eq 7 -and $patrolAfter -eq 7) 'seven authored combat patrols preserved'
$sectorPart=[regex]::Match($currentText,'(?s)  Sectors:\r?\n(.*?)  RecoveryAreas:').Groups[1].Value
$sum=0
foreach($m in [regex]::Matches($sectorPart,'(?ms)^  - Id: (?<id>[^\r\n]+).*?^    Maximum: (?<max>\d+)')){
 $id=$m.Groups['id'].Value.Trim();$cap=[int]$m.Groups['max'].Value;$sum+=$cap
 Check ($cap -eq @($after | Where-Object Sector -eq $id).Count) ('sector cap equals combat posts: '+$id)
}
Check ($sum -eq 29 -and $currentText -match '(?m)^  GlobalMaximum: 29\r?$') 'sector total and global cap = 29'
$log=Get-Content "$backup/Editor_Baseline.log" -Raw
$lastRun=@([regex]::Matches($log,'CIUDAD_EXTERIOR GRUPO_(VALIDADO|BLOQUEADO) ([^/\r\n]+)/([^:\r\n]+):') | Select-Object -Last 23)
$approved=@($lastRun | Where-Object {$_.Groups[1].Value -eq 'VALIDADO'})
Check ($approved.Count -eq 13) 'baseline contains thirteen approved encounters'
$retainedApproved=0;$removedApproved=0
foreach($m in $approved){
 $id=$m.Groups[3].Value.Trim();$old=@($before | Where-Object Group -eq $id);$now=@($after | Where-Object Group -eq $id)
 if($now.Count -eq 0){Check (@($old | Where-Object Enemy -notin $fauna.Guid).Count -eq 0) ('approved group removed ONLY because animal: '+$id);$removedApproved++}
 else{$retainedApproved++;Check ($now.Count -eq $old.Count) ('approved combat encounter retained: '+$id)}
}
Check ($retainedApproved -eq 10 -and $removedApproved -eq 3) 'ten approved combat encounters retained; three approved animal encounters explicitly removed'
Check ((Get-Content Assets/ROM/Scripts/CiudadDelSol/CiudadDelSolPopulation.cs -Raw).Contains('Require(definition && !definition.IsWildlife')) 'CiudadDelSol-only runtime catalog guard present'
Write-Output "TOTAL $script:checks PASS; static configuration checks, not Play Mode"
