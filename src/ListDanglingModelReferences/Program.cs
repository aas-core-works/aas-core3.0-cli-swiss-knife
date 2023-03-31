using Aas = AasCore.Aas3_0; // renamed

using System.CommandLine; // can't alias
using System.Diagnostics.CodeAnalysis; // can't alias
using System.Collections.Generic; // can't alias
using System.Linq; // can't alias

namespace ListDanglingModelReferences
{
    /**
     * <summary>Allow reference keys to be used in a trie.</summary>
     * <remarks>The hashable key should not be modified post-construction.</remarks>
     */
    class HashableKey : Aas.Key
    {
        public override int GetHashCode()
        {
            // From: https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
            int hash = 17;

            // ReSharper disable once NonReadonlyMemberInGetHashCode
            hash = hash * 31 + (int)Type;

            // ReSharper disable once NonReadonlyMemberInGetHashCode
            hash = hash * 31 + Value.GetHashCode();

            return hash;
        }

        public override bool Equals(object? obj)
        {
            var anotherKey = obj as HashableKey;
            if (anotherKey == null)
            {
                // ReSharper disable once BaseObjectEqualsIsObjectEquals
                return base.Equals(obj);
            }

            return anotherKey.Type == Type && anotherKey.Value == Value;
        }

        public HashableKey(Aas.KeyTypes type, string value) : base(type, value)
        {
            // Intentionally empty.
        }
    }

    static class Referencing
    {
        class Enhancement
        {
            /**
         * <summary>List children in the AAS reference system.</summary>
         * * <remarks>
         * Please do not confuse this to an C# object graph or JSON model.
         * </remarks>
         */
            internal readonly Dictionary<HashableKey, Aas.IClass> Children = new();
        }

        private static readonly Aas.Enhancing.Enhancer<Enhancement> Enhancer = new(
            _ => new Enhancement()
        );

        private static readonly Aas.Enhancing.Unwrapper<Enhancement> Unwrapper = new();

        /**
         * <summary>
         * Set the children in the enhancements recursively in-place.
         * </summary>
         * <returns>Error message, if any</returns>
         */
        private static string? RecursivelyProcessSubmodelElementList(
            Aas.ISubmodelElementList submodelElementList
        )
        {
            var enh = Unwrapper.MustUnwrap(submodelElementList);

            var i = 0;
            foreach (var item in submodelElementList.OverValueOrEmpty())
            {
                var key = new HashableKey(
                    ReferableToKeyTypes.Map(item),
                    i.ToString()
                );

                enh.Children.Add(key, item);
                i++;

                var error = RecursivelyProcessSubmodelElement(item);
                if (error != null)
                {
                    return $"In submodel element list at index: {i}: {error}";
                }
            }

            return null;
        }

        /**
         * <summary>
         * Set the children in the enhancements recursively in-place.
         * </summary>
         * <returns>Error message, if any</returns>
         */
        private static string? RecursivelyProcessSubmodelElementCollection(
            Aas.ISubmodelElementCollection submodelElementCollection
        )
        {
            var enh = Unwrapper.MustUnwrap(submodelElementCollection);

            var childrenIdShorts = new HashSet<string>();

            foreach (var item in submodelElementCollection.OverValueOrEmpty())
            {
                if (item.IdShort == null)
                {
                    return
                        "Unexpected item in a submodel element collection " +
                        "without an ID-short";
                }

                if (childrenIdShorts.Contains(item.IdShort))
                {
                    return
                        "Unexpected items in a submodel element collection " +
                        $"with duplicate ID-shorts: {item.IdShort}";
                }

                childrenIdShorts.Add(item.IdShort);

                var key = new HashableKey(
                    ReferableToKeyTypes.Map(item),
                    item.IdShort
                );

                enh.Children.Add(key, item);

                var error = RecursivelyProcessSubmodelElement(item);
                if (error != null)
                {
                    return "In submodel element collection " +
                           $"at ID-short {item.IdShort}: {error}";
                }
            }

            return null;
        }


