using System;
using System.IO;
using Ada.Bass;
using Ada.Interfaces;

namespace Ada.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Init BaseEngine
            var engine = new BassEngine();
            engine.Init();

            // Create source
            ISource source = engine.CreateSource(new Uri(Path.GetFullPath("A1.wav")));

            // Add source to mixer
            engine.Mixer.AddSource(source, false);

            // Start mixer
            engine.Mixer.Play();

            Console.ReadKey();
        }
    }
}