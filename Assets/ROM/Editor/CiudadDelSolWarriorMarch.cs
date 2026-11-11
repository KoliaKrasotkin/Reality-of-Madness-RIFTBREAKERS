using System;
using System.IO;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        static float[] BuildWarriorMarch()
        {
            const double beat = 60.0 / 90;
            var output = new float[WarriorTrailerSeconds * FilmAudioRate * 2];
            var rng = new Random(239);
            for (int bar = 0; bar < 15; bar++)
            {
                double time = bar * 4 * beat;
                float gain = time < 8 ? .78f : time < 26 ? .91f : 1.03f;
                int pattern = bar % 4;
                if (time < 8 || pattern == 0 || pattern == 1)
                {
                    Drum(time + .015, 0, .79f * gain, -.18f);
                    Drum(time + beat * 1.01, 0, .59f * gain, .14f);
                    if (time >= 8)
                    {
                        Drum(time + beat * 2, 1, .39f * gain, .35f);
                        Drum(time + beat * 3.02, 1, .27f * gain, -.37f);
                    }
                }
                else if (pattern == 2)
                {
                    for (int i = 0; i < 3; i++)
                        Drum(time + beat * i, 1, (.48f - .05f * i) * gain, i % 2 == 0 ? .29f : -.29f);
                    Drum(time + beat * 3, 0, .74f * gain, 0);
                }
                else
                {
                    for (int i = 0; i < 4; i++) Drum(time + beat * (1.5 + i * .25), 1, (.26f + .035f * i) * gain, i % 2 == 0 ? .3f : -.3f);
                    Drum(time + beat * 2.53, 0, .86f * gain, 0);
                    Drum(time + beat * 3.28, 0, .54f * gain, .19f);
                }
                if (time >= 17 && time < 34)
                    for (int i = 0; i < 4; i++) Drum(time + beat * (3 + i * .22), 1, .15f + .038f * i, i % 2 == 0 ? .4f : -.36f);
            }
            for (int i = 0; i < 16; i++)
                Drum(34 + i * beat / 3, 1, .17f + .025f * i, i % 2 == 0 ? -.42f : .42f);
            Drum(37.63, 0, 1.30f, 0, 1.26);
            Drum(37.645, 1, .64f, .31f, 1.45);
            float peak = 0;
            foreach (float sample in output) peak = Math.Max(peak, Math.Abs(sample));
            float gainOut = .91f / Math.Max(.91f, peak);
            for (int i = 0; i < output.Length / 2; i++)
            {
                float fade = (float)Math.Min(1, (output.Length / 2 - i) / (FilmAudioRate * .72));
                output[i * 2] *= gainOut * fade;
                output[i * 2 + 1] *= gainOut * fade;
            }
            return output;

            void Drum(double onset, int voice, float velocity, float pan, double stretch = 1)
            {
                int start = (int)(onset * FilmAudioRate);
                int duration = (int)((voice == 0 ? 1.8 : .68) * stretch * FilmAudioRate);
                double detune = .97 + rng.NextDouble() * .06;
                double low = 0, membrane = 0;
                for (int n = 0; n < duration && start + n < output.Length / 2; n++)
                {
                    double t = n / (double)FilmAudioRate;
                    double phase = 2 * Math.PI * (voice == 0 ? 47 : 138) * detune * t;
                    double noise = rng.NextDouble() * 2 - 1;
                    low += (voice == 0 ? .041 : .29) * (noise - low);
                    membrane += .19 * (low - membrane);
                    double strike = Math.Min(1, t * 750) * Math.Exp(-t * (voice == 0 ? 14 : 33));
                    double skin = Math.Sin(phase + .58 * (1 - Math.Exp(-t * 30))) * Math.Exp(-t * (voice == 0 ? 4.3 : 17) / stretch);
                    double overtones = Math.Sin(phase * 1.58 + .15) * .34 * Math.Exp(-t * 13)
                        + Math.Sin(phase * 2.14 - .3) * .19 * Math.Exp(-t * 18);
                    double signal = Math.Min(1, t * 460) * (skin * .63 + overtones) +
                        (voice == 0 ? (low * .42 + membrane * .25) : (noise * .16 + low * .45)) * strike;
                    float value = (float)signal * velocity;
                    int index = (start + n) * 2;
                    output[index] += value * (.73f - pan * .30f);
                    output[index + 1] += value * (.73f + pan * .30f);
                    int roomA = index + 2976, roomB = index + 4848;
                    if (roomB + 1 < output.Length)
                    {
                        output[roomA + 1] += value * .085f;
                        output[roomB] += value * .071f;
                    }
                }
            }
        }

        static void SaveWarriorMarch(float[] samples, string path)
        {
            using var writer = new BinaryWriter(File.Create(path));
            int bytes = samples.Length * 2;
            writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            writer.Write(36 + bytes);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)2);
            writer.Write(FilmAudioRate);
            writer.Write(FilmAudioRate * 4);
            writer.Write((short)4);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(bytes);
            foreach (float value in samples) writer.Write((short)Math.Round(value * 32767));
        }
    }
}
