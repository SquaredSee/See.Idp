# 51 — CSS is minified in all configurations including development

## Context

All `package.json` build scripts pass `--minify` unconditionally:

```json
"build:css:identity": "tailwindcss -i ... -o ... --minify",
"build:css:admin":    "tailwindcss -i ... -o ... --minify"
```

The MSBuild `NpmBuild` target invokes `npm run build:css` regardless of build
configuration. In development, browser DevTools shows minified single-line CSS with
no source maps, making it impractical to inspect which rules apply to an element or
to debug component class output.

## Fix

Add a development build script that omits `--minify`:

```json
"build:css:dev": "tailwindcss -i ./wwwroot/css/identity.css -o ./wwwroot/css/identity.out.css && tailwindcss -i ./wwwroot/css/admin.css -o ./wwwroot/css/admin.out.css"
```

Update the MSBuild `NpmBuild` target to choose based on configuration:

```xml
<Exec Condition="'$(Configuration)' == 'Release'" Command="npm run build:css" />
<Exec Condition="'$(Configuration)' != 'Release'" Command="npm run build:css:dev" />
```

## Acceptance Criteria

- `dotnet build` (Debug) produces unminified CSS
- `dotnet build -c Release` produces minified CSS
- Browser DevTools shows readable CSS in development

## Dependencies

None.
