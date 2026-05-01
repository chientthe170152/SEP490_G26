using System;
using System.Security.Claims;
using Backend.Services.Implements;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Backend_UnitTest.CurrentUserUnitTest
{
    public class CurrentUserServiceTests
    {
        private static CurrentUserService BuildService(ClaimsPrincipal? principal)
        {
            var accessor = new Mock<IHttpContextAccessor>();
            if (principal is null)
            {
                accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            }
            else
            {
                var httpContext = new DefaultHttpContext { User = principal };
                accessor.Setup(a => a.HttpContext).Returns(httpContext);
            }
            return new CurrentUserService(accessor.Object);
        }

        private static ClaimsPrincipal BuildPrincipal(params Claim[] claims) =>
            new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        [Fact(DisplayName = "UserId - UTCID01 - Claim hợp lệ -> trả về int UserId")]
        public void UserId_UTCID01_ValidClaim_ShouldReturnInt()
        {
            var principal = BuildPrincipal(new Claim(ClaimTypes.NameIdentifier, "42"));
            var service = BuildService(principal);

            Assert.Equal(42, service.UserId);
        }

        [Fact(DisplayName = "UserId - UTCID02 - Claim không có -> InvalidOperationException")]
        public void UserId_UTCID02_MissingClaim_ShouldThrow()
        {
            var principal = BuildPrincipal();
            var service = BuildService(principal);

            Assert.Throws<InvalidOperationException>(() => service.UserId);
        }

        [Fact(DisplayName = "UserId - UTCID03 - Claim không phải số -> InvalidOperationException")]
        public void UserId_UTCID03_InvalidClaim_ShouldThrow()
        {
            var principal = BuildPrincipal(new Claim(ClaimTypes.NameIdentifier, "abc"));
            var service = BuildService(principal);

            Assert.Throws<InvalidOperationException>(() => service.UserId);
        }

        [Fact(DisplayName = "Email - UTCID01 - Claim hợp lệ -> trả về email")]
        public void Email_UTCID01_ValidClaim_ShouldReturnEmail()
        {
            var principal = BuildPrincipal(new Claim(ClaimTypes.Email, "user@test.com"));
            var service = BuildService(principal);

            Assert.Equal("user@test.com", service.Email);
        }

        [Fact(DisplayName = "Email - UTCID02 - Claim không có -> InvalidOperationException")]
        public void Email_UTCID02_MissingClaim_ShouldThrow()
        {
            var principal = BuildPrincipal();
            var service = BuildService(principal);

            Assert.Throws<InvalidOperationException>(() => service.Email);
        }

        [Fact(DisplayName = "Role - UTCID01 - Claim hợp lệ -> trả về role")]
        public void Role_UTCID01_ValidClaim_ShouldReturnRole()
        {
            var principal = BuildPrincipal(new Claim(ClaimTypes.Role, "Admin"));
            var service = BuildService(principal);

            Assert.Equal("Admin", service.Role);
        }

        [Fact(DisplayName = "Role - UTCID02 - Claim không có -> InvalidOperationException")]
        public void Role_UTCID02_MissingClaim_ShouldThrow()
        {
            var principal = BuildPrincipal();
            var service = BuildService(principal);

            Assert.Throws<InvalidOperationException>(() => service.Role);
        }

        [Fact(DisplayName = "Principal - UTCID01 - HttpContext null -> InvalidOperationException")]
        public void Principal_UTCID01_NoHttpContext_ShouldThrow()
        {
            var service = BuildService(null);

            Assert.Throws<InvalidOperationException>(() => service.UserId);
            Assert.Throws<InvalidOperationException>(() => service.Email);
            Assert.Throws<InvalidOperationException>(() => service.Role);
        }
    }
}
