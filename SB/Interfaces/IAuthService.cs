using Microsoft.AspNetCore.Identity.Data;
using SB.DTOs;
namespace SB.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user and sends email verification OTP.
        /// </summary>
        Task<AuthResponse> RegisterAsync(UserRegisterDto request);

        /// <summary>
        /// Verifies user email with OTP code.
        /// </summary>
        Task<bool> VerifyEmailAsync(VerifyEmailRequest request);

        /// <summary>
        /// Authenticates user and returns JWT token.
        /// </summary>
        Task<AuthResponse> LoginAsync(SB.DTOs.LoginRequest request);

        /// <summary>
        /// Initiates password reset by sending OTP to email.
        /// Step 1 of forgot password flow.
        /// </summary>
        Task<ForgetPasswordOtpResponse> ForgotPasswordAsync(ForgetPasswordRequest request);

        /// <summary>
        /// Verifies OTP for password reset without resetting password.
        /// Step 2 of forgot password flow.
        /// </summary>
        Task VerifyForgotPasswordOtpAsync(VerifyForgetPasswordOtpRequest request);

        /// <summary>
        /// Resets password after OTP verification.
        /// Step 3 of forgot password flow.
        /// </summary>
        Task ResetPasswordAsync(ResetPasswordAfterOtpRequest request);

        /// <summary>
        /// Resends email verification OTP.
        /// </summary>
        Task<string> ResendVerificationOtpAsync(ForgetPasswordRequest request);
    }
}
