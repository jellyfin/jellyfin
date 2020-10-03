using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.ModelBinders;
using MediaBrowser.Controller.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Jellyfin.Api.Tests.ModelBinders
{
    public sealed class CommaDelimitedArrayModelBinderTests
    {
        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedStringArrayQuery()
        {
            var queryParamName = "test";
            var queryParamValues = new string[] { "lol", "xd" };
            var queryParamString = "lol,xd";
            var queryParamType = typeof(string[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>() { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((string[])bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedIntArrayQuery()
        {
            var queryParamName = "test";
            var queryParamValues = new int[] { 42, 0 };
            var queryParamString = "42,0";
            var queryParamType = typeof(int[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>() { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((int[])bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedEnumArrayQuery()
        {
            var queryParamName = "test";
            var queryParamValues = new TestType[] { TestType.How, TestType.Much };
            var queryParamString = "How,Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>() { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((TestType[])bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidCommaDelimitedEnumArrayQueryWithDoubleCommas()
        {
            var queryParamName = "test";
            var queryParamValues = new TestType[] { TestType.How, TestType.Much };
            var queryParamString = "How,,Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>() { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((TestType[])bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsValidEnumArrayQuery()
        {
            var queryParamName = "test";
            var queryParamValues = new TestType[] { TestType.How, TestType.Much };
            var queryParamString1 = "How";
            var queryParamString2 = "Much";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();

            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>()
                    {
                        { queryParamName, new StringValues(new string[] { queryParamString1, queryParamString2 }) },
                    }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            await modelBinder.BindModelAsync(bindingContextMock.Object);

            Assert.True(bindingContextMock.Object.Result.IsModelSet);
            Assert.Equal((TestType[])bindingContextMock.Object.Result.Model, queryParamValues);
        }

        [Fact]
        public async Task BindModelAsync_CorrectlyBindsEmptyEnumArrayQuery()
        {
            var queryParamName = "test";
            var queryParamValues = Array.Empty<TestType>();
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();

            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>()
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

            Assert.False(bindingContextMock.Object.Result.IsModelSet);
        }

        [Fact]
        public async Task BindModelAsync_ThrowsIfCommaDelimitedEnumArrayQueryIsInvalid()
        {
            var queryParamName = "test";
            var queryParamString = "🔥,😢";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();
            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>() { { queryParamName, new StringValues(queryParamString) } }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            Func<Task> act = async () => await modelBinder.BindModelAsync(bindingContextMock.Object);

            await Assert.ThrowsAsync<FormatException>(act);
        }

        [Fact]
        public async Task BindModelAsync_ThrowsIfCommaDelimitedEnumArrayQueryIsInvalid2()
        {
            var queryParamName = "test";
            var queryParamValues = new TestType[] { TestType.How, TestType.Much };
            var queryParamString1 = "How";
            var queryParamString2 = "😱";
            var queryParamType = typeof(TestType[]);

            var modelBinder = new CommaDelimitedArrayModelBinder();

            var valueProvider = new QueryStringValueProvider(
                    new BindingSource(string.Empty, string.Empty, false, false),
                    new QueryCollection(new Dictionary<string, StringValues>()
                    {
                        { queryParamName, new StringValues(new string[] { queryParamString1, queryParamString2 }) },
                    }),
                    CultureInfo.InvariantCulture);
            var bindingContextMock = new Mock<ModelBindingContext>();
            bindingContextMock.Setup(b => b.ValueProvider).Returns(valueProvider);
            bindingContextMock.Setup(b => b.ModelName).Returns(queryParamName);
            bindingContextMock.Setup(b => b.ModelType).Returns(queryParamType);
            bindingContextMock.SetupProperty(b => b.Result);

            Func<Task> act = async () => await modelBinder.BindModelAsync(bindingContextMock.Object);

            await Assert.ThrowsAsync<FormatException>(act);
        }
    }
}
