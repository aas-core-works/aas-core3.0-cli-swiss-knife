using Directory = System.IO.Directory;
using Encoding = System.Text.Encoding;
using File = System.IO.File;
using Path = System.IO.Path;

using System.Collections.Immutable; // can't alias
using System.Linq;  // can't alias

using NUnit.Framework;  // can't alias

namespace SplitEnvironmentForStaticHosting.Tests
{
    public class TestsAgainstTestData
    {
        [Test]
        public void Test_against_recorded()
        {
            var testDataDir = Path.Join(
                CommonTesting.TestData.TestDataDir,
                "SplitEnvironmentForStaticHosting"
            );

            var recordMode = CommonTesting.TestData.RecordMode;

            var paths = Directory.GetFiles(
                testDataDir,
                "model.json",
                System.IO.SearchOption.AllDirectories
            ).ToList();
            paths.Sort();

            foreach (var environmentPth in paths)
            {
                var caseDir = Path.GetDirectoryName(environmentPth);

                var stderrPth = Path.Join(caseDir, "stderr.txt");

                var expectedOutputDir = Path.Join(caseDir, "Expected_output");

                string outputDir;
                CommonTesting.TemporaryDirectory? tempDir = null;

                if (recordMode)
                {
                    outputDir = expectedOutputDir;
                }
                else
                {
                    tempDir = new CommonTesting.TemporaryDirectory();
                    outputDir = tempDir.Path;
                }

                try
                {
                    using var stderr = new System.IO.StringWriter();
                    Program.Execute(
                        environmentPth,
                        outputDir,
                        stderr
                    );

                    string stderrStr = stderr.ToString();
                    stderrStr = stderrStr
                        .Replace(
                            new System.IO.FileInfo(
                                environmentPth
                            ).FullName,
                            "<model.json>"
                        )
                        .Replace("\r", "");

                    if (recordMode)
                    {
                        File.WriteAllText(
                            stderrPth, stderrStr, Encoding.UTF8
                        );
                    }
                    else
                    {
                        var goldenStderrStr = File.ReadAllText(stderrPth);

                        Assert.AreEqual(
                            goldenStderrStr, stderrStr,
                            $"Content mismatch against {stderrPth}"
                        );

                        var gotFiles = Directory.GetFiles(
                            outputDir,
                            "*",
                            System.IO.SearchOption.AllDirectories
                        );

                        var expectedFiles = Directory.GetFiles(
                            expectedOutputDir,
                            "*",
                            System.IO.SearchOption.AllDirectories
                        );

                        var relativeGotFileSet = gotFiles.Select(
                            pth => Path.GetRelativePath(outputDir, pth)
                        ).ToImmutableSortedSet();

                        var relativeExpectedFileSet = expectedFiles.Select(
                            pth => Path.GetRelativePath(expectedOutputDir, pth)
                        ).ToImmutableSortedSet();

                        if (!relativeExpectedFileSet.SetEquals(relativeExpectedFileSet))
                        {
                            throw new AssertionException(
                                $"Expected files: {relativeExpectedFileSet}, " +
                                $"but got: {relativeGotFileSet}"
                            );
                        }

                        foreach (var relativePth in relativeExpectedFileSet)
                        {
                            var gotPth = Path.Join(outputDir, relativePth);
                            var expectedPth = Path.Join(expectedOutputDir, relativePth);

                            var gotText = File.ReadAllText(gotPth, Encoding.UTF8)
                                .Replace("\r", "");

                            var expectedText =
                                File.ReadAllText(expectedPth, Encoding.UTF8)
                                    .Replace("\r", "");

                            if (gotText != expectedText)
                            {
                                throw new AssertionException(
                                    "Mismatch in content between " +
                                    $"{gotPth} and {expectedPth}"
                                );
                            }
                        }
                    }
                }
                finally
                {
                    tempDir?.Dispose();
                }
            }
        }
    }
}