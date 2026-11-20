using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Services.Languages
{
    public class LocalizationDataParser
    {
        public Dictionary<string, Dictionary<string, string>> Parse(string[] lines, string[] headers)
        {
            var localizations = new Dictionary<string, Dictionary<string, string>>();
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrEmpty(line)) continue;

                var localization = ParseLine(line, headers);
                localizations[localization.Key] = localization.Value;
            }

            return localizations;
        }
        
        private KeyValuePair<string, Dictionary<string, string>> ParseLine(string line, string[] headers)
        {
            List<string> fields = new List<string>();
            StringBuilder field = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // Si hay una comilla doble escapada ("")
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++; // saltar la comilla escapada
                    }
                    else
                    {
                        insideQuotes = !insideQuotes; // Entrar o salir de comillas
                    }
                }
                else if (c == ',' && !insideQuotes)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            fields.Add(field.ToString()); // último campo

            // Validación simple
            if (fields.Count != headers.Length + 1)
                throw new FormatException($"Número de campos inválido en la línea: {line}");

            string key = fields[0];
            var localization = new Dictionary<string, string>();

            for (int i = 0; i < headers.Length; i++)
            {
                localization[headers[i]] = fields[i + 1];
            }

            return new KeyValuePair<string, Dictionary<string, string>>(key, localization);
        }
    }
}