# Milestone 4 client contract tests

The integration tests exercise each typed client against the real API host created by AnimalApiFactory. The factory runs the API Program, replaces SQL Server with in-memory SQLite, and signs local JWTs for protected endpoints. Tests do not require Keycloak, Docker, or a running database server.

## Contract coverage

There is one contract test class per API client under tests/Animal.IntegrationTests/ClientContracts/:

| Client | Contract check |
| --- | --- |
| PetsApiClient | Creates and updates a pet through client request DTOs, then checks every PetResponse field on read. Also exercises query parameter names and delete/not-found behavior. |
| SheltersApiClient | Creates and updates a shelter through client request DTOs, then checks every ShelterResponse field on read. |
| UsersApiClient | Updates a profile through the client request DTO and checks every field in the client UserResponse; missing profiles map to null. |
| AdoptionsApiClient | Applies using the client request DTO, then checks all response fields and the user's list response. |
| AppointmentsApiClient | Books using the client request DTO, then checks every response field and the user's list response. |
| NotificationsApiClient | Reads a notification into the client DTO, checks every field, and marks it read through the client route. |

A successful client call against the API verifies that the request shape and route are accepted; tests assert returned values rather than only checking that the status is not 400. Fixed TimeProvider values keep generated adoption and appointment timestamps deterministic.

The profile-picture client methods are not present on the WI-7 branch's milestone base yet. Add the equivalent upload/get/delete contract checks after WI-6 is merged into the milestone branch.

## Failure handling

ApiClientFailureHandlingTests uses a fake HttpMessageHandler for each API client. It checks readable ApiException messages for HTTP 500 and 503 responses, including empty and malformed error bodies, and verifies timeout exceptions retain a clear timeout message. No external service is contacted.

## Run

From the repository root:

    dotnet test Team-5-Web-Programming.sln --no-restore