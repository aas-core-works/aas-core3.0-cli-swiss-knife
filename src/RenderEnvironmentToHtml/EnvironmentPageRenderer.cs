using Aas = AasCore.Aas3_0; // renamed
using String = System.String;
using StringComparison = System.StringComparison;

// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;  // can't alias

// ReSharper disable once RedundantUsingDirective
using System.Linq; // can't alias

namespace RenderEnvironmentToHtml
{
    /**
     * Render the index page of an environment. 
     */
    internal static class EnvironmentPageRenderer
    {
        private static string RenderIdentifiables(
            IEnumerable<Aas.IIdentifiable> identifiables,
            string subdirectory
        )
        {
            var sorted = identifiables.ToList();
            sorted.Sort((a, b) => String.Compare(
                    a.Id, b.Id, StringComparison.Ordinal
                )
            );

            var parts = new List<string>();

            foreach (var identifiable in sorted)
            {
                parts.Add(
                    "<li>" +
                    $"<a href='{subdirectory}/{IdToBase64.Translate(identifiable.Id)}'>" +
                    $"{identifiable.Id}" +
                    "</a>" +
                    "</li>");
            }

            if (parts.Count == 0)
            {
                return "";
            }

            return $"<ul>{string.Join("\n", parts)}</ul>";
        }

        public static string Render(
            Registering.TypedRegistry<Aas.IAssetAdministrationShell> shells,
            Registering.TypedRegistry<Aas.ISubmodel> submodels,
            Registering.TypedRegistry<Aas.IConceptDescription> conceptDescriptions
        )
        {
            string shellPart = RenderIdentifiables(shells.Items, "shells");
            string submodelPart = RenderIdentifiables(submodels.Items, "submodels");
            string conceptDescriptionPart = RenderIdentifiables(
                conceptDescriptions.Items, "conceptDescriptions");

            var builder = new System.Text.StringBuilder();
            builder.AppendLine(
                "<html><head><title>AAS Environment</title></head><body>");

            if (shellPart.Length != 0)
            {
                builder.AppendLine("<h1>Asset Administration Shells</h1>");
                builder.AppendLine(shellPart);
            }

            if (submodelPart.Length != 0)
            {
                builder.AppendLine("<h1>Submodels</h1>");
                builder.AppendLine(submodelPart);
            }

            if (conceptDescriptionPart.Length != 0)
            {
                builder.AppendLine("<h1>Concept Descriptions</h1>");
                builder.AppendLine(conceptDescriptionPart);
            }

            builder.AppendLine("</body></html>");

            return builder.ToString();
        }
    }
}