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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
                      t => t.SetPackageReleaseNotes(ReleaseNotes));

    public Configure<DotNetPublishSettings> PublishSettings => settings =>
        settings.EnableContinuousIntegrationBuild();

    public Configure<DotNetNuGetPushSettings> PushSettings => settings =>
        settings.EnableSkipDuplicate();

    public IEnumerable<Project> TestProjects => GetTestProjects().ToList();

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
                      Log.Error(e, "Failed to read coverage summary.");
                  }

                  ReportSummary(config => config.AddPair("Coverage", coverage));
              });

    public Configure<ReportGeneratorSettings> ReportGeneratorSettings => settings =>
        settings.SetReports(From<IReportCoverage>().TestResultDirectory / "**/coverage.cobertura.xml")
                .SetReportTypes(ReportTypes.JsonSummary);

    private static AbsolutePath ReleaseNotesFile => RootDirectory / "ReleaseNotes.md";

    private static AbsolutePath SourceDirectory => RootDirectory / "src";

    private static AbsolutePath TestsDirectory => RootDirectory / "tests";

    private String AssemblyVersion { get; set; }

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

    private String ReleaseNotes { get; set; }

    private String SemanticVersion { get; set; }

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

    private static Project CreateProject(AbsolutePath projectFile)
    {
        // Nuke currently does not support SLNX solution files and the Project class has no public constructor,
        // so we use reflection to fake projects until Nuke supports SLNX.
        var type = typeof(Project);
        var constructor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
        var name = Path.GetFileNameWithoutExtension(projectFile);

        var project = (Project)constructor.Invoke(
        [
            new Solution(),
            Guid.NewGuid(),
            name,
            () => projectFile.ToString(),
            Guid.NewGuid(),
            new Dictionary<String, String>()
        ]);

        return project;
    }

    private static IEnumerable<Project> GetTestProjects()
        => TestsDirectory.GlobFiles("**/*.Tests.csproj")
                         .Select(CreateProject);

    private T From<T>() where T : INukeBuild => (T)(Object)this;
}
