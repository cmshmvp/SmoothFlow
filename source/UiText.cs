using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace SmoothFlow {
internal static class UiText {
    static readonly List<KeyValuePair<string,string>> choices = new List<KeyValuePair<string,string>>();
    static readonly Dictionary<string, Dictionary<string, string>> catalogs = Load();
    internal static volatile string Language = Resolve(CultureInfo.CurrentUICulture);
    internal static IEnumerable<KeyValuePair<string,string>> Choices {get {return choices;}}

    internal static string SelectLanguage(string preference) {
        foreach (var choice in choices)
            if (String.Equals(preference, choice.Key, StringComparison.OrdinalIgnoreCase)) {
                Language=choice.Key;
                return choice.Key;
            }
        Language=Resolve(CultureInfo.CurrentUICulture);
        return "auto";
    }

    static Dictionary<string, Dictionary<string, string>> Load() {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        XmlDocument xml = new XmlDocument();
        using (Stream stream = typeof(UiText).Assembly.GetManifestResourceStream("SmoothFlow.Languages.xml"))
            xml.Load(stream);
        foreach (XmlElement language in xml.DocumentElement.ChildNodes) {
            var messages = new Dictionary<string, string>();
            foreach (XmlElement message in language.ChildNodes)
                messages.Add(message.Name, message.InnerText);
            result.Add(language.GetAttribute("id"), messages);
            choices.Add(new KeyValuePair<string,string>(language.GetAttribute("id"),language.GetAttribute("native")));
        }
        return result;
    }

    internal static string Resolve(CultureInfo culture) {
        // Prefer an explicit Chinese script over its region, including on older .NET versions.
        string name = culture.Name;
        if (culture.TwoLetterISOLanguageName == "zh") {
            string[] parts = name.ToUpperInvariant().Split('-');
            if (Array.IndexOf(parts, "HANS") >= 0) return "zh-Hans";
            if (Array.IndexOf(parts, "HANT") >= 0 || Array.IndexOf(parts, "CHT") >= 0) return "zh-Hant";
            foreach (string region in new string[]{"TW", "HK", "MO"})
                if (Array.IndexOf(parts, region) >= 0) return "zh-Hant";
            return "zh-Hans";
        }
        while (culture.Name.Length > 0) {
            if (catalogs.ContainsKey(culture.Name)) return culture.Name;
            culture = culture.Parent;
        }
        return "en";
    }

    internal static string Get(string key) { return catalogs[Language][key]; }

    internal static void CheckCatalogs() {
        var english = catalogs["en"];
        foreach (var language in catalogs) {
            if (language.Value.Count != english.Count) throw new InvalidDataException(language.Key);
            foreach (string key in english.Keys)
                if (!language.Value.ContainsKey(key) || String.IsNullOrWhiteSpace(language.Value[key]))
                    throw new InvalidDataException(language.Key + "/" + key);
        }
    }
}
}
