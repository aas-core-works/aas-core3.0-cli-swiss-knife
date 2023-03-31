using Aas = AasCore.Aas3_0; // renamed
using System.Collections.Generic; // can't alias
using System.CommandLine; // can't alias
using System.Diagnostics.CodeAnalysis; // can't alias


namespace EnvironmentFromCsvConceptDescriptions
{
    public static class Program
    {
        private static (
            Registering.TypedRegistry<Aas.IConceptDescription>?,
            List<string>?
            ) ReadConceptDescriptions(
                System.IO.FileInfo conceptDescriptionsFile
            )
        {
            if (!conceptDescriptionsFile.Exists)
            {
                return (null, new List<string>() { "The file does not exist" });
            }

            using var reader = conceptDescriptionsFile.OpenText();

            var csv = new CsvParsing.CsvDictionaryReader(
                new CsvParsing.CsvReader(reader)
            );

            return ParsingConceptDescriptions.ParseTable(
                csv
            );
        }

        public static int Execute(
            List<string> conceptDescriptionPaths,
            string output,
            System.IO.Stream? stdout,
            System.IO.TextWriter stderr
        )
        {
            List<string> errors = new();

            Registering.TypedRegistry<Aas.IConceptDescription> registry = new();

            foreach (var pth in conceptDescriptionPaths)
            {
                var csvFile = new System.IO.FileInfo(pth);

                // ReSharper disable once JoinDeclarationAndInitializer
                Registering.TypedRegistry<Aas.IConceptDescription>? subRegistry;

                // ReSharper disable once JoinDeclarationAndInitializer
                List<string>? readErrors;

                (subRegistry, readErrors) =
                    ReadConceptDescriptions(csvFile);

                if (readErrors != null)
                {
                    foreach (var error in readErrors)
                    {
                        errors.Add($"In {csvFile.FullName}: {error}");
                    }
                }
                else
                {
                    if (subRegistry == null)
                    {
                        throw new System.InvalidOperationException(
                            "Unexpected conceptDescriptionSubRegistry null"
                        );
                    }

                    foreach (var item in subRegistry.Items)
                    {
                        var existingConceptDescription =
                            registry.TryGet(item.Id);

                        if (existingConceptDescription != null)
                        {
                            var existingEnh =
                                CsvEnhancing.Enhancing.Unwrapper.MustUnwrap(
                                    existingConceptDescription
                                );

                            var itemEnh =
                                CsvEnhancing.Enhancing.Unwrapper.MustUnwrap(
                                    item
                                );

                            errors.Add(
                                $"In {csvFile.FullName} " +
                                $"on row {itemEnh.RowIndex + 1}: " +
                                "the concept description has been already " +
                                $"defined in {existingEnh.Path} " +
                                $"on row {existingEnh.RowIndex + 1}"
                            );
                        }
                        else
                        {
                            registry.Add(item);
                        }
                    }
                }
            }

            var environment = new Aas.Environment()
            {
                ConceptDescriptions = registry.Items.Count > 0
                    ? new(registry.Items)
                    : null
            };

            if (errors.Count > 0)
            {
                stderr.WriteLine("There were one or more errors:");
                foreach (var error in errors)
                {
                    stderr.WriteLine($"* {error}");
                }

                return 1;
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
            var columnsJoined =
                string.Join(
                    ",",
                    ParsingConceptDescriptions.ExpectedHeader
                );

            var rootCommand =
                new System.CommandLine.RootCommand(
                    "Translate concept descriptions, captured " +
                    "as a pre-defined CSV file, into an AAS environment serialized " +
                    "to JSON.\n\n" +
                    $"The expected header is:\n{columnsJoined}"
                );

            var conceptDescriptionsOption = new System.CommandLine.Option<
                List<string>
            >(
                name: "--concept-descriptions",
                description:
                "One or more files containing the information about " +
                "the concept descriptions in CSV format"
            )
            { IsRequired = true, Arity = ArgumentArity.OneOrMore };
            rootCommand.AddOption(conceptDescriptionsOption);

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
                    conceptDescriptionPaths,
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
                                conceptDescriptionPaths,
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
                conceptDescriptionsOption,
                outputOption
            );

            return await rootCommand.InvokeAsync(args);
        }
    }
}