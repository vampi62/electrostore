using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.AuthService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _AuthService;

        public AuthController(IAuthService AuthService)
        {
            _AuthService = AuthService;
        }

        [HttpGet("{sso_method}/url")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Get SSO Authentication URL", Description = "Retrieve the SSO authentication URL for the specified SSO method")]
        public async Task<ActionResult<SsoUrlResponse>> GetSSOAuthUrl([FromRoute] string sso_method)
        {
            var authUrl = await _AuthService.GetSSOAuthUrl(sso_method);
            return Ok(authUrl);
        }

        [HttpPost("{sso_method}/callback")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "SSO Callback", Description = "Handle the SSO callback for the specified SSO method")]
        public async Task<ActionResult<LoginResponse>> SSOCallback([FromRoute] string sso_method, [FromBody] SsoLoginRequest request)
        {
            var loginResponse = await _AuthService.LoginWithSSO(sso_method, request);
            return Ok(loginResponse);
        }
    
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "User Login", Description = "Authenticate user with email and password")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest)
        {
            var tokenLogin = await _AuthService.LoginWithPassword(loginRequest);
            return Ok(tokenLogin);
        }

        [HttpPost("refresh-token")]
        [Authorize(Policy = "RefreshToken")]
        [SwaggerOperation(Summary = "Refresh JWT Token", Description = "Refresh the JWT token using a valid refresh token")]
        public async Task<ActionResult<LoginResponse>> RefreshToken()
        {
            var tokenRefresh = await _AuthService.RefreshJwt();
            return Ok(tokenRefresh);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Forgot Password", Description = "Initiate the forgot password process")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
        {
            await _AuthService.ForgotPassword(forgotPasswordRequest);
            return Ok();
        }

        [HttpPost("resend-verification")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Resend Email Verification", Description = "Send a new email verification message to the user, if the email is not verified yet. The answer is the same whether the email exists or not")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest resendVerificationRequest)
        {
            await _AuthService.ResendVerification(resendVerificationRequest);
            return Ok();
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Verify Email", Description = "Verify the user's email using the token sent by email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest verifyEmailRequest)
        {
            await _AuthService.VerifyEmail(verifyEmailRequest);
            return Ok();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [SwaggerOperation(Summary = "Reset Password", Description = "Reset the user's password using a reset token")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest resetPasswordRequest)
        {
            await _AuthService.ResetPassword(resetPasswordRequest);
            return Ok();
        }
    }
}