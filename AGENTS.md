# HoneyDrunk.Standards agent instructions

Own shared analyzers, build-transitive configuration and engineering conventions. Keep build policy here and reusable CI orchestration in HoneyDrunk.Actions. Verify package behavior with the existing consumer projects when changing analyzers or build assets; documentation does not itself enforce a compiler rule.

Start with [README.md](README.md) and the relevant source/tests. Project files and lockfiles own SDK, framework and dependency versions.
For package changes, also exercise the sample/consumer checks in [README.md](README.md).

Read the [shared engineering conventions](HoneyDrunk.Standards/docs/CONVENTIONS.md) and this repository's owning documentation before editing. Apply the parts relevant to this stack; preserve existing public contracts, dependency direction and repository-specific behavior. Verify shared capabilities in current code before reusing them; a catalog entry or scaffold is not an implemented integration.

Work within the selected request. Preserve unrelated changes and use a separate worktree when needed. Review the final diff, use Conventional Commits and ready-for-review PRs with exactly one accurate `Authorship:` line and a `Request:` line; include the authorship in commit trailers. Run meaningful checks for the affected behavior and report the reviewed/tested revision, failures and unrun checks. For documentation-only changes, check links, paths and instruction consistency. Preserve required checks and inspect actual latest-head Sonar new-code findings where analysis applies; do not suppress findings or weaken gates to obtain a pass. Legacy Grid Review is retired; do not restore its workers, queues or bypass labels. A configured replacement reviewer is not evidence of a completed review or enforcing merge check.

## Verification

From the repository root for code/build changes:

```sh
dotnet restore HoneyDrunk.Standards/HoneyDrunk.Standards.slnx
dotnet build HoneyDrunk.Standards/HoneyDrunk.Standards.slnx -c Release --no-restore
dotnet test HoneyDrunk.Standards/HoneyDrunk.Standards.slnx -c Release --no-build
```

Use the checked-in workflow and relevant test documentation for additional integration prerequisites, coverage and consumer checks. Do not use live resources or credentials merely to make a local check pass.
