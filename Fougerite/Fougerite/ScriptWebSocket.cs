using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Fougerite.Events;

namespace Fougerite
{
    /// <summary>
    /// Represents a WebSocket client implementation using native WinHTTP.
    /// Designed to provide secure, background-threaded WebSocket connections for plugins.
    /// Warning: This class is not thread safe. Only Connect and Close from the main thread ideally from a single plugin.
    /// </summary>
    public class ScriptWebSocket : IDisposable
    {
        /// <summary>
        /// The name of the plugin that owns this WebSocket connection.
        /// </summary>
        private readonly string _pluginName;
        
        /// <summary>
        /// The unique identifier assigned to this WebSocket connection.
        /// </summary>
        private readonly string _socketId;
        
        /// <summary>
        /// The target URL for the WebSocket.
        /// </summary>
        private readonly string _url;

        /// <summary>
        /// The size of the buffer, in bytes, allocated for receiving WebSocket messages.
        /// </summary>
        private readonly int _bufferSize;
        private IntPtr _hSession = IntPtr.Zero;
        private IntPtr _hConnect = IntPtr.Zero;
        private IntPtr _hRequest = IntPtr.Zero;
        private IntPtr _hWebSocket = IntPtr.Zero;
        private bool _isConnected;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the ScriptWebSocket class.
        /// </summary>
        /// <param name="pluginName">The name of the plugin creating the socket.</param>
        /// <param name="socketId">A unique identifier for the socket.</param>
        /// <param name="url">The target WebSocket URL (ws:// or wss://).</param>
        /// <param name="bufferSize">The chuck size to read whole message with. Default is 32kb.</param>
        public ScriptWebSocket(string pluginName, string socketId, string url, int bufferSize = 32768)
        {
            _pluginName = pluginName;
            _socketId = socketId;
            _url = url;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Gets the unique identifier assigned to this WebSocket connection.
        /// </summary>
        public string SocketId
        {
            get { return _socketId; }
        }

        /// <summary>
        /// Gets the name of the plugin that owns this WebSocket connection.
        /// </summary>
        public string PluginName
        {
            get { return _pluginName; }
        }

        /// <summary>
        /// Gets a value indicating whether the WebSocket is currently connected and open.
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
        }

        /// <summary>
        /// Gets the target URL of the WebSocket connection.
        /// </summary>
        public string Url
        {
            get { return _url; }
        }

        /// <summary>
        /// Gets the buffer size used for reading WebSocket messages.
        /// Specifies the maximum chunk size, in bytes, that can be read in a single operation.
        /// </summary>
        public int BufferSize
        {
            get { return _bufferSize; }
        }

        /// <summary>
        /// Initiates the WebSocket connection asynchronously on a background thread.
        /// </summary>
        public void Connect()
        {
            if (_disposed)
            {
                DispatchError("Cannot connect: The WebSocket object has been disposed.");
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => ConnectInternal());
        }

        /// <summary>
        /// Sends a UTF-8 encoded text message over the WebSocket asynchronously.
        /// </summary>
        /// <param name="message">The text message to send.</param>
        /// <returns>True if the send operation was queued successfully; otherwise, false if disconnected or uninitialized.</returns>
        public bool Send(string message)
        {
            if (!_isConnected || _hWebSocket == IntPtr.Zero || _disposed)
            {
                DispatchError("Failed to send message: Socket is not connected or initialized.");
                return false;
            }
            
            ThreadPool.QueueUserWorkItem(_ =>
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(data.Length);
                try
                {
                    Marshal.Copy(data, 0, unmanagedPointer, data.Length);
                    WinHttpClient.WinHttpWebSocketSend(_hWebSocket, WinHttpClient.WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE, unmanagedPointer, (uint)data.Length);
                }
                catch (Exception ex)
                {
                    DispatchError($"Exception during send: {ex.Message}");
                }
                finally
                {
                    Marshal.FreeHGlobal(unmanagedPointer);
                }
            });

