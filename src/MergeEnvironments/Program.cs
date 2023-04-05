using Aas = AasCore.Aas3_0; // renamed

// ReSharper disable RedundantUsingDirective
using System.Collections.Generic;  // can't alias
using System.CommandLine; // can't alias
using System.Diagnostics.CodeAnalysis;  // can't alias
using System.Linq; // can't alias

namespace MergeEnvironments
{
    internal class Enhancement
    {
        internal readonly string Path;

        public Enhancement(string path)
        {
            Path = path;
        }
    }

    public static class Program
    {
        public static int Execute(
            List<string> environmentPaths,
            string output,
            System.IO.Stream? stdout,
            System.IO.TextWriter stderr
        )
        {
            var shellRegistry =
                new Registering.TypedRegistry<Aas.IAssetAdministrationShell>();

            var submodelRegistry = new Registering.TypedRegistry<Aas.ISubmodel>();

            var conceptDescriptionRegistry =
                new Registering.TypedRegistry<Aas.IConceptDescription>();

            var unwrapper = new Aas.Enhancing.Unwrapper<Enhancement>();

            foreach (var envPth in environmentPaths)
            {
                var enhancer = new Aas.Enhancing.Enhancer<Enhancement>(
                    _ => new Enhancement(
                        new System.IO.FileInfo(envPth).FullName
                    )
                );

                var fileInfo = new System.IO.FileInfo(envPth);

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
                        System.IO.File.ReadAllBytes(envPth)
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

                Aas.IEnvironment? env;
                try
                {
                    env = Aas.Jsonization.Deserialize.EnvironmentFrom(node);
                }
                catch (Aas.Jsonization.Exception exception)
                {
                    stderr.WriteLine(
                        $"{fileInfo.FullName}: " +
                        $"JSON parsing failed: {exception.Cause} at {exception.Path}"
                    );
                    return 1;
                }

                if (env == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected null instance"
                    );
                }

                env = (Aas.IEnvironment)enhancer.Wrap(env);

                foreach (var shell in env.OverAssetAdministrationShellsOrEmpty())
                {
                    var otherShell = shellRegistry.TryGet(shell.Id);
                    if (otherShell != null)
                    {
                        stderr.WriteLine(
                            $"{fileInfo.FullName}: " +
                            "Conflict in IDs of asset administration shells; " +
                            $"the shell with the ID {shell.Id} is also defined in " +
                            $"{unwrapper.MustUnwrap(otherShell).Path}"
                        );
                        return 1;
                    }

                    shellRegistry.Add(shell);
                }

                foreach (var submodel in env.OverSubmodelsOrEmpty())
                {
                    var otherSubmodel = submodelRegistry.TryGet(submodel.Id);
                    if (otherSubmodel != null)
                    {
                        stderr.WriteLine(
                            $"{fileInfo.FullName}: " +
                            "Conflict in IDs of submodels; " +
                            $"the submodel with the ID {submodel.Id} " +
                            "is also defined in " +
                            $"{unwrapper.MustUnwrap(otherSubmodel).Path}"
                        );
                        return 1;
                    }

                    submodelRegistry.Add(submodel);
                }

                foreach (var conceptDescription in env.OverConceptDescriptionsOrEmpty())
                {
                    var otherConceptDescription =
                        conceptDescriptionRegistry.TryGet(conceptDescription.Id);

                    if (otherConceptDescription != null)
                    {
                        stderr.WriteLine(
                            $"{fileInfo.FullName}: " +
                            "Conflict in IDs of concept descriptions; " +
                            "the concept description " +
                            $"with the ID {conceptDescription.Id} " +
                            "is also defined in " +
                            $"{unwrapper.MustUnwrap(otherConceptDescription).Path}"
                        );
                        return 1;
                    }

                    conceptDescriptionRegistry.Add(conceptDescription);
                }
            }

            var merged = new Aas.Environment();

            if (shellRegistry.Items.Count > 0)
            {
                merged.AssetAdministrationShells = shellRegistry.Items.ToList();
            }

            if (submodelRegistry.Items.Count > 0)
            {
                merged.Submodels = submodelRegistry.Items.ToList();
            }

            if (conceptDescriptionRegistry.Items.Count > 0)
            {
                merged.ConceptDescriptions = conceptDescriptionRegistry.Items.ToList();
            }

            var errorMessage = CommonOutputting.Jsonization.Serialize(
                merged, output, stdout
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
                    "Merge multiple AAS environments into one."
                );

            var environmentsOption = new System.CommandLine.Option<
                List<string>
            >(
                name: "--environments",
                description:
                "Paths to two or more AAS environments serialized to JSON to be merged"
            )
            { IsRequired = true, Arity = System.CommandLine.ArgumentArity.OneOrMore };
            rootCommand.AddOption(environmentsOption);

            var outputOption = new System.CommandLine.Option<
                string
            >(
                name: "--output",
                description:
                "Path to the merged environment; " +
                "if '-', the output is written to STDOUT"
            )
            { IsRequired = true };
            rootCommand.AddOption(outputOption);

            rootCommand.SetHandler(
                (
                    environmentPaths,
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
                                environmentPaths,
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
                environmentsOption,
                outputOption
            );

            return await rootCommand.InvokeAsync(args);
        }
    }
}