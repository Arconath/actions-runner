# AI context: Arconath Actions Runner fork

**Reviewed:** 2026-08-31

## Identity

This checkout is based on upstream Actions Runner v2.337.0 and carries the
Arconath `credential-seal` patch. It is the executable listener component for
the private one-job `arconath-jit` fleet; the organization runner manager and
host installation remain in `platform/organization/runner`.

## Patch boundary

`ARCONATH_PATCHES.md` is the source of truth for the fork rationale, upstream
tag/commit, owner, patch files, tests, and release requirements. The central
security idea is: consume a bounded one-shot JIT configuration file, seal it
against replacement, unlink credential/RSA files before listening for jobs,
and verify process-dump protections in the pinned Linux layout.

## Invariants

- Never weaken JIT credential ordering, file ownership/mode, one-job root,
  unlink/handle semantics, dumpability, or pre-listener absence checks.
- Keep the upstream remote read-only and record every Arconath patch with a
  rebase/sync procedure, owner, compatibility test, and rollback plan.
- The listener must not receive JIT secrets in argv or environment and must not
  allow workflow code to read its ancestor's sensitive `/proc` state.
- This fork is not a trust boundary for untrusted public fork code; workflows
  must reject fork heads before scheduling on the private group.
- Preserve upstream security updates and supported-version cadence; do not
  carry a local patch without revalidation against the pinned upstream base.

## Verification

Use the source-build/test flow documented by the upstream checkout plus the
Arconath patch/security tests under `src/Test` and the fork patch queue. The
organization runner contract additionally requires a real JIT smoke, credential
file absence before `Listening for Jobs`, cleanup, and rootless BuildKit/Podman
evidence.

## Stop conditions

Do not publish a runner build, change runner group permissions, run a live JIT
job, or update the upstream baseline without explicit platform-owner approval
and an updated patch/provenance record. Never edit `platform/upstream`.
