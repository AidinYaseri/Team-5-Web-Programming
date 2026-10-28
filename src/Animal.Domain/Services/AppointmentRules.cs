using Animal.Domain.Entities;
using Animal.Domain.Enums;
using Animal.Domain.Exceptions;

namespace Animal.Domain.Services
{
    // Pure business rules for appointments. The current time is passed in so the rules stay testable.
    public static class AppointmentRules
    {
        public static void EnsureCanSchedule(Appointment appointment, DateTime now)
        {
            ArgumentNullException.ThrowIfNull(appointment);

            if (appointment.AppointmentDate <= now)
            {
                throw new DomainValidationException("An appointment must be scheduled in the future.");
            }
        }

        public static void Cancel(Appointment appointment)
        {
            ArgumentNullException.ThrowIfNull(appointment);

            EnsureScheduled(appointment, "cancelled");

            appointment.Status = AppointmentStatus.Cancelled;
        }

        public static void Complete(Appointment appointment)
        {
            ArgumentNullException.ThrowIfNull(appointment);

            EnsureScheduled(appointment, "completed");

            appointment.Status = AppointmentStatus.Completed;
        }

        private static void EnsureScheduled(Appointment appointment, string action)
        {
            if (appointment.Status != AppointmentStatus.Scheduled)
            {
                throw new InvalidStatusTransitionException(
                    $"Appointment {appointment.Id} is {appointment.Status} and can't be {action}.");
            }
        }
    }
}
