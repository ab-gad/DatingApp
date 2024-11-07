using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(DataContext _dbContext, ITokenService _tokenService) : BaseController
{
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (await IsUserExists(registerDto.Username)) return BadRequest("This username is already existed");

        using var hmac = new HMACSHA512();
        var user = new AppUser
        {
            UserName = registerDto.Username.ToLower(),
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new UserDto { Username = user.UserName, Token = _tokenService.CreateToken(user) };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == loginDto.Username.ToLower());
        if (user == null) return Unauthorized("User dose not exist");

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var comingPassHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        if (!IsSameHash(comingPassHash, user.PasswordHash)) return Unauthorized("Wrong Password");

        return new UserDto { Username = user.UserName, Token = _tokenService.CreateToken(user) };
    }

    private async Task<bool> IsUserExists(string username)
    {
        return await _dbContext.Users.AnyAsync(u => u.UserName.ToLower() == username.ToLower());
    }
    private bool IsSameHash(byte[] hash1, byte[] hash2)
    {
        if (hash1 == hash2) return true;
        if (hash1.Length != hash2.Length) return false;

        for (int i = 0; i < hash1.Length; i++)
        {
            if (hash1[i] != hash2[i]) return false;
        }

        return true;
    }
}