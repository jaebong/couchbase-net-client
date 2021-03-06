﻿using System;
using Couchbase.Core.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Couchbase.UnitTests.Core.Serialization
{
    [TestFixture]
    public class UnixMillisecondsConverterTests
    {
        private static readonly DateTime TestTime = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly double TestUnixMilliseconds =
            (TestTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        #region Serialization

        [Test]
        public void Serialization_NonNullable_Serializes()
        {
            // Arrange

            var obj = new NonNullablePoco
            {
                Value = TestTime
            };

            // Act

            var json = JsonConvert.SerializeObject(obj);

            // Assert

            var expected = $@"{{""Value"":{TestUnixMilliseconds}}}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void Serialization_NullableWithValue_Serializes()
        {
            // Arrange

            var obj = new NullablePoco
            {
                Value = TestTime
            };

            // Act

            var json = JsonConvert.SerializeObject(obj);

            // Assert

            var expected = $@"{{""Value"":{TestUnixMilliseconds}}}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void Serialization_NullableWithoutValue_Serializes()
        {
            // Arrange

            var obj = new NullablePoco
            {
                Value = null
            };

            // Act

            var json = JsonConvert.SerializeObject(obj);

            // Assert

            var expected = @"{""Value"":null}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void Serialization_NullableWithoutValueNoNulls_SerializesWithoutValue()
        {
            // Arrange

            var obj = new NullableExcludeNullsPoco
            {
                Value = null
            };

            // Act

            var json = JsonConvert.SerializeObject(obj);

            // Assert

            var expected = @"{}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void Serialization_LocalTime_ConvertsToUtc()
        {
            if (TimeZoneInfo.Local.BaseUtcOffset == TimeSpan.Zero)
            {
                Assert.Ignore("Cannot test local time conversion from a system in UTC time zone");
            }

            // Arrange

            var localTime = DateTime.Now;
            Assert.AreEqual(DateTimeKind.Local, localTime.Kind);

            var obj = new NonNullablePoco
            {
                Value = localTime
            };

            // Act

            var json = JsonConvert.SerializeObject(obj);
            var obj2 = JsonConvert.DeserializeObject<NonNullablePoco>(json);

            // Assert

            Assert.AreEqual(DateTimeKind.Utc, obj2.Value.Kind);

            // There will a slight submillisecond difference due to rounding
            // so for the test just make sure the difference is less than half a millisecond
            Assert.AreEqual(0,
                Math.Round((localTime - obj2.Value.ToLocalTime()).TotalMilliseconds, MidpointRounding.AwayFromZero));
        }

        #endregion

        #region Deserialization

        [Test]
        public void Deserialization_NonNullable_Deserializes()
        {
            // Arrange

            var json = $@"{{""Value"":{TestUnixMilliseconds}}}";

            // Act

            var obj = JsonConvert.DeserializeObject<NonNullablePoco>(json);

            // Assert

            Assert.AreEqual(TestTime, obj.Value);
        }

        [Test]
        public void Deserialization_NullableWithValue_Deserializes()
        {
            // Arrange

            var json = $@"{{""Value"":{TestUnixMilliseconds}}}";

            // Act

            var obj = JsonConvert.DeserializeObject<NullablePoco>(json);

            // Assert

            Assert.AreEqual(TestTime, obj.Value);
        }

        [Test]
        public void Deserialization_NullableWithoutValue_Deserializes()
        {
            // Arrange

            var json = @"{""Value"":null}";

            // Act

            var obj = JsonConvert.DeserializeObject<NullablePoco>(json);

            // Assert

            Assert.Null(obj.Value);
        }

        [Test]
        public void Deserialization_AnyTime_ReturnsAsUtcKind()
        {
            // Arrange

            const string json = @"{""Value"":1000000}";

            // Act

            var obj = JsonConvert.DeserializeObject<NonNullablePoco>(json);

            // Assert

            Assert.AreEqual(DateTimeKind.Utc, obj.Value.Kind);
        }

        #endregion

        #region Helpers

        public class NonNullablePoco
        {
            [JsonConverter(typeof(UnixMillisecondsConverter))]
            public DateTime Value { get; set; }
        }

        public class NullablePoco
        {
            [JsonConverter(typeof(UnixMillisecondsConverter))]
            public DateTime? Value { get; set; }
        }

        public class NullableExcludeNullsPoco
        {
            [JsonConverter(typeof(UnixMillisecondsConverter))]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public DateTime? Value { get; set; }
        }

        #endregion
    }
}
