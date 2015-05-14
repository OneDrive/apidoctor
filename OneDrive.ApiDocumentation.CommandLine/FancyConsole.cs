using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OneDrive.ApiDocumentation.ConsoleApp
{
    public class FancyConsole
    {
        public const ConsoleColor ConsoleDefaultColor = ConsoleColor.White;
        public const ConsoleColor ConsoleHeaderColor = ConsoleColor.Cyan;
        public const ConsoleColor ConsoleSubheaderColor = ConsoleColor.DarkCyan;
        public const ConsoleColor ConsoleCodeColor = ConsoleColor.Gray;
        public const ConsoleColor ConsoleErrorColor = ConsoleColor.Red;
        public const ConsoleColor ConsoleWarningColor = ConsoleColor.Yellow;
        public const ConsoleColor ConsoleSuccessColor = ConsoleColor.Green;

        
        private static string _logFileName;
        private static StreamWriter _logWriter;

        public static bool WriteVerboseOutput { get; set; }

        public static string LogFileName 
        {
            get { return _logFileName; }
            set
            {
                _logFileName = value;
                if (!string.IsNullOrEmpty(_logFileName))
                {
                    _logWriter = new StreamWriter(_logFileName, false);
                    _logWriter.AutoFlush = true;
                }
                else
                {
                    _logWriter = null;
                }
            }
        }

        public static void Write(string output)
        {
            if (null != _logWriter) _logWriter.Write(output);
            Console.Write(output);
        }

        public static void Write(ConsoleColor color, string output)
        {
            if (null != _logWriter) _logWriter.Write(output);

            Console.ForegroundColor = color;
            Console.Write(output);
            Console.ResetColor();
        }

        public static void Write(string format, params object[] values)
        {
            if (null != _logWriter) _logWriter.Write(format, values);
            Console.Write(format, values);
        }

        public static void Write(ConsoleColor color, string format, params object[] values)
        {
            if (null != _logWriter) _logWriter.Write(format, values);

            Console.ForegroundColor = color;
            Console.Write(format, values);
            Console.ResetColor();
        }

        public static void WriteLine()
        {
            if (null != _logWriter) _logWriter.WriteLine();
            Console.WriteLine();
        }

        public static void WriteLine(string format, params object[] values)
        {
            if (null != _logWriter) _logWriter.WriteLine(format, values);
            Console.WriteLine(format, values);
        }

        public static void WriteLine(string output)
        {
            if (null != _logWriter) _logWriter.WriteLine(output);
            Console.WriteLine(output);
        }

        public static void WriteLine(ConsoleColor color, string format, params object[] values)
        {
            if (null != _logWriter) _logWriter.WriteLine(format, values);

            Console.ForegroundColor = color;
            Console.WriteLine(format, values);
            Console.ResetColor();
        }

        public static void WriteLineIndented(string indentString, ConsoleColor color, string output)
        {
            Console.ForegroundColor = color;
            WriteLineIndented(indentString, output);
            Console.ResetColor();
        }

        public static void WriteLineIndented(string indentString, ConsoleColor color, string format, params object[] values)
        {
            Console.ForegroundColor = color;
            WriteLineIndented(indentString, format, values);
            Console.ResetColor();
        }

        public static void WriteLineIndented(string indentString, string format, params object[] values)
        {
            using (System.IO.StringReader reader = new System.IO.StringReader(string.Format(format, values)))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    WriteLine(string.Concat(indentString, line));
                }
            }
        }

        public static void WriteLineIndented(string indentString, string output)
        {
            using (System.IO.StringReader reader = new System.IO.StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    WriteLine(string.Concat(indentString, line));
                }
            }
        }

        public static void VerboseWriteLine()
        {
            if (WriteVerboseOutput)
            {
                WriteLine();
            }
        }

        public static void VerboseWriteLine(string output)
        {
            if (WriteVerboseOutput)
            {
                WriteLine(output);
            }
        }

        public static void VerboseWriteLine(string format, params object[] parameters)
        {
            if (WriteVerboseOutput)
            {
                WriteLine(format, parameters);
            }
        }

        public static void VerboseWriteLineIndented(string indent, string output)
        {
            if (WriteVerboseOutput)
            {
                WriteLineIndented(indent, output);
            }
        }

        public static void VerboseWriteLineIndented(string indent, string format, params object[] parameters)
        {
            if (WriteVerboseOutput)
            {
                WriteLineIndented(indent, format, parameters);
            }
        }

    }
}
