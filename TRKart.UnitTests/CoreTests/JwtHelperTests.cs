using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TRKart.Core.Helpers;
using Xunit;

namespace TRKart.UnitTests.CoreTests
{
	public class JwtHelperTests
	{
		private IConfiguration BuildConfig()
		{
			var inMemorySettings = new Dictionary<string, string?>
			{
				{"Jwt:Key", "test_secret_key_1234567890"},
				{"Jwt:Issuer", "TRKart.Tests"},
				{"Jwt:Audience", "TRKart.Tests.Audience"},
				{"Jwt:AccessTokenExpireMinutes", "5"},
				{"Jwt:RefreshTokenExpireDays", "7"}
			};
			return new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();
		}

		[Fact]
		public void GenerateAccessToken_ContainsClaims()
		{
			var helper = new JwtHelper(BuildConfig());
			var token = helper.GenerateAccessToken("user@test.com", 42);
			Assert.False(string.IsNullOrWhiteSpace(token));
		}

		[Fact]
		public void GenerateRefreshToken_ReturnsBase64()
		{
			var helper = new JwtHelper(BuildConfig());
			var token = helper.GenerateRefreshToken();
			Assert.False(string.IsNullOrWhiteSpace(token));
			// Basic sanity: Base64 should decode
			var bytes = Convert.FromBase64String(token);
			Assert.True(bytes.Length > 0);
		}

		[Fact]
		public void GetAccessTokenExpiration_InFuture()
		{
			var helper = new JwtHelper(BuildConfig());
			var exp = helper.GetAccessTokenExpiration();
			Assert.True(exp > DateTimeOffset.UtcNow);
		}

		[Fact]
		public void GetRefreshTokenExpiration_InFuture()
		{
			var helper = new JwtHelper(BuildConfig());
			var exp = helper.GetRefreshTokenExpiration();
			Assert.True(exp > DateTimeOffset.UtcNow);
		}
	}
}


