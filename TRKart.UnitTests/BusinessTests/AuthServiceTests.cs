using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TRKart.Business.Services;
using TRKart.Core.Helpers;
using TRKart.Core.Interfaces;
using TRKart.DataAccess;
using TRKart.Entities.DTOs;
using TRKart.Entities.Models;
using Xunit;

namespace TRKart.UnitTests.BusinessTests
{
    public class AuthServiceTests
    {
        private static IConfiguration BuildTestConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"Jwt:Key", "supersecretkeysupersecretkey123456"},
                {"Jwt:Issuer", "TRKart.Tests"},
                {"Jwt:Audience", "TRKart.Tests"},
                {"Jwt:AccessTokenExpireMinutes", "30"},
                {"Jwt:RefreshTokenExpireDays", "7"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        private static ApplicationDbContext CreateInMemoryContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static AuthService CreateService(ApplicationDbContext context)
        {
            var configuration = BuildTestConfiguration();
            var jwtHelper = new JwtHelper(configuration);
            var uniqueChecker = new AlwaysUniqueNumberChecker();
            return new AuthService(context, jwtHelper, uniqueChecker);
        }

        private static Customers CreateCustomer(string email = "user@test.com", string fullName = "Test User", string password = "P@ssw0rd!")
        {
            return new Customers
            {
                Email = email,
                FullName = fullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CustomerNumber = "C000000001"
            };
        }

        [Fact]
        public async Task VerifyPassword_ReturnsFalse_WhenEmailOrPasswordEmpty()
        {
            using var context = CreateInMemoryContext(nameof(VerifyPassword_ReturnsFalse_WhenEmailOrPasswordEmpty));
            var service = CreateService(context);

            Assert.False(await service.VerifyPasswordAsync("", "x"));
            Assert.False(await service.VerifyPasswordAsync("x@test.com", ""));
        }

        [Fact]
        public async Task Register_ReturnsFalse_WhenEmailExists()
        {
            using var context = CreateInMemoryContext(nameof(Register_ReturnsFalse_WhenEmailExists));
            context.Customers.Add(CreateCustomer());
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.RegisterAsync(new RegisterDto
            {
                Email = "user@test.com",
                FullName = "Another User",
                Password = "NewP@ss1"
            });

            Assert.False(result);
        }

        [Fact]
        public async Task Register_CreatesCustomer_WithHashedPassword()
        {
            using var context = CreateInMemoryContext(nameof(Register_CreatesCustomer_WithHashedPassword));
            var service = CreateService(context);

            var result = await service.RegisterAsync(new RegisterDto
            {
                Email = "new@test.com",
                FullName = "New User",
                Password = "StrongP@ss1"
            });

            Assert.True(result);
            var customer = await context.Customers.FirstOrDefaultAsync(c => c.Email == "new@test.com");
            Assert.NotNull(customer);
            Assert.NotEqual("StrongP@ss1", customer!.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("StrongP@ss1", customer.PasswordHash));
        }

        [Fact]
        public async Task Login_ReturnsNull_WhenPasswordInvalid()
        {
            using var context = CreateInMemoryContext(nameof(Login_ReturnsNull_WhenPasswordInvalid));
            var existing = CreateCustomer();
            context.Customers.Add(existing);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var token = await service.LoginAsync(new LoginDto
            {
                Email = existing.Email,
                Password = "WrongPassword!"
            });

            Assert.Null(token);
        }

        [Fact]
        public async Task Login_CreatesNewSession_WhenNoExistingValidSession()
        {
            using var context = CreateInMemoryContext(nameof(Login_CreatesNewSession_WhenNoExistingValidSession));
            var existing = CreateCustomer();
            context.Customers.Add(existing);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var token = await service.LoginAsync(new LoginDto
            {
                Email = existing.Email,
                Password = "P@ssw0rd!"
            }, ipAddress: "1.1.1.1", deviceInfo: "Chrome");

            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token!.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(token.RefreshToken));

            var session = await context.SessionToken.FirstOrDefaultAsync(s => s.CustomerID == existing.CustomerID);
            Assert.NotNull(session);
            Assert.Equal("1.1.1.1", session!.IPAddress);
            Assert.Equal("Chrome", session.DeviceInfo);
        }

        [Fact]
        public async Task Login_ReusesRefreshToken_WhenExistingSessionAndNotBlacklisted()
        {
            using var context = CreateInMemoryContext(nameof(Login_ReusesRefreshToken_WhenExistingSessionAndNotBlacklisted));
            var configuration = BuildTestConfiguration();
            var jwt = new JwtHelper(configuration);

            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var existingRefresh = "refresh-123";
            var existingAccess = jwt.GenerateAccessToken(user.Email, 0);
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = existingRefresh,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(3),
                AccessToken = existingAccess,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(5),
                IsRevoked = false
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var token = await service.LoginAsync(new LoginDto
            {
                Email = user.Email,
                Password = "P@ssw0rd!"
            }, ipAddress: "2.2.2.2", deviceInfo: "Edge");

            Assert.NotNull(token);
            Assert.Equal(existingRefresh, token!.RefreshToken);
            Assert.True(token.AccessTokenExpiration > DateTime.UtcNow);
            var session = await context.SessionToken.FirstAsync();
            Assert.Equal("2.2.2.2", session.IPAddress);
            Assert.Equal("Edge", session.DeviceInfo);
        }

