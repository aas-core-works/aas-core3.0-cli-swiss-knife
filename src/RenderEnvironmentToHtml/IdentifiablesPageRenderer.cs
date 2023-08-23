using Aas = AasCore.Aas3_0; // renamed
using String = System.String;
using StringComparison = System.StringComparison;

// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic; // can't alias

// ReSharper disable once RedundantUsingDirective
using System.Linq; // can't alias

namespace RenderEnvironmentToHtml
{
    /**
     * Render the index page of a collection of identifiables.
     */
    internal static class IdentifiablesPageRenderer
    {
        public static string Render(
            IEnumerable<Aas.IIdentifiable> identifiables,
            string title
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
                    $"<a href='{IdToBase64.Translate(identifiable.Id)}.html'>" +
                    $"{System.Web.HttpUtility.HtmlEncode(identifiable.Id)}" +
                    "</a>" +
                    "</li>"
                );
            }

            if (parts.Count == 0)
            {
                return "";
            }

            return
                "<html>\n" +
                "<head>\n" +
                "<title>\n" +
                $"{System.Web.HttpUtility.HtmlEncode(title)}\n" +
                "</title>\n" +
                "</head>\n" +
                "<body>\n" +
                $"<h1>{System.Web.HttpUtility.HtmlEncode(title)}</h1>\n" +
                "<ul>\n" +
                $"{string.Join("\n", parts)}\n" +
                "</ul>\n" +
                "</body>\n" +
                "</html>\n";
        }
    }
}