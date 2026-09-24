using System;
using System.Collections.Generic;
using System.Text;
using Animal.Domain.Models;

namespace Animal.Domain.Services
{
    internal interface IShelterService
    {
        Shelter GetShelter(int id);
        List<Shelter> GetShelters();
        Shelter CreateShelter(Shelter shelter);
        void UpdateShelter(Shelter shelter);
        void DeleteShelter(int id);
    }
}
