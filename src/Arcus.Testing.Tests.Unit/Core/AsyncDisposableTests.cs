using System;
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
    }
}
