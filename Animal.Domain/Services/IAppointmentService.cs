using System;
using System.Collections.Generic;
using System.Text;
using Animal.Domain.Models;

namespace Animal.Domain.Services
{
    internal interface IAppointmentService
    {
        Appointment GetAppointment(int id);
        List<Appointment> GetAppointments();
        Appointment CreateAppointment(Appointment appointment);
        void UpdateAppointment(Appointment appointment);
        void CancelAppointment(int id);
    }
}
