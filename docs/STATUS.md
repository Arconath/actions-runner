# Actions Runner fork status and blockers

**Reviewed:** 2026-08-31

## Lifecycle

Maintained Arconath fork for the private JIT runner. It is not an independent
public product and is not a generic hosted-runner distribution.

## Evidence register

| Area | State | Evidence | Meaning |
| --- | --- | --- | --- |
| Upstream baseline | PASS (tracked) | `ARCONATH_PATCHES.md`, repository branch metadata | Upstream tag/commit is explicit |
| Credential sealing patch | IMPLEMENTED FOUNDATION | `ARCONATH_PATCHES.md`, `src/Runner.Listener` | Requires fork build and pre-listener/real-job evidence |
| Patch/security tests | PARTIAL until fresh run | `src/Test`, patch docs | Test execution must be recorded for current build |
| Private JIT fleet integration | BLOCKED until live proof | [`../../../organization/runner/README.md`](../../../organization/runner/README.md) | Host, App, group, egress, cleanup, and smoke are external |
| Upstream update lifecycle | OPEN | patch queue | Rebase/security updates need owner review and compatibility evidence |

## Open blockers

- build/publish an operator-approved immutable fork artifact;
- run real JIT credential-order/absence and cleanup tests on the private fleet;
- complete host egress, rootless BuildKit/Podman, cgroup, `/proc`, and off-host
  log evidence;
- maintain upstream security updates without losing Arconath patch behavior.

## Next milestone

Close the fork test and provenance packet, then validate one disposable JIT job
through the organization manager before any broader runner rollout.
