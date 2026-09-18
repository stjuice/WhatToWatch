using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WhatToWatch.Controllers;
using WhatToWatch.DTOs;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Controllers;

public sealed class PartiesControllerTests
{
    private readonly Mock<IPartyService> _service = new();

    [Fact]
    public async Task GetState_ReadsPlayerTokenFromHeader()
    {
        _service.Setup(service => service.GetStateAsync(
                "party", "secret", It.IsAny<CancellationToken>()))
            .ReturnsAsync(State());
        var controller = CreateController("secret");

        var result = await controller.GetStateAsync("party", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        _service.VerifyAll();
    }

    [Fact]
    public async Task Vote_ReadsPlayerTokenFromHeader()
    {
        var request = new PartyVoteRequest
        {
            MovieId = "tt1",
            Liked = true,
        };
        _service.Setup(service => service.VoteAsync(
                "party", "secret", request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(State());
        var controller = CreateController("secret");

        var result = await controller.VoteAsync("party", request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        _service.VerifyAll();
    }

    [Fact]
    public async Task PartyException_IsReturnedWithTypedErrorAndStatus()
    {
        _service.Setup(service => service.JoinAsync(
                "123", "guest-list", It.IsAny<CancellationToken>()))
            .ThrowsAsync(PartyException.PartyFull());
        var controller = CreateController();

        var result = await controller.JoinAsync(
            new JoinPartyRequest
            {
                JoinCode = "123",
                WatchlistId = "guest-list",
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var error = Assert.IsType<PartyErrorDto>(objectResult.Value);
        Assert.Equal("PartyFull", error.Code);
    }

    [Fact]
    public async Task MissingToken_IsPassedAsEmpty_ForTypedServiceValidation()
    {
        _service.Setup(service => service.GetStateAsync(
                "party", "", It.IsAny<CancellationToken>()))
            .ThrowsAsync(PartyException.InvalidPlayerToken());
        var controller = CreateController();

        var result = await controller.GetStateAsync("party", CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
    }

    private PartiesController CreateController(string? token = null)
    {
        var controller = new PartiesController(_service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        if (token is not null)
            controller.Request.Headers["X-Player-Token"] = token;
        return controller;
    }

    private static PartyStateDto State() =>
        new()
        {
            PartyId = "party",
            JoinCode = "123",
            Status = "Playing",
            PlayerCount = 1,
            Progress = new PartyProgressDto { TotalMovies = 1 },
            Batch = [],
        };
}
