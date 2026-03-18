using System.Net;
using System.Text;

namespace Sts2ServerLib;

/// <summary>
/// 現在の戦闘状態をJSONで配信する簡易HTTPサーバー。
/// localhost:21345 で待ち受け、/state でゲーム状態JSON、それ以外は webRoot の静的ファイルを返す。
/// ロギング処理は注入可能（テスト時はGodot非依存で動作する）。
/// </summary>
public class StateServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Func<string> _getStateJson;
    private readonly Action<string> _log;
    private readonly Action<string> _logErr;
    private readonly string? _webRoot;
    private Task? _listenTask;

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".html", "text/html; charset=utf-8" },
        { ".css",  "text/css; charset=utf-8" },
        { ".js",   "application/javascript; charset=utf-8" },
        { ".json", "application/json; charset=utf-8" },
        { ".png",  "image/png" },
        { ".ico",  "image/x-icon" },
    };

    /// <param name="getStateJson">呼び出されるたびにJSON文字列を返す関数</param>
    /// <param name="port">待ち受けポート番号</param>
    /// <param name="webRoot">静的ファイルを配信するディレクトリ（nullの場合は静的配信なし）</param>
    /// <param name="log">通常ログ出力（nullの場合はConsole.WriteLine）</param>
    /// <param name="logErr">エラーログ出力（nullの場合はConsole.Error.WriteLine）</param>
    public StateServer(Func<string> getStateJson, int port = 21345,
        string? webRoot = null,
        Action<string>? log = null, Action<string>? logErr = null)
    {
        _getStateJson = getStateJson;
        _webRoot = webRoot;
        _log = log ?? Console.WriteLine;
        _logErr = logErr ?? Console.Error.WriteLine;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    /// <summary>HTTPサーバーを起動する</summary>
    public void Start()
    {
        _listener.Start();
        _listenTask = Task.Run(ListenLoop);
        _log($"[Sts2ServerLib] HTTPサーバー起動: http://localhost:{GetPort()}/");
    }

    private int GetPort()
    {
        var prefix = _listener.Prefixes.First();
        return new Uri(prefix).Port;
    }

    private async Task ListenLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                HandleRequest(ctx);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logErr($"[Sts2ServerLib] サーバーエラー: {ex.Message}");
            }
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var response = ctx.Response;

        // CORSヘッダー
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (ctx.Request.HttpMethod == "OPTIONS")
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";

            if (path == "/state")
            {
                ServeJson(response, _getStateJson());
            }
            else if (_webRoot != null)
            {
                ServeStaticFile(response, path);
            }
            else
            {
                response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            _logErr($"[Sts2ServerLib] リクエスト処理エラー: {ex.Message}");
            try
            {
                // エラー時は500を返す
                var errBytes = Encoding.UTF8.GetBytes($"{{\"error\":\"{ex.Message}\"}}");
                response.StatusCode = 500;
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = errBytes.Length;
                response.OutputStream.Write(errBytes, 0, errBytes.Length);
            }
            catch { /* レスポンス書き込み自体が失敗した場合は無視 */ }
        }
        finally
        {
            // 例外が発生しても必ずCloseする
            try { response.Close(); } catch { }
        }
    }

    private void ServeJson(HttpListenerResponse response, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    private void ServeStaticFile(HttpListenerResponse response, string urlPath)
    {
        // "/" → "/index.html" に正規化
        if (urlPath == "/") urlPath = "/index.html";

        // パストラバーサル対策: webRoot 外へのアクセスを禁止
        var relativePath = urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_webRoot!, relativePath));
        if (!fullPath.StartsWith(Path.GetFullPath(_webRoot!), StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = 403;
            return;
        }

        if (!File.Exists(fullPath))
        {
            response.StatusCode = 404;
            return;
        }

        var ext = Path.GetExtension(fullPath);
        response.ContentType = MimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";

        var bytes = File.ReadAllBytes(fullPath);
        response.ContentLength64 = bytes.Length;
        response.OutputStream.Write(bytes, 0, bytes.Length);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();
        _cts.Dispose();
    }
}
