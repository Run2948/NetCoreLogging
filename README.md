# ASP.NET Core Logging Solution

## [Serilog.Extensions.Logging.File](https://github.com/serilog/serilog-extensions-logging-file.git)

[![NuGet Pre Release](https://img.shields.io/nuget/vpre/Serilog.Extensions.Logging.File.svg)](https://nuget.org/packages/Serilog.Extensions.Logging.File) [![Join the chat at https://gitter.im/serilog/serilog](https://img.shields.io/gitter/room/serilog/serilog.svg)](https://gitter.im/serilog/serilog) [![Build status](https://ci.appveyor.com/api/projects/status/rdff6bp9oeqfxif7?svg=true)](https://ci.appveyor.com/project/serilog/serilog-extensions-logging-file)

This package makes it a one-liner - `loggerFactory.AddFile()` - to configure top-quality file logging for ASP.NET Core apps.

 * Text or JSON file output
 * Files roll over on date; capped file size
 * Request ids and event ids included with each message
 * Writes are performed on a background thread
 * Files are periodically flushed to disk (required for Azure App Service log collection)
 * Fast, stable, battle-proven logging code courtesy of [Serilog](https://serilog.net)

You can get started quickly with this package, and later migrate to the full Serilog API if you need more sophisticated log file configuration.

### Getting started

**1.** Add [the NuGet package](https://nuget.org/packages/serilog.extensions.logging.file) as a dependency of your project either with the package manager or directly to the CSPROJ file:

```xml
<PackageReference Include="Serilog.Extensions.Logging.File" Version="2.0.0" />
```

**2.** In your `Program` class, configure logging on the web host builder, and call `AddFile()` on the provided `loggingBuilder`.

```csharp
 Host.CreateDefaultBuilder(args)
        .ConfigureLogging((hostingContext, loggingBuilder) =>
        {
            // loggingBuilder.AddFile("Logs/Logs_{Date}.txt");
            loggingBuilder.AddFile(hostingContext.Configuration.GetSection("Logging"));
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

**3.** Add a custom excetion filter as a global exception handler:

```csharp
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var loggingBuilder = new StringBuilder();

            if (context.HttpContext.Request.GetDisplayUrl() != null)
                loggingBuilder.AppendLine($"\tUrl: {context.HttpContext.Request.GetDisplayUrl()}");

            loggingBuilder.AppendLine($"\tIp: {context.HttpContext.Connection.RemoteIpAddress}");

#if DEBUG
            foreach (var key in context.HttpContext.Request.Headers.Keys)
            {
                loggingBuilder.AppendLine($"\t{key}: {context.HttpContext.Request.Headers[key]}");
            }
#endif

            loggingBuilder.AppendLine($"\tError Message: {context.Exception.Message}");
            if (context.Exception.InnerException != null)
            {
                PrintInnerException(context.Exception.InnerException, loggingBuilder);
            }

            loggingBuilder.AppendLine($"\tError HelpLink: {context.Exception.HelpLink}");
            loggingBuilder.AppendLine($"\tError StackTrace: {context.Exception.StackTrace}");

            _logger.LogError(loggingBuilder.ToString());
        }

        public void PrintInnerException(Exception ex, StringBuilder loggingBuilder)
        {
            loggingBuilder.AppendLine($"\tError InnerMessage: {ex.Message}");
            if (ex.InnerException != null)
            {
                PrintInnerException(ex.InnerException, loggingBuilder);
            }
        }
    }

```

**4.** In your `Startup` class, configure the global exception handler on the `ConfigureServices` method, so we can catch all unhandled exceptions:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...

    services.AddControllersWithViews(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    });
  
    ...
}
```

```csharp
public IActionResult Privacy()
{
    throw new Exception("Unhandled exception");
    return View();
}
```

**5.** In your `Startup` class, add the log directory as static on the `Configure` method, so we can view the log directory:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...

    app.UseStaticFiles();

    //add the log directory as static, so we can view the log directory
    app.UseFileServer(new FileServerOptions()
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Logs")),
        RequestPath = new PathString("/Log"),
        EnableDirectoryBrowsing = true
    });

    app.UseRouting();

    ...
}
```

**Done!** The framework will inject `ILogger` instances into controllers and other classes:

```csharp
class HomeController : Controller
{
    readonly ILogger<HomeController> _log;

