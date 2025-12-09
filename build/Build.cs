// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace BuildPipeline;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Components;
using Serilog;
using System.Globalization;
using System.Text.Json;

internal class Build : NukeBuild,
                       IPublish,
                       IReportCoverage
{
    [Parameter]
    public String ReleaseVersion { get; set; } = "0.1.0-dev";

    public Configure<DotNetBuildSettings> CompileSettings => settings =>
        settings.SetAssemblyVersion(AssemblyVersion)
                .SetFileVersion(AssemblyVersion)
                .SetInformationalVersion(SemanticVersion)
                .EnableContinuousIntegrationBuild()
                .EnableTreatWarningsAsErrors();

    public Configuration Configuration => Configuration.Release;

    public Configure<DotNetPackSettings> PackSettings => settings =>
        settings.DisablePackageRequireLicenseAcceptance()
                .EnableContinuousIntegrationBuild()
                .ResetNoBuild()
                .SetProject("src/BlazorMessageBus/BlazorMessageBus.csproj")
                .AddProperty("AdditionalConstants", "NUGET_RELEASE")
                .AddProperty("SignAssembly", "true")
                .AddProperty("AssemblyOriginatorKeyFile", "../../BlazorMessageBus.snk")
                .EnableIncludeSymbols()
                .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
                .SetVersion(SemanticVersion)
                .When(!String.IsNullOrEmpty(ReleaseNotes),
                      t => t.SetPackageReleaseNotes(EscapeStringForMsBuild(ReleaseNotes)));

    public Configure<DotNetPublishSettings> PublishSettings => settings =>
        settings.EnableContinuousIntegrationBuild();

    public Configure<DotNetNuGetPushSettings> PushSettings => settings =>
        settings.EnableSkipDuplicate();


    public Configure<DotNetTestSettings> TestSettings => settings =>
        settings.EnableNoBuild()
                .When(InvokedTargets.Contains(((IReportCoverage)this).ReportCoverage), transform =>
                          transform.SetDataCollector("XPlat Code Coverage")
                                   .SetSettingsFile("coverlet.xml"));

    public Target ReportCoverage => target =>
        target.Inherit<IReportCoverage>()
              .Executes(() =>
              {
                  var coverage = "unknown";

                  try
                  {
                      var json = File.ReadAllText(CoverageSummary);
                      using var jsonDocument = JsonDocument.Parse(json);

                      if (jsonDocument.RootElement.TryGetProperty("summary", out var summary) &&
                          summary.TryGetProperty("linecoverage", out var lineCoverage))
                      {
                          coverage = $"{lineCoverage.GetDouble().ToString("#.0", CultureInfo.InvariantCulture)}%";
                      }
                  }
                  catch (Exception e)
                  {
                      Log.Error(e, "Failed to read coverage summary");
                  }

                  ReportSummary(config => config.AddPair("Coverage", coverage));
              });

    public Configure<ReportGeneratorSettings> ReportGeneratorSettings => settings =>
        settings.SetReports(From<IReportCoverage>().TestResultDirectory / "**/coverage.cobertura.xml")
                .SetReportTypes(ReportTypes.JsonSummary);

    private static Dictionary<Char, String> EscapeCharacters { get; } = new()
    {
        ['%'] = "%25",
        ['$'] = "%24",
        ['@'] = "%40",
        ['\''] = "%27",
        ['('] = "%28",
        [')'] = "%29",
        [';'] = "%3B",
        ['?'] = "%3F",
        ['*'] = "%2A",
        [','] = "%2C"
    };

    private static AbsolutePath ReleaseNotesFile => RootDirectory / "ReleaseNotes.md";

    private static AbsolutePath SourceDirectory => RootDirectory / "src";

    private static AbsolutePath TestsDirectory => RootDirectory / "tests";

    private String AssemblyVersion { get; set; } = null!;

    private Target Clean => target =>
        target.Before<IRestore>()
              .Executes(() =>
              {
                  DotNetTasks.DotNetClean(x => x.SetConfiguration(Configuration)
                                                .EnableContinuousIntegrationBuild()
                                                .DisableProcessOutputLogging());

                  ((IHazArtifacts)this).ArtifactsDirectory.CreateOrCleanDirectory();
              });

    private AbsolutePath CoverageSummary => From<IReportCoverage>().CoverageReportDirectory / "Summary.json";

    private String ReleaseNotes { get; set; } = null!;

    private String SemanticVersion { get; set; } = null!;

    IEnumerable<Project> ITest.TestProjects =>
        Partition.GetCurrent(From<IHazSolution>().Solution.GetAllProjects("*.Tests"));

    Boolean IReportCoverage.CreateCoverageHtmlReport => true;

    Boolean IReportCoverage.ReportToCodecov => false;

    public static Int32 Main() => Execute<Build>(x => ((ICompile)x).Compile);

    protected override void OnBuildCreated()
    {
        if (!SemanticVersioning.Version.TryParse(ReleaseVersion, out var version))
            Assert.Fail($"Not a valid semantic version: {ReleaseVersion}");

        SemanticVersion = version.ToString();
        AssemblyVersion = $"{version.Major}.{version.Minor}.{version.Patch}.0";

        if (ReleaseNotesFile.FileExists())
        {
            ReleaseNotes = ReleaseNotesFile.ReadAllText();
        }
    }

    private static String EscapeStringForMsBuild(String text)
        => String.Concat(text.Select(c => EscapeCharacters.TryGetValue(c, out var replacement)
                                         ? replacement
                                         : c.ToString()));

    private T From<T>() where T : INukeBuild => (T)(Object)this;
}
