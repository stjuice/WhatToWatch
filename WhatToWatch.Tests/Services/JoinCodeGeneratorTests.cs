using WhatToWatch.Services;

namespace WhatToWatch.Tests.Services;

public class JoinCodeGeneratorTests
{
    [Fact]
    public void Next_ReturnsFourDigitZeroPaddedCode()
    {
        var generator = new JoinCodeGenerator(new FixedRandom(42));

        var code = generator.Next(new HashSet<string>());

        Assert.Equal("0042", code);
    }

    [Fact]
    public void Next_NeverReturnsTakenCode()
    {
        var takenCodes = Enumerable.Range(0, 10_000)
            .Select(value => value.ToString("D4"))
            .Where(code => code != "0042")
            .ToHashSet();
        var generator = new JoinCodeGenerator(new Random(42));

        var code = generator.Next(takenCodes);

        Assert.Equal("0042", code);
        Assert.DoesNotContain(code, takenCodes);
    }

    [Fact]
    public void Next_ThrowsNoCodesAvailable_WhenCodeSpaceIsFull()
    {
        var takenCodes = Enumerable.Range(0, 10_000)
            .Select(value => value.ToString("D4"))
            .ToHashSet();
        var generator = new JoinCodeGenerator(new Random(42));

        var exception = Assert.Throws<NoCodesAvailableException>(
            () => generator.Next(takenCodes));

        Assert.Equal("NoCodesAvailable", exception.Message);
    }

    [Fact]
    public void Next_IsDeterministicForSeededRandom()
    {
        var first = new JoinCodeGenerator(new Random(42));
        var second = new JoinCodeGenerator(new Random(42));
        IReadOnlySet<string> takenCodes = new HashSet<string>
        {
            "1409",
            "1684",
            "5227",
        };

        var firstSequence = Enumerable.Range(0, 5)
            .Select(_ => first.Next(takenCodes))
            .ToArray();
        var secondSequence = Enumerable.Range(0, 5)
            .Select(_ => second.Next(takenCodes))
            .ToArray();

        Assert.Equal(firstSequence, secondSequence);
    }

    private sealed class FixedRandom(int value) : Random
    {
        public override int Next(int maxValue) => value % maxValue;
    }
}
