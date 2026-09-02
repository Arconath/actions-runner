#!/usr/bin/env python3
"""Verify the private-runner trust boundary for the maintained fork workflow.

This is intentionally a small source-level guard. It runs before any build
step, and is also suitable for local review on a machine without a runner.
GitHub branch protection remains the authority; the workflow must skip when
the selected ref is not protected.
"""

from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "arconath-security.yml"
UPSTREAM_WORKFLOW_ROOT = ROOT / ".github" / "upstream-workflows"
EXPECTED_ACTIVE_WORKFLOWS = {"arconath-security.yml"}
EXPECTED_RUNNER = (
    "runs-on:\n"
    "      group: arconath-jit\n"
    "      labels: [self-hosted, linux, x64, arconath-jit, rootless-buildkit]"
)
text = WORKFLOW.read_text(encoding="utf-8")


def require(fragment: str) -> None:
    if fragment not in text:
        raise SystemExit(f"missing protected-source contract: {fragment}")


for fragment in (
    "branches: [main]",
    "github.repository == 'Arconath/actions-runner'",
    "github.ref == 'refs/heads/main'",
    "github.ref_type == 'branch'",
    "github.ref_protected == true",
    "github.event_name == 'push' || github.event_name == 'workflow_dispatch'",
    '[[ "$GITHUB_REPOSITORY" == "$EXPECTED_REPOSITORY" ]]',
    '[[ "$GITHUB_REF_PROTECTED" == true ]]',
):
    require(fragment)

active_workflows = sorted(
    path for pattern in ("*.yml", "*.yaml") for path in (ROOT / ".github" / "workflows").glob(pattern)
)
if {path.name for path in active_workflows} != EXPECTED_ACTIVE_WORKFLOWS:
    raise SystemExit("active workflow set drifted; upstream workflow copies must remain inactive")
if UPSTREAM_WORKFLOW_ROOT.exists():
    if not UPSTREAM_WORKFLOW_ROOT.is_dir():
        raise SystemExit("upstream workflow archive path is not a directory")
    if {path.name for path in UPSTREAM_WORKFLOW_ROOT.iterdir()} & EXPECTED_ACTIVE_WORKFLOWS:
        raise SystemExit("an upstream workflow has crossed into the active workflow namespace")
if EXPECTED_RUNNER not in text:
    raise SystemExit("active workflow must use the canonical private runner group and labels")
if "persist-credentials: false" not in text:
    raise SystemExit("active workflow must not persist checkout credentials")
for action in re.findall(r"^\s*uses:\s*([^\s#]+)", text, flags=re.MULTILINE):
    if action.startswith("./"):
        continue
    if not re.fullmatch(r"[^@\s]+@[0-9a-f]{40}", action):
        raise SystemExit(f"active action is not pinned to a commit: {action}")
if re.search(r"^\s*[A-Za-z0-9_-]+:\s*write\s*$", text, flags=re.MULTILINE):
    raise SystemExit("active runner workflow must not request writable permissions")

if re.search(r"^\s*pull_request\s*:", text, flags=re.MULTILINE):
    raise SystemExit("pull-request source cannot execute on private runner")
if re.search(r"source_commit|github\.event\.pull_request|inputs\.", text):
    raise SystemExit("arbitrary workflow source selection is forbidden")
if re.search(r"arconath/\*\*|v\*[-.]arconath", text):
    raise SystemExit("broad branch/tag trigger is forbidden")

print("Arconath runner workflow is protected-ref and repository pinned")
