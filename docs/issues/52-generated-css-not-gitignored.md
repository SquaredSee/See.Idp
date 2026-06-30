# 52 — Generated `.out.css` files are not excluded from source control

## Context

The `.gitignore` does not exclude `identity.out.css` or `admin.out.css`. The generated
CSS output files are committed to the repository.

This causes:

- Noisy diffs: every CSS change produces a diff against the minified output file in
  addition to the source file change
- Stale output risk: if someone edits a source CSS file without rebuilding, the
  committed output diverges from the source
- False source-of-truth: the generated file appears authoritative when it is not

## Fix

Add to `.gitignore`:

```
**/wwwroot/css/*.out.css
```

Delete the currently tracked files from the repository:

```bash
git rm src/See.Idp.Web/wwwroot/css/identity.out.css
git rm src/See.Idp.Web/wwwroot/css/admin.out.css
```

The MSBuild pipeline regenerates them on every build, so no developer workflow
is disrupted.

## Acceptance Criteria

- `*.out.css` is listed in `.gitignore`
- `identity.out.css` and `admin.out.css` are not tracked by git
- `dotnet build` still produces the output files locally

## Dependencies

None.
