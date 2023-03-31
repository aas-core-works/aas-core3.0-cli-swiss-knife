using Aas = AasCore.Aas3_0; // renamed

// ReSharper disable RedundantUsingDirective
using System.Collections.Generic;  // can't alias
using System.CommandLine; // can't alias
using System.Diagnostics.CodeAnalysis;  // can't alias
using System.Linq; // can't alias

namespace TreeShakeConceptDescriptions
{
    public static class Program
    {
        public static int Execute(
            string environmentPath,
            string output,
            System.IO.Stream? stdout,
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

            if (
                environment.ConceptDescriptions is { Count: > 0 }
            )
            {
                var referencedConceptDescriptions = new HashSet<string>();

                foreach (var hasSemantics in environment.Descend()
                    .OfType<Aas.IHasSemantics>())
                {
                    if (
                        hasSemantics.SemanticId is
                        { Type: Aas.ReferenceTypes.ModelReference }
                        && hasSemantics.SemanticId.Keys.Count > 0
                        && hasSemantics.SemanticId.Keys[0].Type ==
                        Aas.KeyTypes.ConceptDescription
                    )
                    {
                        referencedConceptDescriptions.Add(
                            hasSemantics.SemanticId.Keys[0].Value
                        );
                    }
                }

                var newConceptDescriptions =
                    environment
                        .ConceptDescriptions
                        .Where(
                            cd =>
                                referencedConceptDescriptions.Contains(cd.Id)
                        )
                        .ToList();

                environment.ConceptDescriptions = (newConceptDescriptions.Count > 0)
                    ? newConceptDescriptions
                    : null;
            }

            var errorMessage = CommonOutputting.Jsonization.Serialize(
                environment, output, stdout
            );
            if (errorMessage != null)
            {
                stderr.WriteLine(errorMessage);
                return 1;
            }

            return 0;
        }

        [SuppressMessage("ReSharper", "RedundantNameQualifier")]
        static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            var rootCommand =
                new System.CommandLine.RootCommand(
                    "Remove concept descriptions which are not directly " +
                    "referenced as model references through semantic IDs."
                );

            var environmentOption = new System.CommandLine.Option<
                string
            >(
                name: "--environment",
                description:
                "Path to the AAS environment serialized to JSON " +
                "from which we remove the concept descriptions " +
                "not referenced in any semantic ID"
            )
            { IsRequired = true };
            rootCommand.AddOption(environmentOption);

            var outputOption = new System.CommandLine.Option<
                string
            >(
                name: "--output",
                description:
                "Path to the environment without unreferenced concept descriptions; " +
                "if '-', the output is written to STDOUT"
            )
            { IsRequired = true };
            rootCommand.AddOption(outputOption);

            rootCommand.SetHandler(
                (
                    environmentPath,
                    output
                ) =>
                {
                    System.IO.Stream? stdout = null;
                    if (output == "-")
                    {
                        stdout = System.Console.OpenStandardInput();
                    }

                    try
                    {
                        return System.Threading.Tasks.Task.FromResult(
                            Execute(
                                environmentPath,
                                output,
                                stdout,
                                System.Console.Error
                            )
                        );
                    }
                    finally
                    {
                        stdout?.Close();
                    }
                },
                environmentOption,
                outputOption
            );

            return await rootCommand.InvokeAsync(args);
        }
    }
}