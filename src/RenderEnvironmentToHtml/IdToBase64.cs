namespace RenderEnvironmentToHtml
{
    internal static class IdToBase64
    {
        static readonly char[] Base64Padding = { '=' };

        internal static string Translate(string id)
        {
            var idBytes = System.Text.Encoding.UTF8.GetBytes(id);
            var idBase64 = System.Convert.ToBase64String(idBytes);

            // NOTE (mristin, 2023-04-05):
            // We use here the URL-safe Base64 encoding.
            //
            // See: https://stackoverflow.com/questions/26353710/how-to-achieve-base64-url-safe-encoding-in-c
            idBase64 = idBase64
                .TrimEnd(Base64Padding)
                .Replace('+', '-')
                .Replace('/', '_');

            return idBase64;
        }
    }
}
