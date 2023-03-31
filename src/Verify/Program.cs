using Aas = AasCore.Aas3_0; // renamed
using System.CommandLine; // can't alias
using System.Diagnostics.CodeAnalysis; // can't alias


namespace Verify
{
    public static class Program
    {
        public static int Execute(
            string environmentPath,
            System.IO.TextWriter stderr
        )
        {
            var fileInfo = new System.IO.FileInfo(environmentPath);
            if (!fileInfo.Exists)
            {
                stderr.WriteLine($"{fileInfo.FullName}: File does not exist");
                return 1;
            }

            System.Text.Json.Nodes.JsonNode? node;
            try
            {
                using var file = fileInfo.OpenRead();
                node = System.Text.Json.Nodes.JsonNode.Parse(
                    System.IO.File.ReadAllBytes(environmentPath)
                );
            }
            catch (System.Text.Json.JsonException exception)
            {
                stderr.WriteLine(
                    $"{fileInfo.FullName}: " +
                    $"JSON parsing failed: {exception.Message}"
                );
                return 1;
            }

            if (node == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected null node"
                );
            }

            Aas.Environment? environment;
            try
            {
                environment = Aas.Jsonization.Deserialize.EnvironmentFrom(node);
            }
            catch (Aas.Jsonization.Exception exception)
            {
                stderr.WriteLine(
                    $"{fileInfo.FullName}: " +
                    $"JSON parsing failed: {exception.Cause} at {exception.Path}"
                );
                return 1;
            }

            if (environment == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected null instance"
                );
            }

            var gotVerificationErrors = false;
            foreach (var error in Aas.Verification.Verify(environment))
            {
                stderr.WriteLine(
                    $"{fileInfo.FullName}: " +
                    $"{Aas.Reporting.GenerateJsonPath(error.PathSegments)}: " +
                    $"{error.Cause}"
                );
                gotVerificationErrors = true;
            }

            if (gotVerificationErrors)
            {
                return 1;
            }

            return 0;
        }

        [SuppressMessage("ReSharper", "RedundantNameQualifier")]
        static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            var rootCommand =
                new System.CommandLine.RootCommand(
                    "Verify that the AAS environment satisfies the meta-model " +
                    "constraints.\n\n" +
                    "Only constraints are checked that can be enforced without " +
                    "external dependencies such as registry or language models."
                );

            var environmentOption = new System.CommandLine.Option<
                string
            >(
                name: "--environment",
                description:
                "Path to the AAS environment serialized to JSON to be verified"
            )
            { IsRequired = true };
            rootCommand.AddOption(environmentOption);

            rootCommand.SetHandler(
                (
                    environmentPath
                ) => System.Threading.Tasks.Task.FromResult(
                    Execute(
                        environmentPath,
                        System.Console.Error
                    )
                ),
                environmentOption
            );

            return await rootCommand.InvokeAsync(args);
        }
    }
}