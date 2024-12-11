using System;
using System.IO;
using System.Linq;

namespace MPQToTACT.Helpers
{
    /// <summary>
    /// Stolen from Cromon/Neo
    /// </summary>
    internal static class MPQSorter
    {
        public static int Sort(string a1, string a2)
        {
            var archive1 = new MPQInfo(a1);
            var archive2 = new MPQInfo(a2);

            // Alpha archives under Data/World/
            if (archive1.Name.Length == 0 || archive2.Name.Length == 0)
                return -archive1.Name.Length.CompareTo(archive2.Name.Length);

            if (archive1.IsLocale != archive2.IsLocale)
                return archive2.IsLocale ? -1 : 1;

            if (archive1.IsLocale)
            {
                if (archive1.Name.Contains("expansion") && archive2.Name.Contains("locale"))
                    return -1;
                if (archive2.Name.Contains("expansion") && archive1.Name.Contains("locale"))
                    return 1;
            }

            // WoWTest should take prio
            if (!a1.Contains("WoWTest") && a2.Contains("WoWTest"))
            {
                Console.WriteLine("Prioritizing WoWTest: " + a2 + " > " + a1);
                return -1;
            }

            if (a1.Contains("WoWTest") && !a2.Contains("WoWTest"))
            {
                Console.WriteLine("Prioritizing WoWTest: " + a1 + " > " + a2);
                return -1;
            }

            if (archive1.IsPatch != archive2.IsPatch)
                return archive2.IsPatch ? 1 : -1;

            if (!archive1.IsPatch)
                return -string.Compare(archive1.Name, archive2.Name, StringComparison.Ordinal);

            // no number -> for example patch.MPQ
            if (archive1.Name[archive1.PatchIndex + 5] == '.' || archive1.PatchNum.Length == 0)
                archive1.PatchNum = "0.mpq";

            if (archive2.Name[archive2.PatchIndex + 5] == '.' || archive2.PatchNum.Length == 0)
                archive2.PatchNum = "0.mpq";

            var isLocalePatch1 = !char.IsDigit(archive1.PatchNum[0]);
            var isLocalePatch2 = !char.IsDigit(archive2.PatchNum[0]);
            if (isLocalePatch1 != isLocalePatch2)
                return isLocalePatch1 ? 1 : -1;

            if (isLocalePatch1)
            {
                var hasNum1 = archive1.ExtIndex >= 1 && char.IsDigit(archive1.Name[archive1.ExtIndex - 1]);
                var hasNum2 = archive2.ExtIndex >= 1 && char.IsDigit(archive2.Name[archive2.ExtIndex - 1]);
                if (hasNum1 != hasNum2)
                    return hasNum1 ? -1 : 1;
            }

            return string.Compare(archive1.PatchNum, archive2.PatchNum, StringComparison.OrdinalIgnoreCase);
        }

        private struct MPQInfo
        {
            public string Name;
            public bool IsAlpha;
            public bool IsLocale;
            public bool IsPatch;
            public int PatchIndex;
            public string PatchNum;
            public int ExtIndex;

            public MPQInfo(string archive)
            {
                Name = Path.GetFileName(archive).ToLowerInvariant();

                IsLocale = Name.Contains("locale") || Name.Contains("speech") || Name.Contains("base");
                IsPatch = Name.Contains("patch");
                PatchIndex = Name.IndexOf("patch", StringComparison.Ordinal);
                PatchNum = Name[(PatchIndex + 6)..];
                ExtIndex = Name.LastIndexOf('.');
            }
        }
    }
}
