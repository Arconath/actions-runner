# Arconath Actions Runner documentation

This is a maintained fork of the upstream GitHub Actions runner for the
private Arconath JIT fleet. It is not a general-purpose hosted-runner
distribution.

| Concern | Document |
| --- | --- |
| Fork identity and patch lifecycle | [`../ARCONATH_PATCHES.md`](../ARCONATH_PATCHES.md) |
| AI/agent orientation | [`AI-CONTEXT.md`](AI-CONTEXT.md) |
| Current maturity and blockers | [`STATUS.md`](STATUS.md) |
| Upstream runner behavior | [`../README.md`](../README.md) and the upstream `docs/` |
| Organization JIT operating model | [Arconath organization JIT operating model](https://github.com/Arconath/.github/blob/main/runner/README.md) |

If upstream workflow files are retained under `.github/upstream-workflows/`,
they are only for sync review and remain outside GitHub's active workflow
directory. The current maintained branch does not need to carry those copies.
Do not use upstream hosted-runner, publication, or deployment examples as
Arconath authorization.
