// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;  // can't alias

namespace CsvParsing
{
    public class CsvDictionaryReader
    {
        private readonly CsvReader _reader;
        private List<string>? _header;
        private Dictionary<string, string>? _dictionary;


        public CsvDictionaryReader(CsvReader reader)
        {
            _reader = reader;
        }

        /**
         * <summary>Read the header from the reader.</summary>
         * <returns>Error, if any.</returns>
         */
        public string? ReadHeader()
        {
            if (_header != null)
            {
                throw new System.InvalidOperationException(
                    "The header has been already read"
                );
            }

            var (row, error) = _reader.ReadRow(10);

            if (error != null)
            {
                return error;
            }

            if (row == null)
            {
                return "Expected a header row, but got end-of-stream";
            }

            _header = row;
            return null;
        }

        public IReadOnlyList<string> Header
        {
            get
            {
                if (_header == null)
                {
                    throw new System.InvalidOperationException(
                        "Unexpected null header; have you read it before?"
                    );
                }

                return _header;
            }
        }

        /**
         * <summary>Read a single row of the table, after the header.</summary>
         * <remarks>Use <see cref="Row" /> to access the read row.</remarks>
         * <returns>Error, if any.</returns>
         */
        public string? ReadRow()
        {
            if (_header == null)
            {
                throw new System.InvalidOperationException(
                    "The header has not been read yet"
                );
            }

            var (row, error) = _reader.ReadRow(_header.Count);

            if (error != null)
            {
                return error;
            }

            if (row == null)
            {
                _dictionary = null;
                return null;
            }

            if (row.Count != _header.Count)
            {
                return $"Expected {_header.Count} cell(s), but got {row.Count} cell(s)";
            }

            _dictionary ??= new Dictionary<string, string>();

            for (int i = 0; i < _header.Count; i++)
            {
                _dictionary[_header[i]] = row[i];
            }

            return null;
        }

        /**
         * <summary>Get the current row.</summary>
         * <returns>The row, or null if reached the end-of-stream.</returns>
         */
        public IReadOnlyDictionary<string, string>? Row => _dictionary;
    }
}