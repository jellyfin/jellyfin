using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Jellyfin.Api.ModelBinders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.ModelBinders
{
    public sealed class PipeDelimitedCollectionModelBinderTests
    {
        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidPipeDelimitedStringArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<string> queryParamValues = new[] { "lol", "xd" };
            var queryParamString = "lol|xd";
            var queryParamType = typeof(string[]);

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());
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
        public async Task BindModelAsync_CorrectlyBindsValidDelimitedIntArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<int> queryParamValues = new[] { 42, 0 };
            var queryParamString = "42|0";
            var queryParamType = typeof(int[]);

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());
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
        public async Task BindModelAsync_CorrectlyBindsValidPipeDelimitedEnumArrayQuery()
        {
            var queryParamName = "test";
            IReadOnlyList<TestType> queryParamValues = new[] { TestType.How, TestType.Much };
            var queryParamString = "How|Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());
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
        public async Task BindModelAsync_CorrectlyBindsValidPipeDelimitedEnumArrayQueryWithDoublePipes()
        {
            var queryParamName = "test";
            IReadOnlyList<TestType> queryParamValues = new[] { TestType.How, TestType.Much };
            var queryParamString = "How||Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());
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

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());

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

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());

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
            var queryParamString = "🔥|😢";
            var queryParamType = typeof(IReadOnlyList<TestType>);

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());
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

            var modelBinder = new PipeDelimitedCollectionModelBinder(new NullLogger<PipeDelimitedCollectionModelBinder>());

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
    }
}
