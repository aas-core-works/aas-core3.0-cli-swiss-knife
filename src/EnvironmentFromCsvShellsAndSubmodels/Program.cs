using Aas = AasCore.Aas3_0; // renamed
using System.Collections.Generic; // can't alias
using System.CommandLine; // can't alias
using System.Diagnostics.CodeAnalysis; // can't alias


namespace EnvironmentFromCsvShellsAndSubmodels
{
    public static class Program
    {
        private static (
            Registering.TypedRegistry<Aas.IAssetAdministrationShell>?,
            Registering.TypedRegistry<Aas.ISubmodel>?,
            List<string>?
            ) ReadAssetsAndSubmodels(
                System.IO.FileInfo assetsAndSubmodels
            )
        {
            if (!assetsAndSubmodels.Exists)
            {
                return (null, null, new List<string>() { "The file does not exist" });
            }

            using var reader = assetsAndSubmodels.OpenText();

            var csv = new CsvParsing.CsvDictionaryReader(
                new CsvParsing.CsvReader(reader)
            );

            return ParsingAssetAdministrationShellsAndSubmodels.ParseTable(
                csv, assetsAndSubmodels.FullName
            );
        }

        public static int Execute(
            List<string> assetAndSubmodelPaths,
            string output,
            System.IO.Stream? stdout,
            System.IO.TextWriter stderr
        )
        {
            List<string> errors = new();

            Registering.TypedRegistry<Aas.IAssetAdministrationShell> shellRegistry =
                new();
            Registering.TypedRegistry<Aas.ISubmodel> submodelRegistry = new();

            foreach (var pth in assetAndSubmodelPaths)
            {
                var csvFile = new System.IO.FileInfo(pth);

                // ReSharper disable once JoinDeclarationAndInitializer
                Registering.TypedRegistry<Aas.IAssetAdministrationShell>?
                    shellSubRegistry;

                // ReSharper disable once JoinDeclarationAndInitializer
                Registering.TypedRegistry<Aas.ISubmodel>? submodelSubRegistry;

                // ReSharper disable once JoinDeclarationAndInitializer
                List<string>? readErrors;

                (shellSubRegistry, submodelSubRegistry, readErrors) =
                    ReadAssetsAndSubmodels(csvFile);

                if (readErrors != null)
                {
                    foreach (var error in readErrors)
                    {
                        errors.Add($"In {csvFile.FullName}: {error}");
                    }
                }
                else
                {
                    if (shellSubRegistry == null)
                    {
                        throw new System.InvalidOperationException(
                            "Unexpected shellSubRegistry null"
                        );
                    }

                    if (submodelSubRegistry == null)
                    {
                        throw new System.InvalidOperationException(
                            "Unexpected null submodelSubRegistry"
                        );
                    }

                    foreach (var item in shellSubRegistry.Items)
                    {
                        var existingShell = shellRegistry.TryGet(item.Id);
                        if (existingShell != null)
                        {
                            var existingEnh =
                                CsvEnhancing.Enhancing.Unwrapper.MustUnwrap(
                                    existingShell
                                );

                            var itemEnh = CsvEnhancing.Enhancing.Unwrapper.MustUnwrap(
                                item
                            );

                            errors.Add(
                                $"In {csvFile.FullName} " +
                                $"on row {itemEnh.RowIndex + 1}: " +
                                "the asset administration shell has been already " +
                                $"defined in {existingEnh.Path} " +
                                $"on row {existingEnh.RowIndex + 1}"
                            );
                        }
                        else
                        {
                            shellRegistry.Add(item);
                        }
                    }

                    foreach (var item in submodelSubRegistry.Items)
                    {
                        var existingSubmodel = submodelRegistry.TryGet(item.Id);
                        if (existingSubmodel != null)
                        {
                            var existingEnh =
                                CsvEnhancing.Enhancing.Unwrapper.MustUnwrap(
                                    existingSubmodel
                                );

                            var itemEnh = CsvEnhancing.Enhancing.Unwrapper.MustUnwrap(
                                item
                            );

                            errors.Add(
                                $"In {csvFile.FullName} " +
                                $"on row {itemEnh.RowIndex + 1}: " +
                                "the submodel has been already defined in " +
                                $"{existingEnh.Path} on row {existingEnh.RowIndex + 1}"
                            );
                        }
                        else
                        {
                            submodelRegistry.Add(item);
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                stderr.WriteLine("There were one or more errors:");
                foreach (var error in errors)
                {
                    stderr.WriteLine($"* {error}");
                }

                return 1;
            }

            var environment = new Aas.Environment()
            {
                AssetAdministrationShells = shellRegistry.Items.Count > 0
                    ? new(shellRegistry.Items)
                    : null,
                Submodels = submodelRegistry.Items.Count > 0
                    ? new(submodelRegistry.Items)
                    : null
            };

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
            var columnsJoined = string.Join(
                ",",
                ParsingAssetAdministrationShellsAndSubmodels.ExpectedHeader
            );

            var rootCommand =
                new System.CommandLine.RootCommand(
                    "Translate asset administration shells and submodels " +
                    "from a pre-defined CSV table to an AAS environment serialized " +
                    "to JSON.\n\n" +
                    "The expected header is:\n" +
                    $"{columnsJoined}"
                );

            var assetsAndSubmodelsOption = new System.CommandLine.Option<
                List<string>
            >(
                name: "--assets-and-submodels",
                description:
                "One or more files containing the information about " +
                "the asset administration shells and submodels in CSV format"
            )
            { IsRequired = true, Arity = ArgumentArity.OneOrMore };
            rootCommand.AddOption(assetsAndSubmodelsOption);

            var outputOption = new System.CommandLine.Option<
                string
            >(
                name: "--output",
                description: "The JSON file containing the AAS Environment; " +
                             "if '-', output to STDOUT"
            )
            { IsRequired = true };
            rootCommand.AddOption(outputOption);

            rootCommand.SetHandler(
                (
                    assetAndSubmodelPaths,
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
                                assetAndSubmodelPaths,
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
                assetsAndSubmodelsOption,
                outputOption
            );

            return await rootCommand.InvokeAsync(args);
        }
    }
}