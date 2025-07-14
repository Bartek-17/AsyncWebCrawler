# Async Web Crawler in C#

This is a simple asynchronous web crawler written in C#. It starts from a seed URL, fetches HTML pages, extracts links, and recursively follows them up to a specified depth.

The crawler supports rate limiting, duplicate link detection, and cancellation.

## Features

- Asynchronous HTTP requests using `HttpClient`
- Limits concurrent requests with `SemaphoreSlim`
- Avoids processing the same URL twice using `ConcurrentDictionary`
- Graceful cancellation with `Ctrl+C`
- Extracts and resolves absolute and relative links using regular expressions

## Usage

### Prerequisites

- .NET 6.0 or newer

### Running the crawler

Clone the repository and run:

```bash
dotnet run
