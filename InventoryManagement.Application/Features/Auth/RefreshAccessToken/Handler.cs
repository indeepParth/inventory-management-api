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

namespace InventoryManagement.Application.Features.Auth.RefreshAccessToken
{
    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;

        public Handler(IRefreshTokenRepository refreshTokenRepository , IIdentityService identityService, ITokenService tokenService)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _identityService = identityService;
            _tokenService = tokenService;
        }
        
        public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
        {
            var storedToken =
        await _refreshTokenRepository.GetByTokenAsync(
            request.RefreshToken,
            cancellationToken);

            if (storedToken is null)
                throw new UnauthorizedAccessException(
                    "Invalid refresh token.");

            if (!storedToken.IsActive)
                throw new UnauthorizedAccessException(
                    "Refresh token is no longer active.");

            var userName =
                await _identityService.GetUserNameAsync(
                    storedToken.UserId);

            if (userName is null)
                throw new UnauthorizedAccessException(
                    "User not valid.");

            var roles =
                await _identityService.GetRolesAsync(
                    userName);

            var accessToken =
                _tokenService.GenerateToken(
                    storedToken.UserId,
                    userName,
                    roles);

            var newRefreshToken =
                _tokenService.GenerateRefreshToken();

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshToken;

            await _refreshTokenRepository.AddAsync(
                new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = storedToken.UserId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                },
                cancellationToken);

            await _refreshTokenRepository.SaveChangesAsync(
                cancellationToken);

            return new Response
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = _tokenService.GetAccessTokenExpiration()
            };
        }
    }
}
