using System.Collections.Generic;
using System.Net;

namespace Http.Resilience.Extensions
{
    internal static class WebHeaderCollectionExtensions
    {
        internal static IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetHeaders(this WebHeaderCollection webHeaderCollection)
        {
            if (webHeaderCollection == null)
            {
                yield break;
            }

            var keys = webHeaderCollection.AllKeys;

            for (var i = 0; i < keys.Length; i++)
            {
                yield return new KeyValuePair<string, IEnumerable<string>>(keys[i], webHeaderCollection.GetValues(keys[i]));
            }
        }
    }
}
