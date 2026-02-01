namespace KaraokePlayer.Presentation;

public sealed record LyricLine(double StartMs, double EndMs, string Text, IReadOnlyList<LyricToken> Tokens);
