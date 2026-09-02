# Actions Runner fork status and blockers

**Reviewed:** 2026-09-02

## Lifecycle

Maintained Arconath fork for the private, ephemeral JIT runner. It is not an
independent public product or a generic hosted-runner distribution.

| Area | State | Evidence | Meaning |
| --- | --- | --- | --- |
| Upstream identity | PASS (tracked) | `ARCONATH_PATCHES.md` | Upstream tag and commit are explicit |
| Credential sealing | IMPLEMENTED FOUNDATION | `ARCONATH_PATCHES.md`, `src/Runner.Listener` | Requires build and live security evidence |
| Active workflow boundary | PASS (source) | `scripts/verify-arconath-workflow.py` | Only protected main may schedule the private runner |
| Patch/security tests | PARTIAL | `src/Test`, patch queue | Fresh Linux build and current-run evidence remain required |
| Private JIT integration | BLOCKED | organization runner contract | Host, manager, egress, cleanup, and real-JIT proof are external |
| Artifact publication | BLOCKED | release-control contract | Signing, provenance, registry, and rollback gates are external |

## Open blockers

- build and review an immutable fork artifact;
- run real JIT credential-order, absence, cleanup, procfs, cgroup, and
  rootless BuildKit/Podman checks on the private fleet;
- complete upstream security-sync and compatibility evidence;
- publish only through protected release-control after the full packet passes.
