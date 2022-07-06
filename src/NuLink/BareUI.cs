using System.Linq.Expressions;

namespace NuLink
{
    public class BareUi : IUserInterface
    {
        public void Report(
            VerbosityLevel level, 
            Expression<Func<string>> message, 
            params ConsoleColor[] paramColors)
        {
            switch (level)
            {
                case VerbosityLevel.Error:
                    Console.Error.WriteLine(message.Compile().Invoke());
                    break;
                case VerbosityLevel.Data:
                    Console.WriteLine(message.Compile().Invoke());
                    break;
            }
        }
    }
}
