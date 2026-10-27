using System;
using System.IO;

namespace KnowledgeRPG.Tests;

/// <summary>Locates source-tree assets when tests run from a bin directory.</summary>
internal static class TestPaths
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string DataDirectory => Path.Combine(RepositoryRoot, "Data");
    public static string ContentDirectory => Path.Combine(RepositoryRoot, "Content");

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KnowledgeRPG.csproj")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the KnowledgeRPG repository root from " +
            AppContext.BaseDirectory);
    }
}
