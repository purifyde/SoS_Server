using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SolsticeGameServer {

    /* Handles character loading and creation */
    public class SvcServer {

        public static ManualResetEvent ResetEvent = new ManualResetEvent(false);

        private static int connectionCount;

        public static void StartListening() {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Config.PortSvc);

            Console.WriteLine("[SVC] Starting svc server on port " + localEndPoint.Port);

            // Create a TCP socket
            Socket listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("[SVC] Started socket, waiting for connections...");

            try {
                listener.Bind(localEndPoint);
                listener.Listen(Config.MaxQueueSizeSvc);

                while (true) {
                    // Set the event to nonsignaled state
                    ResetEvent.Reset();

                    // Start an asynchronous socket to listen for connections
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // Wait until a connection is made before continuing
                    ResetEvent.WaitOne();
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static void AcceptCallback(IAsyncResult result) {
            // Signal the main thread to continue
            ResetEvent.Set();

            // Get the socket that handles the client request
            Socket listener = (Socket) result.AsyncState;
            Socket handler = listener.EndAccept(result);

            Console.WriteLine("[SVC] Accepted new connection from {0} [id={1}]", Util.GetSocketAddress(handler), connectionCount);

            // Create the state object.  
            ClientState state = new ClientState(connectionCount, handler, new AsyncCallback(ReceiveCallback), false);

            connectionCount++;
        }

        public static void ReceiveCallback(IAsyncResult ar) {
            // Retrieve the state object and the handler socket from the asynchronous state object
            ClientState state = (ClientState) ar.AsyncState;
            state.ResetEvent.Set();
            Socket socket = state.ClientSocket;

            int bytesRead = socket.EndReceive(ar);
            if (bytesRead == 0) return;
            byte[] data = new byte[bytesRead];
            Array.Copy(state.Buffer, data, data.Length);

            SvcPacketHandler.RecievePacket(state, data);
        }
    }
}
