using System.IO;
using Xunit;

namespace Jellyfin.Drawing.Skia.Tests;

public static class SvgSecurityValidatorTests
{
    public static TheoryData<string> ExternalReferenceSvgs => new()
    {
        // SSRF via <image> (xlink:href and plain href)
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='http://169.254.169.254/latest/meta-data/' width='16' height='16'/></svg>",
        "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><image href='https://example.invalid/a.png' width='16' height='16'/></svg>",
        // Local file disclosure
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='file:///etc/passwd' width='16' height='16'/></svg>",
        // Memory exhaustion DoS
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='file:///dev/urandom' width='16' height='16'/></svg>",
        // <use> external reference
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><use xlink:href='http://example.invalid/c.svg#a'/></svg>",
        // CSS url() external reference in an attribute
        "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><rect width='16' height='16' style=\"fill:url(http://example.invalid/d.svg#g)\"/></svg>",
        // @import in a style block
        "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><style>@import 'http://example.invalid/e.css';</style><rect width='16' height='16'/></svg>",
        // Relative path traversal (resolves against the document location -> local file read)
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='../../../../etc/hosts' width='16' height='16'/></svg>",
        // XXE via external entity
        "<?xml version='1.0'?><!DOCTYPE svg [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><text>&xxe;</text></svg>",
        // Entity-expansion (billion laughs) denial of service
        "<?xml version='1.0'?><!DOCTYPE svg [<!ENTITY a 'aaaaaaaaaa'><!ENTITY b '&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;'><!ENTITY c '&b;&b;&b;&b;&b;&b;&b;&b;&b;&b;'><!ENTITY d '&c;&c;&c;&c;&c;&c;&c;&c;&c;&c;'><!ENTITY e '&d;&d;&d;&d;&d;&d;&d;&d;&d;&d;'><!ENTITY f '&e;&e;&e;&e;&e;&e;&e;&e;&e;&e;'>]><svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><text>&f;</text></svg>",
        // Nested SVG in a base64 data: URI whose inner document references an external resource
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHhtbG5zOnhsaW5rPSdodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rJyB3aWR0aD0nOCcgaGVpZ2h0PSc4Jz48aW1hZ2UgeGxpbms6aHJlZj0naHR0cDovL2V4YW1wbGUuaW52YWxpZC9uZXN0ZWQucG5nJyB3aWR0aD0nOCcgaGVpZ2h0PSc4Jy8+PC9zdmc+' width='16' height='16'/></svg>",
        // Nested SVG in a URL-encoded (non-base64) data: URI referencing an external resource
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='data:image/svg+xml,%3Csvg%20xmlns%3D%27http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%27%20xmlns%3Axlink%3D%27http%3A%2F%2Fwww.w3.org%2F1999%2Fxlink%27%3E%3Cimage%20xlink%3Ahref%3D%27file%3A%2F%2F%2Fetc%2Fpasswd%27%2F%3E%3C%2Fsvg%3E' width='16' height='16'/></svg>",
        // Nested gzip-compressed (svgz) data: URI whose inner document references an external resource
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='data:image/svg+xml;base64,H4sIAAAAAAAC/23OwQrDIBAE0F/x5s217aWK8V+E2N2laiWRKP36Nin0lNvAPIZx64Zi5FTWSVJr1QL03lW/qdeCcNVaw1fIH7EjcXmewYsxBo5Wis5zo0nepaDISG2P3nEOGMVBLC3x8V+JI+SaouKyhcQz4FvVgucz4N1+x38AdK4P3LYAAAA=' width='16' height='16'/></svg>",
    };

    public static TheoryData<string> SafeSvgs => new()
    {
        "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><rect width='16' height='16' fill='red'/></svg>",
        // Same-document fragment references are allowed
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><defs><linearGradient id='g'/></defs><rect width='16' height='16' fill='url(#g)'/><use xlink:href='#g'/></svg>",
        // Inline data URIs are allowed
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==' width='16' height='16'/></svg>",
        // A DOCTYPE without external entities is allowed
        "<?xml version='1.0'?><!DOCTYPE svg PUBLIC '-//W3C//DTD SVG 1.1//EN' 'http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd'><svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><rect width='16' height='16'/></svg>",
        // An internal general entity with no external reference is allowed (and is expanded by the renderer)
        "<?xml version='1.0'?><!DOCTYPE svg [<!ENTITY col 'red'>]><svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><rect width='16' height='16' fill='&col;'/></svg>",
        // A nested data:image/svg+xml payload that is itself self-contained is allowed
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPSc4JyBoZWlnaHQ9JzgnPjxyZWN0IHdpZHRoPSc4JyBoZWlnaHQ9JzgnIGZpbGw9J2JsdWUnLz48L3N2Zz4=' width='16' height='16'/></svg>",
        // A self-contained gzip-compressed (svgz) data: URI is allowed
        "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='16' height='16'><image xlink:href='data:image/svg+xml;base64,H4sIAAAAAAAC/22Muw6AIAwAf6VbN0p0MQb4GBWBBB+Bav18ZXe75C5n6h3g2fJeLUbmcyQSESW9OkqgTmtNX4EgaeFocUCIPoXIDZ0pfuZfBWvK2eKUL4/kTHu4F2NB6oFrAAAA' width='16' height='16'/></svg>",
    };

    [Theory]
    [MemberData(nameof(ExternalReferenceSvgs))]
    public static void IsSafe_ExternalReference_ReturnsFalse(string svg)
    {
        var path = WriteTemp(svg);
        try
        {
            Assert.False(SvgSecurityValidator.IsSafe(path, out var reason));
            Assert.NotNull(reason);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [MemberData(nameof(SafeSvgs))]
    public static void IsSafe_NoExternalReference_ReturnsTrue(string svg)
    {
        var path = WriteTemp(svg);
        try
        {
            Assert.True(SvgSecurityValidator.IsSafe(path, out var reason));
            Assert.Null(reason);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public static void IsSafe_MissingFile_ReturnsFalse()
    {
        Assert.False(SvgSecurityValidator.IsSafe(Path.Combine(Path.GetTempPath(), "does-not-exist-" + Path.GetRandomFileName() + ".svg"), out var reason));
        Assert.NotNull(reason);
    }

    private static string WriteTemp(string svg)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".svg");
        File.WriteAllText(path, svg);
        return path;
    }
}
