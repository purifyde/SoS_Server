using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SolsticeMasterServer {

    // State object for reading client data asynchronously  
    public class ClientState {

        public int Id;
        public Socket ClientSocket = null;
        public const int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];

        public ManualResetEvent ResetEvent;
        private Thread threadRecv;
        private bool active;

        public ClientState(int id, Socket socket) {
            Id = id;
            ClientSocket = socket;

            ResetEvent = new ManualResetEvent(false);
            threadRecv = new Thread(new ThreadStart(checkForIncomingData));
            threadRecv.Start();
        }

        private void checkForIncomingData() {
            active = true;
            while(active) { // TODO: end this thread when the client disconnects
                ResetEvent.Reset();
                ClientSocket.BeginReceive(Buffer, 0, BufferSize, 0, new AsyncCallback(Program.ReceiveCallback), this);
                ResetEvent.WaitOne();
            }
            Console.WriteLine("Closed connection for {0} [id={1}]", Util.GetSocketAddress(ClientSocket), Id);
        }

        public void Close() {
            active = false;
        }
    }
}
