﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Tests.Integration.Core.Fixture;
using Bogus;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;
using static Arcus.Testing.ResourceDirectory;

namespace Arcus.Testing.Tests.Integration.Core
{
    public class TestConfigTests : IAsyncLifetime
    {
        private const string DefaultAppSettingsName = "appsettings.json",
                             DefaultLocalAppSettingsName = "appsettings.local.json";

        private static readonly Faker Bogus = new();
        private readonly DisposableCollection _disposables = new(NullLogger.Instance);

        [Fact]
        public void CreateCustom_WithLocalAppSettingsOnCustomMainFile_RetrievesValue()
        {
            // Arrange
            string mainAppSettingsName = $"{Bogus.Lorem.Word()}.json";
            string localAppSettingsName = $"{Bogus.Lorem.Word()}.local.json";
            string key = Bogus.Lorem.Word(), expected = Bogus.Lorem.Word();
            AddLocalValueToCustomMain(localAppSettingsName, key, expected, mainAppSettingsName);

            var config = TestConfig.Create(options =>
            {
                options.UseMainJsonFile(mainAppSettingsName)
                       .AddOptionalJsonFile(localAppSettingsName);
            });

            // Act
            string actual = config[key];

            // Assert
            Assert.Equal(expected, actual);
        }

        private void AddLocalValueToCustomMain(string fileName, string key, string value, string newMainFile)
        {
            _disposables.Add(TemporaryFile.CreateAt(
                CurrentDirectory.Path, 
                fileName, 
                Encoding.UTF8.GetBytes($"{{ \"{key}\": \"{value}\" }}")));

            AddTokenToCustomMain(key, newMainFile);
        }

        [Fact]
        public void CreateCustom_WithLocalAppSettingsFile_RetrievesValue()
        {
            // Arrange
            string localAppSettingsName = $"{Bogus.Lorem.Word()}.json";
            string key = Bogus.Lorem.Word(), expected = Bogus.Lorem.Word();
            AddLocalValueToDefaultMain(localAppSettingsName, key, expected);

            var config = TestConfig.Create(options => options.AddOptionalJsonFile(localAppSettingsName));

            // Act
            string actual = config[key];

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CreateDefault_WithDefaultLocalValue_RetrievesValue()
        {
            // Arrange
            string key = Bogus.Lorem.Word(), expected = Bogus.Lorem.Word();
            AddLocalValueToDefaultMain(DefaultLocalAppSettingsName, key, expected);

            var config = TestConfig.Create();

            // Act
            string actual = config[key];

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Get_WithoutValue_FailsWithNotFound()
        {
            // Arrange
            string key = Bogus.Lorem.Word();
            AddLocalValueToDefaultMain(DefaultLocalAppSettingsName, key, value: "   ");

            string newMainFile = Bogus.Lorem.Word() + ".json";
            AddLocalValueToCustomMain(DefaultLocalAppSettingsName, key, value: "", newMainFile);

            // Act / Assert
            Assert.All(GenerateTestConfigs(uponNewMainFile: newMainFile), config =>
            {
                AssertNotFound(() => { var _ = config[key]; }, key, "non-blank", "value");
            });
        }

        private void AddLocalValueToDefaultMain(string fileName, string key, string value)
        {
            _disposables.Add(TemporaryFile.CreateAt(
                CurrentDirectory.Path, 
                fileName, 
                Encoding.UTF8.GetBytes($"{{ \"{key}\": \"{value}\" }}")));

            AddTokenToDefaultMain(key);
        }

        [Fact]
        public void Get_WithStillTokenInValue_FailsWithNotFound()
        {
            // Arrange
            string key = Bogus.Lorem.Word();
            AddTokenToDefaultMain(key);

            string newMainFile = Bogus.Lorem.Word() + ".json";
            AddTokenToCustomMain(key, newMainFile);

            // Act / Assert
            Assert.All(GenerateTestConfigs(uponNewMainFile: newMainFile), config =>
            {
                AssertNotFound(() => { var _ = config[key]; }, key, "still", "token");
            });
        }

        private void AddTokenToCustomMain(string key, string newMainFile)
        {
            _disposables.Add(TemporaryFile.CreateAt(CurrentDirectory.Path, newMainFile, "{ }"u8.ToArray()));
            AddTokenToDefaultMain(key, newMainFile);
        }

        private void AddTokenToDefaultMain(string key, string mainFile = DefaultAppSettingsName)
        {
            var defaultPath = new FileInfo(Path.Combine(CurrentDirectory.Path.FullName, mainFile));
            _disposables.Add(TemporaryFileEdit.At(defaultPath,
                json =>
                {
                    var jObject = JObject.Parse(json);
                    jObject[key] = "#{Token}#";

                    return jObject.ToString();
                }));
        }

        [Fact]
        public void Get_WithoutKnownKey_FailsWithNotFound()
        {
            string unknownKey = Bogus.Lorem.Word();
            Assert.All(GenerateTestConfigs(), config =>
            {
                AssertNotFound(() => { var _ = config[unknownKey]; }, unknownKey);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        public static void Get_WithoutKey_FailsWithNotFound(string key)
        {
            Assert.All(GenerateTestConfigs(), config => AssertNotFound(() => { var _ = config[key]; }));
        }

        private static TestConfig[] GenerateTestConfigs(string uponNewMainFile = null)
        {
            var configs = new[]
            {
                TestConfig.Create(),
                TestConfig.Create(opt => opt.AddOptionalJsonFile(Bogus.Random.Word()))
            };

            if (uponNewMainFile != null)
            {
                return configs.Append(TestConfig.Create(opt => opt.UseMainJsonFile(uponNewMainFile))).ToArray();
            }

            return configs;
        }

        private static void AssertNotFound(Action testCode, params string[] errorParts)
        {
            var exception = Assert.Throws<KeyNotFoundException>(testCode);
            Assert.Contains("test configuration", exception.Message);
            Assert.Contains("please make sure", exception.Message);
            Assert.All(errorParts, part => Assert.Contains(part, exception.Message));
        }

        [Fact]
        public void CreateCustom_WithoutMainAppSettingsFile_FailsWithNotFound()
        {
            Assert.Throws<FileNotFoundException>(
                () => TestConfig.Create(opt => opt.UseMainJsonFile(Bogus.System.FileName("json"))));
        }

        [Fact]
        public void Create_Default_Succeeds()
        {
            TestConfig.Create();
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _disposables.DisposeAsync();
        }
    }
}
