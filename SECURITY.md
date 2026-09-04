# Security Policy

## Supported release

Security fixes are applied to the latest published Phantom Installer release.

## Reporting a vulnerability

Please report security issues privately through GitHub's security reporting / private vulnerability reporting features when available. Do not publish credentials, personal Steam data, private CFG contents, or proof-of-concept material that could put users at risk in a public issue.

Phantom Installer is intended to work locally. It should not upload Steam userdata, CFG contents, hardware inventory, or generated signatures to external services.

## Trust boundaries

- Custom CFG files are user-selected local files.
- Generated CFG files use comment-only SHA-256 metadata for tamper detection; this is not a substitute for Authenticode or a secret-key digital signature.
- The Windows EXE currently requests administrator privileges because Steam/CS2 may live under protected directories.
- The release is not currently signed with a commercial Authenticode certificate.
