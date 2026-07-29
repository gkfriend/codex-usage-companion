using System.Text.Json;
using CodexUsageCompanion.RateLimits;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class RateLimitParserTests
{
    [Fact]
    public void ParseResponsePrefersCodexBucketAndClassifiesWindowsByDuration()
    {
        const string json = """
        {
          "id": 2,
          "result": {
            "rateLimits": {
              "primary": {
                "usedPercent": 99,
                "windowDurationMins": 10080
              }
            },
            "rateLimitsByLimitId": {
              "codex": {
                "primary": {
                  "usedPercent": 40,
                  "windowDurationMins": 10080,
                  "resetsAt": 1785915471
                },
                "secondary": {
                  "usedPercent": 20,
                  "windowDurationMins": 300,
                  "resetsAt": 1785312914
                }
              },
              "codex_bengalfox": {
                "primary": {
                  "usedPercent": 90,
                  "windowDurationMins": 10080
                }
              }
            },
            "rateLimitResetCredits": {
              "availableCount": 4
            }
          }
        }
        """;

        var state = RateLimitParser.ParseResponse(json);

        Assert.Equal(80, state.FiveHour?.RemainingPercent);
        Assert.Equal(60, state.Weekly?.RemainingPercent);
        Assert.Equal(4, state.AvailableResetCredits);
    }

    [Fact]
    public void ParseResponseConvertsUsedPercentToRemainingPercent()
    {
        const string json = """
        {
          "id": 2,
          "result": {
            "rateLimits": {
              "primary": {
                "usedPercent": 19,
                "windowDurationMins": 300,
                "resetsAt": 1783697624
              },
              "secondary": {
                "usedPercent": 37,
                "windowDurationMins": 10080,
                "resetsAt": 1784247424
              }
            },
            "rateLimitResetCredits": {
              "availableCount": 2
            }
          }
        }
        """;

        var state = RateLimitParser.ParseResponse(json);

        Assert.Equal(81, state.FiveHour?.RemainingPercent);
        Assert.Equal(300, state.FiveHour?.WindowDurationMins);
        Assert.Equal(63, state.Weekly?.RemainingPercent);
        Assert.Equal(10080, state.Weekly?.WindowDurationMins);
        Assert.Equal(2, state.AvailableResetCredits);
    }

    [Fact]
    public void ParseResponseAllowsMissingSecondaryWindow()
    {
        const string json = """
        {
          "result": {
            "rateLimits": {
              "primary": {
                "usedPercent": 5
              },
              "secondary": null
            }
          }
        }
        """;

        var state = RateLimitParser.ParseResponse(json);

        Assert.Equal(95, state.FiveHour?.RemainingPercent);
        Assert.Null(state.Weekly);
    }

    [Theory]
    [InlineData(-5, 100)]
    [InlineData(120, 0)]
    public void ParseResponseClampsPercentages(int usedPercent, int expectedRemaining)
    {
        var json = $$"""
        {
          "result": {
            "rateLimits": {
              "primary": {
                "usedPercent": {{usedPercent}}
              }
            }
          }
        }
        """;

        var state = RateLimitParser.ParseResponse(json);

        Assert.Equal(expectedRemaining, state.FiveHour?.RemainingPercent);
    }

    [Fact]
    public void ParseResponseRejectsMalformedJson()
    {
        Assert.ThrowsAny<JsonException>(() => RateLimitParser.ParseResponse("{"));
    }
}
