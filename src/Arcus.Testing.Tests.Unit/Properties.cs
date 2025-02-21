using System;
using Arcus.Testing.Tests.Core.Assert_.Fixture;
using Arcus.Testing.Tests.Unit.Assert_.Fixture;
using FsCheck;
using FsCheck.Fluent;

namespace Arcus.Testing.Tests.Unit
{
    /// <summary>
    /// Represents the different ways to generate a <see cref="TestJson"/>.
    /// </summary>
    public enum TestJsonType { Random, Object, Array }

    /// <summary>
    /// Exposes custom test properties based on the required test inputs throughout the project.
    /// </summary>
    public static class Properties
    {
        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(Action<TestCsv> testBody)
        {
            Property(() => TestCsv.Generate(), testBody);
        }

        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(Action<Func<Action<TestCsvOptions>, TestCsv>> testBody)
        {
            Property(() => TestCsv.Generate, testBody);
        }

        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(Action<TestCsv, TestCsv> testBody)
        {
            Property(() => TestCsv.Generate(), testBody);
        }

        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(Action<TestJson> testBody)
        {
            Property(TestJsonType.Random, testBody);
        }

        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(TestJsonType type, Action<TestJson> testBody)
        {
            Func<TestJson> gen = type switch
            {
                TestJsonType.Random => TestJson.Generate,
                TestJsonType.Array => TestJson.GenerateArray,
                TestJsonType.Object => TestJson.GenerateObject,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown test json type")
            };

            Property(gen, testBody);
        }

        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(Action<TestJson, TestJson> testBody)
        {
            Property(TestJson.Generate, testBody);
        }

        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(Action<TestXml> testBody)
        {
            Property(TestXml.Generate, testBody);
        }

        /// <summary>
        /// Runs a test property, generating (default 100) inputs for the given <paramref name="testBody"/>y.
        /// </summary>
        public static void Property(Action<TestXml, TestXml> testBody)
        {
            Property(TestXml.Generate, testBody);
        }

        private static void Property<T>(Func<T> gen, Action<T> testBody)
        {
            Prop.ForAll(Gen.Fresh(gen).ToArbitrary(), testBody)
                .QuickCheckThrowOnFailure();
        }

        private static void Property<T>(Func<T> gen, Action<T, T> testBody)
        {
            Prop.ForAll(Gen.Fresh(gen).ToArbitrary(), Gen.Fresh(gen).ToArbitrary(), testBody)
                .QuickCheckThrowOnFailure();
        }
    }
}
