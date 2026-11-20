using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Api.ModelBinders;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.ModelBinders
{
    public sealed class CommaDelimitedCollectionModelBinderTests
    {
        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedStringArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<string> queryParamValues = new[] { "lol", "xd" };
            var queryParamString = "lol,xd";
            var queryParamType = typeof(string[]);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues> { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((IReadOnlyList<string>?)bindingContextMock.Object?.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedIntArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<int> queryParamValues = new[] { 42, 0 };
            var queryParamString = "42,0";
            var queryParamType = typeof(int[]);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues> { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((IReadOnlyList<int>?)bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedEnumArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<TestType> queryParamValues = new[] { TestType.How, TestType.Much };
            var queryParamString = "How,Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues> { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((IReadOnlyList<TestType>?)bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedEnumArrayQueryWithDoubleCommas()
        {
            var queryParamName = "test";
            IReadOnlyList<TestType> queryParamValues = new[] { TestType.How, TestType.Much };
            var queryParamString = "How,,Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues> { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((IReadOnlyList<TestType>?)bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidEnumArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<TestType> queryParamValues = new[] { TestType.How, TestType.Much };
            var queryParamString1 = "How";
            var queryParamString2 = "Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { queryParamName, new StringValues(new[] { queryParamString1, queryParamString2 }) },
                    }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((IReadOnlyList<TestType>?)bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsEmptyEnumArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<TestType> queryParamValues = Array.Empty<TestType>();
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { queryParamName, new StringValues(value: null) },
                    }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((IReadOnlyList<TestType>?)bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_EnumArrayQuery_BindValidOnly()
        {
            var queryParamName = "test";
            var queryParamString = "🔥,😢";
            var queryParamType = typeof(IReadOnlyList<TestType>);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues> { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);
            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            var listResult = (IReadOnlyList<TestType>?)bindingContextMock.Object.Result.Model;
            Assert.NotNull(listResult);
            Assert.Empty(listResult);
        }

        [Fact]
        public async Task BindModelAsync_EnumArrayQuery_BindValidOnly_2()
        {
            var queryParamName = "test";
            var queryParamString1 = "How";
            var queryParamString2 = "😱";
            var queryParamType = typeof(IReadOnlyList<TestType>);

            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { queryParamName, new StringValues(new[] { queryParamString1, queryParamString2 }) },
                    }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);
            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            var listResult = (IReadOnlyList<TestType>?)bindingContextMock.Object.Result.Model;
            Assert.NotNull(listResult);
            Assert.Single(listResult);
        }

        [Fact]
        public async Task BindModelAsync_ItemFieldsQuery_SkipsLegacyValues()
        {
            var bindingContextMock = CreateBindingContext(
                "fields",
                typeof(ItemFields[]),
                new StringValues("PremiereDate,Overview,UserData,Genres,RunTimeTicks"));
            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.Equal(
                new[] { ItemFields.Overview, ItemFields.Genres },
                (ItemFields[]?)bindingContextMock.Object.Result.Model);
        }

        [Fact]
        public async Task BindModelAsync_ItemFieldsQuery_ReturnsEmptyForOnlyLegacyValues()
        {
            var bindingContextMock = CreateBindingContext(
                "fields",
                typeof(ItemFields[]),
                new StringValues("PremiereDate,UserData,RunTimeTicks"));
            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.Empty((ItemFields[]?)bindingContextMock.Object.Result.Model ?? Array.Empty<ItemFields>());
        }

        [Fact]
        public async Task BindModelAsync_ItemFieldsQuery_BindsCaseInsensitiveValues()
        {
            var bindingContextMock = CreateBindingContext("fields", typeof(ItemFields[]), new StringValues("overview,GENRES"));
            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.Equal(
                new[] { ItemFields.Overview, ItemFields.Genres },
                (ItemFields[]?)bindingContextMock.Object.Result.Model);
        }

        [Fact]
        public async Task BindModelAsync_ItemFieldsQuery_BindsRepeatedQueryParameters()
        {
            var bindingContextMock = CreateBindingContext(
                "fields",
                typeof(ItemFields[]),
                new StringValues(new[] { "Overview", "Genres" }));
            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.Equal(
                new[] { ItemFields.Overview, ItemFields.Genres },
                (ItemFields[]?)bindingContextMock.Object.Result.Model);
        }

        [Fact]
        public async Task BindModelAsync_EnumArrayQuery_PreservesNumericValues()
        {
            var bindingContextMock = CreateBindingContext("test", typeof(TestType[]), new StringValues("0,1"));
            var modelBinder = new CommaDelimitedCollectionModelBinder(new NullLogger<CommaDelimitedCollectionModelBinder>());

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.Equal(new[] { TestType.How, TestType.Much }, (TestType[]?)bindingContextMock.Object.Result.Model);
        }

        [Fact]
        public async Task BindModelAsync_InvalidEnumValues_DoesNotLogExceptions()
        {
            var bindingContextMock = CreateBindingContext(
                "fields",
                typeof(ItemFields[]),
                new StringValues("PremiereDate,UserData,RunTimeTicks"));
            var loggerMock = new Mock<ILogger<CommaDelimitedCollectionModelBinder>>();
            var modelBinder = new CommaDelimitedCollectionModelBinder(loggerMock.Object);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            loggerMock.Verify(
                logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        private static Mock<ModelBindingContext> CreateBindingContext(string modelName, Type modelType, StringValues values)
        {
            var valueProvider = new QueryStringValueProvider(
                new BindingSource(string.Empty, string.Empty, false, false),
                new QueryCollection(new Dictionary<string, StringValues> { { modelName, values } }),
                CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(modelName);
            bindingContextMock.Setup(b => b.ModelType).Returns(modelType);
            bindingContextMock.SetupProperty(b => b.Result);
            return bindingContextMock;
        }
    }
}
