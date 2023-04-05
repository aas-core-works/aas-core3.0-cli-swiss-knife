using Encoding = System.Text.Encoding;
using Path = System.IO.Path;
using System.Linq;  // can't alias

using NUnit.Framework;  // can't alias

namespace ListDanglingModelReferences.Tests
{
    public class TestsAgainstTestData
    {
        [Test]
        public void Test_against_recorded()
        {
            var testDataDir = Path.Join(
                CommonTesting.TestData.TestDataDir,
                "ListDanglingModelReferences"
            );

            var recordMode = CommonTesting.TestData.RecordMode;

            var paths = System.IO.Directory.GetFiles(
                testDataDir,
                "model.json",
                System.IO.SearchOption.AllDirectories
            ).ToList();
            paths.Sort();

            foreach (var environmentPth in paths)
            {
                var caseDir = Path.GetDirectoryName(environmentPth);

                var stdoutPth = Path.Join(caseDir, "stdout.txt");
                var stderrPth = Path.Join(caseDir, "stderr.txt");

                using var stdout = new System.IO.StringWriter();
                using var stderr = new System.IO.StringWriter();
                Program.Execute(
                    environmentPth,
                    stdout,
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

                string stdoutStr = stdout.ToString();
                stdoutStr = stdoutStr
                    .Replace(
                        new System.IO.FileInfo(
                            environmentPth
                        ).FullName,
                        "<model.json>"
                    )
                    .Replace("\r", "");

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
                    var goldenStdoutStr = System.IO.File.ReadAllText(
                        stdoutPth, Encoding.UTF8
                    ).Replace("\r", "");
                    Assert.AreEqual(
                        goldenStdoutStr, stdoutStr,
                        $"Content mismatch against {stdoutPth}"
                    );

                    var goldenStderrStr = System.IO.File.ReadAllText(
                        stderrPth, Encoding.UTF8
                    ).Replace("\r", "");
                    Assert.AreEqual(
                        goldenStderrStr, stderrStr,
                        $"Content mismatch against {stderrPth}"
                    );
                }
            }
        }
    }
}