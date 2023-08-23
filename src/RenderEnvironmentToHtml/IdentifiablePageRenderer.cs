using Aas = AasCore.Aas3_0; // renamed

// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic; // can't alias

namespace RenderEnvironmentToHtml
{
    /**
     * Render the page representing an identifiable.
     */
    internal static class IdentifiablePageRenderer
    {
        public static string Render(
            Aas.IIdentifiable identifiable,
            string title,
            Registering.TypedRegistry<Aas.IAssetAdministrationShell> shellRegistry,
            Registering.TypedRegistry<Aas.ISubmodel> submodelRegistry,
            Registering.TypedRegistry<Aas.IConceptDescription>
                conceptDescriptionRegistry
        )
        {
            var parts = new List<string>()
            {
                "<h1>" +
                $"{System.Web.HttpUtility.HtmlEncode(title)} " +
                $"{System.Web.HttpUtility.HtmlEncode(identifiable.Id)}" +
                "</h1>"
            };

            var elementRenderer = new ElementRenderer(
                shellRegistry,
                submodelRegistry,
                conceptDescriptionRegistry
            );

            parts.Add(elementRenderer.Transform(identifiable));

            return
                "<html>\n" +
                "<head>\n" +
                "<title>\n" +
                $"{System.Web.HttpUtility.HtmlEncode(title)} " +
                $"{System.Web.HttpUtility.HtmlEncode(identifiable.Id)}\n" +
                "</title>\n" +
                "</head><body>\n" +
                $"{string.Join("\n", parts)}\n" +
                "</body>\n" +
                "</html>\n";
        }
    }
}