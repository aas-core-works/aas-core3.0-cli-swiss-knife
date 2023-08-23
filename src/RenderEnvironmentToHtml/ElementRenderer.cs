using Aas = AasCore.Aas3_0; // renamed

// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic; // can't alias

namespace RenderEnvironmentToHtml
{
    /**
     * Render the elements on a page in a customized matter.
     */
    internal class ElementRenderer : GeneratedElementRenderer
    {
        private readonly Registering.TypedRegistry<Aas.IAssetAdministrationShell>
            _shellRegistry;

        private readonly Registering.TypedRegistry<Aas.ISubmodel>
            _submodelRegistry;

        private readonly Registering.TypedRegistry<Aas.IConceptDescription>
            _conceptDescriptionRegistry;

        public ElementRenderer(
            Registering.TypedRegistry<Aas.IAssetAdministrationShell> shellRegistry,
            Registering.TypedRegistry<Aas.ISubmodel> submodelRegistry,
            Registering.TypedRegistry<Aas.IConceptDescription>
                conceptDescriptionRegistry
        )
        {
            _shellRegistry = shellRegistry;
            _submodelRegistry = submodelRegistry;
            _conceptDescriptionRegistry = conceptDescriptionRegistry;
        }

        public override string TransformReference(Aas.IReference that)
        {
            if (that.Type != Aas.ReferenceTypes.ModelReference)
            {
                return base.TransformReference(that);
            }

            if (that.Keys.Count == 0)
            {
                return base.TransformReference(that);
            }

            string identifier = that.Keys[0].Value;

            string basis;
            var textParts = new List<string>();
            switch (that.Keys[0].Type)
            {
                case Aas.KeyTypes.AssetAdministrationShell:
                    if (_shellRegistry.TryGet(identifier) != null)
                    {
                        textParts.Add($"Asset Administration Shell {identifier}");
                        string idBase64 = IdToBase64.Translate(identifier);
                        basis = $"../shells/{idBase64}.html";
                    }
                    else
                    {
                        return base.TransformReference(that);
                    }

                    break;
                case Aas.KeyTypes.Submodel:
                    if (_submodelRegistry.TryGet(identifier) != null)
                    {
                        textParts.Add($"Submodel {identifier}");
                        string idBase64 = IdToBase64.Translate(identifier);
                        basis = $"../submodels/{idBase64}.html";
                    }
                    else
                    {
                        return base.TransformReference(that);
                    }

                    break;
                case Aas.KeyTypes.ConceptDescription:
                    if (_conceptDescriptionRegistry.TryGet(identifier) != null)
                    {
                        textParts.Add($"Concept Description {identifier}");
                        string idBase64 = IdToBase64.Translate(identifier);
                        basis = $"../conceptDescriptions/{idBase64}.html";
                    }
                    else
                    {
                        return base.TransformReference(that);
                    }

                    break;
                default:
                    return base.TransformReference(that);
            }

            var anchorParts = new List<string>();
            var stopAnchoring = false;
            for (var i = 1; i < that.Keys.Count; i++)
            {
                var key = that.Keys[i];

                switch (key.Type)
                {
                    case Aas.KeyTypes.FragmentReference:
                        stopAnchoring = true;
                        textParts.Add($"#{key.Value}");
                        break;
                    case Aas.KeyTypes.GlobalReference:
                        throw new System.InvalidOperationException(
                            "Unexpected global reference in a middle of " +
                            $"a model reference at key {i}: {key.Value}"
                        );
                    default:
                        textParts.Add(key.Value);
                        if (!stopAnchoring)
                        {
                            anchorParts.Add(key.Value);
                        }

                        break;
                }
            }

            var escapedAnchor = System.Web.HttpUtility.UrlEncode(
                string.Join("→", anchorParts)
            );

            var escapedText = System.Web.HttpUtility.HtmlEncode(
                string.Join("→", textParts
                )
            );

            if (escapedAnchor.Length > 0)
            {
                return $"<a href='{basis}#{escapedAnchor}'\n" +
                       $">{escapedText}</a>";
            }

            return $"<a href='{basis}'\n" +
                   $">{escapedText}</a>";
        }
    }
}