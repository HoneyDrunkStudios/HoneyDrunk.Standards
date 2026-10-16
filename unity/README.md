# Unity standards profile

This opt-in source-built profile is separate from the server NuGet packages. It does not change their Roslyn 4.14 baseline, net analyzers, StyleCop, build-transitive defaults, language features, test stack, or package publication. Unity's compiler does not consume those MSBuild imports.

## Supported baseline and distribution

Target: Unity 6000.6, C# 9, .NET Standard 2.0 analyzer assemblies and Roslyn 4.3.0. Build tooling requires the exact .NET SDK pinned in `global.json` (10.0.402) and Python 3. No Unity license is needed to build this distribution. An actual Editor import/test run still requires the project's pinned Unity editor and license.

From a checkout pinned to a reviewed Standards commit:

```sh
python3 unity/scripts/build.py --output artifacts/unity
```

This restores exact locked dependencies, builds and executes the regression harness using the **4.3 compiler API**, and stages:

- `HoneyDrunk.Standards.Unity.Analyzers.dll` and `Microsoft.Unity.Analyzers.dll`
- Disabled-runtime-import `.dll.meta` files with the exact `RoslynAnalyzer` asset label
- `HoneyDrunk.Unity.ruleset`, licenses and SHA-256 `manifest.json`

The DLLs are deterministic builds; the manifest contains no timestamps or machine paths. Consume the generated directory from a pinned checkout in CI, or commit the reviewed DLL distribution plus manifest/licenses to the game repository. Do not fetch a moving branch, publish a NuGet package, or copy compiler/workspaces dependencies into Unity. Do not copy the server analyzer binary.

Microsoft.Unity.Analyzers is pinned to **1.28.0**, upstream source commit `fd250c39b858921df0f9479926338b0bad6259fb`. Its analyzer assembly references are checked against the 4.3 API ceiling, and the harness loads and executes its analyzers on Roslyn 4.3, including positive/negative lifecycle, Unity null handling and serialization fixtures. Merely targeting netstandard2.0 would not establish compiler compatibility. The MIT license is retained. Compiler upgrade decisions require updating and rerunning this compatibility gate.

## Unity import and ownership scope

1. Put both DLLs and matching `.meta` files **once**, in a folder beneath an owned foundational `.asmdef` (for example `Assets/Game/Core/Analyzers`). Assemblies referencing that assembly also receive the analyzers. Do not install at `Assets` root or under an unscoped shared folder. Do not copy the same asset GUID into multiple folders.
2. Copy `HoneyDrunk.Unity.ruleset` next to **each** owned `.asmdef` requiring this policy, including Runtime, Editor, EditMode and PlayMode tests. Keep only one ruleset beside each asmdef. No `.asmdef` JSON property or generated `.csproj` edit is needed. Preserve existing rules through a reviewed merge if a ruleset already exists.
3. Keep third-party/vendor sources and generated code in separately owned assemblies that do not reference the owned foundational assembly. Generated `.g.cs` files and auto-generated headers are recognized by the analyzers. The custom HD0051 also excludes exact `Generated`, `ThirdParty`, `Plugins`, and `Packages` directory segments; this is not a global rule suppression. Do not put handwritten code into these paths to evade checks. Microsoft analyzer scope for third-party code is controlled by assembly boundaries, not this custom filter.
4. Reimport the DLLs and an owned script, and verify in Plugin Inspector that Any Platform and Editor are disabled and `RoslynAnalyzer` is present. Run the game repository's licensed Editor import and test workflow. The host-only harness does not prove asset importer behavior, real Unity API resolution, platform builds or execution of Unity tests.

## Policy

- HD0051 is an error for assembly names ending exactly in `.Tests`, `.Tests.EditMode`, or `.Tests.PlayMode`. It recognizes `System.Threading.Thread.Sleep(int)` and `Sleep(TimeSpan)` semantically, including aliases/static imports. Runtime and tooling assemblies are unaffected. Unity lacks the server `build_property.HD_IsGridTestProject` switch; naming is the explicit Unity opt-in contract.
- UNT0006 (incorrect Unity message signature), UNT0007/UNT0008 (Unity object null coalescing/propagation), and UNT0013 (invalid/redundant serialized field) are errors in the per-assembly ruleset.
- All other Microsoft.Unity.Analyzers diagnostics retain their upstream default severity. No broad `NoWarn`, disable-all, language upgrade, nullable policy change, or global warnings-as-errors is imposed on Unity projects.
- Unity NUnit tests remain Unity NUnit tests. The server xUnit metapackage is never imported into Unity. A separate host test harness may use its own established stack.

For a standalone host `.csproj`, import both generated DLLs using `Analyzer` items and set `CodeAnalysisRuleSet` to the generated ruleset. That checks the source linked into that project, not Editor-only code or Unity's compilation pipeline.

## Verification

```sh
python3 unity/scripts/build.py --output artifacts/unity
```

The fixture runner checks compiler-reference compatibility, actual analyzer loading, ruleset error severity, both Sleep overloads, static imports, valid code, runtime isolation, similarly named methods, generated paths/header/extensions, third-party exclusions and invalid/valid Unity messages, Unity null handling and serialization. Any analyzer exception or unexpected error fails the run. Distribution fixtures verify hashes, importer labels/platform settings, unique GUIDs, ruleset and licenses, and reject deliberate corruption. Lockfiles pin transitive package hashes. The distribution can be built twice and its manifest compared to check reproducibility.

Existing server validation remains:

```sh
dotnet restore HoneyDrunk.Standards/HoneyDrunk.Standards.slnx
dotnet build HoneyDrunk.Standards/HoneyDrunk.Standards.slnx -c Release --no-restore
dotnet test HoneyDrunk.Standards/HoneyDrunk.Standards.slnx -c Release --no-build
```

## References

- [Unity 6.6 analyzer compiler baseline](https://docs.unity3d.com/6000.6/Documentation/Manual/create-roslyn-analyzer.html)
- [Unity analyzer import metadata](https://docs.unity3d.com/6000.6/Documentation/Manual/install-existing-analyzer.html)
- [Unity assembly scope and per-asmdef rulesets](https://docs.unity3d.com/6000.6/Documentation/Manual/analyzer-scope-and-diagnostics.html)
- [Microsoft Unity analyzer source](https://github.com/microsoft/Microsoft.Unity.Analyzers/tree/fd250c39b858921df0f9479926338b0bad6259fb)
