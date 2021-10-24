using System.Collections.Generic;

namespace NotEnoughAV1Encodes.resources
{
    internal class MediaLanguages
    {
        public static Dictionary<string, string> Languages = new();
        public static List<string> LanguageKeys = new();

        public static void FillDictionary()
        {
            // Languages in ISO 639-2 Format: https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes
            Languages.Add("English", "eng");
            Languages.Add("Arabic", "ara");
            Languages.Add("Bosnian", "bos");
            Languages.Add("Bulgarian", "bul");
            Languages.Add("Chinese", "zho");
            Languages.Add("Czech", "ces");
            Languages.Add("Greek", "ell");
            Languages.Add("Estonian", "est");
            Languages.Add("Persian", "per");
            Languages.Add("Filipino", "fil");
            Languages.Add("Finnish", "fin");
            Languages.Add("French", "fra");
            Languages.Add("Georgian", "kat");
            Languages.Add("German", "deu");
            Languages.Add("Croatian", "hrv");
            Languages.Add("Hungarian", "hun");
            Languages.Add("Indonesian", "ind");
            Languages.Add("Icelandic", "isl");
            Languages.Add("Italian", "ita");
            Languages.Add("Japanese", "jpn");
            Languages.Add("Korean", "kor");
            Languages.Add("Latin", "lat");
            Languages.Add("Latvian", "lav");
            Languages.Add("Lithuanian", "lit");
            Languages.Add("Dutch", "nld");
            Languages.Add("Norwegian", "nob");
            Languages.Add("Polish", "pol");
            Languages.Add("Portuguese", "por");
            Languages.Add("Romanian", "ron");
            Languages.Add("Russian", "rus");
            Languages.Add("Slovak", "slk");
            Languages.Add("Slovenian", "slv");
            Languages.Add("Spanish", "spa");
            Languages.Add("Swedish", "swe");
            Languages.Add("Thai", "tha");
            Languages.Add("Turkish", "tur");
            Languages.Add("Ukrainian", "ukr");
            Languages.Add("Vietnamese", "vie");

            foreach(string language in Languages.Keys)
            {
                LanguageKeys.Add(language);
            }
        }
    }
}
