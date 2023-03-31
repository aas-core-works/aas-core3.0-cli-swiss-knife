// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;  // can't alias

namespace CsvParsing
{
    public class CsvReader
    {
        private readonly TextReaderWhichIgnoresReturnCarrier _reader;

        public CsvReader(System.IO.TextReader reader)
        {
            _reader = new TextReaderWhichIgnoresReturnCarrier(reader);
        }

        private (string?, string?) ReadQuotedCell()
        {
            // Pre-condition
            if (_reader.Peek() != '"')
            {
                throw new System.InvalidOperationException(
                    $"Expected to peek a double-quote, but got: {_reader.Peek()}"
                );
            }

            _reader.Read();

            int? current = _reader.Peek();
            _reader.Read();
            int? next = _reader.Peek();

            var builder = new System.Text.StringBuilder();

            while (true)
            {
                if (current == -1)
                {
                    return (
                        null,
                        "Expected to find the closing double-quote ('\"'), " +
                        "but got an end-of-stream"
                    );
                }

                if (current == '"' && next == '"')
                {
                    builder.Append('"');

                    _reader.Read();
                    current = _reader.Peek();
                    _reader.Read();
                    next = _reader.Peek();
                }
                else if (current == '"' && next != '"')
                {
                    return (builder.ToString(), null);
                }
                else
                {
                    builder.Append((char)current);

                    _reader.Read();
                    current = next;
                    next = _reader.Peek();
                }
            }
        }

        private string ReadUnquotedCell()
        {
            // Pre-condition
            if (_reader.Peek() is -1 or ',' or '\n')
            {
                throw new System.InvalidOperationException(
                    $"Expected the cell to start with a valid character, but got: {_reader.Peek()}; " +
                    "the peek has not been handled correctly before."
                );
            }

            var builder = new System.Text.StringBuilder();

            int? lookahead = _reader.Peek();
            while (lookahead is not (-1 or ',' or '\n'))
            {
                builder.Append((char)lookahead);

                _reader.Read();
                lookahead = _reader.Peek();
            }

            return builder.ToString();
        }

        private (string?, string?) ReadCell()
        {
            var lookahead = _reader.Peek();
            if (lookahead is -1 or ',' or '\n')
            {
                return ("", null);
            }

            if (lookahead == '"')
            {
                return ReadQuotedCell();
            }

            return (ReadUnquotedCell(), null);
        }



        public (List<string>?, string?) ReadRow(int expectedCells)
        {
            if (_reader.Peek() == -1)
            {
                return (null, null);
            }

            if (_reader.Peek() == '\n')
            {
                _reader.Read();
                return (new List<string>(), null);
            }

            var row = new List<string>(expectedCells);

            while (true)
            {
                var (cell, error) = ReadCell();
                if (error != null)
                {
                    return (null, error);
                }

                row.Add(
                    cell ?? throw new System.InvalidOperationException(
                        "Unexpected cell null"
                    )
                );

                var lookahead = _reader.Peek();
                if (lookahead == -1)
                {
                    return (row, null);
                }

                if (lookahead == '\n')
                {
                    _reader.Read();
                    return (row, null);
                }

                if (lookahead == ',')
                {
                    _reader.Read();
                    continue;
                }

                return (
                    null,
                    "Expected either a new line, a comma or an end-of-stream, " +
                    $"but got: {(char)lookahead} (character code: {lookahead})"
                );
            }
        }
    }
}