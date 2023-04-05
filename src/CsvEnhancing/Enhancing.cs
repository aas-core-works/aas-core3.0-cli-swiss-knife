using Aas = AasCore.Aas3_0; // renamed

namespace CsvEnhancing
{
    public static class Enhancing
    {
        /**
         * <summary>Enhance the instances with the source in the CSV files.</summary>
         */
        public class Enhancement
        {
            public readonly int RowIndex;
            public readonly string Path;

            public Enhancement(int rowIndex, string path)
            {
                RowIndex = rowIndex;
                Path = path;
            }
        }

        public static readonly Aas.Enhancing.Unwrapper<Enhancement> Unwrapper = new();
    }
}