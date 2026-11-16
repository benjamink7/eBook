using eBook.Model.Exceptions;
using eBook.Model.Profile;
using eBook.Services.Implementations;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        return Ok(await _profileService.GetAsync());
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(ProfileUpdateRequest request)
    {
        return Ok(await _profileService.UpdateAsync(request));
    }

    [HttpPut("picture")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProfileResponse>> SetPicture(IFormFile image)
    {
        if (image.Length > ProfileService.MaxPictureSizeBytes)
        {
            throw new BusinessException("Slika profila mora imati najviše 2 MB.");
        }

        using var memory = new MemoryStream();
        await image.CopyToAsync(memory);

        await _profileService.SetPictureAsync(memory.ToArray());

        return Ok(await _profileService.GetAsync());
    }

    [HttpGet("picture")]
    public async Task<IActionResult> GetPicture()
    {
        var (content, contentType) = await _profileService.GetPictureAsync();
        return File(content, contentType);
    }
}
