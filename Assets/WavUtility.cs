using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    const int HEADER_SIZE = 44;

    public static byte[] FromAudioClip(AudioClip clip, out int length, bool trimSilence = false, float silenceThreshold = 0.01f)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        if (trimSilence)
        {
            samples = TrimSilence(samples, silenceThreshold);
        }

        byte[] wav = ConvertToWav(samples, clip.channels, clip.frequency);
        length = wav.Length;
        return wav;
    }

    private static byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        MemoryStream stream = new MemoryStream();

        int length = samples.Length * 2 + HEADER_SIZE;

        // WAV header
        stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(length - 8), 0, 4);
        stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        stream.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4); // Subchunk1Size
        stream.Write(BitConverter.GetBytes((ushort)1), 0, 2); // PCM format
        stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
        stream.Write(BitConverter.GetBytes(sampleRate * channels * 2), 0, 4); // byte rate
        stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2); // block align
        stream.Write(BitConverter.GetBytes((ushort)16), 0, 2); // bits per sample

        // data subchunk
        stream.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(samples.Length * 2), 0, 4);

        // PCM data
        foreach (var sample in samples)
        {
            short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
            stream.Write(BitConverter.GetBytes(intSample), 0, 2);
        }

        return stream.ToArray();
    }

    private static float[] TrimSilence(float[] samples, float threshold)
    {
        int start = 0;
        int end = samples.Length - 1;

        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > threshold)
            {
                start = i;
                break;
            }
        }

        for (int i = samples.Length - 1; i >= 0; i--)
        {
            if (Mathf.Abs(samples[i]) > threshold)
            {
                end = i;
                break;
            }
        }

        int newLength = end - start + 1;
        float[] trimmed = new float[newLength];
        Array.Copy(samples, start, trimmed, 0, newLength);
        return trimmed;
    }
}