using Microsoft.AspNetCore.Identity;
using SB.DTOs; // Ensure this is the only DTOs import
using SB.Interfaces;
using SB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SB.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtService _jwtService;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        private const int OtpExpiryMinutes = 10;
        private const int MaxOtpAttempts = 5;
        private const int PasswordResetOtpExpiryMinutes = 5;

        public AuthService(
           UserManager<User> userManager,
           RoleManager<IdentityRole> roleManager,
           IJwtService jwtService,
           IOtpService otpService,
           IEmailService emailService,
           IConfiguration configuration,
           ILogger<AuthService> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _otpService = otpService ?? throw new ArgumentNullException(nameof(otpService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers a new user with email verification OTP.
        /// User cannot login until email is verified.
        /// </summary>
        public async Task<AuthResponse> RegisterAsync(UserRegisterDto request)
        {
            ValidateRegisterRequest(request);

            // Check for existing user
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                throw new InvalidOperationException("Email is already registered.");
            }

            // Split full name
            var names = (request.FullName ?? string.Empty).Trim()
                .Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = names.Length > 0 ? names[0] : string.Empty,
                LastName = names.Length > 1 ? names[1] : string.Empty,
                Provider = "Local",
                IsEmailVerified = false,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            };

            // Create user with password
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("User creation failed for {Email}: {Errors}",
                    request.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            // Generate and send email verification OTP
            await GenerateAndSendOtpAsync(user, "email-verification", OtpExpiryMinutes);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            return GenerateAuthResponse(user, "Registration successful. Please verify your email with the OTP sent.");
        }

        /// <summary>
        /// Verifies user email using OTP code.
        /// After verification, user can login.
        /// Input: OTP only. Server resolves user by OTP.
        /// </summary>
        public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
        {
            ValidateVerifyEmailRequest(request);

            // Find the user based on provided OTP (server determines user)
            var user = await FindUserByOtpAsync(request.Otp);
            if (user == null)
            {
                // Keep message generic for security
                throw new InvalidOperationException("Invalid OTP.");
            }

            // Ensure OTP state is valid for this user (expiration, attempts)
            ValidateOtpState(user, user.Email);

            // Mark email verified and clear OTP so it cannot be reused
            user.IsEmailVerified = true;
            user.EmailConfirmed = true;
            ClearOtpData(user);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to mark email as verified for user id {UserId}", user.Id);
                throw new InvalidOperationException("Failed to verify email.");
            }

            _logger.LogInformation("Email verified successfully for {Email}", user.Email);
            return true;
        }

        /// <summary>
        /// Authenticates user and returns JWT token.
        /// User must have verified email to login.
        /// </summary>
        public async Task<AuthResponse> LoginAsync(SB.DTOs.LoginRequest request)
        {
            ValidateLoginRequest(request);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                _logger.LogWarning("Login failed for {Email}: Invalid credentials", request.Email);
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!user.IsEmailVerified || !user.EmailConfirmed)
            {
                _logger.LogWarning("Login attempt with unverified email: {Email}", request.Email);
                throw new InvalidOperationException("Email is not verified. Please check your inbox for verification OTP.");
            }

            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            return GenerateAuthResponse(user, "Login successful.", userRoles);
        }

        /// <summary>
        /// Step 1: Initiates password reset by sending OTP to email.
        /// Does not reset password yet - only sends OTP.
        /// </summary>
        public async Task<ForgetPasswordOtpResponse> ForgotPasswordAsync(ForgetPasswordRequest request)
        {
            ValidateForgotPasswordRequest(request);

            var user = await _userManager.FindByEmailAsync(request.Email);

            // Always return generic message for security (do not reveal if email exists)
            if (user == null)
            {
                _logger.LogInformation("Forgot password request for non-existent email: {Email}", request.Email);
                return new ForgetPasswordOtpResponse
                {
                    Message = "If an account exists with this email, an OTP has been sent.",
                    ExpiresInMinutes = PasswordResetOtpExpiryMinutes
                };
            }

            // Reset the OTP verification flag for new password reset attempt
            user.IsPasswordResetOtpVerified = false;

            await GenerateAndSendOtpAsync(user, "password-reset", PasswordResetOtpExpiryMinutes);

            _logger.LogInformation("Password reset OTP generated and sent for {Email}", request.Email);

            return new ForgetPasswordOtpResponse
            {
                Message = "If an account exists with this email, an OTP has been sent.",
                ExpiresInMinutes = PasswordResetOtpExpiryMinutes
            };
        }

        /// <summary>
        /// Step 2: Verifies OTP for password reset without resetting password yet.
        /// User must complete this step before resetting password.
        /// Input: OTP only. Server resolves user by OTP.
        /// </summary>
        public async Task VerifyForgotPasswordOtpAsync(VerifyForgetPasswordOtpRequest request)
        {
            ValidateVerifyForgotPasswordOtpRequest(request);

            // Locate the user by OTP. This method performs verification using IOtpService.VerifyOtp per user.
            var user = await FindUserByOtpAsync(request.Otp);
            if (user == null)
            {
                // No user matched the provided OTP
                throw new InvalidOperationException("Invalid OTP.");
            }

            // Validate OTP state (expiration / max attempts) for the matched user
            ValidatePasswordResetOtpState(user, user.Email);

            // At this point OTP matched and state is valid. Mark OTP as verified for reset flow.
            user.IsPasswordResetOtpVerified = true;
            user.OtpFailedAttempts = 0;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to mark password reset OTP as verified for user id {UserId}", user.Id);
                throw new InvalidOperationException("Failed to verify OTP.");
            }

            _logger.LogInformation("Password reset OTP verified successfully for {Email}", user.Email);
        }

        /// <summary>
        /// Step 3: Resets password after OTP verification.
        /// User must have verified OTP before calling this endpoint.
        /// </summary>
        public async Task ResetPasswordAsync(ResetPasswordAfterOtpRequest request)
        {
            ValidateResetPasswordAfterOtpRequest(request);

            // Find the user that previously verified OTP for password reset
            var user = await _userManager.Users
                .Where(u => u.IsPasswordResetOtpVerified && u.OtpExpirationDate != null)
                .OrderByDescending(u => u.OtpExpirationDate) // prefer most recent
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Password reset attempt without prior OTP verification.");
                throw new InvalidOperationException("OTP verification required. Please verify OTP first.");
            }

            // Safety: ensure OTP not expired
            if (DateTime.UtcNow > user.OtpExpirationDate)
            {
                _logger.LogWarning("Password reset attempt with expired OTP for user {UserId}", user.Id);
                ClearPasswordResetOtpData(user);
                await _userManager.UpdateAsync(user);
                throw new InvalidOperationException("OTP has expired. Please request a new one.");
            }

            // Reset password
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                _logger.LogError("Failed to remove old password for user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to reset password.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                _logger.LogError("Failed to add new password for user {UserId}", user.Id);
                throw new InvalidOperationException("Failed to reset password.");
            }

            ClearPasswordResetOtpData(user);
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to clear OTP for user {UserId}", user.Id);
            }

            _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);
        }

        /// <summary>
        /// Resends email verification OTP to user.
        /// </summary>
        public async Task<string> ResendVerificationOtpAsync(ForgetPasswordRequest request)
        {
            ValidateForgotPasswordRequest(request);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("Resend OTP request for non-existent email: {Email}", request.Email);
                return "If an account exists with this email, a new OTP has been sent.";
            }

            if (user.IsEmailVerified)
            {
                _logger.LogWarning("Resend OTP request for already verified email: {Email}", request.Email);
                return "Email is already verified. You can login now.";
            }

            await GenerateAndSendOtpAsync(user, "email-verification", OtpExpiryMinutes);

            _logger.LogInformation("Verification OTP resent for {Email}", request.Email);
            return "A new OTP has been sent to your email.";
        }

        // ===== New helper: locate a user by their OTP (VerifyOtp per-user) =====
        private async Task<User> FindUserByOtpAsync(string otp)
        {
            if (string.IsNullOrWhiteSpace(otp))
                throw new ArgumentException("OTP is required.");

            var now = DateTime.UtcNow;

            // Only consider users with a stored OTP that hasn't expired yet.
            // This keeps the in-memory iteration small in normal operation.
            var candidates = await _userManager.Users
                .Where(u => u.OtpCodeHash != null && u.OtpExpirationDate != null && u.OtpExpirationDate >= now)
                .ToListAsync();

            if (candidates == null || candidates.Count == 0)
            {
                _logger.LogWarning("OTP verification attempt but no active OTPs found.");
                return null;
            }

            // Iterate candidates and verify using the IOtpService.VerifyOtp method.
            // This supports non-deterministic/salted OTP hashing implementations.
            foreach (var candidate in candidates)
            {
                try
                {
                    if (_otpService.VerifyOtp(otp, candidate.OtpCodeHash))
                    {
                        return candidate;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while verifying OTP for candidate user {UserId}", candidate.Id);
                    // continue to next candidate
                }
            }

            _logger.LogWarning("OTP verification attempt with unknown or incorrect OTP.");
            return null;
        }

        // ===== Private Helper Methods (validations / existing helpers) =====

        private void ValidateRegisterRequest(UserRegisterDto request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.");

            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new ArgumentException("Full name is required.");
        }

        private void ValidateVerifyEmailRequest(VerifyEmailRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Otp))
                throw new ArgumentException("OTP is required.");
        }

        private void ValidateLoginRequest(LoginRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Email and password are required.");
        }

        private void ValidateForgotPasswordRequest(ForgetPasswordRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required.");
        }

        private void ValidateVerifyForgotPasswordOtpRequest(VerifyForgetPasswordOtpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Otp))
                throw new ArgumentException("OTP is required.");
        }

        private void ValidateResetPasswordAfterOtpRequest(ResetPasswordAfterOtpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                throw new ArgumentException("New password is required.");

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                throw new ArgumentException("Confirm password is required.");

            if (request.NewPassword != request.ConfirmPassword)
                throw new ArgumentException("Passwords do not match.");
        }

        private void ValidateOtpState(User user, string email)
        {
            if (string.IsNullOrWhiteSpace(user.OtpCodeHash) || user.OtpExpirationDate == null)
            {
                _logger.LogWarning("OTP verification attempt without generated OTP for {Email}", email);
                throw new InvalidOperationException("No OTP found. Please request a new one.");
            }

            if (DateTime.UtcNow > user.OtpExpirationDate)
            {
                _logger.LogWarning("OTP verification attempt with expired OTP for {Email}", email);
                ClearOtpData(user);
                _userManager.UpdateAsync(user);
                throw new InvalidOperationException("OTP has expired. Please request a new one.");
            }

            if (user.OtpFailedAttempts >= MaxOtpAttempts)
            {
                _logger.LogWarning("OTP verification attempt exceeded max attempts for {Email}", email);
                ClearOtpData(user);
                _userManager.UpdateAsync(user);
                throw new InvalidOperationException("Too many failed attempts. Please request a new OTP.");
            }
        }

        private void ValidatePasswordResetOtpState(User user, string email)
        {
            if (string.IsNullOrWhiteSpace(user.OtpCodeHash) || user.OtpExpirationDate == null)
            {
                _logger.LogWarning("Password reset OTP verification without generated OTP for {Email}", email);
                throw new InvalidOperationException("No OTP found. Please request a new one.");
            }

            if (DateTime.UtcNow > user.OtpExpirationDate)
            {
                _logger.LogWarning("Password reset OTP verification with expired OTP for {Email}", email);
                ClearPasswordResetOtpData(user);
                _userManager.UpdateAsync(user);
                throw new InvalidOperationException("OTP has expired. Please request a new one.");
            }

            if (user.OtpFailedAttempts >= MaxOtpAttempts)
            {
                _logger.LogWarning("Password reset OTP verification exceeded max attempts for {Email}", email);
                ClearPasswordResetOtpData(user);
                _userManager.UpdateAsync(user);
                throw new InvalidOperationException("Too many failed attempts. Please request a new OTP.");
            }
        }

        private async Task GenerateAndSendOtpAsync(User user, string purpose, int expiryMinutes)
        {
            // Generate OTP
            var otp = _otpService.GenerateOtp();
            var otpHash = _otpService.HashOtp(otp);
            var otpExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            // Store OTP hash and expiry
            user.OtpCodeHash = otpHash;
            user.OtpExpirationDate = otpExpiry;
            user.OtpFailedAttempts = 0;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to store OTP for {Email}", user.Email);
                throw new InvalidOperationException("Failed to process request.");
            }

            // Send OTP via email
            try
            {
                await SendOtpEmailAsync(user, otp, purpose, expiryMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email for {Email}", user.Email);
                // Clear the OTP we just set
                ClearOtpData(user);
                await _userManager.UpdateAsync(user);
                throw;
            }
        }

        private async Task SendOtpEmailAsync(User user, string otp, string purpose, int expiryMinutes)
        {
            string subject, purposeText;

            switch (purpose)
            {
                case "email-verification":
                    subject = "Verify your Smart Bracelet email";
                    purposeText = "Thank you for registering. Please verify your email with the code below:";
                    break;
                case "password-reset":
                    subject = "Your Smart Bracelet password reset code";
                    purposeText = "You requested a password reset. Use the code below to reset your password:";
                    break;
                default:
                    subject = "Your Smart Bracelet verification code";
                    purposeText = "Use the code below to complete your request:";
                    break;
            }

            var htmlBody = $@"
         <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
             <p>Hello {user.FirstName},</p>
             <p>{purposeText}</p>
             <div style=""background-color: #f5f5f5; border: 1px solid #ddd; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;"">
                 <h2 style=""font-family: monospace; letter-spacing: 5px; font-size: 32px; margin: 0; color: #333;"">{otp}</h2>
             </div>
             <p>This code will expire in {expiryMinutes} minutes.</p>
             <p>If you didn't request this, you can safely ignore this email.</p>
             <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;""/>
             <p style=""color: #666; font-size: 12px;"">Regards,<br/>Smart Bracelet Team</p>
         </div>";

            await _emailService.SendAsync(user.Email, subject, htmlBody);
        }

        private AuthResponse GenerateAuthResponse(User user, string message = "", IList<string> roles = null)
        {
            var userResponse = new UserResponse
            {
                Id = user.Id.GetHashCode(), // or (int)(user.Id) if you want to cast Guid to int, but GetHashCode() is safer for a Guid
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                //ProfileImageUrl = user.ProfileImage != null
                   // ? $"data:image/png;base64,{Convert.ToBase64String(user.ProfileImage)}"
                   // : string.Empty,
            };

            var expirationMinutes = _jwtService.GetTokenExpirationMinutes();

            return new AuthResponse
            {
                Token = _jwtService.GenerateToken(user, roles),
                Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
                User = userResponse,
                Message = message
            };
        }

        private void ClearOtpData(User user)
        {
            user.OtpCodeHash = null;
            user.OtpExpirationDate = null;
            user.OtpFailedAttempts = 0;
        }

        private void ClearPasswordResetOtpData(User user)
        {
            user.OtpCodeHash = null;
            user.OtpExpirationDate = null;
            user.OtpFailedAttempts = 0;
            user.IsPasswordResetOtpVerified = false;
        }
    }
}
