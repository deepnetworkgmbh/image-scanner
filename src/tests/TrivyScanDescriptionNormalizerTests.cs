using System;

using FluentAssertions;

using NUnit.Framework;

using webapp.Models;

namespace tests
{
    [TestFixture]
    public class TrivyScanDescriptionNormalizerTests
    {
        [Test]
        public void NotAuthorized()
        {
            var sampleNotAuthorized = @"
\u001b[0m\terror in image scan: failed to analyze image: failed to extract files: failed to create the registry client: Get https://aksrepos.azurecr.io/v2/: http: non-successful response (status=401 body=""{\""errors\"":[{\""code\"":\""UNAUTHORIZED\"",\""message\"":\""authentication required\"",\""detail\"":null}]}\n"")";

            var actualResponse = TrivyScanDescriptionNormalizer.ToHumanReadable(sampleNotAuthorized);

            actualResponse.Should().Be(TrivyScanDescriptionNormalizer.NotAuthorized);
        }

        [Test]
        public void UnknownOS()
        {
            var sampleNotAuthorized = @"\u001b[0m\terror in image scan: failed to scan image: failed to analyze OS: Unknown OS";

            var actualResponse = TrivyScanDescriptionNormalizer.ToHumanReadable(sampleNotAuthorized);

            actualResponse.Should().Be(TrivyScanDescriptionNormalizer.UnknownOS);
        }

        [Test]
        public void UnknownError()
        {
            var sampleNotAuthorized = @"\u001b[0m\terror in image scan: failed to analyze image: failed to extract files: failed to extract files: failed to extract the archive: unexpected EOF";

            var actualResponse = TrivyScanDescriptionNormalizer.ToHumanReadable(sampleNotAuthorized);

            actualResponse.Should().Be(TrivyScanDescriptionNormalizer.UnknownError);
        }

        [Test]
        public void RandomText()
        {
            var sampleNotAuthorized = Guid.NewGuid().ToString();

            var actualResponse = TrivyScanDescriptionNormalizer.ToHumanReadable(sampleNotAuthorized);

            actualResponse.Should().Be(TrivyScanDescriptionNormalizer.UnknownError);
        }
    }
}