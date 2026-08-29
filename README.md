[![](https://img.shields.io/github/actions/workflow/status/soenneker/Soenneker.Quark.Runners.Crawler/build-and-test.yml?style=for-the-badge)](https://github.com/soenneker/Soenneker.Quark.Runners.Crawler/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/Soenneker.Quark.Runners.Crawler/daily-automatic-update.yml?style=for-the-badge&label=Daily%20Update)](https://github.com/soenneker/Soenneker.Quark.Runners.Crawler/actions/workflows/daily-automatic-update.yml)

# Soenneker.Quark.Runners.Crawler

Defines the file operations util contract.

> This is an automation runner, not a package intended for application consumption.

## What the runner does

- `IFileOperationsUtil.Process(cancellationToken)` — Processes the pending work managed by the File Operations.
- `Constants.SuiteRepository` — The suite repository.
- `Constants.CrawledRepository` — The crawled repository.
- `Constants.ComponentsRepository` — The components repository.
- `Constants.ComponentsUrl` — The components url.

## What you get

- `IFileOperationsUtil` — Defines the file operations util contract.
- `Constants` — Represents the constants.
- `ConsoleHostedService` — Represents the console hosted service.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IFileOperationsUtil.Process(cancellationToken)` | Processes the pending work managed by the File Operations. | A task that completes when the full processing workflow has finished. |
| `ConsoleHostedService.StartAsync(cancellationToken)` | Starts the Console Hosted Service and begins its background work. | A task that completes after the Console Hosted Service has started. |
| `ConsoleHostedService.StopAsync(cancellationToken)` | Stops the Console Hosted Service and waits for its background work to finish. | A task that completes after the Console Hosted Service has stopped. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
