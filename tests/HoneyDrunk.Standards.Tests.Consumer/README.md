# HoneyDrunk.Standards.Tests consumer smoke fixture

This project references the packed test-stack package. It runs separately from the solution so both release packages can be packed before the consumer restores them.

From the repository root, build the solution and pack the release candidates into a local feed:

```powershell
dotnet build HoneyDrunk.Standards.slnx -c Release
dotnet pack HoneyDrunk.Standards.slnx -c Release --no-build -o artifacts/smoke/feed
```

Use a fresh package cache for each run. Map the two release packages to the local feed so the smoke test exercises the locally built artifacts even when NuGet.org has the same version.

```powershell
$feed = (Resolve-Path artifacts/smoke/feed).Path
$run = New-Item -ItemType Directory -Path (Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString()))
$config = Join-Path $run.FullName 'nuget.config'
$cache = Join-Path $run.FullName 'packages'
$project = 'tests/HoneyDrunk.Standards.Tests.Consumer/HoneyDrunk.Standards.Tests.Consumer.csproj'
@"
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$([Security.SecurityElement]::Escape($feed))" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local">
      <package pattern="HoneyDrunk.Standards" />
      <package pattern="HoneyDrunk.Standards.Tests" />
    </packageSource>
    <packageSource key="nuget"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content $config -Encoding utf8

dotnet restore $project --configfile $config --packages $cache
dotnet test $project -c Release --no-restore
```
