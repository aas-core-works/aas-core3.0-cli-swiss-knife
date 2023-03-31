namespace CsvParsing
{
    internal class TextReaderWhichIgnoresReturnCarrier : System.IO.TextReader
    {
        private readonly System.IO.TextReader _reader;
        private int _lookahead;

        public TextReaderWhichIgnoresReturnCarrier(System.IO.TextReader reader)
        {
            _reader = reader;

            _lookahead = '\r';
            while (_lookahead == '\r')
            {
                _lookahead = _reader.Read();
            }
        }

        /**
         * <summary>
         * Read the character from the stream while it is not a <c>'\r'</c>.
         * </summary>
         * <returns>The character read, or -1 if no character on the stream.</returns>
         */
        public override int Read()
        {
            int result = _lookahead;

            if (_lookahead != -1)
            {
                _lookahead = '\r';
                while (_lookahead == '\r')
                {
                    _lookahead = _reader.Read();
                }
            }

            return result;
        }

        public override int Peek()
        {
            return _lookahead;
        }
    }
}