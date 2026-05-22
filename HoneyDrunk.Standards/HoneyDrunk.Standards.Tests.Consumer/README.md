# HoneyDrunk.Standards.Tests consumer smoke fixture

This project intentionally references the packed `HoneyDrunk.Standards.Tests` package instead of the local project. It proves the published package shape supplies the ADR-0047 test stack transitively to a consumer test project.

It is not included in `HoneyDrunk.Standards.slnx` because clean solution restores run before `0.2.9` is published. The in-solution `HoneyDrunk.Standards.Tests` project covers the canonical test-stack path during normal CI.

To run this package-consumer smoke fixture before publishing a new package version:

```powershell
$artifacts = Resolve-Path ..\artifacts\standards-tests-fix
$config = Join-Path $env:TEMP "honeydrunk-standards-tests-smoke.nuget.config"
@"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""local"" value=""$artifacts"" />
    <add key=""nuget"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>
"@ | Set-Content $config -Encoding UTF8

dotnet restore .\HoneyDrunk.Standards.Tests.Consumer.csproj --configfile $config
dotnet test .\HoneyDrunk.Standards.Tests.Consumer.csproj -c Release --no-restore
```

Expected setup before running the fixture:

```powershell
dotnet pack ..\HoneyDrunk.Standards\HoneyDrunk.Standards.csproj -c Release --no-build -o ..\artifacts\standards-tests-fix
dotnet pack ..\HoneyDrunk.Standards.Tests\HoneyDrunk.Standards.Tests.csproj -c Release --no-build -o ..\artifacts\standards-tests-fix
```
