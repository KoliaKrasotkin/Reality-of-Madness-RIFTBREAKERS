$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0) { throw 'Run inside the project repository.' }
$hookPath = (git rev-parse --git-path hooks/pre-commit)
$sourcePath = Join-Path $repoRoot '.githooks/pre-commit'
if ((Test-Path -LiteralPath $hookPath) -and
    ((Get-FileHash -LiteralPath $hookPath).Hash -ne (Get-FileHash -LiteralPath $sourcePath).Hash)) {
    throw 'An existing custom pre-commit hook was found. Integrate the validator into it; it has not been overwritten.'
}
Copy-Item -LiteralPath $sourcePath -Destination $hookPath -Force
Write-Output 'Scene validation installed. Existing Git LFS hooks were preserved.'
