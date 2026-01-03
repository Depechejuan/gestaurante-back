using System.ComponentModel.DataAnnotations;

namespace Gestaurante.Validation
{
    public class NussAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string nuss || string.IsNullOrWhiteSpace(nuss))
                return new ValidationResult("El NUSS no es válido");

            if (nuss.Length != 13)
                return new ValidationResult("El NUSS debe tener 13 caracteres (formato: 01-01234567-0)");

            if (nuss[2] != '-' || nuss[11] != '-')
                return new ValidationResult("El NUSS debe tener guiones en las posiciones 3 y 12 (formato: 01-01234567-0)");

            // Extraer las partes (quitando los guiones)
            string[] partes = nuss.Split('-');

            if (partes.Length != 3)
                return new ValidationResult("Formato de NUSS incorrecto (formato: 01-01234567-0)");

            string provincia = partes[0];
            string numero = partes[1];
            string control = partes[2];

            if (provincia.Length != 2 || numero.Length != 8 || control.Length != 1)
                return new ValidationResult("Formato de NUSS incorrecto (XX-XXXXXXXX-X)");


            if (!int.TryParse(provincia, out _) || !long.TryParse(numero, out _) || !int.TryParse(control, out _))
                return new ValidationResult("El NUSS debe contener solo números excepto los guiones");

            return ValidationResult.Success;
        }
    }
}