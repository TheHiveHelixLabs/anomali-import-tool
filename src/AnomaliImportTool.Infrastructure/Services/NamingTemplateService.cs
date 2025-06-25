using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnomaliImportTool.Infrastructure.Services
{
    public class NamingTemplateService
    {
        public string ReplacePlaceholders(string template, Dictionary<string, string> document)
        {
            var result = template;

            // Replace custom field placeholders {field:FieldName}
            var fieldPattern = new Regex("\\{field:(?<name>[a-zA-Z0-9_]+)\\}");
            result = fieldPattern.Replace(result, m =>
            {
                var key = m.Groups["name"].Value;
                if (document.TryGetValue(key, out var val))
                    return val;
                return "";
            });

            return result;
        }
    }
} 