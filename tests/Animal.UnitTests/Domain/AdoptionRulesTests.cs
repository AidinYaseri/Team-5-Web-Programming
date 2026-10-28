using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.UnitTests.Domain
{
    public class AdoptionRulesTests
    {
        private static Pet AvailablePet() =>
            new() { Id = 1, Name = "Biscuit", Species = "Dog", Breed = "Beagle", Age = 3, IsAvailable = true, ShelterId = 1 };

        private static AdoptionApplication PendingApplication() =>
            new() { Id = 10, UserId = 5, PetId = 1, Status = AdoptionStatus.Pending };

        [Fact]
        public void NewApplication_StartsAsPending()
        {
            Assert.Equal(AdoptionStatus.Pending, new AdoptionApplication().Status);
        }

        [Fact]
        public void EnsureCanApply_AdoptedPet_Throws()
        {
            var pet = AvailablePet();
            pet.IsAvailable = false;

            Assert.Throws<DomainValidationException>(() => AdoptionRules.EnsureCanApply(pet));
        }

        [Fact]
        public void Approve_PendingApplication_ApprovesAndTakesPetOffTheList()
        {
            var application = PendingApplication();
            var pet = AvailablePet();

            AdoptionRules.Approve(application, pet);

            Assert.Equal(AdoptionStatus.Approved, application.Status);
            Assert.False(pet.IsAvailable);
        }

        [Theory]
        [InlineData(AdoptionStatus.Approved)]
        [InlineData(AdoptionStatus.Rejected)]
        public void Approve_ApplicationThatIsNotPending_Throws(AdoptionStatus status)
        {
            var application = PendingApplication();
            application.Status = status;

            Assert.Throws<InvalidStatusTransitionException>(() => AdoptionRules.Approve(application, AvailablePet()));
        }

        // Someone else's application for the same pet was already approved.
        [Fact]
        public void Approve_PetAlreadyAdopted_ThrowsAndLeavesApplicationPending()
        {
            var application = PendingApplication();
            var pet = AvailablePet();
            pet.IsAvailable = false;

            Assert.Throws<DomainValidationException>(() => AdoptionRules.Approve(application, pet));
            Assert.Equal(AdoptionStatus.Pending, application.Status);
        }

        [Fact]
        public void Approve_WrongPet_Throws()
        {
            var pet = AvailablePet();
            pet.Id = 2;

            Assert.Throws<ArgumentException>(() => AdoptionRules.Approve(PendingApplication(), pet));
        }

        [Fact]
        public void Reject_PendingApplication_Rejects()
        {
            var application = PendingApplication();

            AdoptionRules.Reject(application);

            Assert.Equal(AdoptionStatus.Rejected, application.Status);
        }

        [Fact]
        public void Reject_ApprovedApplication_Throws()
        {
            var application = PendingApplication();
            application.Status = AdoptionStatus.Approved;

            Assert.Throws<InvalidStatusTransitionException>(() => AdoptionRules.Reject(application));
        }
    }
}
