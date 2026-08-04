using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Tests.IntegrationTests.Common;
using CreateUnitCommand = InventoryManagement.Application.Features.Units.CreateUnit.Command;
using UnitResponse = InventoryManagement.Application.Features.Units.Response;
using UpdateUnitCommand = InventoryManagement.Application.Features.Units.UpdateUnit.Command;

namespace InventoryManagement.Tests.IntegrationTests.Units
{
    public class UnitsTests : TestBase
    {
        public UnitsTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task GetUnits_Should_Return_Default_Seeded_Units()
        {
            await AuthenticateAsync();

            var response = await Client.GetAsync("/api/units");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<List<UnitResponse>>();
            result.Should().NotBeNull();
            result!.Select(x => x.Name).Should().Contain(
                "Ton",
                "Kilogram",
                "Bag",
                "Piece",
                "Cubic foot",
                "Cubic meter");
        }

        [Fact]
        public async Task CreateUnit_Should_Create_Unit()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync("/api/units", new CreateUnitCommand
            {
                Name = $"Toker {Guid.NewGuid():N}",
                ShortName = "Tok"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var result = await response.Content.ReadFromJsonAsync<UnitResponse>();
            result.Should().NotBeNull();
            result!.Name.Should().StartWith("Toker");
            result.ShortName.Should().Be("Tok");
            result.FactorToBaseUnit.Should().Be(1m);
            result.BaseUnitId.Should().Be(result.Id);
            result.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUnit_Should_Reject_Duplicate_Name()
        {
            await AuthenticateAsync();
            var name = $"Duplicate {Guid.NewGuid():N}";

            var request = new CreateUnitCommand
            {
                Name = name,
                ShortName = "Dup"
            };

            await Client.PostAsJsonAsync("/api/units", request);

            var response = await Client.PostAsJsonAsync("/api/units", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateUnit_Should_Validate_Name_And_ShortName()
        {
            await AuthenticateAsync();

            var response = await Client.PostAsJsonAsync("/api/units", new CreateUnitCommand
            {
                Name = string.Empty,
                ShortName = new string('A', 21)
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Units_Should_Support_Get_Update_And_Delete()
        {
            await AuthenticateAsync();
            var unit = await CreateUnitAsync();

            var getResponse = await Client.GetAsync($"/api/units/{unit.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updateResponse = await Client.PutAsJsonAsync($"/api/units/{unit.Id}", new UpdateUnitCommand(
                unit.Id,
                $"Updated {Guid.NewGuid():N}",
                "Upd",
                1m,
                null,
                false));

            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await updateResponse.Content.ReadFromJsonAsync<UnitResponse>();
            updated.Should().NotBeNull();
            updated!.IsActive.Should().BeFalse();
            updated.ShortName.Should().Be("Upd");

            var deleteResponse = await Client.DeleteAsync($"/api/units/{unit.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private async Task<UnitResponse> CreateUnitAsync()
        {
            var response = await Client.PostAsJsonAsync("/api/units", new CreateUnitCommand
            {
                Name = $"Unit {Guid.NewGuid():N}",
                ShortName = "Unt"
            });

            response.EnsureSuccessStatusCode();
            var unit = await response.Content.ReadFromJsonAsync<UnitResponse>();
            unit.Should().NotBeNull();
            return unit!;
        }
    }
}
