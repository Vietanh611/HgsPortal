using Hgs.Share.Exceptions;
using Hgs.Share.Responses.ApiResponses;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestExceptionController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult TestNotFound()
    {
        throw new NotFoundException("Resource not found test");
    }

    [HttpGet("unauthorized")]
    public IActionResult TestUnauthorized()
    {
        throw new UnauthorizedException("Unauthorized access test");
    }

    [HttpGet("forbidden")]
    public IActionResult TestForbidden()
    {
        throw new ForbiddenException("Access denied test");
    }

    [HttpGet("bad-request")]
    public IActionResult TestBadRequest()
    {
        throw new BadRequestException("Bad request test");
    }

    [HttpGet("validation")]
    public IActionResult TestValidation()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Email", new[] { "Email is required", "Invalid email format" } },
            { "Password", new[] { "Password must be at least 8 characters" } }
        };
        throw new ValidationException("Validation failed", errors);
    }

    [HttpGet("conflict")]
    public IActionResult TestConflict()
    {
        throw new ConflictException("Resource conflict test");
    }

    [HttpGet("too-many-requests")]
    public IActionResult TestTooManyRequests()
    {
        throw new TooManyRequestsException("Rate limit exceeded test");
    }

    [HttpGet("business-rule")]
    public IActionResult TestBusinessRule()
    {
        throw new BusinessRuleException("Business rule violation test");
    }

    [HttpGet("null-reference")]
    public IActionResult TestNullReference()
    {
        string? nullString = null;
        var length = nullString.Length; // This will throw NullReferenceException
        return Ok(ApiResponse.SuccessResponse("This should not execute"));
    }

    [HttpGet("generic-error")]
    public IActionResult TestGenericError()
    {
        throw new Exception("Generic unexpected error test");
    }
}
