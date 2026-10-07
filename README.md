# Animal Adoption API

Team 5 · Web Programming III

An ASP.NET Core Web API (.NET 10) for managing shelters, pets, users, adoption applications, appointments and notifications.

## Repository structure

```
Team-5-Web-Programming/
├── .github/
│   └── workflows/Ci.yml          # build + test + EF migration check on every push / PR to main
├── docs/
│   └── diagrams/                 # service + database diagrams (.mmd sources and .png exports)
├── src/
│   ├── Animal.API/               # Web API: Controllers, Contracts (DTOs), Program.cs
│   ├── Animal.Domain/            # Models (entities) and Services (service interfaces)
│   └── Animal.Data/              # EF Core: Context, Configurations, Migrations, SeedData, Repositories
├── Team-5-Web-Programming.slnx   # solution file, open this in Visual Studio
└── README.md
```

Project references: `Animal.API → Animal.Domain, Animal.Data` and `Animal.Data → Animal.Domain`.

## Service Diagram

```mermaid
flowchart TB
    Client(["Client<br/>(Swagger, .http file, front-end)"])

    subgraph API["Animal.API  (Web API layer)"]
        direction LR
        PetsC[PetsController]
        SheltersC[SheltersController]
        UsersC[UsersController]
        AdoptionsC[AdoptionsController]
        AppointmentsC[AppointmentsController]
        NotificationsC[NotificationsController]
    end

    subgraph DOMAIN["Animal.Domain  (models + service interfaces)"]
        direction LR
        PetS[IPetService]
        ShelterS[IShelterService]
        UserS[IUserService]
        AdoptionS[IAdoptionService]
        AppointmentS[IAppointmentService]
        NotificationS[INotificationService]
    end

    subgraph DATA["Animal.Data  (EF Core)"]
        direction LR
        Repos["Repositories"]
        Ctx["AnimalDbContext<br/>Configurations / Migrations / SeedData"]
    end

    DB[("SQL Server")]

    Client -->|HTTP / JSON| API

    PetsC --> PetS
    SheltersC --> ShelterS
    UsersC --> UserS
    AdoptionsC --> AdoptionS
    AppointmentsC --> AppointmentS
    NotificationsC --> NotificationS

    DOMAIN -->|implemented using| DATA
    Repos --> Ctx
    DATA -->|EF Core| DB
```

### Service interfaces

```mermaid
classDiagram
    direction LR
    class IPetService {
        <<interface>>
        +GetPet(id) Pet
        +GetPets() List~Pet~
        +GetAvailablePets() List~Pet~
        +CreatePet(pet) Pet
        +UpdatePet(pet)
        +DeletePet(id)
    }
    class IShelterService {
        <<interface>>
        +GetShelter(id) Shelter
        +GetShelters() List~Shelter~
        +CreateShelter(shelter) Shelter
        +UpdateShelter(shelter)
        +DeleteShelter(id)
    }
    class IUserService {
        <<interface>>
        +GetUser(id) User
        +GetUsers() List~User~
        +CreateUser(user) User
        +UpdateUser(user)
        +DeleteUser(id)
    }
    class IAdoptionService {
        <<interface>>
        +GetApplication(id) AdoptionApplication
        +GetApplications() List~AdoptionApplication~
        +CreateApplication(application) AdoptionApplication
        +ApproveApplication(id)
        +RejectApplication(id)
    }
    class IAppointmentService {
        <<interface>>
        +GetAppointment(id) Appointment
        +GetAppointments() List~Appointment~
        +CreateAppointment(appointment) Appointment
        +UpdateAppointment(appointment)
        +CancelAppointment(id)
    }
    class INotificationService {
        <<interface>>
        +GetNotification(id) Notification
        +GetNotifications() List~Notification~
        +CreateNotification(notification) Notification
        +MarkAsRead(id)
    }
    IAdoptionService ..> IPetService : uses
    IAdoptionService ..> INotificationService : uses
    IAppointmentService ..> INotificationService : uses
```

PNG exports: [service-diagram.png](docs/diagrams/service-diagram.png) · [services-class-diagram.png](docs/diagrams/services-class-diagram.png)

<details>
<summary>Original service diagram sketch</summary>

<img width="393" height="442" alt="Original service diagram" src="https://github.com/user-attachments/assets/21567fdc-b163-442c-8a17-169fd62c7fa9" />

</details>

## Database Diagram

![Database diagram](docs/diagrams/database-diagram.png)

## Getting started

```bash
dotnet restore Team-5-Web-Programming.slnx
dotnet build Team-5-Web-Programming.slnx
dotnet run --project src/Animal.API
```

The API runs on `http://localhost:5015` (or `https://localhost:7298` with the https profile). The OpenAPI document is served at `/openapi/v1.json` in Development.

## Team conventions

- Branch off `main`, open a pull request, and let CI pass before merging.
- Don't commit `bin/`, `obj/`, `.vs/` or `*.user` files (they're in `.gitignore`).
- Line endings are normalized by `.gitattributes`, so Windows and Mac teammates won't get whole-file diffs.
- To update a diagram, edit the `.mmd` file in `docs/diagrams/`, paste it into the README block, and re-export the PNG (e.g. with https://mermaid.live).
