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
    /// </summary>
    public class ScriptWebSocket
    {
        /// <summary>
        /// The name of the plugin that owns this WebSocket connection.
        /// </summary>
        private readonly string _pluginName;
        private readonly string _socketId;
        private readonly string _url;
        private IntPtr _hSession = IntPtr.Zero;
        private IntPtr _hConnect = IntPtr.Zero;
        private IntPtr _hRequest = IntPtr.Zero;
        private IntPtr _hWebSocket = IntPtr.Zero;
        private bool _isConnected;

        /// <summary>
        /// Initializes a new instance of the ScriptWebSocket class.
        /// </summary>
        /// <param name="pluginName">The name of the plugin creating the socket.</param>
        /// <param name="socketId">A unique identifier for the socket.</param>
        /// <param name="url">The target WebSocket URL (ws:// or wss://).</param>
        public ScriptWebSocket(string pluginName, string socketId, string url)
        {
            _pluginName = pluginName;
            _socketId = socketId;
            _url = url;
        }

        /// <summary>
        /// The unique identifier assigned to this WebSocket connection.
        /// </summary>
        public string SocketId
        {
            get { return _socketId; }
        }

        /// <summary>
        /// The name of the plugin that owns this WebSocket connection.
        /// </summary>
        public string PluginName
        {
            get { return _pluginName; }
        }

        /// <summary>
        /// Initiates the WebSocket connection asynchronously on a background thread.
        /// </summary>
        public void Connect()
        {
            ThreadPool.QueueUserWorkItem(_ => ConnectInternal());
        }

        /// <summary>
        /// Sends a UTF-8 encoded text message over the WebSocket asynchronously.
        /// </summary>
        /// <param name="message">The text message to send.</param>
        /// <returns>True if the send operation was queued successfully; otherwise, false if disconnected or uninitialized.</returns>
        public bool Send(string message)
        {
            if (!_isConnected || _hWebSocket == IntPtr.Zero)
                return false;
            
            ThreadPool.QueueUserWorkItem(_ =>
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(data.Length);
                try
                {
                    Marshal.Copy(data, 0, unmanagedPointer, data.Length);
                    WinHttpClient.WinHttpWebSocketSend(_hWebSocket, WinHttpClient.WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE, unmanagedPointer, (uint)data.Length);
                }
                finally
                {
                    Marshal.FreeHGlobal(unmanagedPointer);
                }
            });

            return true;
        }

        /// <summary>
        /// Closes the WebSocket connection and releases all associated unmanaged WinHTTP handles.
        /// </summary>
        public void Close()
        {
            if (!_isConnected) 
                return;
            
            _isConnected = false;

            if (_hWebSocket != IntPtr.Zero)
            {
                WinHttpClient.WinHttpWebSocketClose(_hWebSocket, 1000, IntPtr.Zero, 0);
                WinHttpClient.WinHttpCloseHandle(_hWebSocket);
                _hWebSocket = IntPtr.Zero;
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
                new Thread(ReceiveLoop)
                {
                    IsBackground = true
                }.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[ScriptWebSocket] Connection Error: {ex}");
                Close();
            }
        }

        /// <summary>
        /// Internal loop running on a background thread to continuously read incoming WebSocket messages.
        /// Triggers the global socket event on the main thread upon receiving a full payload.
        /// </summary>
        private void ReceiveLoop()
        {
            int bufferSize = 8192;
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                while (_isConnected)
                {
                    uint bytesRead = 0;
                    uint bufferType = 0;
                    
                    uint error = WinHttpClient.WinHttpWebSocketReceive(_hWebSocket, buffer, (uint)bufferSize, out bytesRead, out bufferType);
                    
                    if (error != 0 || bufferType == WinHttpClient.WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE)
                    {
                        break; 
                    }

                    if (bytesRead > 0)
                    {
                        byte[] data = new byte[bytesRead];
                        Marshal.Copy(buffer, data, 0, (int)bytesRead);
                        messageBuilder.Append(Encoding.UTF8.GetString(data));
                    }

                    if (bufferType == WinHttpClient.WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE || 
                        bufferType == WinHttpClient.WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE)
                    {
                        string msg = messageBuilder.ToString();
                        messageBuilder.Length = 0; 
                        WebSocketEvent wsEvent = new WebSocketEvent(_pluginName, _socketId, msg);
                        
                        Loom.QueueOnMainThread(() =>
                        {
                            Hooks.SocketMessageReceived(wsEvent);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isConnected) 
                    Logger.LogError($"[ScriptWebSocket] Receive Error: {ex}");
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
                Close();
            }
        }
    }
}