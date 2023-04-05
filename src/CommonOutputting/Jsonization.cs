using Aas = AasCore.Aas3_0; // renamed

namespace CommonOutputting
{
    public static class Jsonization
    {
        /**
         * <summary>Output the environment either to a file or to STDOUT.</summary>
         * <returns>Error message, if any</returns>
         */
        public static string? Serialize(
            Aas.IEnvironment environment,
            string output,
            System.IO.Stream? stdout
        )
        {
            System.Text.Json.Nodes.JsonObject? jsonObject;

            try
            {
                jsonObject = Aas.Jsonization.Serialize.ToJsonObject(environment);
            }
            catch (Aas.Jsonization.Exception exception)
            {
                return (
                    $"Failed to serialize the environment to JSON: {exception.Message}"
                );
            }

            var writerOptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = true
            };

            try
            {
                if (output == "-")
                {
                    if (stdout == null)
                    {
                        throw new System.InvalidOperationException(
                            "Unexpected null stdout"
                        );
                    }

                    using var writer = new System.Text.Json.Utf8JsonWriter(
                        stdout, writerOptions
                    );
                    jsonObject.WriteTo(writer);
                }
                else
                {
                    using var outputStream = System.IO.File.OpenWrite(output);
                    using var writer =
                        new System.Text.Json.Utf8JsonWriter(
                            outputStream,
                            writerOptions
                        );
                    jsonObject.WriteTo(writer);
                }
            }
            catch (System.Exception exception)
            {
                return $"Failed to write to {output}: {exception.Message}";
            }

            return null;
        }
    }
}
