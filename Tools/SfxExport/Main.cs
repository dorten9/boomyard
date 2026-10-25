using System.IO;

namespace BoomYard.Game
{
    static class SfxExportMain
    {
        static void Main(string[] args)
        {
            var gen = new SfxGen();
            gen.Build();
            foreach (var kv in gen.All())
            {
                var path = Path.Combine(args[0], kv.Key + ".wav");
                Write(path, kv.Value.data, kv.Value.frequency);
                System.Console.WriteLine(kv.Key + "\t" + kv.Value.length.ToString("0.00") + " s\t" + kv.Value.frequency + " Hz");
            }
        }

        /// <summary>16-bit mono PCM WAV.</summary>
        static void Write(string path, float[] d, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write("RIFF".ToCharArray()); w.Write(36 + d.Length * 2); w.Write("WAVEfmt ".ToCharArray());
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write("data".ToCharArray()); w.Write(d.Length * 2);
                foreach (var v in d) w.Write((short)System.Math.Round(System.Math.Max(-1f, System.Math.Min(1f, v)) * 32767f));
            }
        }
    }
}
