using System;
using System.Threading.Tasks;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Core
{
    public class AsyncDisposableTests
    {
        [Fact]
        public void Create_WithoutDisposable_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => AsyncDisposable.Create(disposable: null));
        }

        [Fact]
        public void Create_WithoutDispose_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => AsyncDisposable.Create(dispose: null));
        }

        [Fact]
        public void Create_WithoutDisposeAsync_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => AsyncDisposable.Create(disposeAsync: null));
        }

        [Fact]
        public async Task Dispose_MultipleTimes_SucceedsByBeingRedundant()
        {
            // Arrange
            int disposeCount = 0;
            var disposable = AsyncDisposable.Create(() => ++disposeCount);

            // Act
            await disposable.DisposeAsync();

            // Assert
            await disposable.DisposeAsync();
            await disposable.DisposeAsync();
            await disposable.DisposeAsync();

            Assert.Equal(1, disposeCount);
        }
    }
}
