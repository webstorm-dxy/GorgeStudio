using System;
using System.IO;
using System.Text;

namespace Gorge.GorgeCompiler
{
    public class LogWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.Default;

        private readonly string _head;

        public LogWriter(string head)
        {
            _head = head;
        }

        public override void Write(string value)
        {
            base.Write(value);
            Console.WriteLine("[" + _head + "] " + value);
        }
    }
}