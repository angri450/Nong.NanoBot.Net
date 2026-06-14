using Nanobot.Web;

namespace Nanobot.Tests;

public class SystemStatusProbeTests
{
    [Fact]
    public void ParseExternalToolStatuses_UsesVersionColumnFromDotnetToolList()
    {
        const string output = """
            Package Id                      Version      Commands
            -----------------------------------------------------
            Angri450.Nong.Tool.Chart        4.0.1        nong-chart
            Angri450.Nong.Tool.Ocr          4.2.0        nong-ocr
            """;

        var statuses = SystemStatusProbe.ParseExternalToolStatuses(output);

        var chart = Assert.Single(statuses, item => item.PackageId == "Angri450.Nong.Tool.Chart");
        var ocr = Assert.Single(statuses, item => item.PackageId == "Angri450.Nong.Tool.Ocr");
        var pdf = Assert.Single(statuses, item => item.PackageId == "Angri450.Nong.Tool.Pdf");

        Assert.True(chart.Installed);
        Assert.Equal("4.0.1", chart.Version);
        Assert.True(ocr.Installed);
        Assert.Equal("4.2.0", ocr.Version);
        Assert.False(pdf.Installed);
        Assert.Null(pdf.Version);
    }

    [Fact]
    public void ParseNongStatus_IgnoresNullEntriesAndDeduplicatesRoots()
    {
        const string json = """
            {
              "status": "ok",
              "meta": { "version": "4.1.0" },
              "data": [
                { "name": "pdf check", "group": "pdf" },
                null,
                { "name": "word check", "group": "word" },
                { "name": "pdf split", "group": "PDF" }
              ]
            }
            """;

        var status = SystemStatusProbe.ParseNongStatus(json);

        Assert.NotNull(status);
        Assert.Equal("4.1.0", status!.Version);
        Assert.Equal(3, status.CommandCount);
        Assert.Equal(new[] { "pdf", "word" }, status.AvailableRoots);
    }

    [Fact]
    public void ParseOcrModelStatus_HandlesSparseModelArrays()
    {
        const string json = """
            {
              "status": "ok",
              "data": {
                "models": [
                  null,
                  {
                    "id": "pp-ocrv6-server",
                    "available": true,
                    "modelSize": "1.2 GB",
                    "modelCachePath": "C:/models/v6"
                  },
                  {
                    "id": "pp-ocrv5-mobile",
                    "available": true
                  }
                ]
              }
            }
            """;

        var status = SystemStatusProbe.ParseOcrModelStatus(json);

        Assert.NotNull(status);
        Assert.True(status!.V6Available);
        Assert.Equal("1.2 GB", status.V6Size);
        Assert.Equal("C:/models/v6", status.V6CachePath);
        Assert.True(status.V5Available);
    }
}
