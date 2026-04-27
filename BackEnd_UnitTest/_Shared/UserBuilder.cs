using System;
using Backend.Models;

namespace BackEnd_UnitTest._Shared;

public class UserBuilder
{
    private int _userId = 1;
    private string _email = "user@test.com";
    private string? _passwordHash = "$2a$11$placeholder";
    private int _roleId = 2;
    private int _status = 1;
    private DateTime _securityStamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private string? _fullName = "Nguyen Van A";
    private string? _phoneNumber = "0123456789";
    private string? _studentId = "HE172047";
    private Role? _role;

    public static UserBuilder New() => new();

    public UserBuilder WithId(int id) { _userId = id; return this; }
    public UserBuilder WithEmail(string email) { _email = email; return this; }
    public UserBuilder WithPasswordHash(string? hash) { _passwordHash = hash; return this; }
    public UserBuilder WithRoleId(int roleId) { _roleId = roleId; return this; }
    public UserBuilder WithRole(string roleName, int roleId = 0)
    {
        _role = new Role { RoleId = roleId == 0 ? _roleId : roleId, Name = roleName };
        if (roleId != 0) _roleId = roleId;
        return this;
    }
    public UserBuilder WithStatus(int status) { _status = status; return this; }
    public UserBuilder WithSecurityStamp(DateTime stamp) { _securityStamp = stamp; return this; }
    public UserBuilder WithFullName(string? fullName) { _fullName = fullName; return this; }
    public UserBuilder WithPhone(string? phone) { _phoneNumber = phone; return this; }
    public UserBuilder WithStudentId(string? studentId) { _studentId = studentId; return this; }

    public User Build() => new()
    {
        UserId = _userId,
        Email = _email,
        PasswordHash = _passwordHash,
        RoleId = _roleId,
        Status = _status,
        SecurityStamp = _securityStamp,
        FullName = _fullName,
        PhoneNumber = _phoneNumber,
        StudentId = _studentId,
        Role = _role!,
        ConcurrencyStamp = Array.Empty<byte>()
    };

    public static User WithBcryptPassword(string password, Action<UserBuilder>? customize = null)
    {
        var b = New().WithPasswordHash(BCrypt.Net.BCrypt.HashPassword(password));
        customize?.Invoke(b);
        return b.Build();
    }
}
