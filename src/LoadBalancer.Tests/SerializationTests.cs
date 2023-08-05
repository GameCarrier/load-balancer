using LoadBalancer.Common;
using LoadBalancer.Extensions;
using static LoadBalancer.Extensions.Comparison;
using static LoadBalancer.Tests.Keys;

namespace LoadBalancer.Tests
{
    public enum Keys
    {
        ip = 1,
        port,
        @null,
        @bool,
        @char,
        @int,
        @float,
        @double,
        @decimal,
        @string,
        guid,
        timespan,
        datetime,
        bytearray,
        intarray,
        doublearray,
        objarray,
        keyvalue,
        keyvaluearray,
        error,
    }

    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void Serialize_Array_ByteArray()
        {
            // Arrange
            var original = new byte[] { 1, 2, 3 };

            // Act
            var data = Serialization.BinarySerialize(writer => writer.WriteArray(original));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadArray());

            // Assert
            Assert.IsTrue(EqualsSmart(original, copy));
        }

        [TestMethod]
        public void Serialize_Array_IntArray()
        {
            // Arrange
            var original = new int[] { 1, 2, 3 };

            // Act
            var data = Serialization.BinarySerialize(writer => writer.WriteArray(original));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadArray());

            // Assert
            Assert.IsTrue(EqualsSmart(original, copy));
        }

        [TestMethod]
        public void Serialize_Array_KeyValueArray()
        {
            // Arrange
            var original = new KeyValueCollection[]
            {
                new KeyValueCollection  { { ip, "127.0.0.1" }, { port, 1 } },
                new KeyValueCollection  { { ip, "127.0.0.2" }, { port, 2 } },
            };

            // Act
            var data = Serialization.BinarySerialize(writer => writer.WriteArray(original));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadArray());

            // Assert
            Assert.IsTrue(EqualsSmart(original, copy));
        }

        [TestMethod]
        public void Serialize_Array_DynamicArray()
        {
            // Arrange
            var original = new object[]
            {
                null,
                true,
                'c',
                (byte)1,
                2,
                3.14f,
                "str",
                Guid.Empty,
                TimeSpan.FromSeconds(1),
                new DateTime(2000, 01, 01),
                new byte[] { 1, 2, 3 },
                new int[] { 1, 2, 3 },
                new object[] { null, true, 'c', 123 },
                new KeyValueCollection  { { ip, "127.0.0.1" }, { port, 1 } },
                new KeyValueCollection[]
                {
                    new KeyValueCollection  { { ip, "127.0.0.1" }, { port, 1 } },
                    new KeyValueCollection  { { ip, "127.0.0.2" }, { port, 2 } },
                }
            };

            // Act
            var data = Serialization.BinarySerialize(writer => writer.WriteArray(original));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadArray());

            // Assert
            Assert.IsTrue(EqualsSmart(original, copy));
        }

        [TestMethod]
        public void Serialize_KeyValueCollection()
        {
            // Arrange
            var original = new KeyValueCollection
            {
                { @null, null },
                { @bool, true },
                { @char, 'c' },
                { @int, 123 },
                { @float, 123.11f },
                { @double, 123.11d },
                { @decimal, 123.11m },
                { @string, "str" },
                { guid, Guid.Empty },
                { timespan, TimeSpan.FromSeconds(1) },
                { datetime, new DateTime(2000, 01, 01) },
                { bytearray, new byte[] { 1, 2, 3 } },
                { intarray, new int[] { 1, 2, 3 } },
                { objarray, new object[] { null, true, 'c', 123 } },
                { keyvalue, new KeyValueCollection  { { ip, "127.0.0.1" }, { port, 1 } } },
                { keyvaluearray,
                    new KeyValueCollection[]
                    {
                        new KeyValueCollection  { { ip, "127.0.0.1" }, { port, 1 } },
                        new KeyValueCollection  { { ip, "127.0.0.2" }, { port, 2 } },
                    }
                }
            };

            // Act
            var data = Serialization.BinarySerialize(writer => writer.WriteDictionary(original));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadDictionary());

            // Assert
            Assert.IsTrue(EqualsSmart(original, copy));
        }

        [TestMethod]
        public void Serialize_KeyValueCollection_ReadKeyValueReader()
        {
            // Arrange
            var original = new KeyValueCollection
            {
                { @null, null },
                { @bool, true },
                { @char, 'c' },
                { @int, 123 },
                { @float, 123.11f },
                { @double, 123.11d },
                { @decimal, 123.11m },
                { @string, "str" },
                { guid, Guid.Empty },
                { timespan, TimeSpan.FromSeconds(1) },
                { datetime, new DateTime(2000, 01, 01) },
                { bytearray, new byte[] { 1, 2, 3 } },
                { intarray, new int[] { 1, 2, 3 } },
                { objarray, new object[] { null, true, 'c', 123 } },
            };

            // Act
            var data = Serialization.BinarySerialize(writer =>
            {
                writer.WriteDictionary(original);
                writer.Write(999);
            });

            // Assert
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                using (var keyvalue = new KeyValueReader(reader))
                {
                    Assert.IsNull(keyvalue.GetValue<int?>(@null));

                    Assert.AreEqual(123, keyvalue.GetValue<int>(@int));
                    Assert.AreEqual(123, keyvalue.GetValue<long>(@int));
                    Assert.AreEqual(123, keyvalue.GetValue<double>(@int));

                    Assert.AreEqual(true, keyvalue.GetValue<bool>(@bool));
                    Assert.AreEqual('c', keyvalue.GetValue<char>(@char));
                    Assert.AreEqual("str", keyvalue.GetValue<string>(@string));

                    Assert.IsTrue(EqualsSmart(new byte[] { 1, 2, 3 }, keyvalue.GetValue<byte[]>(bytearray)));
                    Assert.IsTrue(EqualsSmart(new int[] { 1, 2, 3 }, keyvalue.GetValue<int[]>(intarray)));
                    Assert.IsTrue(EqualsSmart(new long[] { 1, 2, 3 }, keyvalue.GetValue<long[]>(intarray)));
                }

                int finish = reader.ReadInt32();
                Assert.AreEqual(999, finish);
            }
        }

        [TestMethod]
        public void Reflection_Serialize()
        {
            // Arrange
            var original = new TestComplexResult
            {
                Bool = true,
                Int = 1,
                Float = 1.1f,
                Double = 1.1d,
                String = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                ServiceEndpoint = new Endpoint { Address = "127.0.0.1", Port = 1 },
                Endpoints = new Endpoint[]
                {
                    new Endpoint { Address = "127.0.0.1", Port = 81 },
                    new Endpoint { Address = "127.0.0.2", Port = 82 },
                },
                Map = new KeyValueCollection
                {
                    { @null, null },
                    { @bool, true },
                    { @char, 'c' },
                    { @int, 123 },
                },
                Status = error,
                Message = "message",
            };

            // Act
            var dictionary = original.Serialize();
            // dictionary.SetValue("Number", "asdf");

            var data = Serialization.BinarySerialize(writer => writer.WriteDictionary(dictionary));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadDictionary());
            var restored = copy.Materialize<TestComplexResult>();
            Console.WriteLine($"Payload: {data.Length}");

            // Assert
            Assert.IsTrue(EqualsSmart(original, restored));
        }

        [TestMethod]
        public void Reflection_Serialize_Covariance()
        {
            // Arrange
            var original = new TestComplexResult
            {
                Map = new TestKeyValueMap { Name = "player1", Age = 1 }
            };

            // Act
            var dictionary = original.Serialize();
            var data = Serialization.BinarySerialize(writer => writer.WriteDictionary(dictionary));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadDictionary());
            var restored = copy.Materialize<TestComplexResult>();

            // Assert
            Assert.IsTrue(EqualsSmart(original, restored));
        }

        [TestMethod]
        public void Reflection_Serialize_Compression()
        {
            // Arrange
            var original = new TestComplexResult
            {
                ZipEndpoint = Endpoint.Parse("ws://127.0.0.1:1111/zip").AsCompressed(),
                ZipEndpoints = new[]
                {
                    Endpoint.Parse("ws://127.0.0.1:1111/zip"),
                    Endpoint.Parse("ws://127.0.0.1:2222/zip"),
                }.AsCompressed(),
                ZipMap = new KeyValueCollection
                {
                    { @null, null },
                    { @bool, true },
                    { @char, 'c' },
                    { @int, 123 },
                }.AsCompressed(),
                ZipMapArray = new KeyValueCollection[]
                {
                    new TestKeyValueMap { Name = "player1", Age = 1 },
                    new TestKeyValueMap { Name = "player2", Age = 2 },
                    new TestKeyValueMap { Name = "player3", Age = 3 },
                }.AsCompressed()
            };

            // Act
            var dictionary = original.Serialize();
            var data = Serialization.BinarySerialize(writer => writer.WriteDictionary(dictionary));
            var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadDictionary());
            var restored = copy.Materialize<TestComplexResult>();

            // Assert
            Assert.IsTrue(EqualsSmart(original, restored));
        }

        [TestMethod]
        public void Conversion()
        {
            // Arrange
            var original = new KeyValueCollection
            {
                { @null, null },
                { @bool, true },
                { @char, 'c' },
                { @int, 123 },
                { @float, 123.11f },
                { @double, 123.11d },
                { @decimal, 123.11m },
                { @string, "str" },
                { guid, Guid.Empty },
                { timespan, TimeSpan.FromSeconds(1) },
                { datetime, new DateTime(2000, 01, 01) },
                { bytearray, new byte[] { 1, 2, 3 } },
                { intarray, new int[] { 1, 2, 3 } },
                { objarray, new object[] { null, true, 'c', 123 } },
                { doublearray, new double[] { 1.1d, 2.1d, 3.1d } },
            };

            // Assert
            Assert.AreEqual(123, original.GetValue<int>(@int));
            Assert.AreEqual(123, original.GetValue<long>(@int));
            Assert.AreEqual((double)123.11f, original.GetValue<double>(@float));
            Assert.AreEqual((decimal)123.11f, original.GetValue<decimal>(@float));
            Assert.AreEqual((int)123.11m, original.GetValue<int>(@decimal));

            Assert.IsTrue(EqualsSmart(new int[] { 1, 2, 3 }, original.GetValue<int[]>(bytearray)));
            Assert.IsTrue(EqualsSmart(new int[] { 1, 2, 3 }, original.GetValue<int[]>(doublearray)));
            Assert.IsTrue(EqualsSmart(new float[] { (float)1.1d, (float)2.1d, (float)3.1d },
                original.GetValue<float[]>(doublearray)));
        }

        [TestMethod]
        public void LoadTest_Reflection_Serialize()
        {
            // Arrange
            var original = new TestComplexResult
            {
                Bool = true,
                Char = 'c',
                Int = 1,
                Float = 1.1f,
                Double = 1.1d,
                Decimal = 1.1m,
                String = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                Status = error,
                Message = "message",
                ServiceEndpoint = new Endpoint { Address = "127.0.0.1", Port = 1 },
                Endpoints = new Endpoint[]
                {
                    new Endpoint { Address = "127.0.0.1", Port = 81 },
                    new Endpoint { Address = "127.0.0.2", Port = 82 },
                },
                Map = new KeyValueCollection
                {
                    { @null, null },
                    { @bool, true },
                    { @char, 'c' },
                    { @int, 123 },
                }
            };

            {   // First run
                var dictionary = original.Serialize();
                var data = Serialization.BinarySerialize(writer => writer.WriteDictionary(dictionary));
                var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadDictionary());
                var restored = copy.Materialize<TestComplexResult>();
                Console.WriteLine($"Payload: {data.Length}");
            }

            const int duration = 5;
            int count = 0;
            bool isStopped = false;

            var thread = new Thread(() =>
            {
                while (!isStopped)
                {
                    var dictionary = original.Serialize();
                    var data = Serialization.BinarySerialize(writer => writer.WriteDictionary(dictionary));
                    //var copy = Serialization.BinaryDeserialize(data, reader => reader.ReadDictionary());
                    //var restored = copy.Materialize<TestComplexResult>();
                    count++;
                }
            });
            thread.Start();

            Thread.Sleep(duration * 1000);
            isStopped = true;
            thread.Join(500);

            Console.WriteLine($"Duration: {duration}, Passed: {count}, Avg: {(float)count / duration:F3}");
        }
    }
}