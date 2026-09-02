# AI context: Arconath Actions Runner fork

**Reviewed:** 2026-09-02

## Identity

This checkout is based on upstream Actions Runner `v2.337.0` at
`397b032cbf865e9c3ddfab89d533ec19325e1273` and carries the Arconath
`credential-seal` patch. It is the executable listener for the private,
one-job `arconath-jit` fleet; the organization runner manager and host
installation remain outside this repository.

## Boundary invariants

- JIT configuration is file-backed, one-shot, bounded, and unlinked before the
  listener accepts work; credential and RSA files remain listener-private.
- Linux dumpability and procfs isolation are fail-closed; a missing syscall or
  verification step aborts startup.
- The listener must not receive JIT secrets in argv or environment.
- The job worker is a separate identity and must not read listener credentials
  or sensitive ancestor process state.
- The fork is not a trust boundary for public-fork code. Active workflows are
  repository-pinned, protected-ref-only, and use the private `arconath-jit`
  runner with rootless BuildKit/Podman.

## Source and release boundary

`ARCONATH_PATCHES.md` is the patch inventory and upstream-base authority.
`upstream` is fetch-only and its inspection clone is maintained separately in
`platform/upstream`; it is never a deployment source. The repository-owned
workflow is validation-only. Immutable artifact publication, signing, and
promotion belong to protected release-control after the live JIT and
compatibility packet passes.

## Verification

Run `python3 scripts/verify-arconath-workflow.py`, actionlint, the focused
credential-sealing L0 tests, and a Linux layout build. A real JIT smoke,
credential absence before `Listening for Jobs`, cleanup, runner registration,
rootless runtime, and provenance evidence are still required before rollout.
