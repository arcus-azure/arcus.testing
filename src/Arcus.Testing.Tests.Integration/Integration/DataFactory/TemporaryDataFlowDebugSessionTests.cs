using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.Testing.Tests.Integration.Integration.DataFactory
{
    [Collection(DataFactoryDebugSessionCollection.CollectionName)]
    public class TemporaryDataFlowDebugSessionTests : IntegrationTest
    {
        private readonly DataFactoryDebugSession _fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryDataFlowDebugSessionTests" /> class.
        /// </summary>
        public TemporaryDataFlowDebugSessionTests(DataFactoryDebugSession fixture, ITestOutputHelper outputWriter) : base(outputWriter)
        {
            _fixture = fixture;
        }

        private DataFactoryConfig DataFactory => Configuration.GetDataFactory();

        [Fact]
        public async Task StartDebugSession_WithActiveSession_SucceedsByReusingSession()
        {
            await using (var otherSession = await TemporaryDataFlowDebugSession.StartDebugSessionAsync(DataFactory.ResourceId, Logger, 
                options => options.ActiveSessionId = _fixture.Value.SessionId))
            {
                // Assert
                Assert.Equal(_fixture.Value.SessionId, otherSession.SessionId);
            }
            
            await _fixture.ShouldFindActiveSessionAsync(_fixture.Value.SessionId);
        }
    }
}
