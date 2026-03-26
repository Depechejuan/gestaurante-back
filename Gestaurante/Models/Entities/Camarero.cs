using Gestaurante.Models.DTO;

namespace Gestaurante.Models.Entities
{
    public class Camarero : Empleado
    {
        public Camarero() { }

        public Camarero(string email, string password, string firstName,
            string firstLastName, string secondLastName, string dni, string nuss)
        {
            this.Email = email;
            this.Password = password;
            this.FirstName = firstName;
            this.FirstLastName = firstLastName;
            this.SecondLastName = secondLastName;
            this.DNI = dni;
            this.NUSS = nuss;

            this.Id = Guid.NewGuid();
            this.CreatedAt = DateTime.UtcNow;
        }
        public int NumeroMesas { get; private set; }

        public void LiberarMesa()
        {
            //TODO
        }

        public void OcuparMesa()
        {
            //TODO
        }

        public void ServirPlato()
        {
            //TODO
        }

        public void ServirBebida()
        {
            //TODO
        }

        public void CobrarFactura()
        {
            //TODO
        }
    }
}
