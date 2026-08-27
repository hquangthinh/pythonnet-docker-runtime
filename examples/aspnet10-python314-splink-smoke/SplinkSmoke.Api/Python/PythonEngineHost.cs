using Python.Runtime;

namespace SplinkSmoke.Api.Python;

/// <summary>
/// Owns the lifetime of the embedded CPython interpreter. Initialised once at app
/// startup, shut down once at app stop. Request code must take <see cref="Py.GIL"/>
/// before touching Python objects.
/// </summary>
public sealed class PythonEngineHost : IHostedService
{
    private readonly ILogger<PythonEngineHost> _logger;
    private readonly IConfiguration _configuration;

    public PythonEngineHost(ILogger<PythonEngineHost> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        ModulePath = Path.Combine(AppContext.BaseDirectory, "python_modules");
    }

    public bool IsReady { get; private set; }
    public string PythonDll { get; private set; } = string.Empty;
    public string PythonVersion { get; private set; } = string.Empty;
    public string PythonExecutable { get; private set; } = string.Empty;
    public string PythonPrefix { get; private set; } = string.Empty;
    public string ModulePath { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // The base image exports PYTHONNET_PYDLL; a config key is the fallback for local dev.
        PythonDll = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL")
                    ?? _configuration["PythonSmoke:PythonDll"]
                    ?? throw new InvalidOperationException(
                        "PYTHONNET_PYDLL is not set. Point it at libpython3.14.so (Linux) or python314.dll (Windows).");

        if (!File.Exists(PythonDll))
        {
            throw new FileNotFoundException($"Python shared library not found at '{PythonDll}'.", PythonDll);
        }

        if (!Directory.Exists(ModulePath))
        {
            throw new DirectoryNotFoundException($"python_modules directory not found at '{ModulePath}'.");
        }

        // Must be set before Initialize().
        Runtime.PythonDLL = PythonDll;
        PythonEngine.Initialize();

        using (Py.GIL())
        {
            dynamic sys = Py.Import("sys");
            sys.path.append(ModulePath);
            PythonVersion = (string)sys.version;
            PythonExecutable = (string)sys.executable;
            PythonPrefix = (string)sys.prefix;

            // Import once so a broken module fails the app at startup, not on first request.
            Py.Import("splink_smoke.smoke_tests");
        }

        // Release the GIL held by Initialize() so request threads can acquire it.
        PythonEngine.BeginAllowThreads();
        IsReady = true;

        _logger.LogInformation("Python engine ready. {Version} from {Dll}; modules at {ModulePath}",
            PythonVersion.Split('\n')[0], PythonDll, ModulePath);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        IsReady = false;
        try
        {
            PythonEngine.Shutdown();
            _logger.LogInformation("Python engine shut down.");
        }
        catch (Exception ex)
        {
            // Shutdown can throw while tearing down native extension modules; the process is exiting anyway.
            _logger.LogWarning(ex, "Python engine shutdown reported an error.");
        }
        return Task.CompletedTask;
    }
}
