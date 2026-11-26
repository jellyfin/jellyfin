using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class TranscodeTestRequestTests
{
    [Fact]
    public void Kind_AllDeclaredValuesAreValid()
    {
        foreach (var kind in Enum.GetValues<TranscodeTestKind>())
        {
            var request = new TranscodeTestRequestDto { Kind = kind };
            Assert.True(Validator.TryValidateObject(request, new ValidationContext(request), [], true));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public void Kind_UndefinedValueIsInvalid(int value)
    {
        var request = new TranscodeTestRequestDto { Kind = (TranscodeTestKind)value };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), errors, true));
        Assert.Contains(nameof(TranscodeTestRequestDto.Kind), Assert.Single(errors).MemberNames);
    }
}
