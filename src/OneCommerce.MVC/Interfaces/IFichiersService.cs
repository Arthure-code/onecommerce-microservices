namespace OneCommerce.MVC.Interfaces
{
    public interface IFichiersService
    {
        Task<string?> Upload(IFormFile file, string nomFichier);
    }
}
