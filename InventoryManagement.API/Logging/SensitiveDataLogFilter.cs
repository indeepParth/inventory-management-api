using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace InventoryManagement.API.Logging
{
    public sealed class SensitiveDataLogFilter : ILogEventFilter
    {
        private static readonly string[] SensitiveNames =
        [
            "password",
            "token",
            "authorization",
            "secret",
            "apikey",
            "api_key",
            "jwt",
            "key",
            "externalreference",
            "paymentreference",
            "reference"
        ];

        public bool IsEnabled(LogEvent logEvent)
        {
            return !HasSensitivePropertyName(logEvent) &&
                   !HasSensitiveMessageTemplate(logEvent) &&
                   !HasSensitivePropertyValue(logEvent);
        }

        private static bool HasSensitivePropertyName(LogEvent logEvent)
        {
            return logEvent.Properties.Keys.Any(IsSensitiveName) ||
                   logEvent.MessageTemplate.Tokens
                       .OfType<PropertyToken>()
                       .Any(token => IsSensitiveName(token.PropertyName));
        }

        private static bool HasSensitiveMessageTemplate(LogEvent logEvent)
        {
            var template = logEvent.MessageTemplate.Text;

            return template.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                   template.Contains("Authorization", StringComparison.OrdinalIgnoreCase) ||
                   template.Contains("RefreshToken", StringComparison.OrdinalIgnoreCase) ||
                   template.Contains("AccessToken", StringComparison.OrdinalIgnoreCase) ||
                   template.Contains("Password", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasSensitivePropertyValue(LogEvent logEvent)
        {
            return logEvent.Properties.Values.Any(HasSensitiveValue);
        }

        private static bool HasSensitiveValue(LogEventPropertyValue value)
        {
            return value switch
            {
                ScalarValue { Value: string text } => IsSensitiveText(text),
                SequenceValue sequence => sequence.Elements.Any(HasSensitiveValue),
                StructureValue structure => structure.Properties.Any(property =>
                    IsSensitiveName(property.Name) ||
                    HasSensitiveValue(property.Value)),
                DictionaryValue dictionary => dictionary.Elements.Any(element =>
                    HasSensitiveValue(element.Key) ||
                    HasSensitiveValue(element.Value)),
                _ => false
            };
        }

        private static bool IsSensitiveText(string text)
        {
            return text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                   LooksLikeJwt(text);
        }

        private static bool LooksLikeJwt(string text)
        {
            var parts = text.Split('.');

            return parts.Length == 3 &&
                   parts.All(part => part.Length > 10) &&
                   parts.All(part => part.All(character =>
                       char.IsLetterOrDigit(character) ||
                       character is '-' or '_' or '=')); 
        }

        private static bool IsSensitiveName(string name)
        {
            var normalized = name.Replace("_", string.Empty);

            return SensitiveNames.Any(sensitiveName =>
                normalized.Contains(
                    sensitiveName.Replace("_", string.Empty),
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
