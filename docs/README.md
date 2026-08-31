# Arconath Actions Runner documentation

This is a maintained fork of the upstream GitHub Actions runner. Arconath
changes are intentionally narrow and exist to support the private, ephemeral
JIT runner policy—especially credential sealing before job execution.

## Read by concern

| Concern | Document |
| --- | --- |
| Fork identity and patch lifecycle | [`../ARCONATH_PATCHES.md`](../ARCONATH_PATCHES.md) |
| Upstream runner behavior | [`../README.md`](../README.md) and the upstream docs under `docs/` |
| Arconath JIT operating model | [`../../../organization/runner/README.md`](../../../organization/runner/README.md) |
| AI/agent orientation | [`AI-CONTEXT.md`](AI-CONTEXT.md) |
| Current status and blockers | [`STATUS.md`](STATUS.md) |
| Existing upstream automation docs | [`automate.md`](automate.md), [`contribute.md`](contribute.md) |

Do not use the generic upstream automation examples as authorization to bypass
the Arconath organization runner manager or the rootless/credential-sealing
contract.

## Architecture diagram

- [Rendered architecture diagram](diagrams/architecture.svg)
- [Editable Mermaid source](diagrams/architecture.mmd)
