using Microsoft.Extensions.Configuration;

namespace MPQToTACT.Helpers
{
    static class Extensions
    {
        private static readonly char[] Seperators = new char[] { '\\', '/' };

        public static string WoWNormalise(this string str)
        {
            return str.TrimStart(Seperators).Replace('\\', '/');
        }
    }
}
