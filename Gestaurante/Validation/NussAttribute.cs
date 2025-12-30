using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Validation
{
    public class NussAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string nuss)
                return new ValidationResult("El nuss no es válido");

            if (nuss.Length <= 12 || nuss.Length > 13)
                return new ValidationResult("El nuss no es válido");

            if (nuss[2] != '-' && '-' != nuss[11])
                return new ValidationResult("El nuss no es válido");

            string numberPart1 = nuss.Substring(0, 1);
            if (!int.TryParse(numberPart1, out int number1))
                return new ValidationResult("El nuss debe ser numérico");

            string numberPart2 = nuss.Substring(3, 10);
            if (!int.TryParse(numberPart1, out int number2))
                return new ValidationResult("El nuss debe ser numérico");

            string numberPart3 = nuss.Substring(12, 12);
            if (!int.TryParse(numberPart1, out int number3))
                return new ValidationResult("El nuss debe ser numérico");


            return ValidationResult.Success;
        }
    }
}