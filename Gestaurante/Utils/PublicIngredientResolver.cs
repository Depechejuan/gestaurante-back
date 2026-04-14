using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Gestaurante.Models.DTO;

namespace Gestaurante.Utils
{
    public static class PublicIngredientResolver
    {
        private static readonly (string Label, string[] Keywords)[] KnownIngredients =
        [
            ("Ternera", ["ternera"]),
            ("Cerdo", ["cerdo"]),
            ("Pollo", ["pollo"]),
            ("Jamón", ["jamon", "jamón"]),
            ("Guanciale", ["guanciale"]),
            ("Bacon", ["bacon"]),
            ("Pepperoni", ["pepperoni"]),
            ("Chorizo picante", ["chorizo picante"]),
            ("Chorizo", ["chorizo"]),
            ("Tomate", ["tomate"]),
            ("Tomate seco", ["tomate seco", "tomates secos"]),
            ("Tomate cherry", ["tomate cherry", "tomates cherry"]),
            ("Salsa de tomate", ["salsa de tomate"]),
            ("Salsa boloñesa", ["boloñesa", "bolognesa"]),
            ("Mozzarella", ["mozzarella"]),
            ("Queso provolone", ["provolone"]),
            ("Queso pecorino", ["pecorino"]),
            ("Queso parmesano", ["parmesano", "parmesan"]),
            ("Ricotta", ["ricotta"]),
            ("Burrata", ["burrata"]),
            ("Gorgonzola", ["gorgonzola"]),
            ("Queso", ["queso"]),
            ("Nata", ["nata", "crema"]),
            ("Huevo", ["huevo", "yema"]),
            ("Pasta fresca", ["pasta fresca"]),
            ("Spaghetti", ["spaghetti"]),
            ("Tagliatelle", ["tagliatelle"]),
            ("Tortelli", ["tortelli"]),
            ("Ravioli", ["ravioli"]),
            ("Macarrones", ["macarones", "macarrones"]),
            ("Gnocchi", ["gnocchi"]),
            ("Pizza", ["pizza"]),
            ("Setas", ["seta", "setas"]),
            ("Champiñones", ["champiñ", "champin"]),
            ("Cebolla", ["cebolla"]),
            ("Cebolla caramelizada", ["cebolla caramelizada"]),
            ("Ajo", ["ajo"]),
            ("Pimiento", ["pimiento"]),
            ("Pimiento italiano", ["pimiento italiano"]),
            ("Berenjena", ["berenjena"]),
            ("Calabacín", ["calabacin", "calabacín"]),
            ("Aceitunas", ["aceituna"]),
            ("Rúcula", ["rucula", "rúcula"]),
            ("Espinacas", ["espinaca", "espinacas"]),
            ("Albahaca", ["albahaca"]),
            ("Trufa", ["trufa"]),
            ("Mostaza", ["mostaza"]),
            ("Miel", ["miel"]),
            ("Pistacho", ["pistacho", "pistachos"]),
            ("Almendra", ["almendra", "almendras"]),
            ("Gamba", ["gamba", "gambon", "gambón"]),
            ("Calamar", ["calamar"]),
            ("Almejas", ["almeja", "almejas"]),
            ("Mejillones", ["mejillon", "mejillón", "mejillones"]),
            ("Atún", ["atun", "atún"]),
            ("Anchoa", ["anchoa", "anchoas"]),
            ("Salmón", ["salmon", "salmón"]),
            ("Pulpo", ["pulpo"]),
            ("Sepia", ["sepia"]),
            ("Pan", ["pan"]),
            ("Harina", ["harina"])
        ];

        public static List<PlatoIngredienteDTO> ResolveForPublic(IEnumerable<PlatoIngredienteDTO> ingredientes)
        {
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ingrediente in ingredientes ?? [])
            {
                foreach (var label in ResolveNames(ingrediente.Nombre))
                    labels.Add(label);
            }

            return labels
                .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                .Select(label => new PlatoIngredienteDTO
                {
                    IdIngrediente = CreateStableGuid(label),
                    Nombre = label
                })
                .ToList();
        }

        private static IEnumerable<string> ResolveNames(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return [];

            var normalized = Normalize(rawValue);
            var matches = KnownIngredients
                .Where(rule => rule.Keywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal)))
                .Select(rule => rule.Label)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matches.Count > 0)
                return matches;

            return SplitFallback(rawValue);
        }

        private static IEnumerable<string> SplitFallback(string rawValue)
        {
            return rawValue
                .Replace(" y ", ", ", StringComparison.OrdinalIgnoreCase)
                .Replace(" con ", ", ", StringComparison.OrdinalIgnoreCase)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(segment => segment.Trim())
                .Where(segment => segment.Length > 1)
                .Take(4)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string Normalize(string value)
        {
            var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static Guid CreateStableGuid(string value)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
            return new Guid(bytes);
        }
    }
}
