using OneCommerce.MVC.Models;

namespace OneCommerce.MVC.Interfaces
{
    public interface IFichiersService
    {
        Task<LienTeleversement?> LienTeleversement(string nomPropose);

        Task<LienLecture?> LienLecture();
    }
}
