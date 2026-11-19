using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Animal.Client.Contracts;

namespace Animal.Client.Services
{
    public class UsersApiClient
    {
        private readonly HttpClient _http;

        public UsersApiClient(HttpClient http)
        {
            _http = http;
        }

        // Returns null when the authenticated user has not created a profile yet.
        public async Task<UserResponse?> GetMyProfileAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync("api/users/me", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<UserResponse>(cancellationToken);
        }

        public async Task UpdateMyProfileAsync(
            UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _http.PutAsJsonAsync("api/users/me", request, cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task<ProfilePictureImage?> GetMyProfilePictureAsync(
            CancellationToken cancellationToken = default)
        {
            using var response = await _http.GetAsync("api/users/me/picture", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return new ProfilePictureImage(contentType, bytes);
        }

        public async Task UploadMyProfilePictureAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(imageStream);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

            using var multipart = new MultipartFormDataContent();
            using var image = new StreamContent(imageStream);
            image.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            multipart.Add(image, "file", Path.GetFileName(fileName));

            using var response = await _http.PutAsync(
                "api/users/me/picture",
                multipart,
                cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeleteMyProfilePictureAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _http.DeleteAsync("api/users/me/picture", cancellationToken);
            await ApiResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }
}
