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

            if (!IsValidDNI(dni))
                return new ValidationResult("El DNI no es válido");


            return ValidationResult.Success;
        }

        public static bool IsValidDNI(string dni)
        {
            if (dni == null)
                return false;
            if (dni.Length != 10)
                return false;
            if (dni[8] != '-')
                return false;
            if (!IsValidLetter(dni[dni.Length - 1]))
                return false;
            char letter = ToUpper(dni[dni.Length - 1]);

            int DNINumber = 0;
            int mult = 10000000;
            for (int i = 0; i < dni.Length - 1; i++)
            {
                char c = dni[i];
                if (i <= 7)
                {
                    if (!IsValidNumberInChar(c))
                        return false;
                    DNINumber += CharToNumber(c) * mult;
                    mult /= 10;
                }

            }
            return CheckDNI(DNINumber, dni[dni.Length - 1]);
        }

        public static int CharToNumber(char c)
        {
            return c - '0';
        }

        public static char ToUpper(char c)
        {
            if (IsCapitalLetter(c))
                return c;
            return (char)(c - 32);
        }

        private static bool CheckDNI(int dni, char letter)
        {
            int module = dni % 23;
            string validLetters = "TRWAGMYFPDXBNJZSQVHLCKE";
            return letter == validLetters[module];
        }

        public static bool IsCapitalLetter(char c)
        {
            return 'A' <= c && c <= 'Z';
        }

        public static bool IsValidCharEmail(char c)
        {
            if (c == '.' || c == '_' || c == '-' || c == '+')
                return true;
            if (c == '@')
                return true;
            if ('0' <= c && c <= '9')
                return true;
            if ('A' <= c && c <= 'Z')
                return true;
            if ('a' <= c && c <= 'z')
                return true;
            return false;
        }

        public static bool IsValidNumberInChar(char c)
        {
            return '0' <= c && c <= '9';
        }

        public static bool IsValidLetter(char c)
        {
            return ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z');
        }
    }
}
