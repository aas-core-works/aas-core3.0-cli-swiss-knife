using Encoding = System.Text.Encoding;
using Path = System.IO.Path;

using System.Collections.Generic;  // can't alias
using System.Linq;  // can't alias

using NUnit.Framework;  // can't alias

namespace MergeEnvironments.Tests
{
    public class TestsAgainstTestData
    {
        [Test]
        public void Test_against_recorded()
        {
            var testDataDir = Path.Join(
                CommonTesting.TestData.TestDataDir,
                "MergeEnvironments"
            );

            var recordMode = CommonTesting.TestData.RecordMode;

            var paths = System.IO.Directory.GetFiles(
                testDataDir,
                "model*.json",
                System.IO.SearchOption.AllDirectories
            );

            var caseDirSet = new HashSet<string>();
            foreach (var pth in paths)
            {
                var caseDir = Path.GetDirectoryName(pth);
                if (caseDir == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected null caseDir"
                    );
                }

                caseDirSet.Add(caseDir);
            }

            var caseDirs = caseDirSet.ToList();
            caseDirs.Sort();

            foreach (var caseDir in caseDirs)
            {
                var environmentPaths = System.IO.Directory.GetFiles(
                    caseDir,
                    "model*.json",
                    System.IO.SearchOption.AllDirectories
                ).ToList();
                environmentPaths.Sort();

                using var stdout = new System.IO.MemoryStream();
                using var stderr = new System.IO.StringWriter();
                Program.Execute(
                    environmentPaths,
                    "-",
                    stdout,
                    stderr
                );

                string stderrStr = stderr.ToString();
                stderrStr = stderrStr.Replace("\r", "");

                stdout.Flush();
                string stdoutStr = Encoding.UTF8.GetString(
                        stdout.ToArray()
                    )
                    .Replace("\r", "");


                foreach (var environment in environmentPaths)
                {
                    var fileInfo = new System.IO.FileInfo(environment);
                    stderrStr = stderrStr.Replace(
                        fileInfo.FullName,
                        $"<{fileInfo.Name}>"
                    );
                }

                var stdoutPth = Path.Join(caseDir, "output.json");
                var stderrPth = Path.Join(caseDir, "stderr.txt");

                if (recordMode)
                {
                    System.IO.File.WriteAllText(
                        stdoutPth, stdoutStr, Encoding.UTF8
                    );
                    System.IO.File.WriteAllText(
                        stderrPth, stderrStr, Encoding.UTF8
                    );
                }
                else
                {
                    var goldenStdout = System.IO.File.ReadAllText(stdoutPth)
                        .Replace("\r", "");
                    var goldenStderrStr = System.IO.File.ReadAllText(stderrPth)
                        .Replace("\r", "");

                    Assert.AreEqual(
                        goldenStdout, stdoutStr,
                        $"Content mismatch against {stdoutPth}"
                    );
                    Assert.AreEqual(
                        goldenStderrStr, stderrStr,
                        $"Content mismatch against {stderrPth}"
                    );
                }
            }
        }
    }
}