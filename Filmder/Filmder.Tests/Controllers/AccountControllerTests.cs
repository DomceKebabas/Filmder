using Filmder.Controllers;
using Filmder.DTOs;
using Filmder.Models;
using Filmder.Services;
using Filmder.Interfaces;
using Filmder.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;

namespace Filmder.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly Mock<SignInManager<AppUser>> _mockSignInManager;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mockUserManager = MockHelpers.GetMockUserManager();
        _mockSignInManager = MockHelpers.GetMockSignInManager(_mockUserManager);
        _mockTokenService = new Mock<ITokenService>();
        _mockEmailSender = new Mock<IEmailSender>();
        
        _controller = new AccountController(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockTokenService.Object,
            _mockEmailSender.Object
        );
    }

    [Fact(Skip = "Email confirmation flow requires complex mocking")]
    public async Task Register_ValidDto_ReturnsUserDto()
    {
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        var createdUser = new AppUser
        {
            Id = "test-user-id",
            Email = registerDto.Email,
            UserName = registerDto.Email
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<AppUser, string>((user, password) => {
                user.Id = createdUser.Id;
                user.Email = createdUser.Email;
                user.UserName = createdUser.UserName;
            });

        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<AppUser>()))
            .ReturnsAsync("fake-confirmation-token");

        _mockTokenService.Setup(x => x.CreateToken(It.IsAny<AppUser>()))
            .Returns("fake-jwt-token");

        _mockEmailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://test.com/confirm");
        _controller.Url = mockUrlHelper.Object;

        var result = await _controller.Register(registerDto);

        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var userDto = actionResult.Value as UserDto;
        userDto.Should().NotBeNull();
        userDto.Email.Should().Be(registerDto.Email);
        userDto.Token.Should().Be("fake-jwt-token");
    }
    
    [Fact]
    public async Task Register_CreationFails_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "weak"
        };

        var errors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" }
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var actionResult = result.Result as BadRequestObjectResult;
        actionResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsUserDto()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        var user = new AppUser
        {
            Id = "user123",
            Email = loginDto.Email,
            UserName = loginDto.Email
        };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _mockTokenService.Setup(x => x.CreateToken(user))
            .Returns("fake-jwt-token");
        
        //Act
        var result  = await _controller.Login(loginDto);
        
        // Assert
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        
        var userDto = actionResult.Value as UserDto;
        userDto.Should().NotBeNull();
        userDto.Email.Should().Be(loginDto.Email);
        userDto.Token.Should().Be("fake-jwt-token");
    }
    
    [Fact]
    public async Task Login_UserNotFound_ThrowsLoginFailedException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((AppUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Filmder.Exceptions.LoginFailedException>(
            async () => await _controller.Login(loginDto)
        );
        
        exception.Message.Should().Be("User with this email does not exist.");
    }

    [Fact]
    public async Task Login_InvalidPassword_ThrowsLoginFailedException()
    {
        //Arrange
        var loginDto = new LoginDto
        {
            Email = "test1@example.com",
            Password = "WRONGPASWWORD"
        };

        var user = new AppUser
        {
            Id = "user123",
            Email = loginDto.Email,
            UserName = loginDto.Email
        };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager.Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Filmder.Exceptions.LoginFailedException>(
            async () => await _controller.Login(loginDto)
        );
        
        exception.Message.Should().Be("Incorrect password.");
    }
}