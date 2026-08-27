namespace SplinkSmoke.Api.Python;

/// <summary>Outcome of one package smoke test, mirroring the dict returned by <c>splink_smoke.smoke_tests</c>.</summary>
public sealed record SmokeTestResult(string Name, string Package, string Version, bool Ok, string Detail);

/// <summary>Aggregate result of running every smoke test.</summary>
public sealed record SmokeTestRunResponse(bool AllPassed, IReadOnlyList<SmokeTestResult> Results);

/// <summary>Details about the embedded Python interpreter.</summary>
public sealed record PythonInfo(bool Ready, string Version, string Executable, string Prefix, string PythonDll, string ModulePath);
