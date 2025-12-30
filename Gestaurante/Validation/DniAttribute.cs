using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Gestaurante.Validation
{
    public class DniAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string dni)
                return new ValidationResult("El DNI no es válido");

            if (dni.Length <= 9 || dni.Length > 10)
                return new ValidationResult("El DNI no es válido");

            if (dni[9] != '-')
                return new ValidationResult("El DNI no es válido");

            string numberPart = dni.Substring(0, 8);
            if (!int.TryParse(numberPart, out int number))
                return new ValidationResult("El DNI no es válido");

            int division = number % 23;

            char letter = dni[10];
            if ("TRWAGMYFPDXBNJZSQVHLCKE"[division] != letter)
                return new ValidationResult("El DNI no es válido");

            return ValidationResult.Success;
        }
    }
}