    public HomeController(ILogger<HomeController> log)
    {
        _log = log;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("Hello, world!");
        _logger.LogError(new Exception("Custom exception"), "Custom exception");
    }
}
```

The events will appear in the log file:

```
2016-10-18T11:14:11.0881912+10:00 0HKVMUG8EMJO9 [INF] Hello, world! (f83bcf75)
```

### File format

By default, the file will be written in plain text. The fields in the log file are:

| Field | Description | Format | Example |
| ----- | ----------- | ------ | ------- |
| **Timestamp** | The time the event occurred. | ISO-8601 with offset | `2016-10-18T11:14:11.0881912+10:00`  |
| **Request id** | Uniquely identifies all messages raised during a single web request. | Alphanumeric | `0HKVMUG8EMJO9` |
| **Level** | The log level assigned to the event. | Three-character code in brackets | `[INF]` |
| **Message** | The log message associated with the event. | Free text | `Hello, world!` |
| **Event id** | Identifies messages generated from the same format string/message template. | 32-bit hexadecimal, in parentheses | `(f83bcf75)` |
| **Exception** | Exception associated with the event. | `Exception.ToString()` format (not shown) | `System.DivideByZeroException: Attempt to divide by zero\r\n\  at...` |

To record events in newline-separated JSON instead, specify `isJson: true` when configuring the logger:

```csharp
loggingBuilder.AddFile("Logs/myapp-{Date}.txt", isJson: true);
```

This will produce a log file with lines like:

```json
{"@t":"2016-06-07T03:44:57.8532799Z","@m":"Hello, world!","@i":"f83bcf75","RequestId":"0HKVMUG8EMJO9"}
```

The JSON document includes all properties associated with the event, not just those present in the message. This makes JSON formatted logs a better choice for offline analysis in many cases.

### Rolling

The filename provided to `AddFile()` should include the `{Date}` placeholder, which will be replaced with the date of the events contained in the file. Filenames use the `yyyyMMdd` date format so that files can be ordered using a lexicographic sort:

```
log-20160631.txt
log-20160701.txt
log-20160702.txt
```

To prevent outages due to disk space exhaustion, each file is capped to 1 GB in size. If the file size is exceeded, events will be dropped until the next roll point.

### Message templates and event ids

The provider supports the templated log messages used by _Microsoft.Extensions.Logging_. By writing events with format strings or [message templates](https://messagetemplates.org), the provider can infer which messages came from the same logging statement.

This means that although the text of two messages may be different, their **event id** fields will match, as shown by the two "view" logging statements below:

```
2016-10-18T11:14:26.2544709+10:00 0HKVMUG8EMJO9 [INF] Running view at "/Views/Home/About.cshtml". (9707eebe)
2016-10-18T11:14:11.0881912+10:00 0HKVMUG8EMJO9 [INF] Hello, world! (f83bcf75)
2016-10-18T11:14:26.2544709+10:00 0HKVMUG8EMJO9 [INF] Running view at "/Views/Home/Index.cshtml". (9707eebe)
```

Each log message describing view rendering is tagged with `(9707eebe)`, while the "hello" log message is given `(f83bcf75)`. This makes it easy to search the log for messages describing the same kind of event.

### Additional configuration

The `AddFile()` method exposes some basic options for controlling the connection and log volume.

| Parameter | Description | Example value |
| --------- | ----------- | ------------- |
| `pathFormat` | Filename to write. The filename may include `{Date}` to specify how the date portion of the filename is calculated. May include environment variables.| `Logs/log-{Date}.txt` |
| `minimumLevel` | The level below which events will be suppressed (the default is `LogLevel.Information`). | `LogLevel.Debug` |
| `levelOverrides` | A dictionary mapping logger name prefixes to minimum logging levels. | |
| `isJson` | If true, the log file will be written in JSON format. | `true` |
| `fileSizeLimitBytes` | The maximum size, in bytes, to which any single log file will be allowed to grow. For unrestricted growth, pass`null`. The default is 1 GiB. | `1024 * 1024 * 1024` |
| `retainedFileCountLimit` | The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass `null`. The default is `31`. | `31` |
| `outputTemplate` | The template used for formatting plain text log output. The default is `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}` | `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} {Properties:j} ({EventId:x8}){NewLine}{Exception}` |

### `appsettings.json` configuration

The file path and other settings can be read from JSON configuration if desired.

In `appsettings.json` add a `"Logging"` property:

```json
{
  "Logging": {
    "PathFormat": "Logs/log-{Date}.txt",
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

And then pass the configuration section to the `AddFile()` method:

```csharp
loggingBuilder.AddFile(Configuration.GetSection("Logging"));
```

In addition to the properties shown above, the `"Logging"` configuration supports:

| Property | Description | Example |
| -------- | ----------- | ------- |
| `Json` | If `true`, the log file will be written in JSON format. | `true` |
| `FileSizeLimitBytes` | The maximum size, in bytes, to which any single log file will be allowed to grow. For unrestricted growth, pass`null`. The default is 1 GiB. | `1024 * 1024 * 1024` |
| `RetainedFileCountLimit` | The maximum number of log files that will be retained, including the current log file. For unlimited retention, pass `null`. The default is `31`. | `31` |
| `OutputTemplate` | The template used for formatting plain text log output. The default is `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} ({EventId:x8}){NewLine}{Exception}` | `{Timestamp:o} {RequestId,13} [{Level:u3}] {Message} {Properties:j} ({EventId:x8}){NewLine}{Exception}` |

### Using the full Serilog API

This package is opinionated, providing the most common/recommended options supported by Serilog. For more sophisticated configuration, using Serilog directly is recommened. See the instructions in [Serilog.AspNetCore](https://github.com/serilog/serilog-aspnetcore) to get started.

The following packages are used to provide `AddFile()`:

 * [Serilog](https://github.com/serilog/serilog) - the core logging pipeline
 * [Serilog.Sinks.RollingFile](https://github.com/serilog/serilog-sinks-rollingfile) - rolling file output
 * [Serilog.Formatting.Compact](https://github.com/serilog/serilog-formatting-compact) - JSON event formatting
 * [Serilog.Extensions.Logging](https://github.com/serilog/serilog-extensions-logging) - ASP.NET Core integration
 * [Serilog.Sinks.Async](https://github.com/serilog/serilog-sinks-async) - async wrapper to perform log writes on a background thread