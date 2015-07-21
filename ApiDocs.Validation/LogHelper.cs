using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiDocs.Validation
{
    public static class LogHelper
    {
        private static ILogHelper StaticLogHelper { get; set; }
        public static void ProvideLogHelper(ILogHelper helper)
        {
            StaticLogHelper = helper;
        }

        public static void LogFailure(string message)
        {
            var helper = StaticLogHelper;
            if (null != helper)
            {
                helper.RecordFailure(message);
            }
            else
            {
                Console.WriteLine("Failure: " + message);
            }
        }

        public static void LogFailure(string format, params object[] parameters)
        {
            LogFailure(string.Format(format, parameters));
        }

        public static void LogWarning(string message)
        {
            var helper = StaticLogHelper;
            if (null != helper)
            {
                helper.RecordWarning(message);
            }
            else
            {
                Console.WriteLine("Warning: " + message);
            }

        }

        public static void LogWarning(string format, params object[] parameters)
        {
            LogWarning(string.Format(format, parameters));
        }
    }

    public interface ILogHelper
    {
        void RecordFailure(string message);
        void RecordWarning(string message);
    }
}
