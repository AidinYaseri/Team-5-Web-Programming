using System;
using System.Collections.Generic;
using System.Text;
using Animal.Domain.Models;

namespace Animal.Domain.Services
{
    internal interface IUserService
    {
        User GetUser(int id);
        List<User> GetUsers();
        User CreateUser(User user);
        void UpdateUser(User user);
        void DeleteUser(int id);
    }
}
