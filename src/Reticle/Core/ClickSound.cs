using System.IO;
using System.Media;
using Reticle.Engine;

namespace Reticle.Core;

/// <summary>A short, soft synthesized tick played when a dwell clicks (if sound is enabled).</summary>
internal sealed class ClickSound(AppSettings settings) : IClickSound
{
    private readonly SoundPlayer _player = new(new MemoryStream(CreateTick()));

    public void Play()
    {
        if (settings.SoundEnabled)
        {
            _player.Play();
        }
    }

    private static byte[] CreateTick()
    {
        const int sampleRate = 44100;
        const double seconds = 0.07;
        int samples = (int)(sampleRate * seconds);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        int dataBytes = samples * 2;
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / sampleRate;
            double attack = Math.Min(1, t / 0.002);
            double envelope = attack * Math.Exp(-t * 60);
            double tone = 0.7 * Math.Sin(2 * Math.PI * 1320 * t) + 0.3 * Math.Sin(2 * Math.PI * 2640 * t);
            writer.Write((short)(tone * envelope * 0.45 * short.MaxValue));
        }
        writer.Flush();
        return stream.ToArray();
    }
}
