using System;
using System.Collections.Generic;
using System.Text;
using Animal.Domain.Models;

namespace Animal.Domain.Services
{
    public interface IPetService
    {
        Pet GetPet(int id);
        List<Pet> GetPets();
        List<Pet> GetAvailablePets();
        List<Pet> SearchPets(PetSearchCriteria criteria);
        Pet CreatePet(Pet pet);
        void UpdatePet(Pet pet);
        void DeletePet(int id);
    }
}
