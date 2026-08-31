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

if re.search(r"^\s*pull_request\s*:", text, flags=re.MULTILINE):
    raise SystemExit("pull-request source cannot execute on private runner")
if re.search(r"source_commit|github\.event\.pull_request|inputs\.", text):
    raise SystemExit("arbitrary workflow source selection is forbidden")
if re.search(r"arconath/\*\*|v\*[-.]arconath", text):
    raise SystemExit("broad branch/tag trigger is forbidden")

print("Arconath runner workflow is protected-ref and repository pinned")
