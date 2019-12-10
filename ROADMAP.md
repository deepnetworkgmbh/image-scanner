# Roadmap

## Long-term goal

The project should be merged into general [scanners](https://github.com/deepnetworkgmbh/security-monitor-scanners) application.

Potentially, it should be easier after trivy maintainers finish the migration to client/server model.

Before that time, the application should support:

- Requesting new image-scans by tags and for entire cluster.
- Read interfaces to get:
  - a detailed single image scan-result;
  - a summary for a subset of images;
  - a single CVE details. [The issue #17](https://github.com/deepnetworkgmbh/image-scanner/issues/17).
