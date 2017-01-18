using System;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using DictionaryType = System.Collections.Concurrent.ConcurrentDictionary<int, int>;

namespace ConcurrentSerialzationTest
{
    internal static class Program
    {
        private const int MaxKey = 10 * 1000;
        private const int InsertionCount = 100 * 1000 * 1000;
        private const int InsertionPercent = InsertionCount / 100;
        private const int SerializerReportPeriod = 1000;

        private static readonly DictionaryType Dictionary = new DictionaryType();
        private static readonly Thread WriterThread = new Thread(WriterThreadMethod);
        private static readonly Thread SerializerThread = new Thread(SerializerThreadMethod);

        private static string _serializationResult = string.Empty;
        private static int _serializedCounted = 0;

        private static void Main(string[] args)
        {
            WriterThread.Start();
            SerializerThread.Start();

            WriterThread.Join();
            SerializerThread.Join();

            var deserialized = JsonConvert.DeserializeObject<DictionaryType>(_serializationResult);
            var speedFraction = (double) InsertionCount / _serializedCounted;
            PrintWithThreadId($"Serialized {_serializedCounted} times while updated {InsertionCount} times ({speedFraction} insertions for 1 serialization)");
            PrintWithThreadId(deserialized.Sum(kv => kv.Value).ToString());
        }

        private static void WriterThreadMethod()
        {
            PrintWithThreadId("Writer started");
            var random = new Random();
            for (var i = 0; i < InsertionCount; ++i)
            {
                var key = random.Next(MaxKey);
                Dictionary.AddOrUpdate(key, 1, (k, v) => v + 1);
                if (i % InsertionPercent == 0)
                {
                    var percent = i / InsertionPercent;
                    PrintWithThreadId($"Writer {percent}%");
                }
            }
            PrintWithThreadId("Writer finished");
        }

        private static void SerializerThreadMethod()
        {
            PrintWithThreadId("Serializer started");
            while (true)
            {
                var serialized = JsonConvert.SerializeObject(Dictionary, Formatting.Indented);
                _serializedCounted++;
                if (_serializedCounted % SerializerReportPeriod == 0)
                {
                    PrintWithThreadId($"Serialized {_serializedCounted} times");
                }
                if (WriterThread.ThreadState == ThreadState.Stopped)
                {
                    PrintWithThreadId("Serializer finished");
                    _serializationResult = JsonConvert.SerializeObject(Dictionary, Formatting.Indented);
                    return;
                }
            }
        }

        private static void PrintWithThreadId(string message)
            => Console.WriteLine($"Thread {Environment.CurrentManagedThreadId}: {message}");
    }
}
