# 50 — No CSS watch mode for local development

## Context

`package.json` defines only build scripts with no watch variant:

```json
"scripts": {
  "build:css:identity": "tailwindcss -i ./wwwroot/css/identity.css -o ./wwwroot/css/identity.out.css --minify",
  "build:css:admin":    "tailwindcss -i ./wwwroot/css/admin.css    -o ./wwwroot/css/admin.out.css    --minify",
  "build:css":          "npm run build:css:identity && npm run build:css:admin"
}
```

During frontend development, every CSS change requires manually re-running
`npm run build:css` or triggering a full `dotnet build`. There is no way to start a
watcher that rebuilds automatically on file change.

## Fix

Add watch scripts to `package.json`:

```json
"watch:css:identity": "tailwindcss -i ./wwwroot/css/identity.css -o ./wwwroot/css/identity.out.css --watch",
"watch:css:admin":    "tailwindcss -i ./wwwroot/css/admin.css    -o ./wwwroot/css/admin.out.css    --watch",
"watch:css":          "npm run watch:css:identity & npm run watch:css:admin"
```

Document `npm run watch:css` in the project README as the recommended command for
active CSS development.

## Acceptance Criteria

- `npm run watch:css` starts Tailwind watchers for both bundles
- CSS changes are reflected in the output files without a manual rebuild

## Dependencies

None.
