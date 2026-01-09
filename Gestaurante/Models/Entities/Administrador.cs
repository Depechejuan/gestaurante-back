namespace Gestaurante.Models.Entities
{
    public class Administrador : Empleado
    {
        public Administrador() { }
        public Administrador(string email, string password, string firstName,
            string firstLastName, string secondLastName, string dni, string nuss)
        {
            this.Email = email;
            this.Password = password;
            this.FirstName = firstName;
            this.FirstLastName = firstLastName;
            this.SecondLastName = secondLastName;
            this.DNI = dni;
            this.NUSS = nuss;
            
            this.Id = new Guid();
            //this.CreatedAt = DateTime.Now;
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
