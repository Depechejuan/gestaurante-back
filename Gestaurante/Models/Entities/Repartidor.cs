namespace Gestaurante.Models.Entities
{
    public class Repartidor : Empleado
    {
        public Repartidor() { }

        public Repartidor(
            string email,
            string password,
            string firstName,
            string firstLastName,
            string secondLastName,
            string dni,
            string nuss)
        {
            Email = email;
            Password = password;
            FirstName = firstName;
            FirstLastName = firstLastName;
            SecondLastName = secondLastName;
            DNI = dni;
            NUSS = nuss;

            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
