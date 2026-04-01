namespace Gestaurante.Utils
{
    public static class AllergenResolver
    {
        private static readonly (string Allergen, string[] Keywords)[] Rules =
        [
            ("Gluten", ["trigo", "harina", "pan", "pizza", "pasta", "fideo", "espagueti", "macarr", "cebada", "centeno", "avena", "sémola", "semola", "cuscús", "cuscus", "rebozado", "tempura", "masa"]),
            ("Crustáceos", ["gamba", "langost", "cigala", "carabinero", "crust"]),
            ("Huevo", ["huevo", "mayonesa", "alioli", "mahonesa", "tortilla"]),
            ("Pescado", ["atún", "atun", "salmón", "salmon", "bacalao", "boquer", "anchoa", "pescado", "merluza", "pez espada", "bonito"]),
            ("Cacahuetes", ["cacahuete", "cacahuete", "mani", "maní", "peanut"]),
            ("Soja", ["soja", "soy", "edamame", "tofu"]),
            ("Leche", ["leche", "queso", "mozzarella", "parmesano", "parmesan", "cheddar", "gouda", "brie", "camembert", "nata", "crema", "mantequilla", "bechamel", "yogur", "yogurt"]),
            ("Frutos de cáscara", ["almendra", "avellana", "nuez", "pistacho", "anacardo", "pecana", "piñon", "piñón", "macadamia"]),
            ("Apio", ["apio"]),
            ("Mostaza", ["mostaza"]),
            ("Sésamo", ["sésamo", "sesamo"]),
            ("Sulfitos", ["vino", "vinagre", "sulfito"]),
            ("Moluscos", ["mejill", "almeja", "pulpo", "calamar", "sepia", "choco", "molusc"]),
            ("Altramuces", ["altramuz", "altramuces"])
        ];

        public static List<string> ResolveFromIngredientes(IEnumerable<string> ingredientes)
        {
            var normalizedIngredientes = (ingredientes ?? [])
                .Where(ingrediente => !string.IsNullOrWhiteSpace(ingrediente))
                .Select(ingrediente => ingrediente.Trim().ToLowerInvariant())
                .ToList();

            return Rules
                .Where(rule => normalizedIngredientes.Any(ingrediente => rule.Keywords.Any(keyword => ingrediente.Contains(keyword))))
                .Select(rule => rule.Allergen)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(allergen => allergen, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
