using System.Globalization;
using WPFLocalizeExtension.Engine;

namespace NotEnoughAV1Encodes.resources.lang
{
    public class LocalizedStrings
    {
        public static LocalizedStrings Instance { get; } = new LocalizedStrings();

        public static void SetCulture(CultureInfo cultureInfo)
        {
            LocalizeDictionary.Instance.Culture = cultureInfo;
        }

        public string this[string key]
        {
            get
            {
                var result = LocalizeDictionary.Instance.GetLocalizedObject("NotEnoughAV1Encodes", "Strings", key, LocalizeDictionary.Instance.Culture);
                return result as string;
            }
        }
    }
}
