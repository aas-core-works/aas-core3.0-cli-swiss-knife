using Encoding = System.Text.Encoding;
using Path = System.IO.Path;
using System.Linq; // can't alias
using NUnit.Framework; // can't alias

namespace TreeShakeConceptDescriptions.Tests
{
    public class TestsAgainstTestData
    {
        [Test]
        public void Test_against_recorded()
        {
            var testDataDir = Path.Join(
                CommonTesting.TestData.TestDataDir,
                "TreeShakeConceptDescriptions"
            );

            var recordMode = CommonTesting.TestData.RecordMode;

            var paths = System.IO.Directory.GetFiles(
                testDataDir,
                "model.json",
                System.IO.SearchOption.AllDirectories
            ).ToList();
            paths.Sort();

            foreach (var inputPth in paths)
            {
                var caseDir = Path.GetDirectoryName(inputPth);

                var stdoutPth = Path.Join(caseDir, "output.json");
                var stderrPth = Path.Join(caseDir, "stderr.txt");

                using var stdout = new System.IO.MemoryStream();
                using var stderr = new System.IO.StringWriter();
                Program.Execute(
                    inputPth,
                    "-",
                    stdout,
                    stderr
                );

                string stderrStr = stderr.ToString();
                stderrStr = stderrStr
                    .Replace(
                        new System.IO.FileInfo(
                            inputPth
                        ).FullName,
                        "<model.json>"
                    )
                    .Replace("\r", "");

                stdout.Flush();
                string stdoutStr = Encoding.UTF8.GetString(
                        stdout.ToArray()
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
                    var goldenStdout = System.IO.File.ReadAllText(
                            stdoutPth, Encoding.UTF8
                        )
                        .Replace("\r", "");
                    var goldenStderrStr = System.IO.File.ReadAllText(
                            stderrPth, Encoding.UTF8
                        )
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