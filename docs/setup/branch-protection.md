# Branch Protection & Repo Settings (T-001)

> `gh` CLI مش متثبّت على الجهاز، فالإعدادات دي تتعمل مرة واحدة من واجهة GitHub:
> **Settings → Branches → Add branch ruleset** (أو Classic branch protection).

## القواعد المطلوبة لـ `main` و `dev`

| Setting | Value |
|---|---|
| Require a pull request before merging | ✅ |
| Required approvals | 1 |
| Dismiss stale approvals on new commits | ✅ |
| Require status checks to pass | ✅ → اختَر `build-and-test` |
| Require branches to be up to date | ✅ |
| Require conversation resolution | ✅ |
| Require linear history | ✅ (لـ main) |
| Do not allow bypassing | ✅ |
| Block force pushes | ✅ |

## Branching model
```
main      ← production (protected, releases only)
 └─ dev   ← integration (protected, كل الـ features بتتدمج هنا)
     └─ feature/T-0xx-short-description
     └─ fix/T-0xx-short-description
```

## لو حابب تعملها بالـ CLI بعد تثبيت gh
```bash
brew install gh && gh auth login

gh api -X PUT repos/Mahmod-mourad/nexaflow/branches/main/protection \
  --input docs/setup/branch-protection-main.json
```
الـ `status check` المطلوب اسمه `build-and-test` (جاي من CI في T-006).
