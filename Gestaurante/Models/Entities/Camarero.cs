using Gestaurante.Models.DTO;

namespace Gestaurante.Models.Entities
{
    public class Camarero : Empleado
    {
        public Camarero(RegistroDTO dto)
        {
            this.Id = dto.Id;
            this.Email = dto.Email;
            this.Password = dto.Password;
            this.FirstName = dto.FirstName;
            this.FirstLastName = dto.FirstLastName;
            this.SecondLastName = dto.SecondLastName;
            this.DNI = dto.DNI;
            this.NUSS = dto.NUSS;
            this.CreatedAt = dto.CreatedAt;
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
