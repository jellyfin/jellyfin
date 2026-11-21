using System;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class UserItemDataTests
{
    public static TheoryData<double> ValidRatings => new()
    {
        0,
        0.1,
        1,
        5,
        6.4,
        6.49,
        UserItemData.MinLikeValue,
        6.51,
        9.9,
        10
    };

    public static TheoryData<double> OutOfRangeRatings => new()
    {
        -0.1,
        -1,
        10.1,
        11,
        100,
        double.MaxValue,
        double.MinValue,
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity
    };

    [Theory]
    [MemberData(nameof(ValidRatings))]
    public void Rating_WithinRange_IsAccepted(double rating)
    {
        var data = new UserItemData { Key = "key", Rating = rating };

        Assert.Equal(rating, data.Rating);
    }

    [Theory]
    [MemberData(nameof(OutOfRangeRatings))]
    public void Rating_OutsideRange_Throws(double rating)
    {
        var data = new UserItemData { Key = "key" };

        Assert.Throws<ArgumentOutOfRangeException>(() => data.Rating = rating);
    }

    [Fact]
    public void Rating_Null_IsAccepted()
    {
        var data = new UserItemData { Key = "key", Rating = 5 };

        data.Rating = null;

        Assert.Null(data.Rating);
        Assert.Null(data.Likes);
    }

    [Theory]
    // Below the like threshold.
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(6, false)]
    [InlineData(6.4, false)]
    [InlineData(6.49, false)]
    // At and above the like threshold. MinLikeValue itself counts as liked.
    [InlineData(6.5, true)]
    [InlineData(6.51, true)]
    [InlineData(7, true)]
    [InlineData(10, true)]
    public void Likes_IsDerivedFromRating(double rating, bool expectedLikes)
    {
        var data = new UserItemData { Key = "key", Rating = rating };

        Assert.Equal(expectedLikes, data.Likes);
    }

    [Fact]
    public void Likes_JustBelowThreshold_IsNotLiked()
    {
        // BitDecrement, not double.Epsilon: 6.5 - double.Epsilon is still exactly 6.5.
        var justBelow = Math.BitDecrement(UserItemData.MinLikeValue);
        Assert.NotEqual(UserItemData.MinLikeValue, justBelow);

        var data = new UserItemData { Key = "key", Rating = justBelow };

        // Guards the exact boundary: anything strictly below MinLikeValue is a dislike.
        Assert.False(data.Likes);
    }

    [Fact]
    public void Likes_NoRating_IsNull()
    {
        var data = new UserItemData { Key = "key" };

        Assert.Null(data.Rating);
        Assert.Null(data.Likes);
    }

    [Theory]
    [InlineData(true, 10)]
    [InlineData(false, 1)]
    public void Likes_Set_WritesThroughToRating(bool likes, double expectedRating)
    {
        var data = new UserItemData { Key = "key" };

        data.Likes = likes;

        Assert.Equal(expectedRating, data.Rating);
        Assert.Equal(likes, data.Likes);
    }

    [Fact]
    public void Likes_SetNull_ClearsRating()
    {
        var data = new UserItemData { Key = "key", Rating = 8 };

        data.Likes = null;

        Assert.Null(data.Rating);
        Assert.Null(data.Likes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Likes_RoundTrip_IsStable(bool likes)
    {
        var data = new UserItemData { Key = "key" };

        data.Likes = likes;
        var rating = data.Rating;
        data.Rating = rating;

        Assert.Equal(likes, data.Likes);
        Assert.Equal(rating, data.Rating);
    }

    [Fact]
    public void Rating_OverwritesPreviousLikes()
    {
        var data = new UserItemData { Key = "key" };

        data.Likes = true;
        Assert.Equal(10, data.Rating);

        // An explicit low rating must flip the derived like state.
        data.Rating = 2;
        Assert.False(data.Likes);
    }
}
