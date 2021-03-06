﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using core.core;
using core.helpers;

using Microsoft.AspNetCore.Mvc;

using webapp.Models;

namespace webapp.Controllers
{
    /// <summary>
    /// Provides scan results.
    /// </summary>
    [ApiController]
    [Route("scan-results")]
    public class ScanResultsController : ControllerBase
    {
        private readonly ImageScannerFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanResultsController"/> class.
        /// </summary>
        public ScanResultsController(ImageScannerFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Returns scan results for requested images.
        /// </summary>
        /// <param name="images">Array of image tags.</param>
        /// <returns>Scan results.</returns>
        [HttpGet]
        [Route("trivy")]
        public async Task<IEnumerable<TrivyScanResultShort>> GetShortImageScanResults([FromQuery]string[] images)
        {
            // TODO: try to scan images in real-time?
            var containerImages = images.Select(ContainerImage.FromFullName).ToArray();

            var scanDetails = (await this.factory.GetImporter().Get(containerImages)).ToArray();

            var results = scanDetails
                .Select(sd =>
                {
                    if (sd.ScanResult == ScanResult.Succeeded)
                    {
                        try
                        {
                            var targets = JsonSerializerWrapper.Deserialize<TrivyScanTarget[]>(sd.Payload);

                            var counters = targets
                                .Where(i => i.Vulnerabilities != null)
                                .SelectMany(i => i.Vulnerabilities)
                                .GroupBy(i => i.Severity)
                                .Select(i => new VulnerabilityCounters { Severity = i.Key, Count = i.Count() })
                                .ToArray();

                            return new TrivyScanResultShort
                            {
                                Image = sd.Image.FullName,
                                ScanResult = sd.ScanResult,
                                Counters = counters,
                            };
                        }
                        catch (Exception)
                        {
                            return new TrivyScanResultShort
                            {
                                Image = sd.Image.FullName,
                                ScanResult = ScanResult.Failed,
                                Description = "Corrupted content",
                            };
                        }
                    }

                    return new TrivyScanResultShort
                    {
                        Image = sd.Image.FullName,
                        ScanResult = sd.ScanResult,
                        Description = sd.Payload,
                    };
                })
                .ToArray();

            return results;
        }

        /// <summary>
        /// Returns scan result for requested image.
        /// </summary>
        /// <param name="image">The image tag.</param>
        /// <returns>Scan results.</returns>
        [HttpGet]
        [Route("trivy/{image}")]
        public async Task<ObjectResult> GetImageScanResult([FromRoute]string image)
        {
            // TODO: try to scan images in real-time?
            var decodedTag = HttpUtility.UrlDecode(image);
            var containerImage = ContainerImage.FromFullName(decodedTag);

            var result = (await this.factory.GetImporter().Get(containerImage)).FirstOrDefault();

            if (result != null)
            {
                if (result.ScanResult == ScanResult.Succeeded)
                {
                    var targets = JsonSerializerWrapper.Deserialize<TrivyScanTarget[]>(result.Payload);

                    return this.Ok(
                        new TrivyScanResultFull
                        {
                            Image = containerImage.FullName,
                            ScanResult = result.ScanResult,
                            Targets = targets,
                        });
                }

                return this.Ok(
                    new TrivyScanResultFull
                    {
                        Image = containerImage.FullName,
                        ScanResult = result.ScanResult,
                        Description = TrivyScanDescriptionNormalizer.ToHumanReadable(result.Payload),
                    });
            }
            else
            {
                return this.StatusCode(500, null);
            }
        }
    }
}
