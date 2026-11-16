using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Users;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _userService;

    public UsersController(IUserAdminService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserAdminResponse>>> Get([FromQuery] UserSearchRequest request)
    {
        return Ok(await _userService.GetAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserAdminResponse>> GetById(int id)
    {
        return Ok(await _userService.GetByIdAsync(id));
    }

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<UserAdminResponse>> Activate(int id)
    {
        return Ok(await _userService.SetActiveAsync(id, true));
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<UserAdminResponse>> Deactivate(int id)
    {
        return Ok(await _userService.SetActiveAsync(id, false));
    }
}
