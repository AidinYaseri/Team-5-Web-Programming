using System;
using System.Collections.Generic;
using System.Text;
using Animal.Domain.Models;

namespace Animal.Domain.Services
{
    public interface IAdoptionService
    {
        AdoptionApplication GetApplication(int id);
        List<AdoptionApplication> GetApplications();
        AdoptionApplication CreateApplication(AdoptionApplication application);
        void ApproveApplication(int id);
        void RejectApplication(int id);
    }
}
