// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace BuildPipeline;

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

internal class Build : NukeBuild,
                       IHazArtifacts,
                       IHazSolution,
                       IHazGitRepository,
                       ICompile,
                       ITest
{
    private Target Clean => target =>
        target.Before<IRestore>()
              .Executes(() =>
              {
                  SourceDirectory.GlobDirectories("**/bin", "**/obj").DeleteDirectories();
                  TestsDirectory.GlobDirectories("**/bin", "**/obj").DeleteDirectories();

                  ((IHazArtifacts)this).ArtifactsDirectory.CreateOrCleanDirectory();
              });

    private static AbsolutePath TestsDirectory => RootDirectory / "tests";

    private AbsolutePath SourceDirectory => RootDirectory / "src";

    public Configure<DotNetBuildSettings> CompileSettings => settings =>
        settings.EnableContinuousIntegrationBuild()
                .EnableTreatWarningsAsErrors();

    public Configure<DotNetPublishSettings> PublishSettings => settings =>
        settings.EnableContinuousIntegrationBuild();

    public Configuration Configuration => Configuration.Release;

    public IEnumerable<Project> TestProjects => GetTestProjects().ToList();

    private IEnumerable<Project> GetTestProjects()
        => TestsDirectory.GlobFiles("**/*.Tests.csproj")
                         .Select(CreateProject);

    private Project CreateProject(AbsolutePath projectFile)
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

    public static Int32 Main() => Execute<Build>(x => ((ICompile)x).Compile);
}
