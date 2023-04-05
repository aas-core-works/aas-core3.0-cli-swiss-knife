using Aas = AasCore.Aas3_0; // renamed

// ReSharper disable RedundantUsingDirective
using System.CommandLine; // can't alias
using System.Diagnostics.CodeAnalysis;  // can't alias
using System.Linq; // can't alias

namespace SplitEnvironmentForStaticHosting
{
    public static class Program
    {
        static readonly char[] Base64Padding = { '=' };

        private static string? OutputRegistry(
            string outputDir,
            Registering.TypedRegistry<Aas.IIdentifiable> registry
        )
        {
            System.IO.Directory.CreateDirectory(outputDir);

            var indexArray = new System.Text.Json.Nodes.JsonArray(
                registry.Items.Select(
                        identifiable =>
                        {
                            var value =
                                System.Text.Json.Nodes.JsonValue
                                    .Create(identifiable.Id);

                            if (value == null)
                            {
                                throw new System.InvalidOperationException(
                                    "Unexpected null value"
                                );
                            }

                            return value;
                        })
                    .ToArray<System.Text.Json.Nodes.JsonNode?>()
            );

            if (indexArray == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected null indexArray"
                );
            }

            var writerOptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = true
            };

            var indexArrayPath = System.IO.Path.Join(outputDir, "index.json");

            try
            {
                using var outputStream = System.IO.File.OpenWrite(indexArrayPath);
                using var writer =
                    new System.Text.Json.Utf8JsonWriter(
                        outputStream,
                        writerOptions
                    );
                indexArray.WriteTo(writer);
            }
            catch (System.Exception exception)
            {
                return $"Failed to write to {indexArrayPath}: {exception.Message}";
            }

            foreach (var identifiable in registry.Items)
            {
                var idBytes = System.Text.Encoding.UTF8.GetBytes(identifiable.Id);
                var idBase64 = System.Convert.ToBase64String(idBytes);

                // NOTE (mristin, 2023-04-05):
                // We use here the URL-safe Base64 encoding.
                //
                // See: https://stackoverflow.com/questions/26353710/how-to-achieve-base64-url-safe-encoding-in-c
                idBase64 = idBase64
                    .TrimEnd(Base64Padding)
                    .Replace('+', '-')
                    .Replace('/', '_');

                var pth = System.IO.Path.Join(outputDir, idBase64);

                System.Text.Json.Nodes.JsonObject? jsonable;

                try
                {
                    jsonable = Aas.Jsonization.Serialize.ToJsonObject(identifiable);
                }
                catch (System.Exception exception)
                {
                    return
                        $"Failed to serialize {identifiable.GetType().Name} " +
                        $"with ID {identifiable.Id} to JSON: {exception.Message}";
                }

                if (jsonable == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected null jsonable"
                    );
                }

                try
                {
                    using var outputStream = System.IO.File.OpenWrite(pth);
                    using var writer =
                        new System.Text.Json.Utf8JsonWriter(
                            outputStream,
                            writerOptions
                        );
                    jsonable.WriteTo(writer);
                }
                catch (System.Exception exception)
                {
                    return $"Failed to write to {indexArrayPath}: {exception.Message}";
                }
            }

            return null;
        }

        public static int Execute(
            string environmentPath,
            string outputDir,
            System.IO.TextWriter stderr
        )
        {
            var shellRegistry =
                new Registering.TypedRegistry<Aas.IIdentifiable>();

            var submodelRegistry = new Registering.TypedRegistry<Aas.IIdentifiable>();

            var conceptDescriptionRegistry =
                new Registering.TypedRegistry<Aas.IIdentifiable>();

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

            foreach (var shell in env.OverAssetAdministrationShellsOrEmpty())
            {
                var otherShell = shellRegistry.TryGet(shell.Id);
                if (otherShell != null)
                {
                    stderr.WriteLine(
                        $"{fileInfo.FullName}: " +
                        "Conflict in IDs of asset administration shells; " +
                        $"the shell with the ID {shell.Id} is defined " +
                        "more than once"
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
                        "is defined more than once."
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
                        "is defined more than once."
                    );
                    return 1;
                }

                conceptDescriptionRegistry.Add(conceptDescription);
            }

            var targetDir = System.IO.Path.Join(outputDir, "shells");
            var error = OutputRegistry(
                targetDir,
                shellRegistry
            );
            if (error != null)
            {
                stderr.WriteLine(
                    $"Error when outputting asset administration shells to {targetDir}: " +
                    $"{error}"
                );
                return 1;
            }

            targetDir = System.IO.Path.Join(outputDir, "submodels");
            error = OutputRegistry(
                targetDir,
                submodelRegistry
            );
            if (error != null)
            {
                stderr.WriteLine(
                    $"Error when outputting submodels to {targetDir}: " +
                    $"{error}"
                );
                return 1;
            }

            targetDir = System.IO.Path.Join(outputDir, "conceptDescriptions");
            error = OutputRegistry(
                targetDir,
                conceptDescriptionRegistry
            );
            if (error != null)
            {
                stderr.WriteLine(
                    $"Error when outputting concept descriptions to {targetDir}: " +
                    $"{error}"
                );
                return 1;
            }

            return 0;
        }

        [SuppressMessage("ReSharper", "RedundantNameQualifier")]
        static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            var rootCommand =
                new System.CommandLine.RootCommand(
                    "Split an AAS environment into multiple files " +
                    "so that they can be readily served as static files."
                );

            var environmentOption = new System.CommandLine.Option<
                string
            >(
                name: "--environments",
                description:
                "Path to the AAS environment serialized as a JSON file " +
                "to be split into different files for static hosting"
            )
            { IsRequired = true };
            rootCommand.AddOption(environmentOption);

            var outputDirOption = new System.CommandLine.Option<
                string
            >(
                name: "--output-dir",
                description:
                "Path to the output directory"
            )
            { IsRequired = true };
            rootCommand.AddOption(outputDirOption);

            rootCommand.SetHandler(
                (
                    environmentPaths,
                    outputDir
                ) => System.Threading.Tasks.Task.FromResult(
                    Execute(
                        environmentPaths,
                        outputDir,
                        System.Console.Error
                    )
                ),
                environmentOption,
                outputDirOption
            );

            return await rootCommand.InvokeAsync(args);
        }
    }
}