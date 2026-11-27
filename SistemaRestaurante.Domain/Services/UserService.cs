using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SistemaRestaurante.Domain.Entities;
using SistemaRestaurante.Domain.Enums;
using SistemaRestaurante.Domain.Interfaces;
using SistemaRestaurante.Domain.Interfaces.Repositories;
using SistemaRestaurante.Domain.Interfaces.Services;

namespace SistemaRestaurante.Domain.Services
{
    public class UserService : IUserService
    {
        public const int MinPasswordLength = 8;

        // Hash de una contraseña cualquiera, solo para igualar tiempos cuando el usuario no existe.
        private static readonly string DummyHash =
            new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ITenantContext _tenant;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            ITenantContext tenant,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tenant = tenant;
            _logger = logger;
        }

        public async Task<User?> ValidateCredentialsAsync(string username, string password)
        {
            var user = await _userRepository.GetForLoginAsync(NormalizeUsername(username));
            if (user == null || !user.IsActive)
            {
                // Mismo coste que una comprobación real, para no revelar qué usuarios existen.
                _passwordHasher.VerifyHashedPassword(new User(), DummyHash, password);
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Failed login for user {Username}", user.Username);
                return null;
            }

            // Solo se informa de la suspensión a quien ya ha demostrado conocer la contraseña.
            if (user.Role != UserRole.PlatformAdmin && user.Restaurant is not { IsActive: true })
                throw new ForbiddenAccessException("El restaurante está suspendido. Contacta con el soporte.");

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = _passwordHasher.HashPassword(user, password);

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return user;
        }

        public Task<IEnumerable<User>> GetAllAsync() => _userRepository.GetAllIncludingInactiveAsync();

        public Task<User?> GetByIdAsync(int id) => _userRepository.GetByIdIncludingInactiveAsync(id);

        public Task<User> CreateAsync(string username, string fullName, UserRole role, string password)
        {
            return CreateForRestaurantAsync(_tenant.RequireRestaurantId(), username, fullName, role, password);
        }

        public async Task<User> CreateForRestaurantAsync(int restaurantId, string username, string fullName, UserRole role, string password)
        {
            EnsureRestaurantRole(role);
            return await CreateUserAsync(username, fullName, role, password, restaurantId);
        }

        public async Task EnsureCanCreateAsync(string username, string password)
        {
            ValidatePassword(password);
            if (await _userRepository.GetForLoginAsync(NormalizeUsername(username)) != null)
                throw new InvalidOperationException($"El usuario \"{NormalizeUsername(username)}\" ya existe");
        }

        public async Task UpdateAsync(int id, string fullName, UserRole role, bool isActive, int currentUserId)
        {
            EnsureRestaurantRole(role);

            var user = await _userRepository.GetByIdIncludingInactiveAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");

            if (user.Role == UserRole.PlatformAdmin)
                throw new ForbiddenAccessException("El administrador de la plataforma no se gestiona desde aquí");

            if (id == currentUserId && (!isActive || role != user.Role))
                throw new InvalidOperationException("No puedes cambiar tu propio rol ni desactivarte");

            var losesAdmin = user.Role == UserRole.Admin && user.IsActive && (role != UserRole.Admin || !isActive);
            if (losesAdmin && await _userRepository.CountActiveByRoleAsync(UserRole.Admin) <= 1)
                throw new InvalidOperationException("Debe quedar al menos un administrador activo en el restaurante");

            // Cambiar el rol o desactivar invalida las sesiones abiertas de ese usuario.
            if (user.Role != role || user.IsActive != isActive)
                user.SecurityStamp = NewStamp();

            user.FullName = fullName.Trim();
            user.Role = role;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updating user {UserId}: role {Role}, active {IsActive}", id, role, isActive);
            await _userRepository.UpdateAsync(user);
        }

        public async Task ResetPasswordAsync(int id, string newPassword)
        {
            ValidatePassword(newPassword);
            var user = await _userRepository.GetByIdIncludingInactiveAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el usuario con ID {id}");

            SetPassword(user, newPassword);
            _logger.LogInformation("Password reset for user {UserId}", id);
            await _userRepository.UpdateAsync(user);
        }

        public async Task ChangeOwnPasswordAsync(int id, string currentPassword, string newPassword)
        {
            var user = await _userRepository.GetForAuthAsync(id)
                ?? throw new KeyNotFoundException("Usuario no encontrado");

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
                throw new ArgumentException("La contraseña actual no es correcta");

            ValidatePassword(newPassword);
            SetPassword(user, newPassword);
            await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> EnsurePlatformAdminAsync(string username, string fullName, string password)
        {
            if (await _userRepository.AnyPlatformAdminAsync())
                return false;

            await CreateUserAsync(username, fullName, UserRole.PlatformAdmin, password, restaurantId: null);
            return true;
        }

        private async Task<User> CreateUserAsync(string username, string fullName, UserRole role, string password, int? restaurantId)
        {
            username = NormalizeUsername(username);
            await EnsureCanCreateAsync(username, password);

            var user = new User
            {
                Username = username,
                FullName = fullName.Trim(),
                Role = role,
                RestaurantId = restaurantId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _logger.LogInformation("Creating user {Username} with role {Role} in restaurant {RestaurantId}", username, role, restaurantId);
            return await _userRepository.CreateAsync(user);
        }

        private static void EnsureRestaurantRole(UserRole role)
        {
            if (role == UserRole.PlatformAdmin)
                throw new ArgumentException("El rol no es válido para el personal de un restaurante");
        }

        private void SetPassword(User user, string password)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            user.SecurityStamp = NewStamp();
            user.UpdatedAt = DateTime.UtcNow;
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
                throw new ArgumentException($"La contraseña debe tener al menos {MinPasswordLength} caracteres");
        }

        private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

        private static string NewStamp() => Guid.NewGuid().ToString("N");
    }
}
