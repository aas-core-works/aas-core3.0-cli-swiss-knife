using Aas = AasCore.Aas3_0; // renamed
using System.Collections.Generic; // can't alias

namespace EnvironmentFromCsvConceptDescriptions
{
    internal static class ParsingConceptDescriptions
    {
        internal static class ColumnNames
        {
            internal const string Id = "ID";
            internal const string PreferredName = "Preferred Name";
            internal const string ShortName = "Short Name";
            internal const string Unit = "Unit";
            internal const string Symbol = "Symbol";
            internal const string Definition = "Definition";
            internal const string SourceOfDefinition = "Source of Definition";
        }

        internal static readonly List<string> ExpectedHeader = new()
        {
            ColumnNames.Id,
            ColumnNames.PreferredName,
            ColumnNames.ShortName,
            ColumnNames.Unit,
            ColumnNames.Symbol,
            ColumnNames.Definition,
            ColumnNames.SourceOfDefinition
        };

        internal static (
            Registering.TypedRegistry<Aas.IConceptDescription>?,
            List<string>?
            ) ParseTable(CsvParsing.CsvDictionaryReader csv)
        {
            var error = csv.ReadHeader();
            if (error != null)
            {
                return (
                    null,
                    new List<string>() { $"Failed to parse the header: {error}" }
                );
            }

            var errors = CsvParsing.Parsing.CheckHeader(
                ExpectedHeader, csv.Header
            );
            if (errors != null)
            {
                return (null, errors);
            }

            // First row corresponds to the header.
            var rowIndex = 1;

            errors = new List<string>();

            var registry = new Registering.TypedRegistry<Aas.IConceptDescription>();

            while (true)
            {
                error = csv.ReadRow();
                rowIndex++;

                if (error != null)
                {
                    errors.Add($"In row {rowIndex}: {error}");
                    return (null, errors);
                }

                if (csv.Row == null)
                {
                    break;
                }

                List<string>? rowErrors = null;

                var id = csv.Row[ColumnNames.Id];
                if (id == "")
                {
                    rowErrors ??= new List<string>();
                    rowErrors.Add(
                        $"In row {rowIndex} " +
                        $"and column {ColumnNames.Id}: " +
                        "Unexpected empty"
                    );
                }

                var preferredName = csv.Row[ColumnNames.PreferredName];
                if (preferredName == "")
                {
                    rowErrors ??= new List<string>();
                    rowErrors.Add(
                        $"In row {rowIndex} " +
                        $"and column {ColumnNames.PreferredName}: " +
                        "Unexpected empty"
                    );
                }

                if (rowErrors != null)
                {
                    errors.AddRange(rowErrors);
                    continue;
                }

                var shortName = csv.Row[ColumnNames.ShortName];
                var unit = csv.Row[ColumnNames.Unit];
                var symbol = csv.Row[ColumnNames.Symbol];
                var definition = csv.Row[ColumnNames.Definition];
                var sourceOfDefinition = csv.Row[ColumnNames.SourceOfDefinition];

                var dataSpecificationContent = new Aas.DataSpecificationIec61360(
                    new List<Aas.ILangStringPreferredNameTypeIec61360>
                    {
                        new Aas.LangStringPreferredNameTypeIec61360(
                            "en",
                            preferredName
                        )
                    }
                )
                {
                    ShortName = (shortName.Length > 0)
                        ? new List<Aas.ILangStringShortNameTypeIec61360>
                        {
                            new Aas.LangStringShortNameTypeIec61360(
                                "en",
                                shortName
                            )
                        }
                        : null,
                    Unit = (unit.Length > 0)
                        ? unit
                        : null,
                    Symbol = (symbol.Length > 0)
                        ? symbol
                        : null,
                    Definition = (definition.Length > 0)
                        ? new List<Aas.ILangStringDefinitionTypeIec61360>
                        {
                            new Aas.LangStringDefinitionTypeIec61360(
                                "en",
                                definition
                            )
                        }
                        : null,
                    SourceOfDefinition = (sourceOfDefinition.Length > 0)
                        ? sourceOfDefinition
                        : null,
                };

                var conceptDescription = new Aas.ConceptDescription(id)
                {
                    EmbeddedDataSpecifications = new()
                    {
                        new Aas.EmbeddedDataSpecification(
                            new Aas.Reference(
                                Aas.ReferenceTypes.ExternalReference,
                                new List<Aas.IKey>
                                {
                                    new Aas.Key(
                                        Aas.KeyTypes.GlobalReference,
                                        id
                                    )
                                }
                            ),
                            dataSpecificationContent
                        )
                    }
                };

                registry.Add(conceptDescription);
            }

            if (errors.Count > 0)
            {
                return (null, errors);
            }

            return (registry, null);
        }
    }
}