            return true;
        }

        /// <summary>
        /// Closes the WebSocket connection, releases WinHTTP handles, and fires the Disconnected event.
        /// Does NOT dispose the object, meaning it can potentially be reconnected later.
        /// </summary>
        /// <param name="errorMessage">Optional error message explaining why the socket was closed.</param>
        public void Close(string errorMessage = null)
        {
            if (!_isConnected) 
                return;
            
            _isConnected = false;

            ReleaseUnmanagedHandles();

            WebSocketEvent closedEvent = new WebSocketEvent(_pluginName, _socketId, string.Empty, errorMessage);
            
            // Dispatch Socket Closed Event
            Loom.QueueOnMainThread(() =>
            {
                Hooks.SocketClosed(closedEvent);
            });
        }

        /// <summary>
        /// Disposes the WebSocket connection, releasing all unmanaged resources and preventing further use.
        /// </summary>
        public void Dispose()
        {
            Dispose(true, "Object disposed.");
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal implementation of the dispose pattern.
        /// </summary>
        protected virtual void Dispose(bool disposing, string errorMessage = null)
        {
            if (_disposed) 
                return;

            if (disposing)
            {
                // Cleanly disconnect and fire the event if called via explicit Dispose()
                Close(errorMessage);
            }
            else
            {
                // Called from the finalizer. Just silently release memory, NO Loom dispatching.
                _isConnected = false;
                ReleaseUnmanagedHandles();
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer to ensure unmanaged WinHTTP handles are released if the object is garbage collected without being disposed.
        /// </summary>
        ~ScriptWebSocket()
        {
            Dispose(false);
        }

        /// <summary>
        /// Helper to cleanly wipe out all native WinHTTP pointers.
        /// </summary>
        private void ReleaseUnmanagedHandles()
        {
            if (_hWebSocket != IntPtr.Zero)
            {
                WinHttpClient.WinHttpWebSocketClose(_hWebSocket, 1000, IntPtr.Zero, 0);
                WinHttpClient.WinHttpCloseHandle(_hWebSocket);
                _hWebSocket = IntPtr.Zero;
            }

            if (_hRequest != IntPtr.Zero)
            {
                WinHttpClient.WinHttpCloseHandle(_hRequest);
                _hRequest = IntPtr.Zero;
            }

            if (_hConnect != IntPtr.Zero)
            {
                WinHttpClient.WinHttpCloseHandle(_hConnect);
                _hConnect = IntPtr.Zero;
            }

            if (_hSession != IntPtr.Zero)
            {
                WinHttpClient.WinHttpCloseHandle(_hSession);
                _hSession = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Dispatches a socket error event to the main thread.
        /// </summary>
        /// <param name="errorMsg">The error string to pass to the plugin.</param>
        private void DispatchError(string errorMsg)
        {
            WebSocketEvent errorEvent = new WebSocketEvent(_pluginName, _socketId, string.Empty, errorMsg);
            Loom.QueueOnMainThread(() =>
            {
                Hooks.SocketErrorEvent(errorEvent);
            });
        }

        /// <summary>
        /// Internal method that performs the WinHTTP connection, HTTP upgrade, and handshake process.
        /// </summary>
        private void ConnectInternal()
        {
            try
            {
                Uri uri = new Uri(_url);
                ushort port = (ushort)(uri.Port > 0 ? uri.Port : (uri.Scheme == "wss" ? 443 : 80));
                uint flags = uri.Scheme == "wss" ? WinHttpClient.WINHTTP_FLAG_SECURE : 0;

                _hSession = WinHttpClient.WinHttpOpen("Fougerite WinHTTP WebSocket", WinHttpClient.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, null, null, 0);
                _hConnect = WinHttpClient.WinHttpConnect(_hSession, uri.Host, port, 0);
                
                string path = uri.PathAndQuery;
                _hRequest = WinHttpClient.WinHttpOpenRequest(_hConnect, "GET", path, null, null, IntPtr.Zero, flags);

                uint ignoreFlags = WinHttpClient.SECURITY_FLAG_IGNORE_UNKNOWN_CA | WinHttpClient.SECURITY_FLAG_IGNORE_CERT_WRONG_USAGE | WinHttpClient.SECURITY_FLAG_IGNORE_CERT_CN_INVALID | WinHttpClient.SECURITY_FLAG_IGNORE_CERT_DATE_INVALID;
                byte[] flagsBuffer = BitConverter.GetBytes(ignoreFlags);
                GCHandle flagsPin = GCHandle.Alloc(flagsBuffer, GCHandleType.Pinned);
                WinHttpClient.WinHttpSetOption(_hRequest, WinHttpClient.WINHTTP_OPTION_SECURITY_FLAGS, flagsPin.AddrOfPinnedObject(), sizeof(uint));
                flagsPin.Free();

                WinHttpClient.WinHttpSetOption(_hRequest, WinHttpClient.WINHTTP_OPTION_UPGRADE_TO_WEB_SOCKET, IntPtr.Zero, 0);

                WinHttpClient.WinHttpSendRequest(_hRequest, null, 0, IntPtr.Zero, 0, 0, IntPtr.Zero);
                WinHttpClient.WinHttpReceiveResponse(_hRequest, IntPtr.Zero);

                _hWebSocket = WinHttpClient.WinHttpWebSocketCompleteUpgrade(_hRequest, IntPtr.Zero);
                WinHttpClient.WinHttpCloseHandle(_hRequest);
                _hRequest = IntPtr.Zero;

                _isConnected = true;

                // Dispatch Socket Connected Event
                WebSocketEvent connectedEvent = new WebSocketEvent(_pluginName, _socketId, string.Empty);
                Loom.QueueOnMainThread(() =>
                {
                    Hooks.SocketConnected(connectedEvent);
                });

                new Thread(ReceiveLoop)
                {
                    IsBackground = true
                }.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[ScriptWebSocket] Connection Error: {ex}");
                Close(ex.Message);
            }
        }

        /// <summary>
        /// Internal loop running on a background thread to continuously read incoming WebSocket messages.
        /// Triggers the global socket event on the main thread upon receiving a full payload.
        /// </summary>
        private void ReceiveLoop()
        {
            IntPtr buffer = Marshal.AllocHGlobal(_bufferSize);
            string closeErrorMsg = null;

            try
            {
                using (System.IO.MemoryStream messageStream = new System.IO.MemoryStream())
                {
                    while (_isConnected && !_disposed)
                    {
                        uint bytesRead = 0;
                        uint bufferType = 0;

                        uint error = WinHttpClient.WinHttpWebSocketReceive(_hWebSocket, buffer, (uint)_bufferSize, out bytesRead, out bufferType);

                        if (error != 0 || bufferType == WinHttpClient.WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE)
                        {
                            if (error != 0)
                                closeErrorMsg = $"Receive failed with WinHTTP error code: {error}";
                            break;
                        }

                        if (bytesRead > 0)
                        {
                            byte[] data = new byte[bytesRead];
                            Marshal.Copy(buffer, data, 0, (int)bytesRead);
                            messageStream.Write(data, 0, (int)bytesRead);
                        }

                        if (bufferType == WinHttpClient.WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE 
                            || bufferType == WinHttpClient.WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE)
                        {
                            string msg = Encoding.UTF8.GetString(messageStream.ToArray());
                            messageStream.SetLength(0);

                            WebSocketEvent wsEvent = new WebSocketEvent(_pluginName, _socketId, msg);

                            Loom.QueueOnMainThread(() =>
                            {
                                Hooks.SocketMessageReceived(wsEvent);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    Logger.LogError($"[ScriptWebSocket] Receive Error: {ex}");
                    closeErrorMsg = ex.Message;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
                Close(closeErrorMsg);
            }
        }
    }
}