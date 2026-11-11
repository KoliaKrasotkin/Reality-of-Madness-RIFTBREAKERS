using System;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolNaveJardinesPreview
    {
        static float[] BuildWarriorV3Score(bool v4 = false)
        {
            var score = new float[(v4 ? 78 : TrailerV3Seconds) * FilmAudioRate * 2];
            var random = new Random(9317);
            int[] motif = { 62, 65, 69, 67, 65, 62, 60, 57 };
            int[] bass = { 50, 46, 48, 45, 50, 46, 43, 45 };
            double[] phrasing = { 0, 1.55, 2.42, 3.75, 4.80, 6.02, 7.10, 8.20 };
            double[] lengths = { 1.42, .69, 1.12, .92, .95, .92, .76, 1.40 };
            for (int i = 0; i < motif.Length; i++)
                Voice((v4 ? .60 : .24) + phrasing[i], lengths[i], motif[i], 0, v4 ? .17 : .27, -.22);

            const double beat = 60.0 / 100;
            for (int bar = 0; bar < (v4 ? 31 : 25); bar++)
            {
                double t = bar * beat * 4;
                int fundamental = bass[bar % bass.Length];
                if (t < 9.6)
                {
                    Voice(t + .06, 1.45, 36, 3, v4 ? .53 : .18, -.12);
                    if (v4)
                    {
                        Voice(t + .91, 1.15, 38, 3, .29, .23);
                        Voice(t + 1.8, 1.10, 36, 3, .39, -.12);
                        Voice(t + .08, 2.25, fundamental, 2, .12, .31);
                    }
                    continue;
                }

                double intensity = v4 ? t < 30 ? .55 : t < 48 ? .69 + .24 * (t - 30) / 18 :
                    t < 55 ? .34 : t < 72 ? 1 : .65 : t < 21.6 ? .48 : t < 36 ? .77 : t < 50.4 ? 1 : .79;
                Voice(t, 2.7, fundamental, 2, .17 * intensity, -.46);
                Voice(t + .04, 2.62, fundamental + 7, 2, .12 * intensity, .46);
                Voice(t + .09, 2.57, fundamental + 12, 2, .09 * intensity, .10);
                for (int i = 0; i < 7; i++)
                {
                    if (i == 3 && bar % 2 == 0) continue;
                    int note = motif[(i + bar * 2) % motif.Length] - (bar % 4 == 3 ? 12 : 0);
                    Voice(t + i * beat * .5, .62, note, 1, intensity * (i % 2 == 0 ? .20 : .11), .24);
                }
                Voice(t, 1.35, 36, 3, intensity * .43, -.18);
                Voice(t + beat * 1.5, 1.25, 38, 3, intensity * .31, .22);
                if (v4 ? t >= 30 && (t < 48 || t >= 55 && t < 72) : t >= 21.6 && t < 55)
                {
                    Voice(t + beat * 2.5, 1.2, 36, 3, intensity * .43, .08);
                    Voice(t + beat * 3.27, .67, 48, 3, intensity * .25, -.32);
                    Voice(t + beat * 3.62, .65, 47, 3, intensity * .22, .35);
                }
                if (bar % 4 == 0 && t < (v4 ? 72 : 56))
                {
                    Voice(t + .04, 1.31, motif[(bar / 4) % motif.Length], 0, .19, -.25);
                    Voice(t + 1.35, .91, motif[(bar / 4 + 2) % motif.Length], 0, .17, -.18);
                }
            }

            foreach (double start in v4 ? Array.Empty<double>() : new[] { 22.0, 36.0 })
            {
                Voice(start + 5.50, .32, 72, 4, .30, -.15);
                Voice(start + 8.35, .46, 77, 4, .39, .18);
                Voice(start + 11.25, .44, 66, 4, .37, -.10);
                Voice(start + 11.25, 1.35, 34, 3, .53, .03);
            }
            if (v4)
                foreach (double hit in new[] { 59.65, 62.65, 66.1, 69.25 })
                {
                    Voice(hit, .36, hit < 60 ? 77 : 67, 4, .41, -.13);
                    Voice(hit, 1.27, 34, 3, .52, .03);
                }
            float title = v4 ? 75 : TitleV3Time;
            Voice(title, 2.65, 32, 3, .93, 0);
            Voice(title + .012, 2.52, 50, 2, .32, -.33);
            Voice(title + .016, 2.48, 57, 2, .29, .33);
            Voice(title + .021, 2.48, 62, 2, .27, 0);
            Voice(title + .06, 1.87, 74, 0, .22, -.21);

            float peak = 0;
            foreach (float sample in score) peak = Math.Max(peak, Math.Abs(sample));
            float normalization = .91f / Math.Max(.91f, peak);
            for (int i = 0; i < score.Length; i += 2)
            {
                float fade = (float)Math.Min(1, (score.Length - i) / (FilmAudioRate * 2.4));
                score[i] *= normalization * fade;
                score[i + 1] *= normalization * fade;
            }
            return score;

            void Voice(double when, double length, int midi, int instrument, double gain, double pan)
            {
                int start = (int)(when * FilmAudioRate);
                int samples = Math.Min((int)(length * FilmAudioRate), score.Length / 2 - start);
                double frequency = 440 * Math.Pow(2, (midi - 69) / 12.0);
                double phase = 0;
                double air = 0;
                for (int n = 0; n < samples; n++)
                {
                    double t = n / (double)FilmAudioRate;
                    double noise = random.NextDouble() * 2 - 1;
                    air += .17 * (noise - air);
                    double envelope = Math.Min(1, Math.Max(0, length - t) / .13);
                    double value;
                    switch (instrument)
                    {
                        case 0:
                            phase += 2 * Math.PI * frequency / FilmAudioRate *
                                (1 - .034 * Math.Exp(-t * 21) + .0054 * Math.Sin(2 * Math.PI * 4.7 * t));
                            value = (Math.Sin(phase) + .25 * Math.Sin(phase * 2) + .09 * Math.Sin(phase * 3) +
                                (v4 ? .045 : .44) * air + (v4 ? .004 : .065) * noise) *
                                Math.Min(1, t * (v4 ? 5.5 : 8)) * (.87 + .13 * Math.Sin(t * 17));
                            break;
                        case 1:
                            phase += 2 * Math.PI * frequency / FilmAudioRate * (1 + .013 * Math.Exp(-t * 25));
                            value = Math.Sin(phase) * Math.Exp(-t * 5.8) +
                                .36 * Math.Sin(phase * 2.006) * Math.Exp(-t * 12) +
                                .27 * Math.Sin(phase * 3.013 + .4) * Math.Exp(-t * 18) +
                                (.14 * noise + .10 * Math.Sin(phase * 7.02)) * Math.Exp(-t * 90);
                            break;
                        case 2:
                            phase += 2 * Math.PI * frequency / FilmAudioRate * (1 + .004 * Math.Sin(t * 33 + midi));
                            double ensemble = .45 * Math.Sin(phase * 1.0021) + .35 * Math.Sin(phase * .9978 + .8) +
                                .18 * Math.Sin(phase * 2) + .115 * Math.Sin(phase * 3.003) + .069 * Math.Sin(phase * 4.011);
                            value = (ensemble + .015 * air) * Math.Min(1, t * 3) * (.83 + .17 * Math.Sin(t * 4.6));
                            break;
                        case 3:
                            phase += 2 * Math.PI * (frequency * .64 + 37 * Math.Exp(-t * 27)) / FilmAudioRate;
                            value = .88 * Math.Sin(phase) * Math.Exp(-t * 4.6) +
                                .29 * Math.Sin(phase * 1.57) * Math.Exp(-t * 12) +
                                .20 * Math.Sin(phase * 2.18) * Math.Exp(-t * 19) +
                                (air * .9 + .13 * noise) * Math.Exp(-t * 36);
                            break;
                        default:
                            phase += 2 * Math.PI * frequency / FilmAudioRate;
                            value = (noise * .44 + .25 * Math.Sin(phase * 3.83) + .23 * Math.Sin(phase * 5.27)) *
                                Math.Exp(-t * 13) + .22 * Math.Sin(phase * .59) * Math.Exp(-t * 24);
                            break;
                    }
                    float sample = (float)(gain * envelope * value * Math.Min(1, t * 650));
                    int index = (start + n) * 2;
                    score[index] += sample * (float)(.75 - .26 * pan);
                    score[index + 1] += sample * (float)(.75 + .26 * pan);
                    int echo = index + 7872;
                    if (echo + 1 < score.Length)
                    {
                        score[echo] += sample * .062f;
                        score[echo + 1] += sample * .078f;
                    }
                }
            }
        }
    }
}

