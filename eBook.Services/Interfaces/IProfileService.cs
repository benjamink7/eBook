using eBook.Model.Profile;

namespace eBook.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileResponse> GetAsync();
    Task<ProfileResponse> UpdateAsync(ProfileUpdateRequest request);
    Task SetPictureAsync(byte[] image);
    Task<(byte[] Content, string ContentType)> GetPictureAsync();
}
