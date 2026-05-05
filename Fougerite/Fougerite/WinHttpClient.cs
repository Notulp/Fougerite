using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Fougerite.Concurrent;

namespace Fougerite
{
    /// <summary>
    /// Since UnityEngine is running on Mono and updating the security dlls would take a lot of work
    /// we use WinHTTP via P/Invoke to make HTTP requests with modern TLS support so websites do not reject us
    /// with TLS 1.3 enforcement.
    /// It should work with Wine under Linux as well.
    /// </summary>
    public class WinHttpClient
    {
        private static readonly Lazy<WinHttpClient> Instance = new Lazy<WinHttpClient>(() => new WinHttpClient());

        [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr WinHttpOpen(string pszAgentW, uint dwAccessType, string pszProxyW,
            string pszProxyBypassW, uint dwFlags);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern bool WinHttpCloseHandle(IntPtr hInternet);

        [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr WinHttpConnect(IntPtr hSession, string pszServerName, ushort nServerPort,
            uint dwReserved);

        [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr WinHttpOpenRequest(IntPtr hConnect, string pszVerb, string pszObjectName,
            string pszVersion, string pszReferrer, IntPtr ppwszAcceptTypes, uint dwFlags);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern bool WinHttpSetOption(IntPtr hRequest, uint dwOption, IntPtr lpBuffer,
            uint dwBufferLength);

        [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool WinHttpSendRequest(IntPtr hRequest, string lpszHeaders, uint dwHeadersLength,
            IntPtr lpOptional, uint dwOptionalLength, uint dwTotalLength, IntPtr lpContext);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern bool WinHttpReceiveResponse(IntPtr hRequest, IntPtr lpReserved);

        [DllImport("winhttp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool WinHttpQueryHeaders(IntPtr hRequest, uint dwInfoLevel, string pwszName,
            IntPtr lpBuffer, ref uint lpdwBufferLength, ref uint lpdwIndex);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern bool WinHttpReadData(IntPtr hRequest, byte[] lpBuffer, uint dwNumberOfBytesToRead,
            out uint lpdwNumberOfBytesRead);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern bool WinHttpWriteData(IntPtr hRequest, byte[] lpBuffer, uint dwNumberOfBytesToWrite, 
            out uint lpdwNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateEventW(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState,
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        
        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern IntPtr WinHttpWebSocketCompleteUpgrade(IntPtr hRequest, IntPtr pContext);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern uint WinHttpWebSocketSend(IntPtr hWebSocket, uint eBufferType, IntPtr pvBuffer, uint dwBufferLength);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern uint WinHttpWebSocketReceive(IntPtr hWebSocket, IntPtr pvBuffer, uint dwBufferLength, out uint pdwBytesRead, out uint peBufferType);

        [DllImport("winhttp.dll", SetLastError = true)]
        public static extern uint WinHttpWebSocketClose(IntPtr hWebSocket, ushort usStatus, IntPtr pvReason, uint dwReasonLength);

        public const uint WINHTTP_OPTION_UPGRADE_TO_WEB_SOCKET = 114;
        public const uint WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE = 0;
        public const uint WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE = 1;
        public const uint WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE = 2;
        public const uint WINHTTP_WEB_SOCKET_UTF8_FRAGMENT_BUFFER_TYPE = 3;
        public const uint WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE = 4;
        public const uint WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0;
        public const uint WINHTTP_FLAG_SECURE = 0x00800000;
        public const ushort INTERNET_DEFAULT_HTTPS_PORT = 443;
        public const ushort INTERNET_DEFAULT_HTTP_PORT = 80;
        public const uint WINHTTP_OPTION_SECURITY_FLAGS = 31;
        public const uint WINHTTP_OPTION_CONNECT_TIMEOUT = 2;
        public const uint WINHTTP_OPTION_SEND_TIMEOUT = 3;
        public const uint WINHTTP_OPTION_RECEIVE_TIMEOUT = 4;
        public const uint SECURITY_FLAG_IGNORE_UNKNOWN_CA = 0x00000100;
        public const uint SECURITY_FLAG_IGNORE_CERT_WRONG_USAGE = 0x00000200;
        public const uint SECURITY_FLAG_IGNORE_CERT_CN_INVALID = 0x00001000;
        public const uint SECURITY_FLAG_IGNORE_CERT_DATE_INVALID = 0x00002000;
        public const uint WINHTTP_QUERY_STATUS_CODE = 19;
        public const uint WINHTTP_QUERY_FLAG_NUMBER = 0x20000000;
        public const uint WAIT_OBJECT_0 = 0;
        public const uint WAIT_TIMEOUT = 0x102;

        /// <summary>
        /// The buffer size for reading response data per chunk (4 KB).
        /// </summary>
        public int BUFFER_SIZE = 4096;

        /// <summary>
        /// The maximum size of the response body to read (10 MB).
        /// </summary>
        public int MAX_SIZE = 10 * 1024 * 1024;

        /// <summary>
        /// Session handle for WinHTTP.
        /// No need to access this from a plugin.
        /// </summary>
        private IntPtr _sessionHandle;

        /// <summary>
        /// Session lock for thread safety.
        /// No need to access this from a plugin.
        /// </summary>
        private readonly object _sessionLock;

        private WinHttpClient()
        {
            _sessionLock = new object();
            _sessionHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Returns the instance of the Web class.
        /// </summary>
        /// <returns></returns>
        public static WinHttpClient GetInstance()
        {
            return Instance.Value;
        }

        /// <summary>
        /// Initializes the WinHTTP session.
        /// No need to call this from a plugin.
        /// </summary>
        public void InitSession()
        {
            lock (_sessionLock)
            {
                if (_sessionHandle == IntPtr.Zero)
                {
                    _sessionHandle = WinHttpOpen($"Fougerite Mod (v{Bootstrap.Version}; https://fougerite.com)",
                        WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, null, null, 0);
                }
            }
        }

        /// <summary>
        /// Closes the WinHTTP session.
        /// No need to call this from a plugin.
        /// </summary>
        public void CloseSession()
        {
            lock (_sessionLock)
            {
                if (_sessionHandle != IntPtr.Zero)
                {
                    WinHttpCloseHandle(_sessionHandle);
                    _sessionHandle = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Queues an HTTP request to be executed asynchronously on a ThreadPool thread using WinHTTP.
        /// Non-blocking for the caller, the result is returned via the callback when the request completes.
        /// </summary>
        /// <param name="url">Full URL including protocol (https://example.com/api).</param>
        /// <param name="callback">
        /// Delegate invoked as callback(statusCode, responseBody) after the request finishes.
        /// This runs on a ThreadPool thread, use Loom.QueueOnMainThread if you need to touch Unity objects in your callback.
        /// </param>
        /// <param name="method">HTTP method to use (GET, POST, PUT, DELETE). Defaults to GET.</param>
        /// <param name="inputBody">Optional request body payload. Use null or empty for methods like GET.</param>
        /// <param name="additionalHeaders">
        /// Optional collection of extra HTTP headers to send with the request.
        /// </param>
        /// <param name="contentType">
        /// Content-Type header for the request body (application/json, application/x-www-form-urlencoded).
        /// Defaults to application/x-www-form-urlencoded.
        /// </param>
        /// <param name="timeout">
        /// Timeout in seconds applied to connect, send, and receive operations. Use 0 for the default.
        /// </param>
        public void MakeRequest(string url, Action<int, string> callback, string method = "GET",
            string inputBody = null, Dictionary<string, string> additionalHeaders = null,
            string contentType = "application/x-www-form-urlencoded", float timeout = 0f)
        {
            ThreadPool.QueueUserWorkItem(_ =>
                DoWinHttpRequest(url, callback, method, inputBody, additionalHeaders, contentType, timeout));
        }

        /// <summary>
        /// Executes a synchronous HTTP request using WinHTTP. Blocks the calling thread until completion.
        /// Should only be called from background threads (automatically handled by MakeRequest).
        /// </summary>
        /// <param name="url">Full URL including protocol (https://example.com/api)</param>
        /// <param name="callback">Action invoked with (statusCode, responseBody) on the same thread after request completes</param>
        /// <param name="method">HTTP method: GET, POST, PUT, DELETE</param>
        /// <param name="inputBody">Request body data. Use null for GET requests.</param>
        /// <param name="additionalHeaders">Custom HTTP headers</param>
        /// <param name="contentType">Content-Type header value</param>
        /// <param name="timeout">Timeout in seconds. Use 0 for default.</param>
        /// <remarks>
        /// Blocks thread during WinHttpConnect, WinHttpSendRequest, WinHttpReceiveResponse, WinHttpReadData.
        /// SSL validation disabled. Response limited to 10MB, you can change class variable.
        /// </remarks>
        public void DoWinHttpRequest(string url, Action<int, string> callback, string method, string inputBody,
            Dictionary<string, string> additionalHeaders, string contentType, float timeout)
        {
            IntPtr connectHandle = IntPtr.Zero;
            IntPtr requestHandle = IntPtr.Zero;
            GCHandle bodyPin = new GCHandle();
            GCHandle flagsPin = new GCHandle();

            try
            {
                InitSession();

                if (_sessionHandle == IntPtr.Zero)
                {
                    callback(0, "NoSession");
                    return;
                }

                // Parse the URL
                Uri uri;
                try
                {
                    uri = new Uri(url);
                }
                catch (Exception ex)
                {
                    callback(0, $"BadUrl{ex.Message}");
                    return;
                }

                // We support http as well as https
                ushort port = uri.Scheme == "https" ? INTERNET_DEFAULT_HTTPS_PORT : INTERNET_DEFAULT_HTTP_PORT;
                uint flags = uri.Scheme == "https" ? WINHTTP_FLAG_SECURE : 0;

                // Create connection handle
                connectHandle = WinHttpConnect(_sessionHandle, uri.Host, port, 0);
                if (connectHandle == IntPtr.Zero)
                {
                    callback(0, "ConnectFail");
                    return;
                }

                string path = uri.PathAndQuery;
                if (!string.IsNullOrEmpty(uri.Fragment)) path += uri.Fragment;

                requestHandle = WinHttpOpenRequest(connectHandle, method, path, null, null, IntPtr.Zero, flags);
                if (requestHandle == IntPtr.Zero)
                {
                    callback(0, "RequestFail");
                    return;
                }

                // Calculate timeout in milliseconds based on input float seconds
                uint timeoutMs = timeout > 0 ? (uint)(timeout * 1000) : 30000;

                byte[] connectTimeoutBuf = BitConverter.GetBytes(timeoutMs);
                GCHandle connectTimeoutPin = GCHandle.Alloc(connectTimeoutBuf, GCHandleType.Pinned);
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_CONNECT_TIMEOUT, connectTimeoutPin.AddrOfPinnedObject(),
                    sizeof(uint));
                connectTimeoutPin.Free();

                byte[] sendTimeoutBuf = BitConverter.GetBytes(timeoutMs);
                GCHandle sendTimeoutPin = GCHandle.Alloc(sendTimeoutBuf, GCHandleType.Pinned);
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_SEND_TIMEOUT, sendTimeoutPin.AddrOfPinnedObject(),
                    sizeof(uint));
                sendTimeoutPin.Free();

                byte[] recvTimeoutBuf = BitConverter.GetBytes(timeoutMs);
                GCHandle recvTimeoutPin = GCHandle.Alloc(recvTimeoutBuf, GCHandleType.Pinned);
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_RECEIVE_TIMEOUT, recvTimeoutPin.AddrOfPinnedObject(),
                    sizeof(uint));
                recvTimeoutPin.Free();

                // Initialize ignore flags
                uint ignoreFlags = SECURITY_FLAG_IGNORE_UNKNOWN_CA | SECURITY_FLAG_IGNORE_CERT_WRONG_USAGE |
                                   SECURITY_FLAG_IGNORE_CERT_CN_INVALID | SECURITY_FLAG_IGNORE_CERT_DATE_INVALID;

                // Allocate pinned memory for ignore flags
                byte[] flagsBuffer = BitConverter.GetBytes(ignoreFlags);
                flagsPin = GCHandle.Alloc(flagsBuffer, GCHandleType.Pinned);
                IntPtr flagsPtr = flagsPin.AddrOfPinnedObject();

                // Set security flags to ignore certificate errors
                bool sslSet = WinHttpSetOption(requestHandle, WINHTTP_OPTION_SECURITY_FLAGS, flagsPtr, sizeof(uint));

                // Prepare body
                byte[] bodyBytes = string.IsNullOrEmpty(inputBody) ? new byte[0] : Encoding.UTF8.GetBytes(inputBody);
                uint bodyLength = (uint)bodyBytes.Length;

                // Allocate pinned memory for body if needed
                IntPtr bodyPtr = IntPtr.Zero;
                if (bodyLength > 0)
                {
                    bodyPin = GCHandle.Alloc(bodyBytes, GCHandleType.Pinned);
                    bodyPtr = bodyPin.AddrOfPinnedObject();
                }

                // Total length is body length for now
                uint totalLength = bodyLength;

                // Prepare headers
                string headersString = null;
                if (!string.IsNullOrEmpty(contentType) || (additionalHeaders != null && additionalHeaders.Count > 0))
                {
                    StringBuilder headerBuilder = new StringBuilder();

                    if (!string.IsNullOrEmpty(contentType) && bodyLength > 0)
                    {
                        headerBuilder.Append("Content-Type: ");
                        headerBuilder.Append(contentType);
                        headerBuilder.Append("\r\n");
                    }

                    if (additionalHeaders != null)
                    {
                        foreach (var header in additionalHeaders)
                        {
                            headerBuilder.Append(header.Key);
                            headerBuilder.Append(": ");
                            headerBuilder.Append(header.Value);
                            headerBuilder.Append("\r\n");
                        }
                    }

                    headersString = headerBuilder.ToString();
                }

                // Send the request
                bool sent = WinHttpSendRequest(requestHandle, headersString,
                    headersString != null ? (uint)headersString.Length : 0, bodyPtr, bodyLength, totalLength,
                    IntPtr.Zero);

                if (!sent)
                {
                    callback(0, "SendFail");
                    return;
                }

                // Receive the response
                bool received = WinHttpReceiveResponse(requestHandle, IntPtr.Zero);

                if (!received)
                {
                    callback(0, "RecvFail");
                    return;
                }

                uint statusCode = 0;
                uint bufLen = sizeof(uint);
                uint index = 0;

                // Allocate pinned memory for status code
                GCHandle statusCodePin = GCHandle.Alloc(statusCode, GCHandleType.Pinned);

                // Query the HTTP status code
                bool queryResult = WinHttpQueryHeaders(requestHandle,
                    WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, null, statusCodePin.AddrOfPinnedObject(),
                    ref bufLen, ref index);
                statusCode = (uint)Marshal.ReadInt32(statusCodePin.AddrOfPinnedObject());
                statusCodePin.Free();

                if (!queryResult || statusCode == 0)
                {
                    callback(0, "NoStatus");
                    return;
                }

                StringBuilder responseBuilder = new StringBuilder();
                byte[] buffer = new byte[BUFFER_SIZE];
                uint bytesRead;
                int totalRead = 0;

                // Read response data in chunks until done or max size reached
                while (WinHttpReadData(requestHandle, buffer, (uint)BUFFER_SIZE, out bytesRead) && bytesRead > 0)
                {
                    totalRead += (int)bytesRead;
                    responseBuilder.Append(Encoding.UTF8.GetString(buffer, 0, (int)bytesRead));

                    if (totalRead > MAX_SIZE)
                    {
                        break;
                    }
                }

                callback((int)statusCode, responseBuilder.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogError($"[WinHttp] EXCEPTION: {ex}");
                callback(0, $"Exception: {ex.Message}");
            }
            finally
            {
                if (bodyPin != null && bodyPin.IsAllocated) bodyPin.Free();
                if (flagsPin != null && flagsPin.IsAllocated) flagsPin.Free();
                if (requestHandle != IntPtr.Zero) WinHttpCloseHandle(requestHandle);
                if (connectHandle != IntPtr.Zero) WinHttpCloseHandle(connectHandle);
            }
        }


        /// <summary>
        /// Performs a synchronous HTTP GET request that blocks the calling thread until completion or timeout.
        /// WARNING: Calling this from Unity's main thread may freeze game for x time.
        /// Use MakeRequest with callbacks for non-blocking behavior, or call this from a background thread.
        /// </summary>
        /// <param name="url">Full URL to request (https://example.com/api).</param>
        /// <param name="timeout">
        /// Request timeout in seconds. Defaults to 5 seconds.
        /// Actual wait time is timeout + 1 second to allow for cleanup.
        /// </param>
        /// <returns>
        /// Response body string if status code is 2xx (success).
        /// "HTTP {statusCode}" string if request succeeded but returned non-2xx status.
        /// "Timeout" if request exceeded timeout duration.
        /// "Error {exception}" if request failed with an exception.
        /// </returns>
        public string GetBlocking(string url, float timeout = 5f)
        {
            try
            {
                var result = new ManualResetEvent(false);
                string responseBody = null;
                int responseCode = 0;

                MakeRequest(url, (code, body) =>
                {
                    responseCode = code;
                    responseBody = body;
                    result.Set();
                }, "GET", null, null, "text/plain", timeout);

                if (result.WaitOne((int)(timeout * 1000) + 1000, false))
                    return responseCode >= 200 && responseCode < 300 ? responseBody : $"HTTP {responseCode}";
                return "Timeout";
            }
            catch (Exception ex)
            {
                Logger.LogError($"[GetBlocking] {ex}");
                return $"Error {ex.Message}";
            }
        }

        /// <summary>
        /// Performs a synchronous HTTP POST request that blocks the calling thread until completion or timeout.
        /// WARNING: Calling this from Unity's main thread may freeze game for x time.
        /// Use MakeRequest with callbacks for non-blocking behavior, or call this from a background thread.
        /// </summary>
        /// <param name="url">Full URL to request (e.g., https://example.com/api).</param>
        /// <param name="inputBody">Request body payload to send. Use null or empty for no body.</param>
        /// <param name="contentType">
        /// Content-Type header value. Use 'application/json' for JSON payloads.
        /// Defaults to 'application/x-www-form-urlencoded'.
        /// </param>
        /// <param name="timeout">
        /// Request timeout in seconds. Defaults to 5 seconds.
        /// Actual wait time is timeout + 1 second to allow for cleanup.
        /// </param>
        /// <returns>
        /// Response body string if status code is 2xx (success).
        /// "HTTP {statusCode}" string if request succeeded but returned non-2xx status.
        /// "Timeout" if request exceeded timeout duration.
        /// "Error {exception}" if request failed with an exception.
        /// </returns>
        public string PostBlocking(string url, string inputBody,
            string contentType = "application/x-www-form-urlencoded", float timeout = 5f)
        {
            try
            {
                var result = new ManualResetEvent(false);
                string responseBody = null;
                int responseCode = 0;

                MakeRequest(url, (code, body) =>
                {
                    responseCode = code;
                    responseBody = body;
                    result.Set();
                }, "POST", inputBody, null, contentType, timeout);

                if (result.WaitOne((int)(timeout * 1000) + 1000, false))
                    return responseCode >= 200 && responseCode < 300 ? responseBody : $"HTTP {responseCode}";
                return "Timeout";
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PostBlocking] {ex}");
                return $"Error {ex.Message}";
            }
        }

        /// <summary>
        /// Backwards compatible wrapper for DownloadFileBlocking that doesn't expose the error string.
        /// </summary>
        public bool DownloadFileBlocking(string url, string destinationPath, float timeout = 30f)
        {
            string error;
            return DownloadFileBlocking(url, destinationPath, out error, timeout);
        }

        /// <summary>
        /// Performs a synchronous HTTP GET request directly downloading the binary content into a file.
        /// Blocks the calling thread until completion or timeout.
        /// </summary>
        /// <param name="url">The URL of the file to download.</param>
        /// <param name="destinationPath">The local file path where the downloaded content will be saved.</param>
        /// <param name="error">Outputs the error message if the operation fails.</param>
        /// <param name="timeout">The maximum time in seconds to wait for the operation to complete. Defaults to 30 seconds.</param>
        /// <returns>True if the file is successfully downloaded, otherwise, false.</returns>
        public bool DownloadFileBlocking(string url, string destinationPath, out string error, float timeout = 30f)
        {
            error = string.Empty;
            IntPtr connectHandle = IntPtr.Zero;
            IntPtr requestHandle = IntPtr.Zero;
            GCHandle flagsPin = new GCHandle();
            FileStream fs = null;

            try
            {
                InitSession();

                if (_sessionHandle == IntPtr.Zero)
                {
                    error = "NoSession";
                    return false;
                }

                Uri uri;
                try
                {
                    uri = new Uri(url);
                }
                catch (Exception ex)
                {
                    error = $"BadUrl: {ex.Message}";
                    return false;
                }

                ushort port = uri.Scheme == "https" ? INTERNET_DEFAULT_HTTPS_PORT : INTERNET_DEFAULT_HTTP_PORT;
                uint flags = uri.Scheme == "https" ? WINHTTP_FLAG_SECURE : 0;

                connectHandle = WinHttpConnect(_sessionHandle, uri.Host, port, 0);
                if (connectHandle == IntPtr.Zero)
                {
                    error = "ConnectFail";
                    return false;
                }

                string path = uri.PathAndQuery;
                if (!string.IsNullOrEmpty(uri.Fragment)) path += uri.Fragment;

                requestHandle = WinHttpOpenRequest(connectHandle, "GET", path, null, null, IntPtr.Zero, flags);
                if (requestHandle == IntPtr.Zero)
                {
                    error = "RequestFail";
                    return false;
                }

                uint timeoutMs = timeout > 0 ? (uint)(timeout * 1000) : 30000;
                byte[] timeoutBuf = BitConverter.GetBytes(timeoutMs);
                GCHandle timeoutPin = GCHandle.Alloc(timeoutBuf, GCHandleType.Pinned);
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_CONNECT_TIMEOUT, timeoutPin.AddrOfPinnedObject(), sizeof(uint));
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_SEND_TIMEOUT, timeoutPin.AddrOfPinnedObject(), sizeof(uint));
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_RECEIVE_TIMEOUT, timeoutPin.AddrOfPinnedObject(), sizeof(uint));
                timeoutPin.Free();

                uint ignoreFlags = SECURITY_FLAG_IGNORE_UNKNOWN_CA | SECURITY_FLAG_IGNORE_CERT_WRONG_USAGE |
                                   SECURITY_FLAG_IGNORE_CERT_CN_INVALID | SECURITY_FLAG_IGNORE_CERT_DATE_INVALID;

                byte[] flagsBuffer = BitConverter.GetBytes(ignoreFlags);
                flagsPin = GCHandle.Alloc(flagsBuffer, GCHandleType.Pinned);
                IntPtr flagsPtr = flagsPin.AddrOfPinnedObject();

                WinHttpSetOption(requestHandle, WINHTTP_OPTION_SECURITY_FLAGS, flagsPtr, sizeof(uint));

                string headersString = $"Fougerite Mod (v{Bootstrap.Version}; https://fougerite.com)\r\n";
                
                bool sent = WinHttpSendRequest(requestHandle, headersString, (uint)headersString.Length, IntPtr.Zero, 0, 0, IntPtr.Zero);
                if (!sent)
                {
                    error = "SendFail";
                    return false;
                }

                bool received = WinHttpReceiveResponse(requestHandle, IntPtr.Zero);
                if (!received)
                {
                    error = "RecvFail";
                    return false;
                }

                uint statusCode = 0;
                uint bufLen = sizeof(uint);
                uint index = 0;

                GCHandle statusCodePin = GCHandle.Alloc(statusCode, GCHandleType.Pinned);
                bool queryResult = WinHttpQueryHeaders(requestHandle, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, null, statusCodePin.AddrOfPinnedObject(), ref bufLen, ref index);
                statusCode = (uint)Marshal.ReadInt32(statusCodePin.AddrOfPinnedObject());
                statusCodePin.Free();

                if (!queryResult)
                {
                    error = "QueryHeadersFail";
                    return false;
                }

                if (statusCode < 200 || statusCode >= 300)
                {
                    error = $"HTTP {statusCode}";
                    return false;
                }

                fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                byte[] buffer = new byte[BUFFER_SIZE];
                uint bytesRead;

                while (WinHttpReadData(requestHandle, buffer, (uint)BUFFER_SIZE, out bytesRead) && bytesRead > 0)
                {
                    fs.Write(buffer, 0, (int)bytesRead);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[WinHttp Download] EXCEPTION: {ex}");
                error = ex.Message;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Flush();
                    fs.Close();
                    fs.Dispose();
                }
                if (flagsPin != null && flagsPin.IsAllocated) flagsPin.Free();
                if (requestHandle != IntPtr.Zero) WinHttpCloseHandle(requestHandle);
                if (connectHandle != IntPtr.Zero) WinHttpCloseHandle(connectHandle);
            }
        }

        /// <summary>
        /// Performs an asynchronous HTTP GET request to download binary content.
        /// Returns the success status and error message via a callback on the main thread.
        /// </summary>
        public void DownloadFileAsync(string url, string destinationPath, Action<bool, string> onComplete, float timeout = 30f)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string error = string.Empty;
                bool result = DownloadFileBlocking(url, destinationPath, out error, timeout);
                if (onComplete != null)
                {
                    Loom.QueueOnMainThread(() =>
                    {
                        onComplete(result, error);
                    });
                }
            });
        }

        /// <summary>
        /// Backwards compatible wrapper for UploadFileBlocking that doesn't expose the error string.
        /// </summary>
        public bool UploadFileBlocking(string url, string filePath, float timeout = 30f)
        {
            string error;
            return UploadFileBlocking(url, filePath, out error, timeout);
        }

        /// <summary>
        /// Performs a synchronous HTTP POST request directly uploading the binary content of a file.
        /// Blocks the calling thread until completion or timeout.
        /// </summary>
        public bool UploadFileBlocking(string url, string filePath, out string error, float timeout = 30f)
        {
            error = string.Empty;
            if (!File.Exists(filePath))
            {
                error = "FileDoesNotExist";
                return false;
            }

            IntPtr connectHandle = IntPtr.Zero;
            IntPtr requestHandle = IntPtr.Zero;
            GCHandle flagsPin = new GCHandle();
            FileStream fs = null;

            try
            {
                InitSession();

                if (_sessionHandle == IntPtr.Zero)
                {
                    error = "NoSession";
                    return false;
                }

                Uri uri;
                try
                {
                    uri = new Uri(url);
                }
                catch (Exception ex)
                {
                    error = $"BadUrl: {ex.Message}";
                    return false;
                }

                ushort port = uri.Scheme == "https" ? INTERNET_DEFAULT_HTTPS_PORT : INTERNET_DEFAULT_HTTP_PORT;
                uint flags = uri.Scheme == "https" ? WINHTTP_FLAG_SECURE : 0;

                connectHandle = WinHttpConnect(_sessionHandle, uri.Host, port, 0);
                if (connectHandle == IntPtr.Zero)
                {
                    error = "ConnectFail";
                    return false;
                }

                string path = uri.PathAndQuery;
                if (!string.IsNullOrEmpty(uri.Fragment)) path += uri.Fragment;

                requestHandle = WinHttpOpenRequest(connectHandle, "POST", path, null, null, IntPtr.Zero, flags);
                if (requestHandle == IntPtr.Zero)
                {
                    error = "RequestFail";
                    return false;
                }

                uint timeoutMs = timeout > 0 ? (uint)(timeout * 1000) : 30000;
                byte[] timeoutBuf = BitConverter.GetBytes(timeoutMs);
                GCHandle timeoutPin = GCHandle.Alloc(timeoutBuf, GCHandleType.Pinned);
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_CONNECT_TIMEOUT, timeoutPin.AddrOfPinnedObject(), sizeof(uint));
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_SEND_TIMEOUT, timeoutPin.AddrOfPinnedObject(), sizeof(uint));
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_RECEIVE_TIMEOUT, timeoutPin.AddrOfPinnedObject(), sizeof(uint));
                timeoutPin.Free();

                uint ignoreFlags = SECURITY_FLAG_IGNORE_UNKNOWN_CA | SECURITY_FLAG_IGNORE_CERT_WRONG_USAGE |
                                   SECURITY_FLAG_IGNORE_CERT_CN_INVALID | SECURITY_FLAG_IGNORE_CERT_DATE_INVALID;
                byte[] flagsBuffer = BitConverter.GetBytes(ignoreFlags);
                flagsPin = GCHandle.Alloc(flagsBuffer, GCHandleType.Pinned);
                WinHttpSetOption(requestHandle, WINHTTP_OPTION_SECURITY_FLAGS, flagsPin.AddrOfPinnedObject(), sizeof(uint));

                FileInfo fi = new FileInfo(filePath);
                uint fileSize = (uint)fi.Length;

                string headersString = $"Fougerite Mod (v{Bootstrap.Version}; https://fougerite.com)\r\nContent-Type: application/octet-stream\r\n";

                bool sent = WinHttpSendRequest(requestHandle, headersString, (uint)headersString.Length, IntPtr.Zero, 0, fileSize, IntPtr.Zero);
                if (!sent)
                {
                    error = "SendFail";
                    return false;
                }

                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead;
                uint bytesWritten;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (!WinHttpWriteData(requestHandle, buffer, (uint)bytesRead, out bytesWritten) || bytesWritten == 0)
                    {
                        error = "WriteDataFail";
                        return false;
                    }
                }

