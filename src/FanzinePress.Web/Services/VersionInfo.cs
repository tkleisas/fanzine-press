using System.Reflection;

namespace FanzinePress.Web.Services;

/// <summary>
/// Exposes the build-time version and git hash, read once from the assembly's
/// <see cref="AssemblyInformationalVersionAttribute"/>.
/// </summary>
/// <remarks>
/// The Dockerfile passes <c>APP_VERSION</c> / <c>GIT_SHA</c> build args through to
/// <c>dotnet publish</c> as <c>/p:Version=...</c> and <c>/p:SourceRevisionId=...</c>.
/// MSBuild stamps them into <see cref="AssemblyInformationalVersionAttribute"/> as
/// <c>{version}+{sha}</c>. When built without those args (e.g. local <c>dotnet run</c>),
/// <see cref="Version"/> falls back to <c>"dev"</c>.
/// </remarks>
public sealed class VersionInfo
{
    public string Version { get; }
    public string ShortHash { get; }

    public VersionInfo()
    {
        var raw = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? "";

        // Expected shapes:
        //   "0.1.0+a1b2c3d"                — exactly on a tag
        //   "0.1.0-7-ga1b2c3d+a1b2c3d"     — 7 commits past a tag
        //   "a1b2c3d-dirty+a1b2c3d"        — no tag yet, dirty
        //   "1.0.0+{sha}"                  — default MSBuild when Version not set
        //   ""                             — attribute missing (unlikely)
        var plus = raw.IndexOf('+');
        if (plus >= 0)
        {
            Version = raw[..plus];
            ShortHash = raw[(plus + 1)..];
        }
        else
        {
            Version = raw;
            ShortHash = "";
        }

        // Default MSBuild value when no explicit version is passed
        if (string.IsNullOrWhiteSpace(Version) || Version == "1.0.0")
        {
            Version = "dev";
        }

        // Trim to 7 chars if we somehow got a full 40-char sha
        if (ShortHash.Length > 7)
        {
            ShortHash = ShortHash[..7];
        }
    }

    /// <summary>"v0.1.0 · a1b2c3d" or "dev · unknown".</summary>
    public string Display =>
        string.IsNullOrEmpty(ShortHash)
            ? $"v{Version}"
            : $"v{Version} · {ShortHash}";
}
