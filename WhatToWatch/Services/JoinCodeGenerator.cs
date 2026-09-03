using Microsoft.Extensions.Options;
using WhatToWatch.Options;

namespace WhatToWatch.Services;

public sealed class JoinCodeGenerator
{
    private readonly int _codeLength;
    private readonly int _codeSpaceSize;
    private readonly Random _random;
    private readonly object _randomLock = new();

    public JoinCodeGenerator(IOptions<PartyOptions> options)
        : this(
            options?.Value ?? throw new ArgumentNullException(nameof(options)),
            Random.Shared)
    {
    }

    public JoinCodeGenerator(PartyOptions options, Random random)
        : this(
            random,
            options?.JoinCodeLength ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    public JoinCodeGenerator(Random random, int codeLength = 4)
    {
        ArgumentNullException.ThrowIfNull(random);

        if (codeLength is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(codeLength),
                codeLength,
                "Join-code length must be between 1 and 9.");
        }

        _random = random;
        _codeLength = codeLength;
        _codeSpaceSize = (int)Math.Pow(10, codeLength);
    }

    public string Next(IReadOnlySet<string> takenCodes)
    {
        ArgumentNullException.ThrowIfNull(takenCodes);

        var availableCount = 0;
        for (var value = 0; value < _codeSpaceSize; value++)
        {
            if (!takenCodes.Contains(Format(value)))
                availableCount++;
        }

        if (availableCount == 0)
            throw new NoCodesAvailableException();

        int selectedAvailableIndex;
        lock (_randomLock)
        {
            selectedAvailableIndex = _random.Next(availableCount);
        }

        for (var value = 0; value < _codeSpaceSize; value++)
        {
            var code = Format(value);
            if (takenCodes.Contains(code))
                continue;

            if (selectedAvailableIndex-- == 0)
                return code;
        }

        throw new NoCodesAvailableException();
    }

    private string Format(int value) => value.ToString($"D{_codeLength}");
}

public sealed class NoCodesAvailableException()
    : InvalidOperationException("NoCodesAvailable");
