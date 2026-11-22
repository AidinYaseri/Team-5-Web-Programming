# Milestone 4 client contract tests

The integration tests exercise each typed client against the real API host created by AnimalApiFactory. The factory runs the API Program, replaces SQL Server with in-memory SQLite, and signs local JWTs for protected endpoints. Tests do not require Keycloak, Docker, or a running database server.

## Contract coverage

There is one contract test class per API client under tests/Animal.IntegrationTests/ClientContracts/:

| Client | Contract check |
| --- | --- |
| PetsApiClient | Creates and updates a pet through client request DTOs, then checks every PetResponse field on read. Also exercises query parameter names and delete/not-found behavior. |
| SheltersApiClient | Creates and updates a shelter through client request DTOs, then checks every ShelterResponse field on read. |
| UsersApiClient | Updates a profile through the client request DTO and checks every field in the client UserResponse; missing profiles map to null. Uploads a picture using the client multipart call, reads the returned image content type and PNG signature, then deletes it and confirms the client reports no picture. |
| AdoptionsApiClient | Applies using the client request DTO, then checks all response fields and the user's list response. |
| AppointmentsApiClient | Books using the client request DTO, then checks every response field and the user's list response. |
| NotificationsApiClient | Reads a notification into the client DTO, checks every field, and marks it read through the client route. |

A successful client call against the API verifies that the request shape and route are accepted; tests assert returned values rather than only checking that the status is not 400. Fixed TimeProvider values keep generated adoption and appointment timestamps deterministic.

The profile-picture contract check uses AnimalApiFactory and the client UsersApiClient methods. The test confirms multipart upload, image response parsing, and deletion through the real API host; the failure-handler tests also exercise 500/503, invalid or empty error bodies, and timeout behavior for each picture call.

## Failure handling

ApiClientFailureHandlingTests uses a fake HttpMessageHandler for each API client. It checks readable ApiException messages for HTTP 500 and 503 responses, including empty and malformed error bodies, and verifies timeout exceptions retain a clear timeout message. No external service is contacted.

## Run

From the repository root:

    dotnet test Team-5-Web-Programming.sln --no-restore
