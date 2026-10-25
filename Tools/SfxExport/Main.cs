using System.IO;

namespace BoomYard.Game
{
    static class SfxExportMain
    {
        static void Main(string[] args)
        {
            var gen = new SfxGen();
            gen.Build();
            var all = gen.All();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            all["carAlarm"] = StreetSounds.CarAlarm;
            all["whistle"] = StreetSounds.Whistle;
            all["rain"] = StreetSounds.Rain;
            for (int i = 0; i < StreetSounds.Bark.Length; i++) all["bark" + i] = StreetSounds.Bark[i];
            double streetMs = sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            all["spray"] = SfxGen.SprayClip();
            all["rattle"] = SfxGen.RattleClip();
            double lazyMs = sw.Elapsed.TotalMilliseconds;
            System.Console.WriteLine("time: Sfx startup " + gen.awakeMs.ToString("0") + " ms, music " + gen.musicMs.ToString("0") + " ms, street " + streetMs.ToString("0") + " ms, spray+rattle " + lazyMs.ToString("0") + " ms");
            foreach (var kv in all)
            {
                var path = Path.Combine(args[0], kv.Key + ".wav");
                Write(path, kv.Value.data, kv.Value.frequency, kv.Value.channels);
                System.Console.WriteLine(kv.Key + "\t" + kv.Value.length.ToString("0.00") + " s\t" + kv.Value.frequency + " Hz\t" + kv.Value.channels + " ch");
            }
        }

        /// <summary>16-bit PCM WAV; <paramref name="d"/> holds interleaved frames of <paramref name="ch"/> channels.</summary>
        static void Write(string path, float[] d, int rate, int ch)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write("RIFF".ToCharArray()); w.Write(36 + d.Length * 2); w.Write("WAVEfmt ".ToCharArray());
                w.Write(16); w.Write((short)1); w.Write((short)ch); w.Write(rate); w.Write(rate * 2 * ch); w.Write((short)(2 * ch)); w.Write((short)16);
                w.Write("data".ToCharArray()); w.Write(d.Length * 2);
                foreach (var v in d) w.Write((short)System.Math.Round(System.Math.Max(-1f, System.Math.Min(1f, v)) * 32767f));
            }
        }
    }
}
