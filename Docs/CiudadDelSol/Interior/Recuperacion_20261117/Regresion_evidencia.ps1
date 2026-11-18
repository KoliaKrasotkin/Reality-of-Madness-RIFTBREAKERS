param([string]$ProjectRoot = (Get-Location).Path)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $ProjectRoot 'Assets/ROM/Editor/CiudadDelSolWalkEvidence.cs')
$options=[System.Text.Json.JsonSerializerOptions]::new()
$options.IncludeFields=$true
$options.WriteIndented=$true
$source=Join-Path $ProjectRoot 'Backups/CiudadDelSol/Interior/WarriorWalk_20261117T184352415Z/Recorrido_Warriors.json'
$shaBefore=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$raw=[IO.File]::ReadAllText($source)
$results=[Collections.Generic.List[string]]::new()
function Read-Capture { [System.Text.Json.JsonSerializer]::Deserialize($raw,[ROM.EditorTools.CiudadDelSolWalkEvidence+Capture],$options) }
function Analyze($capture,$prior=$null) { [ROM.EditorTools.CiudadDelSolWalkEvidence]::Analyze($capture,$prior) }
function Assert($condition,[string]$name) { if(!$condition){throw ('FALLO: '+$name)}; $results.Add($name) }
function Point($x,$y,$z) { $p=[ROM.EditorTools.CiudadDelSolWalkEvidence+Point]::new(); $p.x=$x;$p.y=$y;$p.z=$z;return $p }
function Sample($seconds,$p) {
  $s=[ROM.EditorTools.CiudadDelSolWalkEvidence+Sample]::new()
  $s.character=3;$s.seconds=$seconds;$s.position=$p;$s.support=$true;$s.nav=$true;$s.grounded=$true;$s.collisionFlags=4;$s.edge=2
  return $s
}
function Synthetic([int]$last=8,[int]$start=0) {
  $c=Read-Capture;$c.samples.Clear();$c.events.Clear();$c.result='DATOS_SINTETICOS_SOLO_REGRESION'
  $c.initialCheckpoint=$start
  $c.participants[0].initialPosition=Point $c.checkpoints[$start].x -16 $c.checkpoints[$start].z
  $c.samples.Add((Sample 0 $c.participants[0].initialPosition))
  [double]$t=0
  for($i=$start+1;$i -le $last;$i++) {
    $a=$c.checkpoints[$i-1];$b=$c.checkpoints[$i]
    $distance=[Math]::Sqrt([Math]::Pow($b.x-$a.x,2)+[Math]::Pow($b.z-$a.z,2))
    $n=[Math]::Ceiling($distance/.4)
    for($j=1;$j -le $n;$j++) { $t+=.1;$u=$j/$n;$p=Point ($a.x+($b.x-$a.x)*$u) -16 ($a.z+($b.z-$a.z)*$u);$c.samples.Add((Sample $t $p)) }
  }
  return $c
}
$actual=Analyze (Read-Capture)
Assert ($actual.routeValid -and !$actual.complete -and $actual.people[0].next -eq 8) 'Captura real: conserva 0–7; no inventa el punto final 8'
Assert ($actual.stoppedOutsideXZ -and !$actual.stoppedWrongHeight) 'Cierre real clasificado como salida XZ; no se inventa una caída'
Assert ($actual.people[0].sampleCount -eq 2181 -and $actual.people[0].supportFailures -eq 0 -and $actual.people[0].navFailures -eq 0) 'Captura real: 2181 muestras sin fallos de apoyo/NavMesh'
Assert ($actual.people[0].nextClosestDistance -gt 17) 'El comienzo outbound no se recicla como llegada final'
Assert ($actual.people[0].gaps.Count -eq 1 -and $actual.physicalWarnings) 'Hueco real de 1,708 s permanece visible'
Assert ((Analyze (Synthetic)).complete) 'Secuencia completa ordenada y continua acredita nueve puntos'
Assert (!(Analyze (Synthetic 7)).complete) 'Punto final ausente sigue pendiente'
$c=Synthetic 0;$c.samples.Add((Sample .1 (Point 175 -16 99)))
Assert ((Analyze $c).people[0].next -eq 1) 'Salto abrupto no acredita una llegada'
$c=Synthetic 1
foreach($s in $c.samples){if([Math]::Abs($s.position.x-174) -lt 1){$s.position=Point 173 -16 99}}
Assert ((Analyze $c).people[0].next -eq 1) 'Distancia exactamente un metro no cumple el radio estricto'
$c.samples.Add((Sample ($c.samples[-1].seconds+.1) (Point 173.001 -16 99)))
Assert ((Analyze $c).people[0].next -eq 2) 'Distancia menor a un metro sí cumple sin ampliar tolerancias'
$c=Synthetic 1;foreach($s in $c.samples){$s.position.y=-12}
Assert (!(Analyze $c).complete -and (Analyze $c).people[0].next -le 1) 'No acredita otro nivel vertical'
$c=Synthetic;$c.requested=4
Assert (!(Analyze $c).routeValid) 'Un Warrior no se convierte en cuatro conexiones'
$continuation=Synthetic 8 7
Assert (!(Analyze $continuation).complete) 'Continuación sin fuente acreditada no completa el informe'
$joined=Analyze $continuation $actual
Assert ($joined.complete -and $joined.verifiedPrefix -and $joined.physicalWarnings) 'Solo tramo 7→8 permite consolidar, conservando límites del original'
$continuation.participants[0].initialPosition=Point 170 -16 99
Assert (!(Analyze $continuation $actual).complete) 'Continuación fuera del ancla no acepta el prefijo'
$c=Synthetic 7;$terminal=Sample ($c.samples[-1].seconds+.1) $c.checkpoints[8];$terminal.physicalNotChecked=$true;$c.samples.Add($terminal)
Assert (!(Analyze $c).complete) 'Observación terminal sin física no acredita puntos'
Assert ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -eq $shaBefore) 'JSON original intacto por SHA-256'
$output=Join-Path $ProjectRoot 'Backups/CiudadDelSol/Interior/Recuperacion_20261117'
[IO.File]::WriteAllText((Join-Path $output 'Analisis_evidencia.json'),[System.Text.Json.JsonSerializer]::Serialize($actual,$options))
@{testsPassed=$results.Count;checks=$results;sourceSHA256=$shaBefore;utc=[DateTime]::UtcNow.ToString('o');unityPlayModeExecuted=$false}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $output 'Regresiones.json') -Encoding utf8
Write-Output ('Regresiones aprobadas: '+$results.Count)
