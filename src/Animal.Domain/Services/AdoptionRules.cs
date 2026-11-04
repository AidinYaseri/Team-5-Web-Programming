using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Animal.Domain.Exceptions;

namespace Animal.Domain.Services
{
    // Pure business rules for adoption applications. Only Pending applications can be approved or rejected.
    public static class AdoptionRules
    {
        public static void EnsureCanApply(Pet pet)
        {
            ArgumentNullException.ThrowIfNull(pet);

            if (!pet.IsAvailable)
            {
                throw new DomainValidationException($"Pet {pet.Id} is not available for adoption.");
            }
        }

        // Approving an application also takes the pet off the available list.
        public static void Approve(AdoptionApplication application, Pet pet)
        {
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(pet);

            if (application.PetId != pet.Id)
            {
                throw new ArgumentException($"Application {application.Id} is for pet {application.PetId}, not pet {pet.Id}.", nameof(pet));
            }

            EnsurePending(application, "approved");
            EnsureCanApply(pet);

            application.Status = AdoptionStatus.Approved;
            pet.IsAvailable = false;
        }

        public static void Reject(AdoptionApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);

            EnsurePending(application, "rejected");

            application.Status = AdoptionStatus.Rejected;
        }

        private static void EnsurePending(AdoptionApplication application, string action)
        {
            if (application.Status != AdoptionStatus.Pending)
            {
                throw new InvalidStatusTransitionException(
                    $"Application {application.Id} is {application.Status} and can't be {action}.");
            }
        }
    }
}
