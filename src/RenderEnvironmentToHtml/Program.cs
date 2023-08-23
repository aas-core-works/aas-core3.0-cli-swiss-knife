using Aas = AasCore.Aas3_0; // renamed

// ReSharper disable once RedundantUsingDirective
using System.CommandLine; // can't alias

// ReSharper disable once RedundantUsingDirective
using System.Diagnostics.CodeAnalysis; // can't alias

namespace RenderEnvironmentToHtml
{
    public static class Program
    {
        public static int Execute(
            string environmentPath,
            string outputDir,
            System.IO.TextWriter stderr
        )
        {
            var shellRegistry =
                new Registering.TypedRegistry<Aas.IAssetAdministrationShell>();

            var submodelRegistry = new Registering.TypedRegistry<Aas.ISubmodel>();

            var conceptDescriptionRegistry =
                new Registering.TypedRegistry<Aas.IConceptDescription>();

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

            #region EnvironmentIndex

            var targetPath = System.IO.Path.Join(outputDir, "index.html");
            var text = EnvironmentPageRenderer.Render(
                shellRegistry,
                submodelRegistry,
                conceptDescriptionRegistry);

            try
            {
                System.IO.File.WriteAllText(
                    targetPath,
                    text,
                    System.Text.Encoding.UTF8
                );
            }
            catch (System.Exception exception)
            {
                stderr.WriteLine(
                    $"Failed to write to {targetPath}: {exception.Message}");
                return 1;
            }

            #endregion

            #region ShellsIndex

            targetPath = System.IO.Path.Join(outputDir, "shells", "index.html");
            text = IdentifiablesPageRenderer.Render(
                shellRegistry.Items,
                "Asset Administration Shells");

            try
            {
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(targetPath)
                    ?? throw new System.InvalidOperationException(
                        $"Could not get the directory of: {targetPath}"
                    )
                );

                System.IO.File.WriteAllText(
                    targetPath,
                    text,
                    System.Text.Encoding.UTF8
                );
            }
            catch (System.Exception exception)
            {
                stderr.WriteLine(
                    $"Failed to write to {targetPath}: {exception.Message}");
                return 1;
            }

            #endregion

            #region SubmodelsIndex

            targetPath = System.IO.Path.Join(outputDir, "submodels", "index.html");
            text = IdentifiablesPageRenderer.Render(
                submodelRegistry.Items,
                "Submodels");

            try
            {
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(targetPath)
                    ?? throw new System.InvalidOperationException(
                        $"Could not get the directory of: {targetPath}"
                    )
                );

                System.IO.File.WriteAllText(
                    targetPath,
                    text,
                    System.Text.Encoding.UTF8
                );
            }
            catch (System.Exception exception)
            {
                stderr.WriteLine(
                    $"Failed to write to {targetPath}: {exception.Message}");
                return 1;
            }

            #endregion

            #region ConceptDescriptionsIndex

            targetPath = System.IO.Path.Join(
                outputDir, "conceptDescriptions", "index.html");
            text = IdentifiablesPageRenderer.Render(
                conceptDescriptionRegistry.Items,
                "Concept Descriptions");

            try
            {
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(targetPath)
                    ?? throw new System.InvalidOperationException(
                        $"Could not get the directory of: {targetPath}"
                    )
                );

                System.IO.File.WriteAllText(
                    targetPath,
                    text,
                    System.Text.Encoding.UTF8
                );
            }
            catch (System.Exception exception)
            {
                stderr.WriteLine(
                    $"Failed to write to {targetPath}: {exception.Message}");
                return 1;
            }

            #endregion

            foreach (var shell in shellRegistry.Items)
            {
                var idBase64 = IdToBase64.Translate(shell.Id);
                text = IdentifiablePageRenderer.Render(
                    shell,
                    "Asset Administration Shell",
                    shellRegistry,
                    submodelRegistry,
                    conceptDescriptionRegistry
                );

                targetPath = System.IO.Path.Join(
                    outputDir, "shells", $"{idBase64}.html"
                );
                System.IO.File.WriteAllText(
                    targetPath,
                    text,
                    System.Text.Encoding.UTF8
                );
            }

            foreach (var submodel in submodelRegistry.Items)
            {
                var idBase64 = IdToBase64.Translate(submodel.Id);
                text = IdentifiablePageRenderer.Render(
                    submodel,
                    "Submodel",
                    shellRegistry,
                    submodelRegistry,
                    conceptDescriptionRegistry
                );

                targetPath = System.IO.Path.Join(
                    outputDir, "submodels", $"{idBase64}.html"
                );
                System.IO.File.WriteAllText(
                    targetPath,
                    text,
                    System.Text.Encoding.UTF8
                );
            }

            foreach (var conceptDescription in conceptDescriptionRegistry.Items)
            {
                var idBase64 = IdToBase64.Translate(conceptDescription.Id);
                text = IdentifiablePageRenderer.Render(
                    conceptDescription,
                    "Concept Description",
                    shellRegistry,
                    submodelRegistry,
                    conceptDescriptionRegistry
                );

                targetPath = System.IO.Path.Join(
                    outputDir, "conceptDescriptions", $"{idBase64}.html"
                );
                System.IO.File.WriteAllText(
                    targetPath,
                    text,
                    System.Text.Encoding.UTF8
                );
            }

            return 0;
        }

        [SuppressMessage("ReSharper", "RedundantNameQualifier")]
        static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            var rootCommand =
                new System.CommandLine.RootCommand(
                    "Render an AAS environment into multiple HTML files."
                );

            var environmentOption = new System.CommandLine.Option<
                    string
                >(
                    name: "--environment",
                    description:
                    "Path to the AAS environment serialized as a JSON file " +
                    "to be rendered into HTML"
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