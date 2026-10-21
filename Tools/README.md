# Unity scene validation

Install the local pre-commit guard with `./Tools/Install-GitValidation.ps1` from PowerShell. Existing LFS hooks remain intact. Requires Python 3.

- `python Tools/validate_unity_scenes.py`: checks the staged index.
- `python Tools/validate_unity_scenes.py --revision HEAD`: checks the committed revision.

The LFS objects must be available locally. Fetch first-party scenes if necessary:

```sh
git lfs fetch --include='Assets/ROM/**/*.unity' --exclude='' origin HEAD
```

The validator checks first-party ROM scenes, not vendor sample scenes. Both pointer structure and real Unity YAML contents are checked. A valid-looking LFS pointer can itself reference a corrupted pointer file: validating only the outer pointer is insufficient.

The GitHub workflow runs the revision check on pushes and pull requests once committed. Branch protection must require that check if the team wants GitHub to block bad merges.
