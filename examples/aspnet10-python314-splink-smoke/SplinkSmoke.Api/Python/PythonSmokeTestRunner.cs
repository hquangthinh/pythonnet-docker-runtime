using Python.Runtime;

namespace SplinkSmoke.Api.Python;

/// <summary>Calls into <c>splink_smoke.smoke_tests</c> and converts results to C# records.</summary>
public sealed class PythonSmokeTestRunner
{
    private readonly PythonEngineHost _engine;
    // One Python call at a time keeps the example simple; the GIL serialises anyway.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public PythonSmokeTestRunner(PythonEngineHost engine) => _engine = engine;

    public IReadOnlyList<string> ListTests() =>
        WithModule(module =>
        {
            using PyObject names = module.InvokeMethod("list_tests");
            return names.As<string[]>().ToList();
        });

    public IReadOnlyList<SmokeTestResult> RunAll() =>
        WithModule(module =>
        {
            using PyObject results = module.InvokeMethod("run_all");
            return ToResults(results);
        });

    /// <returns><c>null</c> when <paramref name="name"/> is not a known test.</returns>
    public SmokeTestResult? RunOne(string name) =>
        WithModule(module =>
        {
            try
            {
                using PyObject arg = name.ToPython();
                using PyObject result = module.InvokeMethod("run_one", arg);
                return ToResult(result);
            }
            catch (PythonException ex) when (ex.Type.Name == "KeyError")
            {
                return null;
            }
        });

    public PythonInfo GetInfo() => new(
        _engine.IsReady,
        _engine.PythonVersion,
        _engine.PythonExecutable,
        _engine.PythonPrefix,
        _engine.PythonDll,
        _engine.ModulePath);

    private T WithModule<T>(Func<PyObject, T> action)
    {
        if (!_engine.IsReady)
        {
            throw new InvalidOperationException("Python engine is not initialised.");
        }

        _gate.Wait();
        try
        {
            using (Py.GIL())
            {
                using PyObject module = Py.Import("splink_smoke.smoke_tests");
                return action(module);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static List<SmokeTestResult> ToResults(PyObject list)
    {
        var results = new List<SmokeTestResult>();
        using var pyList = new PyList(list);
        foreach (PyObject item in pyList)
        {
            using (item)
            {
                results.Add(ToResult(item));
            }
        }
        return results;
    }

    private static SmokeTestResult ToResult(PyObject dict) => new(
        Name: Get<string>(dict, "name"),
        Package: Get<string>(dict, "package"),
        Version: Get<string>(dict, "version"),
        Ok: Get<bool>(dict, "ok"),
        Detail: Get<string>(dict, "detail"));

    private static T Get<T>(PyObject dict, string key)
    {
        using PyObject value = dict.GetItem(key);
        return value.As<T>();
    }
}
