using System.IO;
using KaraokePlayer.Presentation;
using KaraokeCore.Library;
using KaraokeCore.Queue;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace KaraokePlayer.Web;

public sealed class QueueWebHost : IAsyncDisposable
{
  private readonly MainViewModel _viewModel;
  private WebApplication? _app;

  public QueueWebHost(MainViewModel viewModel)
  {
    _viewModel = viewModel;
  }

  public async Task StartAsync(CancellationToken cancellationToken = default)
  {
    if (_app is not null)
    {
      return;
    }

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
      Args = Array.Empty<string>(),
      ContentRootPath = AppContext.BaseDirectory
    });

    builder.WebHost.UseUrls("http://0.0.0.0:5100");
    var app = builder.Build();

    var webRoot = Path.Combine(AppContext.BaseDirectory, "Web");
    if (Directory.Exists(webRoot))
    {
      app.UseDefaultFiles(new DefaultFilesOptions
      {
        FileProvider = new PhysicalFileProvider(webRoot),
        RequestPath = ""
      });
      app.UseStaticFiles(new StaticFileOptions
      {
        FileProvider = new PhysicalFileProvider(webRoot),
        RequestPath = ""
      });
    }

    app.MapGet("/api/library", () =>
    {
      var songs = _viewModel.GetLibrarySnapshot();
      return songs.Select(entry => ToDto(entry, _viewModel.LibraryPath)).ToList();
    });

    app.MapGet("/api/queue", () =>
    {
      var items = _viewModel.GetQueueSnapshot();
      return items.Select(entry => ToDto(entry, _viewModel.LibraryPath)).ToList();
    });

    app.MapPost("/api/queue", async (HttpRequest request) =>
    {
      var payload = await request.ReadFromJsonAsync<QueueAddRequest>(cancellationToken: cancellationToken);
      if (payload is null || string.IsNullOrWhiteSpace(payload.SongId))
      {
        return Results.BadRequest();
      }

      var added = _viewModel.TryEnqueueById(payload.SongId);
      return added ? Results.Ok() : Results.NotFound();
    });

    await app.StartAsync(cancellationToken);
    _app = app;
  }

  public async ValueTask DisposeAsync()
  {
    if (_app is null)
    {
      return;
    }

    await _app.StopAsync();
    await _app.DisposeAsync();
    _app = null;
  }

  private static SongDto ToDto(SongEntry entry, string libraryPath)
  {
    return new SongDto(
      QueueSongId.FromEntry(libraryPath, entry),
      entry.Metadata?.Title ?? "--",
      entry.Metadata?.Artist ?? "--");
  }

  private sealed record SongDto(string Id, string Title, string Artist);
  private sealed record QueueAddRequest(string SongId);
}
