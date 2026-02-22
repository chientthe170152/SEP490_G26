using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public AuthService(IAuthRepository authRepository, IConfiguration configuration, IEmailService emailService, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var user = await _authRepository.GetUserByUsernameAsync(request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var token = GenerateJwtToken(user);
            return new LoginResponse
            {
                Token = token,
                Username = user.Username,
                RoleName = user.Role?.RoleName ?? "User",
                Email = user.Email ?? user.Username
            };
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            var userEmail = payload.Email;

            var user = await _authRepository.GetUserByUsernameAsync(userEmail);

            if (user == null)
            {
                return new LoginResponse
                {
                    NeedsRegistration = true,
                    Email = userEmail
                };
            }

            var token = GenerateJwtToken(user);
            return new LoginResponse
            {
                Token = token,
                Username = user.Username,
                RoleName = user.Role?.RoleName ?? "User",
                Email = user.Email ?? user.Username
            };
        }

        public async Task<LoginResponse> GoogleRegisterAsync(GoogleRegisterRequest request)
        {
            var payload = await ValidateGoogleTokenAsync(request.IdToken);
            var userEmail = payload.Email;

            var existingUser = await _authRepository.GetUserByUsernameAsync(userEmail);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            var user = new User
            {
                Username = userEmail,
                PasswordHash = Guid.NewGuid().ToString(),
                RoleId = request.RoleId,
                Email = userEmail
            };

            await _authRepository.AddUserAsync(user);

            var token = GenerateJwtToken(user);
            return new LoginResponse
            {
                Token = token,
                Username = user.Username,
                RoleName = user.Role?.RoleName ?? "User",
                Email = user.Email ?? user.Username
            };
        }

        public async Task SendOtpAsync(RegisterRequest request)
        {
            // 1. Check if username or email already exists
            var existingUserByUsername = await _authRepository.GetUserByUsernameAsync(request.Username);
            if (existingUserByUsername != null && existingUserByUsername.Username == request.Username) 
                throw new InvalidOperationException("Tên đăng nhập đã được sử dụng.");
            
            var existingUserByEmail = await _authRepository.GetUserByUsernameAsync(request.Email);
            if (existingUserByEmail != null && existingUserByEmail.Email == request.Email) 
                throw new InvalidOperationException("Email này đã được đăng ký trong hệ thống. Vui lòng sử dụng Email khác.");

            // 2. Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // 3. Save Registration Info and OTP to Cache (expires in 10 minutes)
            var cacheKey = $"OTP_{request.Email}";
            var cacheData = new { Request = request, Otp = otp };
            _cache.Set(cacheKey, cacheData, TimeSpan.FromMinutes(10));

            // 4. Send Email
            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xác thực Email đăng ký</h2>
                    <p>Chào bạn,</p>
                    <p>Mã OTP để hoàn tất đăng ký tài khoản của bạn là:</p>
                    <h1 style='color: #2b6cb0; letter-spacing: 5px;'>{otp}</h1>
                    <p>Mã này chỉ được sử dụng một lần và sẽ hết hạn sau 10 phút.</p>
                </div>";
            
            await _emailService.SendEmailAsync(request.Email, "Mã Xác Thực OTP - Math Test Creator", htmlMessage);
        }

        public async Task<LoginResponse> VerifyOtpAndRegisterAsync(VerifyOtpRequest request)
        {
            var cacheKey = $"OTP_{request.Email}";
            
            if (!_cache.TryGetValue(cacheKey, out dynamic? cacheData) || cacheData == null)
            {
                throw new UnauthorizedAccessException("OTP đã hết hạn hoặc không tồn tại.");
            }

            if (cacheData.Otp != request.OtpCode)
            {
                throw new UnauthorizedAccessException("Mã OTP không chính xác.");
            }

            // ONE-TIME USE: Remove OTP from cache IMMEDIATELY upon verification
            _cache.Remove(cacheKey);

            // OTP valid, proceed to register
            RegisterRequest regRequest = cacheData.Request;

            var existingUser = await _authRepository.GetUserByUsernameAsync(regRequest.Username);
            if (existingUser != null) {
                 _cache.Remove(cacheKey); // Cleanup
                 throw new InvalidOperationException("User already exists");
            }

            var user = new User
            {
                Username = regRequest.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(regRequest.Password),
                RoleId = regRequest.RoleId,
                Email = regRequest.Email
            };

            await _authRepository.AddUserAsync(user);

            var token = GenerateJwtToken(user);
            return new LoginResponse
            {
                Token = token,
                Username = user.Username,
                RoleName = user.Role?.RoleName ?? "User",
                Email = user.Email ?? user.Username
            };
        }

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
        {
            var clientId = _configuration["Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrEmpty(clientId) && clientId != "YOUR_GOOGLE_CLIENT_ID_HERE")
            {
                settings.Audience = new[] { clientId };
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                if (payload == null)
                    throw new UnauthorizedAccessException("Invalid Google token");
                
                return payload;
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedAccessException("Invalid Google token signature");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Username), // Use username as email
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(2);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
