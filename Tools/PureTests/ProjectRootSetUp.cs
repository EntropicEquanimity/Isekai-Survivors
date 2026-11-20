using System;
using System.IO;
using NUnit.Framework;

// Only compiled by the .NET runner (Tools/PureTests). Unity runs tests from the project root, and some Pure tests read
// project files by relative path (Assets/...), so start from the same folder here.
[SetUpFixture]
public sealed class ProjectRootSetUp
{
    [OneTimeSetUp]
    public void UseProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets"))) dir = dir.Parent;
        if (dir == null) throw new DirectoryNotFoundException("No Unity project root (a folder with Assets/) above " + AppContext.BaseDirectory);
        Environment.CurrentDirectory = dir.FullName;
    }
}