        /**
         * <summary>
         * Set the children in the enhancements recursively in-place.
         * </summary>
         * <returns>Error message, if any</returns>
         */
        private static string? RecursivelyProcessSubmodelElement(
            Aas.ISubmodelElement submodelElement
        )
        {
            switch (submodelElement)
            {
                case Aas.ISubmodelElementList submodelElementList:
                    return RecursivelyProcessSubmodelElementList(submodelElementList);

                case Aas.ISubmodelElementCollection submodelElementCollection:
                    return RecursivelyProcessSubmodelElementCollection(
                        submodelElementCollection
                    );

                default:
                    return null;
            }
        }

        /**
         * <summary>
         * Set the children in the enhancements recursively in-place.
         * </summary>
         * <returns>Error message, if any</returns>
         */
        private static string? RecursivelyProcessSubmodel(Aas.ISubmodel submodel)
        {
            var childrenIdShorts = new HashSet<string>();

            var enh = Unwrapper.MustUnwrap(submodel);
            foreach (var submodelElement in submodel.OverSubmodelElementsOrEmpty())
            {
                if (submodelElement.IdShort == null)
                {
                    return
                        "Unexpected submodel element " +
                        $"without ID-short in submodel {submodel.Id}";
                }


                if (childrenIdShorts.Contains(submodelElement.IdShort))
                {
                    return
                        "Unexpected submodel elements " +
                        $"with duplicate ID-shorts: {submodelElement.IdShort}";
                }

                childrenIdShorts.Add(submodelElement.IdShort);

                var key = new HashableKey(
                    ReferableToKeyTypes.Map(submodelElement),
                    submodelElement.IdShort
                );

                enh.Children.Add(key, submodelElement);

                var error = RecursivelyProcessSubmodelElement(
                    submodelElement
                );
                if (error != null)
                {
                    return
                        $"In submodel element {submodelElement.IdShort}: {error}";
                }
            }

            return null;
        }

        /**
         * <summary>Set the enhancement properties for the given environment.</summary>
         * <returns>Enhanced environment, or error if any</returns>
         */
        internal static (Aas.IEnvironment?, string?) Process(
            Aas.IEnvironment environment
        )
        {
            var envWrapped = (Aas.IEnvironment)Enhancer.Wrap(environment);

            var envEnh = Unwrapper.MustUnwrap(envWrapped);

            foreach (var shell in envWrapped.OverAssetAdministrationShellsOrEmpty())
            {
                envEnh.Children.Add(
                    new HashableKey(
                        Aas.KeyTypes.AssetAdministrationShell,
                        shell.Id
                    ), shell
                );
            }

            foreach (var submodel in envWrapped.OverSubmodelsOrEmpty())
            {
                envEnh.Children.Add(
                    new HashableKey(
                        Aas.KeyTypes.Submodel,
                        submodel.Id
                    ), submodel
                );

                var error = RecursivelyProcessSubmodel(submodel);
                if (error != null)
                {
                    return (null, $"In submodel {submodel.Id}: {error}");
                }
            }

            foreach (var conceptDescription in
                envWrapped.OverConceptDescriptionsOrEmpty())
            {
                envEnh.Children.Add(
                    new HashableKey(
                        Aas.KeyTypes.ConceptDescription,
                        conceptDescription.Id
                    ), conceptDescription
                );
            }

            return (envWrapped, null);
        }

