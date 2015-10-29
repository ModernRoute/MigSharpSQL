using System;
using System.Diagnostics;
using System.Reflection;

namespace MigSharpSQL.Logging
{
    public static class LoggerFactory
    {
        private static string GetClassFullName()
        {
            string className;
            Type declaringType;
            int framesToSkip = 2;

            do
            {
                StackFrame frame = new StackFrame(framesToSkip, false);
                MethodBase method = frame.GetMethod();
                declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    className = method.Name;
                    break;
                }

                framesToSkip++;
                className = declaringType.FullName;
            } while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

            return className;
        }

        private static Func<string, ILogger> _Builder = name => new NullLogger();

        public static void SetBuilder(Func<string, ILogger> builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            _Builder = builder;
        }

        public static ILogger GetCurrentClassLogger()
        {
            return _Builder(GetClassFullName());
        }
    }
}