                bool received = WinHttpReceiveResponse(requestHandle, IntPtr.Zero);
                if (!received)
                {
                    error = "RecvFail";
                    return false;
                }

                uint statusCode = 0;
                uint bufLen = sizeof(uint);
                uint index = 0;

                GCHandle statusCodePin = GCHandle.Alloc(statusCode, GCHandleType.Pinned);
                bool queryResult = WinHttpQueryHeaders(requestHandle, WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY_FLAG_NUMBER, null, statusCodePin.AddrOfPinnedObject(), ref bufLen, ref index);
                statusCode = (uint)Marshal.ReadInt32(statusCodePin.AddrOfPinnedObject());
                statusCodePin.Free();

                if (!queryResult)
                {
                    error = "QueryHeadersFail";
                    return false;
                }

                if (statusCode < 200 || statusCode >= 300)
                {
                    error = $"HTTP {statusCode}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[WinHttp Upload] EXCEPTION: {ex}");
                error = ex.Message;
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
                if (flagsPin.IsAllocated) flagsPin.Free();
                if (requestHandle != IntPtr.Zero) WinHttpCloseHandle(requestHandle);
                if (connectHandle != IntPtr.Zero) WinHttpCloseHandle(connectHandle);
            }
        }

        /// <summary>
        /// Performs an asynchronous HTTP POST request directly uploading the binary content of a file.
        /// Returns the success status and error message via a callback on the main thread.
        /// </summary>
        public void UploadFileAsync(string url, string filePath, Action<bool, string> onComplete, float timeout = 30f)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string error = string.Empty;
                bool result = UploadFileBlocking(url, filePath, out error, timeout);
                if (onComplete != null)
                {
                    Loom.QueueOnMainThread(() =>
                    {
                        onComplete(result, error);
                    });
                }
            });
        }
    }
}