        /**
         * <summary>Follow the model reference from the environment.</summary>
         */
        public static Aas.IClass? Dereference(
            Aas.IEnvironment environment,
            Aas.IReference reference
        )
        {
            if (reference.Type != Aas.ReferenceTypes.ModelReference)
            {
                var typeAsStr = Aas.Stringification.ToString(reference.Type);
                if (typeAsStr == null)
                {
                    throw new System.InvalidOperationException(
                        $"Unexpected null typeAsStr for {reference.Type}"
                    );
                }

                throw new System.ArgumentException(
                    $"Expected a model reference, but got {typeAsStr}"
                );
            }

            if (reference.Keys.Count == 0)
            {
                throw new System.ArgumentException(
                    "Unexpected empty list of keys in the reference"
                );
            }

            Aas.IClass? cursor = environment;

            foreach (var key in reference.Keys.Select(
                    unhashableKey => new HashableKey(
                        unhashableKey.Type, unhashableKey.Value
                    )
                )
            )
            {
                if (cursor == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected null cursor"
                    );
                }

                var enh = Unwrapper.MustUnwrap(cursor);

                bool got = enh.Children.TryGetValue(key, out cursor);
                if (!got)
                {
                    return null;
                }
            }

            if (cursor == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected null cursor"
                );
            }

            return cursor;
        }

        public static string StringifyAsJsonList(IEnumerable<Aas.IKey> keys)
        {
            var array = new System.Text.Json.Nodes.JsonArray(
                keys.Select(key =>
                {
                    var node = System.Text.Json.Nodes.JsonValue.Create(key.Value);
                    if (node == null)
                    {
                        throw new System.InvalidOperationException(
                            "Unexpected null node"
                        );
                    }

                    return node;
                }).ToArray<System.Text.Json.Nodes.JsonNode>()
            );

            return array.ToJsonString();
        }
    }

    class Verifier : PassThruVerifierWithJsonPaths
    {
        private readonly Aas.IEnvironment _environment;

        public Verifier(Aas.IEnvironment environment)
        {
            _environment = environment;
        }

        public override IEnumerable<Aas.Reporting.Error> TransformReference(
            Aas.IReference that
        )
        {
            if (that.Type == Aas.ReferenceTypes.ModelReference)
            {
                var dereferenced = Referencing.Dereference(
                    _environment,
                    that
                );

                if (dereferenced == null)
                {
                    yield return new Aas.Reporting.Error(
                        $"Dangling reference {Referencing.StringifyAsJsonList(that.Keys)}"
                    );
                }
            }


            foreach (var error in base.TransformReference(that))
            {
                yield return error;
            }
        }
    }

    public static class Program
    {
        public static int Execute(
            string environmentPath,
            System.IO.TextWriter stdout,
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

            Aas.IEnvironment? environment;
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

            // ReSharper disable once JoinDeclarationAndInitializer
            string? error;
            (environment, error) = Referencing.Process(
                environment ?? throw new System.InvalidOperationException(
                    "Unexpected null instance"
                )
            );
            if (error != null)
            {
                stderr.WriteLine($"{fileInfo.FullName}: {error}");
                return 1;
            }

            if (environment == null)
            {
                throw new System.InvalidOperationException(
                    "Unexpected null environment"
                );
            }

            var verifier = new Verifier(environment);

            foreach (var verificationError in verifier.Transform(environment))
            {
                var jsonPath = Aas.Reporting.GenerateJsonPath(
                    verificationError.PathSegments
                );

                stdout.WriteLine(
                    $"{fileInfo.FullName}#{jsonPath}: {verificationError.Cause}"
                );
            }

            return 0;
        }

        [SuppressMessage("ReSharper", "RedundantNameQualifier")]
        static async System.Threading.Tasks.Task<int> Main(string[] args)
        {
            var rootCommand =
                new System.CommandLine.RootCommand(
                    "List model references in the environment which are " +
                    "not contained in it"
                );

            var environmentOption = new System.CommandLine.Option<
                string
            >(
                name: "--environment",
                description:
                "An AAS environment serialized as a JSON file to be searched for " +
                "dangling model references"
            )
            { IsRequired = true };
            rootCommand.AddOption(environmentOption);

            rootCommand.SetHandler(
                (
                    environmentPath
                ) => System.Threading.Tasks.Task.FromResult(
                    Execute(
                        environmentPath,
                        System.Console.Out,
                        System.Console.Error
                    )
                ),
                environmentOption
            );

            return await rootCommand.InvokeAsync(args);
        }
    }
}