        [Fact]
        public async Task RefreshToken_ReturnsNull_WhenBlacklisted()
        {
            using var context = CreateInMemoryContext(nameof(RefreshToken_ReturnsNull_WhenBlacklisted));
            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var blacklisted = "blacklisted-rt";
            context.TokenBlacklist.Add(new TokenBlacklist { RefreshToken = blacklisted, BlacklistedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.RefreshTokenAsync(blacklisted, ipAddress: "1.1.1.1");
            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshToken_Succeeds_AndBlacklistsOldToken()
        {
            using var context = CreateInMemoryContext(nameof(RefreshToken_Succeeds_AndBlacklistsOldToken));
            var configuration = BuildTestConfiguration();
            var jwt = new JwtHelper(configuration);

            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var oldRefresh = "old-refresh";
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = oldRefresh,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(1),
                AccessToken = jwt.GenerateAccessToken(user.Email, user.CustomerID),
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(10),
                IPAddress = "1.1.1.1"
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var response = await service.RefreshTokenAsync(oldRefresh, ipAddress: "1.1.1.1");

            Assert.NotNull(response);
            Assert.NotEqual(oldRefresh, response!.RefreshToken);
            Assert.True(await context.TokenBlacklist.AnyAsync(t => t.RefreshToken == oldRefresh));
        }

        [Fact]
        public async Task RefreshToken_ReturnsNull_WhenSuspiciousIpChange()
        {
            using var context = CreateInMemoryContext(nameof(RefreshToken_ReturnsNull_WhenSuspiciousIpChange));
            var configuration = BuildTestConfiguration();
            var jwt = new JwtHelper(configuration);

            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var oldRefresh = "old-refresh-ip";
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = oldRefresh,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(1),
                AccessToken = jwt.GenerateAccessToken(user.Email, user.CustomerID),
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(10),
                IPAddress = "1.1.1.1"
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var response = await service.RefreshTokenAsync(oldRefresh, ipAddress: "9.9.9.9");

            Assert.Null(response);
            var session = await context.SessionToken.FirstAsync();
            Assert.True(session.IsRevoked);
            Assert.True(await context.TokenBlacklist.AnyAsync(t => t.RefreshToken == oldRefresh));
        }

        [Fact]
        public async Task ValidateAccessToken_ReturnsTrue_WhenActiveAndNotBlacklisted()
        {
            using var context = CreateInMemoryContext(nameof(ValidateAccessToken_ReturnsTrue_WhenActiveAndNotBlacklisted));
            var configuration = BuildTestConfiguration();
            var jwt = new JwtHelper(configuration);

            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var access = jwt.GenerateAccessToken(user.Email, user.CustomerID);
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = "rt-ok",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(1),
                AccessToken = access,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(30)
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.ValidateAccessTokenAsync(access);

            Assert.True(result.IsValid);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.CustomerID, result.CustomerID);
        }

        [Fact]
        public async Task ValidateAccessToken_ReturnsFalse_AndRevokes_WhenRefreshBlacklisted()
        {
            using var context = CreateInMemoryContext(nameof(ValidateAccessToken_ReturnsFalse_AndRevokes_WhenRefreshBlacklisted));
            var configuration = BuildTestConfiguration();
            var jwt = new JwtHelper(configuration);

            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var access = jwt.GenerateAccessToken(user.Email, user.CustomerID);
            var rt = "rt-bl";
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = rt,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(1),
                AccessToken = access,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(30)
            });
            context.TokenBlacklist.Add(new TokenBlacklist { RefreshToken = rt, BlacklistedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.ValidateAccessTokenAsync(access);

            Assert.False(result.IsValid);
            Assert.True((await context.SessionToken.FirstAsync()).IsRevoked);
        }

        [Fact]
        public async Task RevokeToken_RevokesAndBlacklists()
        {
            using var context = CreateInMemoryContext(nameof(RevokeToken_RevokesAndBlacklists));
            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var rt = "rt-revoke";
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = rt,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(1)
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var ok = await service.RevokeTokenAsync(rt);

            Assert.True(ok);
            var session = await context.SessionToken.FirstAsync();
            Assert.True(session.IsRevoked);
            Assert.True(await context.TokenBlacklist.AnyAsync(t => t.RefreshToken == rt));
        }

        [Fact]
        public async Task GetCustomerIdFromAccessToken_ReturnsId_WhenSessionExists()
        {
            using var context = CreateInMemoryContext(nameof(GetCustomerIdFromAccessToken_ReturnsId_WhenSessionExists));
            var configuration = BuildTestConfiguration();
            var jwt = new JwtHelper(configuration);

            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var access = jwt.GenerateAccessToken(user.Email, user.CustomerID);
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = "rt-x",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(1),
                AccessToken = access,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(30)
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var id = await service.GetCustomerIdFromAccessTokenAsync(access);
            Assert.Equal(user.CustomerID, id);
        }

        [Fact]
        public async Task GetUserEmailByAccessToken_ReturnsEmail_WhenSessionExists()
        {
            using var context = CreateInMemoryContext(nameof(GetUserEmailByAccessToken_ReturnsEmail_WhenSessionExists));
            var configuration = BuildTestConfiguration();
            var jwt = new JwtHelper(configuration);

            var user = CreateCustomer();
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var access = jwt.GenerateAccessToken(user.Email, user.CustomerID);
            context.SessionToken.Add(new SessionToken
            {
                CustomerID = user.CustomerID,
                RefreshToken = "rt-y",
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(1),
                AccessToken = access,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(30)
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var email = await service.GetUserEmailByAccessTokenAsync(access);
            Assert.Equal(user.Email, email);
        }

        [Fact]
        public async Task ChangePassword_Succeeds_AndStoresHistory()
        {
            using var context = CreateInMemoryContext(nameof(ChangePassword_Succeeds_AndStoresHistory));
            var user = CreateCustomer(password: "OldP@ss1");
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var ok = await service.ChangePasswordAsync(new ChangePasswordDto
            {
                Email = user.Email,
                CurrentPassword = "OldP@ss1",
                NewPassword = "NewP@ss1"
            });

            Assert.True(ok);
            var updated = await context.Customers.FirstAsync();
            Assert.True(BCrypt.Net.BCrypt.Verify("NewP@ss1", updated.PasswordHash));
            Assert.Single(context.PasswordHistory);
        }

        [Fact]
        public async Task ChangePassword_Fails_WhenNewMatchesOldOrRecent()
        {
            using var context = CreateInMemoryContext(nameof(ChangePassword_Fails_WhenNewMatchesOldOrRecent));
            var user = CreateCustomer(password: "Old1!");
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            // Put an old password into history that equals the upcoming new password
            context.PasswordHistory.Add(new PasswordHistory
            {
                CustomerID = user.CustomerID,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recent1!"),
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Same as current
            var sameAsCurrent = await service.ChangePasswordAsync(new ChangePasswordDto
            {
                Email = user.Email,
                CurrentPassword = "Old1!",
                NewPassword = "Old1!"
            });
            Assert.False(sameAsCurrent);

            // Same as recent
            var sameAsRecent = await service.ChangePasswordAsync(new ChangePasswordDto
            {
                Email = user.Email,
                CurrentPassword = "Old1!",
                NewPassword = "Recent1!"
            });
            Assert.False(sameAsRecent);
        }

        [Fact]
        public async Task ChangeEmail_Succeeds_WhenValidUniqueAndPasswordCorrect()
        {
            using var context = CreateInMemoryContext(nameof(ChangeEmail_Succeeds_WhenValidUniqueAndPasswordCorrect));
            var user = CreateCustomer(email: "old@test.com", password: "Good1!");
            context.Customers.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var ok = await service.ChangeEmailAsync("old@test.com", new ChangeEmailDto
            {
                NewEmail = "new@test.com",
                Password = "Good1!"
            });

            Assert.True(ok);
            Assert.Equal("new@test.com", (await context.Customers.FirstAsync()).Email);
        }

        [Fact]
        public async Task ChangeEmail_Fails_WhenDuplicateOrInvalid()
        {
            using var context = CreateInMemoryContext(nameof(ChangeEmail_Fails_WhenDuplicateOrInvalid));
            var user1 = CreateCustomer(email: "a@test.com", password: "A1!a");
            var user2 = CreateCustomer(email: "b@test.com", password: "B1!b");
            context.Customers.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // invalid email (no dot)
            var invalid = await service.ChangeEmailAsync("a@test.com", new ChangeEmailDto { NewEmail = "x@x", Password = "A1!a" });
            Assert.False(invalid);

            // duplicate
            var duplicate = await service.ChangeEmailAsync("a@test.com", new ChangeEmailDto { NewEmail = "b@test.com", Password = "A1!a" });
            Assert.False(duplicate);
        }

        private sealed class AlwaysUniqueNumberChecker : IUniqueNumberChecker
        {
            public Task<bool> IsCardNumberUniqueAsync(string cardNumber) => Task.FromResult(true);
            public Task<bool> IsCustomerNumberUniqueAsync(string customerNumber) => Task.FromResult(true);
        }
    }
}


