using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SB.DTOs;
using SB.Interfaces;

namespace SB.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers a new user with email verification OTP.
        /// </summary>
        /// <remarks>
        /// User cannot login until email is verified with OTP.
        /// OTP is sent to the provided email address.
        /// </remarks>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Registration argument validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                return StatusCode(500, new ErrorResponse { Error = "An unexpected error occurred during registration." });
            }
        }

        /// <summary>
        /// Verifies user email with OTP code.
        /// User must verify email before they can login.
        /// </summary>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _authService.VerifyEmailAsync(request);
                return Ok(new { message = "Email verified successfully. You can now login." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Email verification failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Email verification argument validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during email verification");
                return StatusCode(500, new ErrorResponse { Error = "An unexpected error occurred during verification." });
            }
        }

        /// <summary>
        /// Authenticates user and returns JWT token.
        /// Email must be verified before login is allowed.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> Login([FromBody] SB.DTOs.LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Login authentication failed");
                return Unauthorized(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Login validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Login argument validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                return StatusCode(500, new ErrorResponse { Error = "An unexpected error occurred during login." });
            }
        }

        /// <summary>
        /// Step 1: Initiates password reset by sending OTP to email.
        /// Does not reset password yet - only sends OTP.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ForgetPasswordOtpResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _authService.ForgotPasswordAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Forgot password validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during forgot password");
                return StatusCode(500, new ErrorResponse { Error = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Step 2: Verifies OTP for password reset without resetting password yet.
        /// User must complete this step before resetting password.
        /// </summary>
        [HttpPost("verify-forgot-password-otp")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] VerifyForgetPasswordOtpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _authService.VerifyForgotPasswordOtpAsync(request);
                return Ok(new { message = "OTP verified successfully." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "OTP verification failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "OTP verification validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during OTP verification");
                return StatusCode(500, new ErrorResponse { Error = "An unexpected error occurred during OTP verification." });
            }
        }

        /// <summary>
        /// Step 3: Resets password after OTP verification.
        /// User must have verified OTP before calling this endpoint.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordAfterOtpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Password reset successfully. You can now login with your new password." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Password reset failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Password reset validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password reset");
                return StatusCode(500, new ErrorResponse { Error = "An unexpected error occurred during password reset." });
            }
        }

        /// <summary>
        /// Resends email verification OTP.
        /// Use this endpoint if the initial OTP expires.
        /// </summary>
        [HttpPost("resend-verification-otp")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> ResendVerificationOtp([FromBody] ForgetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var message = await _authService.ResendVerificationOtpAsync(request);
                return Ok(new { message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Resend OTP validation failed");
                return BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during resend OTP");
                return StatusCode(500, new ErrorResponse { Error = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Example protected endpoint - requires authentication.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserResponse), 200)]
        [ProducesResponseType(401)]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            return Ok(new { userId, email, name, message = "You are authenticated!" });
        }
    }
}
