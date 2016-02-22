using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kinect;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LibRawFrameData;
using System.Diagnostics;

namespace kinect_test
{
    class Kinect
    {
        private KinectSensor kinectSensor = null;
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;

        private int frame_cnt = 0;
        private readonly int PERCENT = 5;
        private BinaryFormatter formatter;
        private UdpClient udpServer;
        private List<string> clientIp;
        private List<int> clientPort;

        public Kinect()
        {
            this.udpServer = new UdpClient();
            this.clientIp = new List<string>();
            this.clientPort = new List<int>();
            this.formatter = new BinaryFormatter();
            this.kinectSensor = KinectSensor.GetDefault();
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.kinectSensor.Open();
            this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
        }

        //Handles the body frame data arriving from the sensor.
        //Another thread will come to execute this, each time new frame arrived.
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            List<KinectData> players = new List<KinectData>(2);

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                frame_cnt++;
                if (frame_cnt == PERCENT)
                {
                    foreach (Body body in this.bodies)
                    {
                        int id = (int)body.TrackingId;
                        if (body.IsTracked)
                        {
                            var data = get_gesture_data(body, id);
                            players.Add(data);
                        }
                    }
                    send_data(players);
                    frame_cnt = 0;
                }
            }
        }

        private KinectData get_gesture_data(Body body, int bodyIndex)
        {
            KinectData data = new KinectData();
            data.clippedEdges = (int)body.ClippedEdges;
            data.isTracked = body.IsTracked;
            data.trackingId = (ulong)bodyIndex;

            for (int i = 0; i < body.Joints.Count(); i++)
            {
                data.joints[i, 0] = body.Joints[(JointType)i].Position.X;
                data.joints[i, 1] = body.Joints[(JointType)i].Position.Y;
                data.joints[i, 2] = body.Joints[(JointType)i].Position.Z;
                data.trackingState[i] = (int)body.Joints[(JointType)i].TrackingState;
            }
            return data;
        }

        // use udp to send data
        private void send_data(List<KinectData> players)
        {
            IPEndPoint ep = null;
            
            for (int i=0; i<this.clientIp.Count(); i++)
            {
                string ip = this.clientIp[i];
                int port = this.clientPort[i];
                Console.WriteLine(ip + " " + port);
                ep = new IPEndPoint(IPAddress.Parse(ip), port);
                MemoryStream memoryStream = new MemoryStream();
                try
                {
                    formatter.Serialize(memoryStream, players);
                    byte[] data = memoryStream.ToArray();
                    System.Console.WriteLine(data.Length);
                    Console.WriteLine(players.Count());
                    udpServer.Send(data, data.Length, ep);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void setupClient(string ip, int port)
        {
            if (!this.clientIp.Contains(ip))
            {
                this.clientIp.Add(ip);
                this.clientPort.Add(port);
            }
        }

        public void deleteClient()
        {
            this.clientIp.Clear();
            this.clientPort.Clear();
        }

        static void Main(string[] args)
        {
            Kinect kinect = new Kinect();

            string serverIp = "0.0.0.0";
            int serverPort = 5005;
            TcpListener server = null;
            List<string> playerIp = new List<string>();
            List<int> playerPort = new List<int>();

            try
            {
                server = new TcpListener(IPAddress.Parse(serverIp), serverPort);
                server.Start();
                Byte[] bytes = new byte[1024];
                Stopwatch sw = new Stopwatch();
                Console.WriteLine("1");
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    IPEndPoint ipep = client.Client.RemoteEndPoint as IPEndPoint;
                    string clientIp = ipep.Address.ToString();
                    Console.WriteLine(clientIp);
                    BinaryReader br = new BinaryReader(client.GetStream());
                    int flag = br.ReadInt32();
                    Console.WriteLine(flag);
                    if (flag == 1)
                    {
                        int udpPort = br.ReadInt32();
                        Console.WriteLine(clientIp);
                        Console.WriteLine(udpPort);
                        kinect.setupClient(clientIp, udpPort);
                    }
                    
                    if (sw.ElapsedMilliseconds > 500000)
                    {
                        kinect.deleteClient();
                        sw.Restart();
                    }
                    client.Close();
                }
            }catch(Exception e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }finally
            {
                server.Stop();
            }
            Console.ReadLine();
        }
    }
}
