# Continuous Port Heist - source verification

Verified by GitHub Actions from comparison input `f64e4c5aa65c9459e1804552154a9ab2912ae31d`.

## Story/runtime output
```text
PASS: Without M13 and M15 the harbor is at full strength
PASS: Three launches at the breakwater when nothing thinned them or opened the gate
1365 story/runtime checks passed.
fetching Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3...
```
## Regression output
```text
PASS: Partial native failure still releases restart suppression
165 checks passed (stand-ins; live GTA validation still required).
```

Production build, parser, mission lint, location validator, authored-scene freshness, mission-map freshness and packaging also passed in this job.

Prebuilt SHA-256: `032b07ad69f1076dd3380d201b4419118d419d84a9b74b525c34b9da90323de9`.

No GTA installation was available. Physics, streaming, pathfinding, animations and live playthrough remain unverified. See CONTINUOUS-PORT-HEIST.md for the acceptance route.
