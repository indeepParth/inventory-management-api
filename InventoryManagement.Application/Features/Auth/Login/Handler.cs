using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using InventoryManagement.Application.Common.Exceptions;
using InventoryManagement.Application.Common.Identity;
using InventoryManagement.Application.Common.Interfaces;
using InventoryManagement.Application.Common.Persistence;
using InventoryManagement.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Features.Auth.Login
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<Handler> _logger;

        public Handler(IIdentityService identityService,
                        ITokenService tokenService,
                        IRefreshTokenRepository refreshTokenRepository,
                        ILogger<Handler> logger)
        {
            _identityService = identityService;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }
        
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var isValid = await _identityService.CheckPasswordAsync(
                request.UserName,
                request.Password);

            if (!isValid)
            {
                _logger.LogWarning("Invalid username or password for this user {user}", request.UserName);
                throw new UnauthorizedAccessException(
                    "Invalid username or password.");
            }

            var userId = await _identityService.GetUserIdAsync(
                request.UserName);

            var roles = await _identityService.GetRolesAsync(
                request.UserName);

            var token = _tokenService.GenerateToken(
                userId!,
                request.UserName,
                roles);

            var refreshToken = _tokenService.GenerateRefreshToken();

            await _refreshTokenRepository.AddAsync(
                new RefreshToken
                {
                    Token = refreshToken,
                    UserId = userId!,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                },
                cancellationToken);

            await _refreshTokenRepository.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation("User {UserName} logged in successfully", request.UserName);

            return new Response
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }
    }
}