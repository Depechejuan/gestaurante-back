using Gestaurante.Models.DTO;

namespace Gestaurante.Models.Entities
{
    public class Administrador : Empleado
    {
        public Administrador(RegistroDTO dto)
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
        public void CambiarZonaCamarero()
        {
            //TODO
        }

        public void CompletarPedido()
        {
            //TODO
        }
    }
}
