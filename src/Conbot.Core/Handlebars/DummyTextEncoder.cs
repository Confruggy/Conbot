using System.Collections.Generic;
using System.IO;
using System.Text;

using HandlebarsDotNet;

namespace Conbot
{
    internal class DummyTextEncoder : ITextEncoder
    {
        public void Encode(StringBuilder text, TextWriter target) => target.Write(text);

        public void Encode(string text, TextWriter target) => target.Write(text);

        public void Encode<T>(T text, TextWriter target) where T : IEnumerator<char> => target.Write(text);
    }
}
