using Aas = AasCore.Aas3_0; // renamed

using System.Collections.Generic;  // can't alias
using System.Linq; // can't alias

namespace EnvironmentFromCsvShellsAndSubmodels
{
    internal static class ParsingAssetAdministrationShellsAndSubmodels
    {
        private static class ColumnNames
        {
            internal const string AssetAdministrationShellId =
                "Asset Administration Shell ID";

            internal const string SubmodelId = "Submodel ID";
            internal const string SubmodelElement = "Submodel Element";
            internal const string IdShort = "ID-short";
            internal const string Value = "Value";
            internal const string DataType = "Data Type";
            internal const string ContentType = "Content Type";
            internal const string Min = "Min";
            internal const string Max = "Max";
            internal const string SemanticId = "Semantic ID";
        }

        static class SubmodelElementTypes
        {
            internal const string Property = "Property";
            internal const string File = "File";
            internal const string Range = "Range";
        }

        private static readonly List<string> ExpectedEmptyColumnsInProperty =
            new()
            {
                ColumnNames.Max,
                ColumnNames.Min,
                ColumnNames.ContentType
            };

        private static (Aas.ISubmodelElement?, List<string>?) ParseProperty(
            IReadOnlyDictionary<string, string> row,
            int rowIndex
        )
        {
            List<string>? errors = null;

            var dataType = Aas.Stringification.DataTypeDefXsdFromString(
                row[ColumnNames.DataType]
            );

            if (row[ColumnNames.DataType].Length == 0)
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.DataType}: " +
                    "Unexpected empty"
                );
            }
            else if (dataType == null)
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.DataType}: " +
                    $"Invalid type {row[ColumnNames.DataType]}"
                );
            }

            foreach (var columnName in ExpectedEmptyColumnsInProperty)
            {
                if (row[columnName].Length != 0)
                {
                    errors ??= new();
                    errors.Add(
                        $"In row {rowIndex} and column {columnName}: " +
                        "Expected empty"
                    );
                }
            }

            var idShort = row[ColumnNames.IdShort];
            if (idShort == "")
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.IdShort}: " +
                    "Unexpected empty"
                );
            }

            string value = row[ColumnNames.Value];

            Aas.Reference? semanticId = null;
            var semanticIdValue = row[ColumnNames.SemanticId];
            if (semanticIdValue != "")
            {
                semanticId = new Aas.Reference(
                    Aas.ReferenceTypes.ModelReference,
                    new List<Aas.IKey>()
                    {
                        new Aas.Key(
                            Aas.KeyTypes.ConceptDescription,
                            semanticIdValue
                        )
                    }
                );
            }

            if (errors != null)
            {
                return (null, errors);
            }

            var property = new Aas.Property(
                dataType ?? throw new System.NullReferenceException()
            )
            {
                IdShort = idShort != "" ? idShort : null,
                Value = value != "" ? value : null,
                SemanticId = semanticId
            };

            return (property, null);
        }

        private static readonly List<string> ExpectedEmptyColumnsInFile =
            new()
            {
                ColumnNames.Max,
                ColumnNames.Min,
                ColumnNames.DataType,
            };

        private static (Aas.ISubmodelElement?, List<string>?) ParseFile(
            IReadOnlyDictionary<string, string> row,
            int rowIndex
        )
        {
            List<string>? errors = null;

            var contentType = row[ColumnNames.ContentType];

            if (contentType == "")
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.ContentType}: " +
                    "Unexpected empty"
                );
            }

            foreach (var columnName in ExpectedEmptyColumnsInFile)
            {
                if (row[columnName].Length != 0)
                {
                    errors ??= new();
                    errors.Add(
                        $"In row {rowIndex} and column {columnName}: " +
                        "Expected empty"
                    );
                }
            }

            var idShort = row[ColumnNames.IdShort];
            if (idShort == "")
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.IdShort}: " +
                    "Unexpected empty"
                );
            }

            string value = row[ColumnNames.Value];

            Aas.Reference? semanticId = null;
            var semanticIdValue = row[ColumnNames.SemanticId];
            if (semanticIdValue != "")
            {
                semanticId = new Aas.Reference(
                    Aas.ReferenceTypes.ModelReference,
                    new List<Aas.IKey>()
                    {
                        new Aas.Key(
                            Aas.KeyTypes.ConceptDescription,
                            semanticIdValue
                        )
                    }
                );
            }

            if (errors != null)
            {
                return (null, errors);
            }

            var file = new Aas.File(
                contentType
            )
            {
                IdShort = idShort != "" ? idShort : null,
                Value = value != "" ? value : null,
                SemanticId = semanticId
            };

            return (file, null);
        }

        private static readonly List<string> ExpectedEmptyColumnsInRange =
            new()
            {
                ColumnNames.Value,
                ColumnNames.ContentType
            };

        private static (Aas.ISubmodelElement?, List<string>?) ParseRange(
            IReadOnlyDictionary<string, string> row,
            int rowIndex
        )
        {
            List<string>? errors = null;

            var dataType = Aas.Stringification.DataTypeDefXsdFromString(
                row[ColumnNames.DataType]
            );

            if (row[ColumnNames.DataType].Length == 0)
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.DataType}: " +
                    "Unexpected empty"
                );
            }
            else if (dataType == null)
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.DataType}: " +
                    $"Invalid type {row[ColumnNames.DataType]}"
                );
            }

            foreach (var columnName in ExpectedEmptyColumnsInRange)
            {
                if (row[columnName].Length != 0)
                {
                    errors ??= new();
                    errors.Add(
                        $"In row {rowIndex} and column {columnName}: " +
                        "Expected empty"
                    );
                }
            }

            var idShort = row[ColumnNames.IdShort];
            if (idShort == "")
            {
                errors ??= new();
                errors.Add(
                    $"In row {rowIndex} and column {ColumnNames.IdShort}: " +
                    "Unexpected empty"
                );
            }

            Aas.Reference? semanticId = null;
            var semanticIdValue = row[ColumnNames.SemanticId];
            if (semanticIdValue != "")
            {
                semanticId = new Aas.Reference(
                    Aas.ReferenceTypes.ModelReference,
                    new List<Aas.IKey>()
                    {
                        new Aas.Key(
                            Aas.KeyTypes.ConceptDescription,
                            semanticIdValue
                        )
                    }
                );
            }

            if (errors != null)
            {
                return (null, errors);
            }

            var min = row[ColumnNames.Min];
            var max = row[ColumnNames.Max];

            var range = new Aas.Range(
                dataType ?? throw new System.NullReferenceException()
            )
            {
                IdShort = idShort != "" ? idShort : null,
                SemanticId = semanticId,
                Min = min != "" ? min : null,
                Max = max != "" ? max : null,
            };

            return (range, null);
        }

        internal static readonly List<string> ExpectedHeader = new()
        {
            ColumnNames.AssetAdministrationShellId,
            ColumnNames.SubmodelId,
            ColumnNames.SubmodelElement,
            ColumnNames.IdShort,
            ColumnNames.Value,
            ColumnNames.DataType,
            ColumnNames.ContentType,
            ColumnNames.Min,
            ColumnNames.Max,
            ColumnNames.SemanticId,
        };

        internal static (
            Registering.TypedRegistry<Aas.IAssetAdministrationShell>?,
            Registering.TypedRegistry<Aas.ISubmodel>?,
            List<string>?
            ) ParseTable(CsvParsing.CsvDictionaryReader csv, string path)
        {
            var error = csv.ReadHeader();
            if (error != null)
            {
                return (
                    null,
                    null,
                    new List<string>() { $"Failed to parse the header: {error}" }
                );
            }

            var errors = CsvParsing.Parsing.CheckHeader(
                ExpectedHeader, csv.Header
            );
            if (errors != null)
            {
                return (null, null, errors);
            }

            var shellRegistry =
                new Registering.TypedRegistry<Aas.IAssetAdministrationShell>();
            var submodelRegistry = new Registering.TypedRegistry<Aas.ISubmodel>();

            errors = new List<string>();

            var shellIdToSubmodelIds =
                new StrictlyIncreasingMapOfOrderedValues<string, string>();

            // First row corresponds to the header.
            var rowIndex = 1;

            Aas.Enhancing.Enhancer<CsvEnhancing.Enhancing.Enhancement> enhancer =
                new(
                    // ReSharper disable once AccessToModifiedClosure
                    (_) => new CsvEnhancing.Enhancing.Enhancement(
                        rowIndex,
                        path
                    )
                );

            while (true)
            {
                error = csv.ReadRow();
                rowIndex++;

                if (error != null)
                {
                    errors.Add($"In row {rowIndex}: {error}");
                    return (null, null, errors);
                }

                if (csv.Row == null)
                {
                    break;
                }

                #region Parse_shell_and_submodel

                var shellId = csv.Row[ColumnNames.AssetAdministrationShellId];
                var submodelId = csv.Row[ColumnNames.SubmodelId];

                if (shellId == "" || submodelId == "")
                {
                    if (shellId == "")
                    {
                        errors.Add(
                            $"In row {rowIndex} " +
                            $"and column {ColumnNames.AssetAdministrationShellId}: " +
                            "The ID is missing."
                        );
                    }

                    if (submodelId == "")
                    {
                        errors.Add(
                            $"In row {rowIndex} " +
                            $"and column {ColumnNames.SubmodelId}: " +
                            "The ID is missing."
                        );
                    }

                    continue;
                }

                var shell = shellRegistry.TryGet(shellId);
                if (shell == null)
                {
                    shell = new Aas.AssetAdministrationShell(
                        shellId,
                        new Aas.AssetInformation(Aas.AssetKind.Instance)
                    );
                    shellRegistry.Add(shell);
                }


                var submodel = submodelRegistry.TryGet(submodelId);
                if (submodel == null)
                {
                    submodel = new Aas.Submodel(submodelId);
                    submodelRegistry.Add(submodel);
                }

                shellIdToSubmodelIds.Add(shellId, submodelId);

                #endregion

                #region Parse_submodel_element

                var submodelElementType = csv.Row[ColumnNames.SubmodelElement];
                if (submodelElementType == "")
                {
                    errors.Add(
                        $"In row {rowIndex} " +
                        $"and column {ColumnNames.SubmodelElement}: " +
                        "the type is missing"
                    );
                    continue;
                }

                System.Func<
                    IReadOnlyDictionary<string, string>,
                    int,
                    (Aas.ISubmodelElement?, List<string>?)
                >? parsingFunc;

                switch (csv.Row[ColumnNames.SubmodelElement])
                {
                    case SubmodelElementTypes.Property:
                        parsingFunc = ParseProperty;
                        break;
                    case SubmodelElementTypes.Range:
                        parsingFunc = ParseRange;
                        break;
                    case SubmodelElementTypes.File:
                        parsingFunc = ParseFile;
                        break;
                    default:
                        errors.Add(
                            $"In row {rowIndex} " +
                            $"and column {ColumnNames.SubmodelElement}: " +
                            "the type is invalid; expected either " +
                            $"{SubmodelElementTypes.Property}, " +
                            $"{SubmodelElementTypes.Range} or " +
                            $"{SubmodelElementTypes.File}"
                        );
                        continue;
                }

                var (submodelElement, parseErrors) = parsingFunc(
                    csv.Row, rowIndex
                );

                if (parseErrors != null)
                {
                    errors.AddRange(parseErrors);
                    continue;
                }

                if (submodelElement == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected null submodelElement"
                    );
                }

                submodel.SubmodelElements ??=
                    new List<Aas.ISubmodelElement>();

                submodel.SubmodelElements.Add(
                    (Aas.ISubmodelElement)enhancer.Wrap(
                        submodelElement
                    )
                );

                #endregion
            }

            if (errors.Count > 0)
            {
                return (null, null, errors);
            }

            #region Associate_submodels_and_shells

            foreach (var (shellId, submodelIds) in shellIdToSubmodelIds)
            {
                var shell = shellRegistry.MustGet(shellId);
                shell.Submodels = (
                    submodelIds.Select(
                            (id) => new Aas.Reference(
                                Aas.ReferenceTypes.ModelReference,
                                new List<Aas.IKey>()
                                {
                                    new Aas.Key(Aas.KeyTypes.Submodel, id)
                                }
                            )
                        )
                        .ToList<Aas.IReference>()
                );
            }

            #endregion

            return (shellRegistry, submodelRegistry, null);
        }
    }
}