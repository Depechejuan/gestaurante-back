using Gestaurante.Models.DTO;

namespace Gestaurante.Models.Entities
{
    public class Cocinero : Empleado
    {
        public Cocinero() { }
        public Cocinero(Guid id, string email, string password, string firstName,
            string firstLastName, string secondLastName, string dni, string nuss, DateTime createdAt)
        {
            this.Id = id;
            this.Email = email;
            this.Password = password;
            this.FirstName = firstName;
            this.FirstLastName = firstLastName;
            this.SecondLastName = secondLastName;
            this.DNI = dni;
            this.NUSS = nuss;
            this.CreatedAt = createdAt;
        }

        public void CompletarPlato()
        {
            //TODO
        }

        public void CompletarPedido()
        {
            //TODO
        }
    }
}
