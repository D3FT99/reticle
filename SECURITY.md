# Security policy

## Supported versions

Only the latest release gets security fixes.

## Reporting a vulnerability

**Please don't open a public issue.** Report it privately instead:
[Security → Report a vulnerability](https://github.com/D3FT99/reticle/security/advisories/new).

Include what you found, how to reproduce it, and the Reticle and Windows versions you used. You should hear back
within a week.

## Why this matters for Reticle

Reticle runs with uiAccess, which lets it send input to elevated windows, and its installer trusts the Reticle
code-signing certificate as a root certificate on the machine. Issues in either area are treated as high severity:

- anything that lets another program drive Reticle's input or reach elevated windows through it
- anything that exposes or misuses the signing certificate or its private key
- installer or uninstaller behaviour that leaves the certificate trusted when it shouldn't be

If you think the signing key has been compromised, say so in the first line of your report.
