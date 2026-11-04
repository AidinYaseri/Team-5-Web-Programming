using Animal.Domain.Entities;

namespace Animal.API.Services
{
    // Planned application service, not implemented yet. When you build it, follow IPetService/PetService:
    // async methods, request/response DTOs from Contracts/ instead of entities, and repositories from Animal.Data.
    public interface IUserService
    {
        User GetUser(int id);
        List<User> GetUsers();
        User CreateUser(User user);
        void UpdateUser(User user);
        void DeleteUser(int id);
    }
}
