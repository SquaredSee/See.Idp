# 40 — `EnableAuthenticator.cshtml` embeds model data into JS with `Html.Raw`

## Context

The QR code setup page injects the TOTP URI into a JavaScript string literal using
`Html.Raw`:

```html
<script>
    new QRCode(document.getElementById("qrcode"), {
        text: "@Html.Raw(Model.AuthenticatorUri)",
        width: 160,
        height: 160,
    });
</script>
```

`Html.Raw` bypasses HTML encoding but does not apply JavaScript string escaping.
The TOTP URI can legitimately contain characters that break a JS string literal —
double quotes in the issuer name, backslashes, or newlines. An email address
containing a double quote (valid per RFC 5321) would produce an invalid script,
silently failing authenticator setup for that user.

## Fix

Use `Json.Serialize` to produce a properly escaped JS value:

```html
<script>
    new QRCode(document.getElementById("qrcode"), {
      text: @Json.Serialize(Model.AuthenticatorUri),
      width: 160,
      height: 160
    });
</script>
```

Alternatively, pass the URI via a `data-` attribute and read it from the script,
keeping model data out of script blocks entirely.

## Acceptance Criteria

- `Html.Raw` is not used to embed model data into any JavaScript context
- QR code renders correctly for URIs containing special characters

## Dependencies

None.
