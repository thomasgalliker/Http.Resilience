using System.Linq;
using System.Net;
using FluentAssertions;
using Http.Resilience.Extensions;
using Xunit;

namespace Http.Resilience.Tests.Extensions
{
    public class WebHeaderCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(WebHeaderCollectionTestdata))]
        public void ShouldGetHeaders(WebHeaderCollection webHeaderCollection, int expectedKeysCount, int expectedValuesCount)
        {
            // Act
            var output = webHeaderCollection.GetHeaders().ToList();

            // Assert
            output.Should().NotBeNull();
            output.Should().HaveCount(expectedKeysCount);
            output.All(o => o.Value.Count() == expectedValuesCount).Should().BeTrue();
        }

        public class WebHeaderCollectionTestdata : TheoryData<WebHeaderCollection, int, int>
        {
            public WebHeaderCollectionTestdata()
            {
                this.Add(null, 0, 0);
                this.Add(new WebHeaderCollection(), 0, 0);
                this.Add(new WebHeaderCollection
                {
                    { "key1", "value1" },
                }, 1, 1);
                this.Add(new WebHeaderCollection
                {
                    { "key1", "value1" },
                    { "key1", "value2" },
                }, 1, 2);
                this.Add(new WebHeaderCollection
                {
                    { "key1", "value1" },
                    { "key1", "value2" },
                    { "key2", "value3" },
                    { "key2", "value4" },
                }, 2, 2);
            }
        }
    }
}
