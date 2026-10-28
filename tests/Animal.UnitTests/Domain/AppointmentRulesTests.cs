using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Animal.Domain.Exceptions;
using Animal.Domain.Services;

namespace Animal.UnitTests.Domain
{
    public class AppointmentRulesTests
    {
        private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        private static Appointment ScheduledAppointment() =>
            new() { Id = 1, UserId = 5, PetId = 1, ShelterId = 1, AppointmentDate = Now.AddDays(1), Status = AppointmentStatus.Scheduled };

        [Fact]
        public void EnsureCanSchedule_FutureDate_DoesNotThrow()
        {
            AppointmentRules.EnsureCanSchedule(ScheduledAppointment(), Now);
        }

        [Fact]
        public void EnsureCanSchedule_RightNow_Throws()
        {
            var appointment = ScheduledAppointment();
            appointment.AppointmentDate = Now;

            Assert.Throws<DomainValidationException>(() => AppointmentRules.EnsureCanSchedule(appointment, Now));
        }

        [Fact]
        public void EnsureCanSchedule_PastDate_Throws()
        {
            var appointment = ScheduledAppointment();
            appointment.AppointmentDate = Now.AddMinutes(-1);

            Assert.Throws<DomainValidationException>(() => AppointmentRules.EnsureCanSchedule(appointment, Now));
        }

        [Fact]
        public void Cancel_ScheduledAppointment_Cancels()
        {
            var appointment = ScheduledAppointment();

            AppointmentRules.Cancel(appointment);

            Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        }

        [Theory]
        [InlineData(AppointmentStatus.Cancelled)]
        [InlineData(AppointmentStatus.Completed)]
        public void Cancel_AppointmentThatIsNotScheduled_Throws(AppointmentStatus status)
        {
            var appointment = ScheduledAppointment();
            appointment.Status = status;

            Assert.Throws<InvalidStatusTransitionException>(() => AppointmentRules.Cancel(appointment));
        }

        [Fact]
        public void Complete_ScheduledAppointment_Completes()
        {
            var appointment = ScheduledAppointment();

            AppointmentRules.Complete(appointment);

            Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        }

        [Fact]
        public void Complete_CancelledAppointment_Throws()
        {
            var appointment = ScheduledAppointment();
            appointment.Status = AppointmentStatus.Cancelled;

            Assert.Throws<InvalidStatusTransitionException>(() => AppointmentRules.Complete(appointment));
        }
    }
}
